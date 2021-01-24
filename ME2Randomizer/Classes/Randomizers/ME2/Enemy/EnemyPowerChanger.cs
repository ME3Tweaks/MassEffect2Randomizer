using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using Newtonsoft.Json;
using Octokit;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Enemy
{
    class EnemyPowerChanger
    {

        public static List<PowerInfo> Powers;

        /// <summary>
        /// Loadouts matching these names can have an invisible weapon assigned to them
        /// </summary>
        private static List<string> LoadoutsSupportingHiddenMeshes = new List<string>()
        {
            "BioChar_Loadouts.Mechs.SUB_HeavyWeaponMech",
        };

        public static void LoadPowers()
        {
            if (Powers == null)
            {
                string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("powerlistme2.json");
                Powers = JsonConvert.DeserializeObject<List<PowerInfo>>(fileContents);
            }
        }

        /// <summary>
        /// Hack to force power lists to load with only a single check
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool Init(RandomizationOption option)
        {
            LoadPowers();
            return true;
        }

        internal class PowerInfo
        {

            /// <summary>
            ///  Name of the power
            /// </summary>
            [JsonProperty("powername")]
            public string PowerName { get; set; }
            [JsonProperty("packagename")]
            public string PackageName { get; set; } = "SFXGameContent_Powers";
            [JsonProperty("packagefilename")]
            public string PackageFileName { get; set; }
            [JsonProperty("sourceuindex")]
            public int SourceUIndex { get; set; }
            [JsonProperty("type")]
            public EPowerCapabilityType Type { get; set; }

            private bool MapPowerType(ExportEntry classExport)
            {
                var uClass = ObjectBinary.From<UClass>(classExport);
                var defaults = classExport.FileRef.GetUExport(uClass.Defaults);
                var bct = defaults.GetProperty<EnumProperty>("CapabilityType");
                if (bct == null)
                    return false;
                switch (bct.Value.Name)
                {
                    case "BioCaps_AllTypes":
                        Debugger.Break();
                        //Type = ;
                        return true;
                    case "BioCaps_SingleTargetAttack":
                        Type = EPowerCapabilityType.Attack;
                        return true;
                    case "BioCaps_AreaAttack":
                        Type = EPowerCapabilityType.Attack;
                        return true;
                    case "BioCaps_Disable":
                        Type = EPowerCapabilityType.Debuff;
                        return true;
                    case "BioCaps_Debuff":
                        Type = EPowerCapabilityType.Debuff;
                        return true;
                    case "BioCaps_Defense":
                        Type = EPowerCapabilityType.Defense;
                        return true;
                    case "BioCaps_Heal":
                        Type = EPowerCapabilityType.Heal;
                        return true;
                    case "BioCaps_Buff":
                        Type = EPowerCapabilityType.Buff;
                        return true;
                    case "BioCaps_Suicide":
                        Type = EPowerCapabilityType.Suicide;
                        return true;
                    case "BioCaps_Death":
                        Type = EPowerCapabilityType.Death;
                        return true;
                    default:
                        Debugger.Break();
                        return true;
                }

            }

            public PowerInfo() { }
            public PowerInfo(ExportEntry export)
            {
                if (!MapPowerType(export))
                {
                    // Powers that do not list a capability type are subclasses. We will not support using these
                    IsUsable = false;
                    return;
                }

                PowerName = export.ObjectName;
                if (PowerName.Contains("Ammo")
                    || PowerName.Contains("Base")
                    || PowerName.Contains("FirstAid")
                    || PowerName.Contains("Player")
                    || PowerName.Contains("GunshipRocket")
                    || PowerName.Contains("NPC")
                    || PowerName.Contains("Player")
                    || PowerName.Contains("HuskTesla")



                    )
                {
                    IsUsable = false;
                    return; // Do not use ammo or base powers as they're player only in the usable code
                }

                PackageFileName = Path.GetFileName(export.FileRef.FilePath);
                PackageName = export.ParentName;
                SourceUIndex = export.UIndex;
            }

            [JsonIgnore]
            public bool IsUsable { get; set; } = true;
        }

        internal enum EPowerCapabilityType
        {
            Attack,
            Disable,
            Debuff,
            Defense,
            Heal,
            Buff,
            Suicide,
            Death
        }

        public static ExportEntry PortPowerIntoPackage(IMEPackage targetPackage, PowerInfo powerInfo)
        {
            var sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage(powerInfo.PackageFileName);
            if (sourcePackage != null)
            {
                var sourceExport = sourcePackage.GetUExport(powerInfo.SourceUIndex);
                if (!sourceExport.InheritsFrom("SFXPower") || sourceExport.IsDefaultObject)
                {
                    throw new Exception("Wrong setup!");
                }
                if (sourceExport.Parent != null && sourceExport.Parent.ClassName != "Package")
                {
                    throw new Exception("Cannot port power - parent object is not Package!");
                }

                // 1. Setup the link that will be used.
                int link = 0;

                List<IEntry> parents = new List<IEntry>();
                var parent = sourceExport.Parent;
                while (parent != null)
                {
                    if (parent.ClassName != "Package")
                        throw new Exception("Parent is not package!");
                    parents.Add(parent);
                    parent = parent.Parent;
                }

                // Create the parents
                parents.Reverse();
                IEntry newParent = null;
                foreach (var p in parents)
                {
                    var sourceFullPath = p.InstancedFullPath;
                    var matchingParent = targetPackage.FindExport(sourceFullPath) as IEntry;
                    if (matchingParent == null)
                    {
                        matchingParent = targetPackage.FindImport(sourceFullPath);
                    }

                    if (matchingParent != null)
                    {
                        newParent = matchingParent;
                        continue;
                    }

                    newParent = ExportCreator.CreatePackageExport(targetPackage, p.ObjectName, newParent);
                }

                Dictionary<IEntry, IEntry> crossPCCObjectMap = new Dictionary<IEntry, IEntry>(); // Not sure what this is used for these days. SHould probably just be part of the method
                var relinkResults = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceExport, targetPackage,
                    newParent, true, out IEntry newEntry, crossPCCObjectMap);

                if (relinkResults.Any())
                {
                    Debugger.Break();
                }

                return newEntry as ExportEntry;
            }
            return null; // No package was found
        }

        // This can probably be changed later
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == "SFXLoadoutData"
                                                                && !export.ObjectName.Name.Contains("Drone");

        internal static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var powers = export.GetProperty<ArrayProperty<ObjectProperty>>("Powers");

            if (powers == null)
            {
                // This loadout has no powers!
                // Randomly give them some powers.
                if (ThreadSafeRandom.Next(1) == 0)
                {
                    // unlimited power
                    List<ObjectProperty> blankPows = new List<ObjectProperty>();
                    // Add two blanks. We'll strip blanks before writing it
                    blankPows.Add(new ObjectProperty(int.MinValue));
                    blankPows.Add(new ObjectProperty(int.MinValue));
                    powers = new ArrayProperty<ObjectProperty>(blankPows, "Powers");
                }
                else
                {
                    // Sorry mate no powers for you
                    return false;
                }
            }

            foreach (var power in powers)
            {
                if (power.Value == 0) return false; // Null entry in weapons list
                IEntry existingPowerEntry = null;
                if (power.Value != int.MinValue)
                {
                    // Husk AI kinda depends on melee or they just kinda breath on you all creepy like
                    // We'll give them a chance to change it up though
                    existingPowerEntry = power.ResolveToEntry(export.FileRef);
                    if (existingPowerEntry.ObjectName.Name.Contains("Melee", StringComparison.InvariantCultureIgnoreCase) && ThreadSafeRandom.Next(2) == 0)
                    {
                        continue; // Don't randomize power
                    }
                }

                var randomNewPower = Powers.RandomElement();
                if (existingPowerEntry == null || randomNewPower.PowerName != existingPowerEntry.ObjectName)
                {
                    if (powers.Any(x => power.Value != int.MinValue && power.ResolveToEntry(export.FileRef).ObjectName == randomNewPower.PowerName))
                        continue; // Duplicate powers crash the game

                    Log.Information($@"Changing power {export.ObjectName} {existingPowerEntry?.ObjectName ?? "New Power"} => {randomNewPower.PowerName }");
                    // It's a different gun.

                    // See if we need to port this in
                    var fullName = randomNewPower.PackageName + "." + randomNewPower.PowerName; // SFXGameContent_Powers.SFXPower_Hoops
                    var repoint = export.FileRef.FindImport(fullName) as IEntry;
                    if (repoint == null)
                    {
                        repoint = export.FileRef.FindExport(fullName);
                    }

                    if (repoint != null)
                    {
                        // Gun does not need ported in
                        power.Value = repoint.UIndex;
                    }
                    else
                    {
                        // Gun needs ported in
                        power.Value = PortPowerIntoPackage(export.FileRef, randomNewPower).UIndex;
                    }
                }
            }

            // Strip any blank powers we might have added
            powers.RemoveAll(x => x.Value == int.MinValue);
            export.WriteProperty(powers);

            // Our precalculated map should have accounted for imports already, so we odn't need to worry about missing imports upstream
            // If this is not a master or localization file (which are often used for imports) 
            // Change the number around so it will work across packages.
            // May need disabled if game becomes unstable.

            // We check if less than 10 as it's very unlikely there will be more than 10 loadouts in a non-persistent package
            // if it's > 10 it's likely already a memory-changed item by MER
            var pName = Path.GetFileName(export.FileRef.FilePath);
            if (export.indexValue < 10 && !PackageTools.IsPersistentPackage(pName) && !PackageTools.IsLocalizationPackage(pName))
            {
                export.ObjectName = new NameReference(export.ObjectName, ThreadSafeRandom.Next(2000));
            }

            return true;
        }
    }
}

