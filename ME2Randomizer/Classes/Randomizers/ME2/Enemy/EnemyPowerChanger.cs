using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Misc;
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
        private static string[] PowersToNotSwap = new[]
        {
            // Collector powers, used by it's AI
            "SFXPower_CollectorWarp", //Used by Combat_Collector_Possessed
            "SFXPower_Singularity_NPC", // Used by Combat_Collector_Possessed
            "SFXPower_Collector_Pulse", // Used by Combat_Collector_Possessed

            "SFXPower_HuskMelee_Right", //Used by SwipeAttack() in SFXAI_Husk
            "SFXPower_HuskMelee_Left",

            "SFXPower_BioticChargeLong_NPC", // Used by the asari in LOTSB
            "SFXPower_Shockwave_NPC", //Used by the asari in LOTSB

            "SFXPower_Geth_Supercharge", // Used by SFXAI_GethTrooper Combat_Geth_Berserk
            "SFXPower_KroganCharge", // Krogan charge, used by it's AI
            "SFXPower_CombatDrone_Death", // Used by combat drone
        };

        /// <summary>
        /// List of loadouts that have all their powers locked for randomization due to their AI. Add more powers so their AI behaves differently.
        /// </summary>
        public static string[] LoadoutsToAddPowersTo = new[]
        {
            "SUB_COL_Possessed",
        };

        public static List<PowerInfo> Powers;

        public static void LoadPowers()
        {
            if (Powers == null)
            {
                string fileContents = MERUtilities.GetEmbeddedStaticFilesTextFile("powerlistme2.json");
                Powers = new List<PowerInfo>();
                var powermanifest = JsonConvert.DeserializeObject<List<PowerInfo>>(fileContents);
                foreach (var powerInfo in powermanifest)
                {
                    var powerFilePath = MERFileSystem.GetPackageFile(powerInfo.PackageFileName);
                    if (powerInfo.IsCorrectedPackage || (powerFilePath != null && File.Exists(powerFilePath)))
                    {
                        if (powerInfo.FileDependency != null && MERFileSystem.GetPackageFile(powerInfo.FileDependency) == null)
                            continue; // Dependency not met

                        Powers.Add(powerInfo);
                    }
                }
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

            /// <summary>
            /// If not null, test if this file exists when loading the power list (essentially a hack DLC check)
            /// </summary>
            [JsonProperty("filedependency")]
            public string FileDependency { get; set; }

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

            private static bool IsWhitelistedPower(ExportEntry export)
            {
                return IsWhitelistedPower(export.ObjectName);
            }

            private static bool IsWhitelistedPower(string powername)
            {
                if (powername == "SFXPower_Flashbang_NPC") return true;
                if (powername == "SFXPower_ZaeedUnique_Player") return true;
                //if (powername == "SFXPower_StasisNew") return true; //Doesn't work on player and player squad. It's otherwise identical to other powers so no real point, plus it has lots of embedded pawns
                return false;
            }

            public PowerInfo() { }
            public PowerInfo(ExportEntry export, bool isCorrectedPackage)
            {
                PowerName = export.ObjectName;
                if (!MapPowerType(export) && !IsWhitelistedPower(export))
                {
                    // Whitelisted powers bypass this check
                    // Powers that do not list a capability type are subclasses. We will not support using these
                    IsUsable = false;
                    return;
                }
                if (!IsWhitelistedPower(PowerName) &&
                    // Forced blacklist after whitelist
                    (
                        PowerName.Contains("Ammo")
                        || PowerName.Contains("Base")
                        || PowerName.Contains("FirstAid")
                        || PowerName.Contains("Player")
                        || PowerName.Contains("GunshipRocket")
                        || (PowerName.Contains("NPC") && PowerName != "SFXPower_CombatDrone_NPC") // this technically should be used, but too lazy to write algo to figure it out
                        || PowerName.Contains("Player")
                        || PowerName.Contains("Zaeed") // Only use player version. The normal one doesn't throw the grenade
                        || PowerName.Contains("HuskTesla")
                        || PowerName.Contains("Kasumi") // Depends on her AI
                        || PowerName.Contains("CombatDroneDeath") // Crashes the game
                        || PowerName.Contains("DeathChoir") // Buggy on non-praetorian, maybe crashes game?
                        || PowerName.Contains("Varren") // Don't use this
                        || PowerName.Contains("Lift_TwrMwA") // Not sure what this does, but culling itCrashes the game, maybe
                        || PowerName.Contains("Crush") // Don't let enemies use this, it won't do anything useful for the most part
                        || PowerName == "SFXPower_MechDog" // dunno what this is
                        || PowerName == "SFXPower_CombatDroneAttack" // Combat drone only
                        || PowerName == "SFXPower_HeavyMechExplosion" // This is not actually used and doesn't seem to work but is on some pawns
                        || PowerName == "SFXPower_CombatDrone" // Player version is way too OP. Enforce NPC version
                        || PowerName.Contains("Dominate") // This is pointless against player squad
                    )
                    )
                {
                    IsUsable = false;
                    return; // Do not use ammo or base powers as they're player only in the usable code
                }

                PackageFileName = Path.GetFileName(export.FileRef.FilePath);
                PackageName = export.ParentName;
                SourceUIndex = export.UIndex;

                var hasShaderCache = export.FileRef.FindExport("SeekFreeShaderCache") != null;
                RequiresStartupPackage = hasShaderCache && !export.FileRef.FilePath.Contains("Startup", StringComparison.InvariantCultureIgnoreCase);
                ImportOnly = hasShaderCache;
                IsCorrectedPackage = isCorrectedPackage;

                if (hasShaderCache && !IsWhitelistedPower(export))
                {
                    IsUsable = false; // only allow whitelisted DLC powers
                }

                SetupDependencies(export);
            }

            private void SetupDependencies(ExportEntry export)
            {
                switch (export.ObjectName)
                {
                    // Check for 01's as basegame has stub 00 versions
                    case "SFXPower_Flashbang_NPC":
                        FileDependency = "BioH_Thief_01.pcc"; // Test for Kasumi DLC
                        break;
                    case "SFXPower_ZaeedUnique_Player":
                        FileDependency = "BioH_Veteran_01.pcc"; // Test for Kasumi DLC
                        break;
                }
            }

            /// <summary>
            /// If this power file is stored in the executable
            /// </summary>
            [JsonProperty("iscorrectedpackage")]
            public bool IsCorrectedPackage { get; set; }

            [JsonIgnore]
            public bool IsUsable { get; set; } = true;

            /// <summary>
            /// If this power can be used as an import (it's in a startup file)
            /// </summary>
            [JsonProperty("importonly")]
            public bool ImportOnly { get; set; }
            /// <summary>
            /// If this power requires the package that contains it to be added to the startup list. Used for DLC powers
            /// </summary>
            [JsonProperty("requiresstartuppackage")]
            public bool RequiresStartupPackage { get; set; }

            /// <summary>
            /// A list of additional related powers that are required for this power to work, for example Shadow Strike requires teleport and assasination abilities. Full asset paths to power class
            /// </summary>
            [JsonProperty("additionalrequiredpowers")]
            public string[] AdditionalRequiredPowers { get; set; } = new string[] { };
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

        /// <summary>
        /// Ports a power into a package
        /// </summary>
        /// <param name="targetPackage"></param>
        /// <param name="powerInfo"></param>
        /// <param name="additionalPowers">A list of additioanl powers that are referenced when this powerinfo is an import only power (prevent re-opening package)</param>
        /// <returns></returns>
        public static IEntry PortPowerIntoPackage(IMEPackage targetPackage, PowerInfo powerInfo, out IMEPackage sourcePackage)
        {
            if (powerInfo.IsCorrectedPackage)
            {
                var sourceData = MERUtilities.GetEmbeddedStaticFilesBinaryFile("correctedloadouts.powers." + powerInfo.PackageFileName);
                sourcePackage = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(sourceData));
            }
            else
            {
                sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage(powerInfo.PackageFileName);
            }

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

                var newParent = EntryExporter.PortParents(sourceExport, targetPackage);
                IEntry newEntry;
                if (powerInfo.ImportOnly)
                {
                    //Debug.WriteLine($"ImportOnly in file {targetPackage.FilePath}");
                    if (powerInfo.RequiresStartupPackage)
                    {
                        ThreadSafeDLCStartupPackage.AddStartupPackage(Path.GetFileNameWithoutExtension(powerInfo.PackageFileName));
                        if (powerInfo.IsCorrectedPackage)
                        {
                            // File must be added to MERFS DLC
                            var outP = Path.Combine(MERFileSystem.DLCModCookedPath, powerInfo.PackageFileName);
                            if (!File.Exists(outP))
                            {
                                sourcePackage.Save(outP, true);
                            }
                        }
                    }

                    newEntry = PackageTools.CreateImportForClass(sourceExport, targetPackage, newParent);

                    // Port in extra imports so the calling class can reference them as necessary.
                    foreach (var addlPow in powerInfo.AdditionalRequiredPowers)
                    {
                        var addlSourceExp = sourcePackage.FindExport(addlPow);
                        PackageTools.CreateImportForClass(addlSourceExp, targetPackage, EntryExporter.PortParents(addlSourceExp, targetPackage));
                    }

                }
                else
                {

                    // DEBUG ONLY-----------------------------------
                    //var defaults = sourceExport.GetDefaults();
                    ////defaults.RemoveProperty("VFX");
                    //var vfx = defaults.GetProperty<ObjectProperty>("VFX").ResolveToEntry(sourcePackage) as ExportEntry;
                    //vfx.RemoveProperty("PlayerCrust");
                    //vfx.FileRef.GetUExport(1544).RemoveProperty("oPrefab");

                    ////vfx = defaults.FileRef.GetUExport(6211); // Prefab
                    ////vfx.RemoveProperty("WorldImpactVisualEffect");
                    //MERPackageCache cached = new MERPackageCache();
                    //EntryExporter.ExportExportToPackage(vfx, targetPackage, out newEntry, cached);
                    //PackageTools.AddReferencesToWorld(targetPackage, new [] {newEntry as ExportEntry});

                    //return null;


                    // END DEBUG ONLY--------------------------------

                    List<EntryStringPair> relinkResults = null;
                    if (powerInfo.IsCorrectedPackage || (PackageTools.IsPersistentPackage(powerInfo.PackageFileName) && MERFileSystem.GetPackageFile(powerInfo.PackageFileName.ToLocalizedFilename()) == null))
                    {
                        // Faster this way, without having to check imports
                        Dictionary<IEntry, IEntry> crossPCCObjectMap = new Dictionary<IEntry, IEntry>(); // Not sure what this is used for these days. SHould probably just be part of the method
                        relinkResults = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceExport, targetPackage,
                            newParent, true, out newEntry, crossPCCObjectMap);
                    }
                    else
                    {
                        // MEMORY SAFE (resolve imports to exports)
                        MERPackageCache cache = new MERPackageCache();
                        relinkResults = EntryExporter.ExportExportToPackage(sourceExport, targetPackage, out newEntry, cache);
                    }

                    if (relinkResults.Any())
                    {
                        Debugger.Break();
                    }
                }

                return newEntry;
            }
            return null; // No package was found
        }

        // This can probably be changed later
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == "SFXLoadoutData"
                                                                && !export.ObjectName.Name.Contains("Drone")
                                                                && Path.GetFileName(export.FileRef.FilePath).StartsWith("Bio");

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

            var originalPowerUIndexes = powers.Where(x => x.Value > 0).Select(x => x.Value).ToList();

            foreach (var power in powers.ToList())
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
                    if (PowersToNotSwap.Contains(existingPowerEntry.ObjectName.Name))
                    {
                        Debug.WriteLine($"Not changing power {existingPowerEntry.ObjectName.Name}");
                        continue; // Do not change this power
                    }
                }

                // DEBUG
                PowerInfo randomNewPower = Powers.RandomElement();
                //if (option.SliderValue < 0)
                //{
                //    randomNewPower = Powers.RandomElement();
                //}
                //else
                //{
                //    randomNewPower = Powers[(int)option.SliderValue];
                //}

                while (export.ObjectName.Name.Contains("Krogan", StringComparison.InvariantCultureIgnoreCase) && randomNewPower.Type == EPowerCapabilityType.Death)
                {
                    Debug.WriteLine(@"Re-roll no-death-power on krogan");
                    // Reroll. Krogan AI has something weird about it
                    randomNewPower = Powers.RandomElement();
                }

                if (existingPowerEntry == null || randomNewPower.PowerName != existingPowerEntry.ObjectName)
                {
                    if (powers.Any(x => power.Value != int.MinValue && power.ResolveToEntry(export.FileRef).ObjectName == randomNewPower.PowerName))
                        continue; // Duplicate powers crash the game. It seems this code is not bulletproof here and needs changed a bit...


                    Log.Information($@"Changing power {export.ObjectName} {existingPowerEntry?.ObjectName ?? "(+New Power)"} => {randomNewPower.PowerName }");
                    // It's a different gun.

                    // See if we need to port this in
                    var fullName = randomNewPower.PackageName + "." + randomNewPower.PowerName; // SFXGameContent_Powers.SFXPower_Hoops
                    var existingVersionOfPower = export.FileRef.FindEntry(fullName);

                    if (existingVersionOfPower != null)
                    {
                        // Power does not need ported in
                        power.Value = existingVersionOfPower.UIndex;
                    }
                    else
                    {
                        // Power needs ported in
                        power.Value = PortPowerIntoPackage(export.FileRef, randomNewPower, out _)?.UIndex ?? int.MinValue;
                    }

                    if (existingPowerEntry != null && existingPowerEntry.UIndex > 0 && PackageTools.IsPersistentPackage(export.FileRef.FilePath))
                    {
                        // Make sure we add the original power to the list of referenced memory objects
                        // so subfiles that depend on this power existing don't crash the game!
                        var world = export.FileRef.FindExport("TheWorld");
                        var worldBin = ObjectBinary.From<World>(world);
                        var extraRefs = worldBin.ExtraReferencedObjects.ToList();
                        extraRefs.Add(new UIndex(existingPowerEntry.UIndex));
                        worldBin.ExtraReferencedObjects = extraRefs.Distinct().ToArray(); // Filter out duplicates that may have already been in package
                        world.WriteBinary(worldBin);
                    }

                    foreach (var addlPow in randomNewPower.AdditionalRequiredPowers)
                    {
                        var existingPow = export.FileRef.FindEntry(addlPow);
                        //if (existingPow == null && randomNewPower.ImportOnly && sourcePackage != null)
                        //{
                        //    existingPow = PackageTools.CreateImportForClass(sourcePackage.FindExport(randomNewPower.PackageName + "." + randomNewPower.PowerName), export.FileRef);
                        //}

                        if (existingPow == null)
                        {
                            Debugger.Break();
                        }
                        powers.Add(new ObjectProperty(existingPow));
                    }
                }
            }

            // Strip any blank powers we might have added, remove any duplicates
            powers.RemoveAll(x => x.Value == int.MinValue);
            powers.ReplaceAll(powers.ToList().Distinct()); //tolist prevents concurrent modification in nested linq

            // DEBUG
#if DEBUG
            var duplicates = powers
                .GroupBy(i => i.Value)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
            if (duplicates.Any())
            {
                foreach (var dup in duplicates)
                {
                    Debug.WriteLine($"DUPLICATE POWER IN LOADOUT {export.FileRef.GetEntry(dup).ObjectName}");
                }
                Debugger.Break();
            }
#endif
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

            if (originalPowerUIndexes.Any())
            {
                // We should ensure the original objects are still referenced so shared objects they have (vfx?) are kept in memory
                // Dunno if this actually fixes the problems...
                PackageTools.AddReferencesToWorld(export.FileRef, originalPowerUIndexes.Select(x => export.FileRef.GetUExport(x)));
            }

            return true;
        }
    }
}

