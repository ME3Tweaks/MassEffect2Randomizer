using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class ClassTalents
    {

        private static IlliumHub.AssetSource[] CharacterClassAssets = new IlliumHub.AssetSource[]
        {
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Adept", PackageFile = "SFXCharacterClass_Adept.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Engineer", PackageFile = "SFXCharacterClass_Engineer.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Infiltrator", PackageFile = "SFXCharacterClass_Infiltrator.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Sentinel", PackageFile = "SFXCharacterClass_Sentinel.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Soldier", PackageFile = "SFXCharacterClass_Soldier.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Vanguard", PackageFile = "SFXCharacterClass_Vanguard.pcc"},
        };

        public static bool ShuffleClassAbilitites(RandomizationOption option)
        {
            List<ExportEntry> allPowers = new();
            List<ExportEntry> assets = new();

            // We can have up to 6 UI powers assigned. 
            // One is reserved for first aid and another is reserved for the user chooseable bonus power.


            // Step 1: Build list of all powers
            foreach (var asset in CharacterClassAssets)
            {
                var loadout = asset.GetAsset();
                assets.Add(loadout);

                var powersList = loadout.GetProperty<ArrayProperty<ObjectProperty>>("Powers");
                foreach (var pow in powersList)
                {
                    var power = pow.ResolveToEntry(loadout.FileRef) as ExportEntry;
                    if (CanShufflePower(power))
                    {
                        allPowers.Add(power);
                        Debug.WriteLine($"Shuffleable power: {power.ObjectName}");
                    }
                }
            }

            // Step 2. Shuffle lists to ensure randomness of assignments
            allPowers.Shuffle();
            assets.Shuffle();

            // Step 3. Assign and map powers

            // This doesn't seem to work in parallel due to some internal non-thread safe stuff in my code
            // Too lazy to figure out what it is
            var biogame = CoalescedHandler.GetIniFile("BIOGame.ini");
            foreach (var loadout in assets)
            {
                var kit = loadout.ObjectName.Name.Substring(4);
                var mappedPowers = biogame.GetOrAddSection($"SFXGamePawns.SFXCharacterClass_{kit}");
                mappedPowers.Entries.Add(new DuplicatingIni.IniEntry("!MappedPowers")); //Clear

                var powersList = loadout.GetProperty<ArrayProperty<ObjectProperty>>("Powers");
                var originalCount = powersList.Count;
                var powersToKeep = powersList.Skip(5).Take(4).ToList();
                powersList.Clear();
                List<ExportEntry> newPowers = new();

                // Build new powers list
                int i = 5;
                while (i > 0)
                {
                    var newPower = allPowers.PullFirstItem();
                    // Prevent duplicate superclass powers, like IncendiaryAmmo Vanguard/Soldier
                    var superclassPath = newPower.SuperClass.InstancedFullPath;
                    while (newPowers.Any(x => x.SuperClass.InstancedFullPath == superclassPath))
                    {
                        allPowers.Add(newPower);
                        newPower = allPowers.PullFirstItem();
                        superclassPath = newPower.InstancedFullPath;
                    }

                    var portedPower = PackageTools.PortExportIntoPackage(loadout.FileRef, newPower);
                    newPowers.Add(portedPower);
                    powersList.Add(new ObjectProperty(portedPower.UIndex));

                    // Get power internal name
                    var origPowerForDebugging = newPower;
                    while (newPower.SuperClass.ObjectName != "SFXPower" && newPower.SuperClass.ObjectName != "SFXPower_PassivePower")
                    {
                        newPower = newPower.SuperClass as ExportEntry;
                    }

                    if (!newPower.SuperClass.ObjectName.Name.Contains("Passive"))
                    {
                        mappedPowers.Entries.Add(new DuplicatingIni.IniEntry("MappedPowers", newPower.ObjectName));
                    }
                    i--;
                }

                // Add passives back
                powersList.AddRange(powersToKeep);

                // Write loadout data
                if (powersList.Count != originalCount)
                {
                    Debugger.Break();
                }
                loadout.WriteProperty(powersList);


                // Correct the unlock criteria to ensure every power can be unlocked.
                for (i = 0; i < newPowers.Count; i++)
                {
                    // i = Power Index (0 indexed)

                    // Power dependency map:
                    // Power 1: Rank 1
                    // Power 2: Depends on Power 1 Rank 2
                    // Power 3: Rank 1
                    // Power 4: Depends on Power 3 Rank 2
                    // Power 5: Depends on Power 4 Rank 2
                    // Power 6: No requirements, but no points assigned

                    var power = newPowers[i];
                    var defaults = power.GetDefaults();

                    var properties = defaults.GetProperties();
                    properties.RemoveNamedProperty("Rank");
                    properties.RemoveNamedProperty("UnlockRequirements");

                    if (i == 0 || i == 2)
                    {
                        properties.Add(new FloatProperty(1, "Rank"));
                    }
                    else if (i == 1 || i == 3 || i == 4)
                    {
                        // Has dependency
                        var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                        dependencies.AddRange(GetUnlockRequirementsForPower(newPowers[i - 1]));
                        properties.Add(dependencies);
                    }
                    defaults.WriteProperties(properties);
                }
                MERFileSystem.SavePackage(loadout.FileRef);
            }

            Debug.WriteLine($"Unassigned powers count: {allPowers.Count}");
            return true;
        }

        /// <summary>
        /// Generates the structs for UnlockRequirements that are used to setup a dependenc on this power
        /// </summary>
        /// <param name="powerClass">The kit-power to depend on.</param>
        /// <returns></returns>
        private static List<StructProperty> GetUnlockRequirementsForPower(ExportEntry powerClass)
        {
            void PopulateProps(PropertyCollection props, ExportEntry export, float rank)
            {
                props.Add(new ObjectProperty(export.UIndex, "PowerClass")); // The power that will be checked
                props.Add(new FloatProperty(rank, "Rank")); // Required rank. 1 means at least one point in it and is used for evolved powers which area always at 4.
                props.Add(new StringRefProperty(0, "CustomUnlockText")); // Unused
            }

            List<StructProperty> powerRequirements = new();

            // Unevolved power
            PropertyCollection props = new PropertyCollection();
            PopulateProps(props, powerClass, 2);
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            // EVOLVED POWER

            // Find base power
            var baseClass = powerClass;
            var evolvedPower1 = powerClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
            while (evolvedPower1 == null || evolvedPower1.Value == 0)
            {
                baseClass = (ExportEntry)baseClass.SuperClass;
                evolvedPower1 = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
            }

            // Evolved power 1
            props = new PropertyCollection();
            PopulateProps(props, evolvedPower1.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            // Evolved power 2
            var evolvedPower2 = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass2");
            props = new PropertyCollection();
            PopulateProps(props, evolvedPower2.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            return powerRequirements;
        }

        private static bool CanShufflePower(ExportEntry power)
        {
            if (power.ObjectName.Name.Contains("Passive")) return false; // Passives are actually visible and upgradable
            if (power.ObjectName.Name.Contains("SFXPower_Player")) return false;
            if (power.ObjectName.Name.Contains("FirstAid")) return false;
            return true;
        }
    }
}
