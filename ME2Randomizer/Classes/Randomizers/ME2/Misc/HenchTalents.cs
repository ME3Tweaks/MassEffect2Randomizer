using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ME2Randomizer.Classes;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class HenchTalents
    {
        [DebuggerDisplay("HTalent - {PowerExport.ObjectName}, Base {BasePower.ObjectName}, IsPassive: {IsPassive}")]
        class HTalent
        {
            public HTalent(ExportEntry powerClass, bool isEvolution = false)
            {
                PowerExport = powerClass;
                IsEvolution = isEvolution;
                var baseClass = powerClass;
                var baseClassObj = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
                while (baseClassObj == null || baseClassObj.Value == 0)
                {
                    baseClass = (ExportEntry)baseClass.SuperClass;
                    baseClassObj = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
                }

                BasePower = baseClass;

                if (BasePower.ObjectName.Name.Contains("Passive"))
                {
                    IsPassive = true;
                }
                else
                {
                    var baseName = baseClass.GetDefaults().GetProperty<NameProperty>("BaseName");
                    while (baseName == null)
                    {
                        baseClass = (ExportEntry)baseClass.SuperClass;
                        baseName = baseClass.GetDefaults().GetProperty<NameProperty>("BaseName");
                    }

                    BaseName = baseName.Value.Name;
                }

                // Setup name
                var superDefaults = PowerExport.GetDefaults();
                var displayNameProps = superDefaults.GetProperties();
                var superProps = displayNameProps;
                var displayName = superProps.GetProp<StringRefProperty>("DisplayName");
                while (displayName == null)
                {
                    superDefaults = ((superDefaults.Class as ExportEntry).SuperClass as ExportEntry).GetDefaults();
                    superProps = superDefaults.GetProperties();
                    superProps.GetProp<StringRefProperty>("DisplayName");
                    displayName = superProps.GetProp<StringRefProperty>("DisplayName");
                }
                PowerName = TLKHandler.TLKLookupByLang(displayName.Value, "INT");

                if (IsEvolution)
                {
                    // Setup the blurb
                    var blurbDesc = TLKHandler.TLKLookupByLang(displayNameProps.GetProp<StringRefProperty>("TalentDescription").Value, "INT").Split('\n')[0];
                    EvolvedBlurb = $"{PowerName}: {blurbDesc}";
                }

                IsAmmoPower = PowerName.Contains("Ammo");
                IsCombatPower = !IsAmmoPower && !IsPassive;
            }

            /// <summary>
            /// Is this a combat power? (Like Warp, Throw, Tech Armor)
            /// </summary>
            public bool IsCombatPower { get; set; }
            /// <summary>
            /// Is this an ammo power? (Like Disruptor, Cryo)
            /// </summary>
            public bool IsAmmoPower { get; set; }

            /// <summary>
            /// Blurb text to use when changing the evolution
            /// </summary>
            public string EvolvedBlurb { get; set; }

            /// <summary>
            /// If this is a passive power
            /// </summary>
            public bool IsPassive { get; set; }

            /// <summary>
            /// If this is an evolved power
            /// </summary>
            public bool IsEvolution { get; set; }
            /// <summary>
            /// The base class of the power - player version
            /// </summary>
            public ExportEntry BasePower { get; }
            /// <summary>
            /// The usable power export
            /// </summary>
            public ExportEntry PowerExport { get; }
            /// <summary>
            /// The base name of the power that is used for mapping in config
            /// </summary>
            public string BaseName { get; }

            /// <summary>
            /// The name of the power (localized)
            /// </summary>
            public string PowerName { get; }

            public bool HasEvolution()
            {
                return !IsEvolution;
            }
        }

        class TalentSet
        {
            /// <summary>
            /// If we were able to find a solution of compatible powers for this set
            /// </summary>
            public bool IsBaseValid { get; }
            public TalentSet(List<ExportEntry> allPowers)
            {
                int numPassives = 0;
                for (int i = 0; i < 6; i++)
                {
                    var talent = new HTalent(allPowers.PullFirstItem());
                    int retry = allPowers.Count;
                    while (retry > 0 && Powers.Any(x => x.BasePower.InstancedFullPath == talent.BasePower.InstancedFullPath))
                    {
                        allPowers.Add(talent.PowerExport);
                        talent = new HTalent(allPowers.PullFirstItem());
                        retry--;
                        if (retry <= 0)
                        {
                            IsBaseValid = false;
                            return;
                        }
                    }

                    if (talent.BasePower.ObjectName.Name.Contains("Passive"))
                    {
                        numPassives++;
                        if (numPassives > 3)
                        {
                            // We must ensure there is not a class full of passives
                            // or the evolutions will not have a solution
                            // only doing half allows us to give the evolution solution finder
                            // a better chance at a quick solution
                            IsBaseValid = false;
                            return;
                        }
                    }
                    Powers.Add(talent);
                }

                IsBaseValid = true;
            }

            public bool SetEvolutions(List<ExportEntry> availableEvolutions)
            {
                // 1. Calculate the number of required bonus evolutions
                var numToPick = Powers.Count(x => x.HasEvolution()) * 2;
                while (numToPick > 0)
                {
                    var numAttempts = availableEvolutions.Count;
                    var evolutionToCheck = new HTalent(availableEvolutions.PullFirstItem(), true);
                    while (numAttempts > 0 && EvolvedPowers.Any(x => x.PowerExport.InstancedFullPath == evolutionToCheck.PowerExport.InstancedFullPath)) // Ensure there are no duplicate power exports
                    {
                        // Repick
                        availableEvolutions.Add(evolutionToCheck.BasePower);
                        evolutionToCheck = new HTalent(availableEvolutions.PullFirstItem(), true);
                        numAttempts--;
                        if (numAttempts == 0)
                        {
                            Debug.WriteLine("Could not find suitable evolution for talentset!");
                            return false; // There is no viable solution
                        }
                    }

                    EvolvedPowers.Add(evolutionToCheck);
                    numToPick--;
                }

                return true;
            }

            /// <summary>
            /// The base power set of the kit
            /// </summary>
            public List<HTalent> Powers { get; } = new();

            /// <summary>
            /// The evolution power pool. Should not contain any items that are the same as an item in the Powers list
            /// </summary>
            public List<HTalent> EvolvedPowers { get; } = new();
        }

        private static IlliumHub.AssetSource[] CharacterClassAssets = new IlliumHub.AssetSource[]
        {
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Adept", PackageFile = "SFXCharacterClass_Adept.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Engineer", PackageFile = "SFXCharacterClass_Engineer.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Infiltrator", PackageFile = "SFXCharacterClass_Infiltrator.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Sentinel", PackageFile = "SFXCharacterClass_Sentinel.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Soldier", PackageFile = "SFXCharacterClass_Soldier.pcc"},
            new IlliumHub.AssetSource() {AssetPath = "BioChar_Loadouts.Player.PLY_Vanguard", PackageFile = "SFXCharacterClass_Vanguard.pcc"},
        };

        private static Dictionary<string, int> ClasStrRefMap = new Dictionary<string, int>()
        {
            {"Soldier", 127089 },
            {"Engineer", 127090 },
            {"Adept", 127091 },
            {"Infiltrator", 127092 },
            {"Vanguard", 127093 },
            {"Sentinel", 127094 },
        };

        class MappedPower
        {
            public int StartingRank { get; set; }
            public HTalent BaseTalent { get; set; }
            public HTalent EvolvedTalent1 { get; set; }
            public HTalent EvolvedTalent2 { get; set; }
        }

        public static bool ShuffleSquadmateAbilities(RandomizationOption option)
        {
            List<ExportEntry> allPowers = new();
            List<ExportEntry> assets = new();
            List<ExportEntry> powerEvolutions = new();
            // We can have up to 5 UI powers assigned. However, kits will only have 4 powers total assigned. 

            var squadmatePackageMap = new CaseInsensitiveDictionary<List<IMEPackage>>();

            #region Build squadmate sets
            var henchFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioH_") && !x.Contains("_LOC_") && x != "BioH_SelectGUI" && x.Contains("00"));
            foreach (var h in henchFiles)
            {
                var internalName = h.Substring(5); // Remove BioH_
                internalName = h.Substring(0, h.IndexOf("_", StringComparison.InvariantCultureIgnoreCase)); // "Assassin"
                var sqm = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(h));
                var sqmList = new List<IMEPackage>();
                sqmList.Add(sqm);
                var endGmFile = MERFileSystem.GetPackageFile($"BioH_END_{internalName}_00.pcc", false);
                if (endGmFile != null)
                {
                    // Liara doesn't have endgm file
                    sqmList.Add(MEPackageHandler.OpenMEPackage(endGmFile));
                }

                squadmatePackageMap[internalName] = sqmList;

                // Build out the rest of the list for this pawn
                int sqmIndex = 0;
                while (true)
                {
                    sqmIndex++;
                    var fName = $"BioH_{internalName}_0{sqmIndex}.pcc";
                    var newPackageF = MERFileSystem.GetPackageFile(fName, false);
                    if (newPackageF != null)
                    {
                        sqmList.Add(MEPackageHandler.OpenMEPackage(newPackageF));
                        sqmList.Add(MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_END_{internalName}_0{sqmIndex}.pcc")));
                    }
                    else
                    {
                        // No more squadmates in this set
                        break;
                    }
                }
            }
            #endregion

            #region Build power pool

            List<HTalent> talentPoolMaster = new List<HTalent>();
            List<HTalent> evolvedTalentPoolMaster = new List<HTalent>();
            foreach (var henchInfo in squadmatePackageMap)
            {
                var henchP = henchInfo.Value[0];
                var loadout = henchP.Exports.FirstOrDefault(x => x.ClassName == "SFXPlayerSquadLoadoutData");

                var powers = loadout.GetProperty<ArrayProperty<ObjectProperty>>("Powers").Select(x => x.ResolveToEntry(loadout.FileRef) as ExportEntry).ToList();
                foreach (var p in powers)
                {
                    if (CanBeShuffled(p))
                    {
                        talentPoolMaster.Add(new HTalent(p));
                    }
                }

            }
            #endregion


            // OLD CODE BELOW

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

                        // Calculate evolutions
                        // Get power internal name
                        while (power.SuperClass.ObjectName != "SFXPower" && power.SuperClass.ObjectName != "SFXPower_PassivePower")
                        {
                            power = power.SuperClass as ExportEntry;
                        }

                        var defaults = power.GetDefaults();
                        powerEvolutions.Add(defaults.GetProperty<ObjectProperty>("EvolvedPowerClass1").ResolveToEntry(power.FileRef) as ExportEntry);
                        powerEvolutions.Add(defaults.GetProperty<ObjectProperty>("EvolvedPowerClass2").ResolveToEntry(power.FileRef) as ExportEntry);
                    }
                }
            }

            // Step 2. Shuffle lists to ensure randomness of assignments
            allPowers.Shuffle();
            assets.Shuffle();
            powerEvolutions.Shuffle();

            // Step 3. Precalculate talent sets that will be assigned
            List<TalentSet> talentSets = new List<TalentSet>(6);

            int baseAttempt = 0;
            bool powersConfigured = false; //if a solution was found
            while (!powersConfigured)
            {
                // Reset
                Debug.WriteLine($"Assigning base talent sets, attempt #{baseAttempt++}");
                var assignmentPowers = allPowers.ToList();
                assignmentPowers.Shuffle();
                talentSets.Clear();

                // Attempt to build a list of compatible base powers
                bool powerSetsValid = true;
                for (int ti = 0; ti < 6; ti++)
                {
                    var talSet = new TalentSet(assignmentPowers);
                    if (talSet.IsBaseValid)
                    {
                        talentSets.Add(talSet);
                    }
                    else
                    {
                        powerSetsValid = false;
                        break; // Try again, this is not a valid solution to the problem
                    }
                }

                if (!powerSetsValid)
                {
                    continue; // try again
                }

                // We have configured a set of compatible base powers
                // There should be a configuration of evolutions that works,
                // but we will only attempt 5 ways
                bool configuredEvolutions = false;
                int evolutionAttempt = 0;
                while (!configuredEvolutions)
                {
                    // Reset
                    Debug.WriteLine($"Assigning evolution talent sets, attempt #{baseAttempt - 1}-{evolutionAttempt++}");
                    var assignmentEvolutions = powerEvolutions.ToList();
                    foreach (var ts in talentSets)
                    {
                        ts.EvolvedPowers.Clear();
                    }

                    // Attempt solution
                    assignmentEvolutions.Shuffle(); // reshuffle for new attempt
                    bool foundSolution = true;
                    foreach (var ts in talentSets)
                    {
                        foundSolution &= ts.SetEvolutions(assignmentEvolutions);
                        if (!foundSolution)
                            break; //short circuit search for solution
                    }

                    if (!foundSolution)
                    {
                        if (evolutionAttempt > 3)
                            break; // Force full retry
                        continue; // Try again, a solution was not found
                    }

                    configuredEvolutions = true;
                }

                if (!configuredEvolutions)
                    continue; // Do a full retry
                powersConfigured = true;
            }

            // Step 4. Assign and map powers

            // This doesn't seem to work in parallel due to some internal non-thread safe stuff in my code
            // Too lazy to figure out what it is
            var biogame = CoalescedHandler.GetIniFile("BIOGame.ini");
            int kitIndex = 0;

            // SINGLE THREAD FOR NOW!! Ini handler is not thread safe
            Parallel.ForEach(assets, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, loadout =>
                {
                    //foreach (var loadout in assets)
                    //{
                    // We must load a new copy of the package file into memory, one for reading, one for modifying,
                    // otherwise it will be concurrent modification
                    loadout = MEPackageHandler.OpenMEPackage(loadout.FileRef.FilePath).FindExport(loadout.InstancedFullPath);
                    var kit = loadout.ObjectName.Name.Substring(4);

                    MERLog.Information($"Randomizing class powers for {kit}");
                    List<MappedPower> configuredPowers = new List<MappedPower>();

                    // Clear mapping
                    var mappedPowers = biogame.GetOrAddSection($"SFXGamePawns.SFXCharacterClass_{kit}");
                    mappedPowers.Entries.Add(new DuplicatingIni.IniEntry("!MappedPowers")); //Clear

                    var loadoutProps = loadout.GetProperties();
                    var powersList = loadoutProps.GetProp<ArrayProperty<ObjectProperty>>("Powers");
                    var originalCount = powersList.Count;
                    var powersToKeep = powersList.Skip(6).Take(3).ToList();
                    powersList.Clear();

                    var talentSet = talentSets[kitIndex]; // Talent set for this kit
                    kitIndex++;

                    // Assign base powers
                    int powIndex = 0;
                    // Lists for updating the class description in the character creator
                    List<string> combatPowers = new List<string>();
                    List<string> ammoPowers = new List<string>();
                    foreach (var basePower in talentSet.Powers)
                    {
                        if (basePower.IsAmmoPower)
                            ammoPowers.Add(basePower.PowerName);
                        else if (basePower.IsCombatPower)
                            combatPowers.Add(basePower.PowerName);
                        var portedPower = PackageTools.PortExportIntoPackage(loadout.FileRef, basePower.PowerExport);
                        powersList.Add(new ObjectProperty(portedPower.UIndex));

                        if (basePower.BaseName != null)
                        {
                            mappedPowers.Entries.Add(new DuplicatingIni.IniEntry("MappedPowers", $"SFXPower_{basePower.BaseName}"));
                        }

                        // For each power, change the evolutions
                        var defaults = portedPower.GetDefaults();
                        var props = defaults.GetProperties();
                        var evolution1 = talentSet.EvolvedPowers[powIndex * 2];
                        var evolution2 = talentSet.EvolvedPowers[(powIndex * 2) + 1];
                        configuredPowers.Add(new MappedPower() { BaseTalent = basePower, EvolvedTalent1 = evolution1, EvolvedTalent2 = evolution2 });
                        props.AddOrReplaceProp(new ObjectProperty(PackageTools.PortExportIntoPackage(portedPower.FileRef, evolution1.PowerExport), "EvolvedPowerClass1"));
                        props.AddOrReplaceProp(new ObjectProperty(PackageTools.PortExportIntoPackage(portedPower.FileRef, evolution2.PowerExport), "EvolvedPowerClass2"));

                        // Update the evolution text via override
                        var ranksSource = basePower.BasePower.GetDefaults().GetProperty<ArrayProperty<StructProperty>>("Ranks");
                        if (ranksSource == null)
                        {
                            // Projectile powers are subclassed for player
                            ranksSource = (basePower.BasePower.SuperClass as ExportEntry).GetDefaults().GetProperty<ArrayProperty<StructProperty>>("Ranks");
                        }

                        var evolveRank = ranksSource[3];
                        var descriptionProp = evolveRank.Properties.GetProp<StringRefProperty>("Description");
                        var description = TLKHandler.TLKLookupByLang(descriptionProp.Value, "INT");
                        var descriptionLines = description.Split('\n');
                        descriptionLines[2] = $"1) {evolution1.EvolvedBlurb}";
                        descriptionLines[4] = $"2) {evolution2.EvolvedBlurb}";
                        var newStringID = TLKHandler.GetNewTLKID();
                        TLKHandler.ReplaceString(newStringID, string.Join('\n', descriptionLines));
                        descriptionProp.Value = newStringID;

                        props.AddOrReplaceProp(ranksSource); // copy the source rank info into our power with the modification

                        defaults.WriteProperties(props);
                        powIndex++;
                    }

                    // Add passives back
                    powersList.AddRange(powersToKeep);

                    // Write loadout data
                    if (powersList.Count != originalCount)
                    {
                        Debugger.Break();
                    }

                    loadoutProps.AddOrReplaceProp(powersList);

                    // Build the autoranks
                    loadoutProps.AddOrReplaceProp(BuildAutoRankList(loadout, configuredPowers));

                    // Finalize loadout export
                    loadout.WriteProperties(loadoutProps);

                    // Correct the unlock criteria to ensure every power can be unlocked.
                    for (int i = 0; i < talentSet.Powers.Count; i++)
                    {
                        // i = Power Index (0 indexed)

                        // Power dependency map:
                        // Power 1: Rank 1
                        // Power 2: Depends on Power 1 Rank 2
                        // Power 3: Rank 1
                        // Power 4: Depends on Power 3 Rank 2
                        // Power 5: Depends on Power 4 Rank 2
                        // Power 6: No requirements, but no points assigned

                        var power = loadout.FileRef.FindExport(talentSet.Powers[i].PowerExport.InstancedFullPath);
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
                            dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport(talentSet.Powers[i - 1].PowerExport.InstancedFullPath)));
                            properties.Add(dependencies);
                        }

                        defaults.WriteProperties(properties);
                    }

                    // Update the string ref for the class description
                    var tlkStrRef = ClasStrRefMap[kit];
                    var existingStr = TLKHandler.TLKLookupByLang(tlkStrRef, "INT");
                    var existingLines = existingStr.Split('\n').ToList();
                    var powersLineIdx = existingLines.FindIndex(x => x.StartsWith("Power Training:"));
                    var weaponsLineIdx = existingLines.FindIndex(x => x.StartsWith("Weapon Training:"));
                    var ammoLineIdx = existingLines.FindIndex(x => x.StartsWith("Ammo Training:"));

                    existingLines[powersLineIdx] = $"Power Training: {string.Join(", ", combatPowers)}";
                    var ammoText = $"Ammo Training: {string.Join(", ", ammoPowers)}";

                    if (ammoLineIdx >= 0 && !ammoPowers.Any())
                    {
                        existingLines.RemoveAt(ammoLineIdx); // no ammo powers. Remove the line
                    }
                    else if (ammoLineIdx == -1 && ammoPowers.Any())
                    {
                        existingLines.Insert(weaponsLineIdx + 1, ammoText); // Adding ammo powers. Add the line
                    }
                    else if (ammoLineIdx >= 0 && ammoText.Any())
                    {
                        // Replace existing line
                        existingLines[ammoLineIdx] = ammoText;
                    }
                    else if (ammoLineIdx == -1 && !ammoPowers.Any())
                    {
                        // Do nothing. There's no ammo line and there's no ammo powers.
                    }
                    else
                    {
                        // Should not occur!
                        Debugger.Break();
                    }

                    TLKHandler.ReplaceString(tlkStrRef, string.Join('\n', existingLines), "INT");

                    // Update the autolevel up s t ruct


                    MERFileSystem.SavePackage(loadout.FileRef);
                });

            return true;
        }


        private static string[] UnshuffleablePowerNames = new[]
        {
            "SFXPower_LoyaltyRequirement",
            "SFXPower_StasisNew_Liara", // CHECK TO SEE IF THIS ACTUALLY DEPENDS ON SHADER CACHE, WHO KNOWS?
            "SFXPower_KasumiCloakTeleport", // AI, shaders
            "SFXPower_KasumiUnique", // AI, shaders
            "SFXPower_KasumiAssassinate", // AI, shaders
            "SFXPower_ZaeedUnique", // shaders
        };

        /// <summary>
        /// If the power can be shuffled/reassigned
        /// </summary>
        /// <param name="exportEntry"></param>
        /// <returns></returns>
        private static bool CanBeShuffled(ExportEntry exportEntry)
        {
            return UnshuffleablePowerNames.Contains(exportEntry.ObjectName.Name, StringComparer.InvariantCultureIgnoreCase);
        }

        private static ExportEntry GetEvolution(IMEPackage package, List<ExportEntry> kitPowers, List<ExportEntry> evolvedPowersInKit, List<ExportEntry> powerEvolutions)
        {
            var power = powerEvolutions.PullFirstItem();
            int ix = 20;
            while (ix > 0 && evolvedPowersInKit.Any(x => x.ClassName == power.ClassName || x.InheritsFrom(power.ClassName)) && kitPowers.Any(x => power.InheritsFrom(x.ClassName)))
            {
                powerEvolutions.Add(power);
                power = powerEvolutions.PullFirstItem();
                ix--; //give up if we have to
            }

            return PackageTools.PortExportIntoPackage(package, power);
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
            ExportEntry baseDefaults = baseClass.GetDefaults();
            var evolvedPower1 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass1");
            while (evolvedPower1 == null || evolvedPower1.Value == 0)
            {
                baseClass = (ExportEntry)baseClass.SuperClass;
                baseDefaults = baseClass.GetDefaults();
                evolvedPower1 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass1");
            }

            // Evolved power 1
            props = new PropertyCollection();
            PopulateProps(props, evolvedPower1.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            // Evolved power 2
            var evolvedPower2 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass2");
            props = new PropertyCollection();
            PopulateProps(props, evolvedPower2.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            return powerRequirements;
        }

        private static bool CanShufflePower(ExportEntry power)
        {
            //if (power.ObjectName.Name.Contains("Passive")) return false; // Passives are actually visible and upgradable
            if (power.ObjectName.Name.Contains("SFXPower_Player")) return false;
            if (power.ObjectName.Name.Contains("FirstAid")) return false;
            return true;
        }

        private static ArrayProperty<StructProperty> BuildAutoRankList(ExportEntry loadout, List<MappedPower> mappedPowers)
        {
            ArrayProperty<StructProperty> alui = new ArrayProperty<StructProperty>("PowerLevelUpInfo");
            Dictionary<MappedPower, int> rankMap = mappedPowers.ToDictionary(x => x, x => 1);
            while (rankMap.Any(x => x.Value != 4))
            {
                // Add ranks
                var mp = mappedPowers.RandomElement();
                while (rankMap[mp] >= 4)
                {
                    mp = mappedPowers.RandomElement(); // Repick
                }

                var newRank = rankMap[mp] + 1;

                PropertyCollection props = new PropertyCollection();
                props.Add(new ObjectProperty(loadout.FileRef.FindExport(mp.BaseTalent.PowerExport.InstancedFullPath).UIndex, "PowerClass"));
                props.Add(new FloatProperty(newRank, "Rank"));
                if (newRank < 4)
                {
                    props.Add(new ObjectProperty(0, "EvolvedPowerClass"));
                }
                else
                {
                    var evo = ThreadSafeRandom.Next(2) == 0 ? mp.EvolvedTalent1 : mp.EvolvedTalent2;
                    props.Add(new ObjectProperty(loadout.FileRef.FindExport(evo.PowerExport.InstancedFullPath).UIndex, "EvolvedPowerClass"));
                }
                props.Add(new NoneProperty());
                alui.Add(new StructProperty("PowerLevelUp", props));
                rankMap[mp] = newRank;
            }


            int i = 0;
            while (i < 17)
            {
                i++;
            }
            return alui;
        }
    }
}
