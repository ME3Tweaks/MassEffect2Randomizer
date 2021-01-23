using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using Octokit;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Enemy
{
    class EnemyPowerChanger
    {
        internal class PowerInfo
        {
            public string PackageName { get; set; } = "SFXGameContent_Powers";
            public string PackageFileName { get; set; }
            public int SourceUIndex { get; set; }
            public EPowerCapabilityType Type { get; set; }

            public PowerInfo() { }
            public PowerInfo(ExportEntry export) {
                PackageFileName = Path.GetFileName(export.FileRef.FilePath);
                PackageName = export.ParentName;
                SourceUIndex = export.UIndex;

                // Type?
            }
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

        private static Dictionary<string, PowerInfo> PackageMapping = new Dictionary<string, PowerInfo>()
        {
            { "SFXPower_AbominationExplosion", new PowerInfo(){ PackageFileName = "BioP_RprGtA.pcc", SourceUIndex = 712, Type = EPowerCapabilityType.Death} }, // DEATH POWER
            { "SFXPower_HeavyMechExplosion", new PowerInfo(){ PackageFileName = "BioP_ProCer.pcc", SourceUIndex = 757, Type = EPowerCapabilityType.Death} }, // DEATH POWER
            
            /*{ "SFXPower_VorchaRegen", new PowerInfo(){ SourcePackage = "BioP_OmgPrA.pcc", SourceUIndex = 666, Type = EPowerCapabilityType.Buff} }, // DEATH POWER
            { "SFXPower_GethSupercharge", new PowerInfo(){ SourcePackage = "BioP_BlbGtl.pcc", SourceUIndex = 577, Type = EPowerCapabilityType.Buff} },
            { "SFXPower_Shockwave", new PowerInfo(){ SourcePackage = "BioP_Char.pcc", SourceUIndex = 1147, Type = EPowerCapabilityType.Attack} },
            { "SFXPower_Incinerate", new PowerInfo(){ SourcePackage = "BioP_Char.pcc", SourceUIndex = 1012, Type = EPowerCapabilityType.Attack} },
            { "SFXPower_Singularity", new PowerInfo(){ SourcePackage = "BioH_Vixen_00.pcc", SourceUIndex = 430, Type = EPowerCapabilityType.Attack} },
            { "SFXPower_Cloak", new PowerInfo(){ SourcePackage = "BioP_RprGtA.pcc", SourceUIndex = 731, Type = EPowerCapabilityType.Buff} },
            { "SFXPower_ScionTesla", new PowerInfo(){ SourcePackage = "BioP_RprGtA.pcc", SourceUIndex = 84, Type = EPowerCapabilityType.Attack} }, // May need way to adjust the cooldown to higher values
            { "SFXPower_Overload", new PowerInfo(){ SourcePackage = "BioP_RprGtA.pcc", SourceUIndex = 839, Type = EPowerCapabilityType.Attack} },
            { "SFXPower_GethShieldBoost", new PowerInfo(){ SourcePackage = "BioP_RprGtA.pcc", SourceUIndex = 799, Type = EPowerCapabilityType.Buff} },
            { "SFXPower_NeuralShock", new PowerInfo(){ SourcePackage = "SFXPower_NeuralShock_Player.pcc", SourceUIndex = 55, Type = EPowerCapabilityType.Debuff} }, // Can cause loops since drone has this power too
            { "SFXPower_Reave", new PowerInfo(){ SourcePackage = "SFXPower_Reave_Player.pcc", SourceUIndex = 26, Type = EPowerCapabilityType.Attack} }, // Can cause loops since drone has this power too

            { "SFXPower_HuskMeleeLeft", new PowerInfo(){ SourcePackage = "BioP_RprGtA.pcc", SourceUIndex = 267, Type = EPowerCapabilityType.Attack} }, // Melee
            { "SFXPower_HuskMeleeRight", new PowerInfo(){ SourcePackage = "BioP_RprGtA.pcc", SourceUIndex = 269, Type = EPowerCapabilityType.Attack} }, // Melee

            { "SFXPower_RiotShield", new PowerInfo(){ SourcePackage = "BioP_HorCr1.pcc", SourceUIndex = 685, Type = EPowerCapabilityType.Defense} }, // Melee*/

        };

        public static ExportEntry PortPowerIntoPackage(IMEPackage targetPackage, string weaponName)
        {
            var portingInfo = PackageMapping[weaponName];
            var sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage(portingInfo.PackageFileName);
            if (sourcePackage != null)
            {
                var sourceExport = sourcePackage.GetUExport(portingInfo.SourceUIndex);
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
                    var sourceFullPath = p.FullPath;
                    var matchingParent = targetPackage.Exports.FirstOrDefault(x => x.FullPath == sourceFullPath) as IEntry;
                    if (matchingParent == null)
                    {
                        matchingParent = targetPackage.Imports.FirstOrDefault(x => x.FullPath == sourceFullPath) as IEntry;
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

                var randomNewPower = PackageMapping.Keys.ToList().RandomElement();
                if (existingPowerEntry == null || randomNewPower != existingPowerEntry.ObjectName)
                {
                    var PowerInfo = PackageMapping[randomNewPower];

                    if (powers.Any(x => power.Value != int.MinValue && power.ResolveToEntry(export.FileRef).ObjectName == randomNewPower))
                        continue; // Duplicate powers crash the game

                    Log.Information($@"Changing power {export.ObjectName} {(existingPowerEntry != null ? existingPowerEntry.ObjectName : "New Power")} => {randomNewPower }");
                    // It's a different gun.

                    // See if we need to port this in
                    var fullName = PowerInfo.PackageName + "." + randomNewPower;
                    var repoint = export.FileRef.Imports.FirstOrDefault(x => x.FullPath == fullName) as IEntry;
                    if (repoint == null)
                    {
                        repoint = export.FileRef.Exports.FirstOrDefault(x => x.FullPath == fullName) as IEntry;
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

