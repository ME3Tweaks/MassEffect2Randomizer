using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using Newtonsoft.Json;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Enemy
{
    class EnemyWeaponChanger
    {
        public static List<GunInfo> AvailableWeapons;
        public static void LoadGuns()
        {
            if (AvailableWeapons == null)
            {
                string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("weaponlistme2.json");
                AvailableWeapons = JsonConvert.DeserializeObject<List<GunInfo>>(fileContents).Where(x=>x.ImportOnly).ToList();
            }
        }

        /// <summary>
        /// Hack to force power lists to load with only a single check
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool Init(RandomizationOption option)
        {
            LoadGuns();
            return true;
        }


        internal class GunInfo
        {
            public enum EWeaponClassification
            {
                AssaultRifle,
                Pistol,
                SMG,
                Shotgun,
                SniperRifle,
                HeavyWeapon
            }
            /// <summary>
            /// The parent package export of this export
            /// </summary>
            [JsonProperty("packagename")]
            public string PackageName { get; set; } = "SFXGameContent_Inventory";
            /// <summary>
            /// Package file that contains this class export
            /// </summary>
            [JsonProperty("packagefilename")]
            public string PackageFileName { get; set; }
            [JsonProperty("sourceuindex")]

            public int SourceUIndex { get; set; }
            /// <summary>
            /// Weapon selection weighting
            /// </summary>
            [JsonIgnore]
            public float Weight { get; set; } = 1.0f;
            [JsonProperty("weaponclassification")]
            public EWeaponClassification WeaponClassification { get; set; }

            /// <summary>
            /// If gun has a mesh. If it doesn't, it can only be used by pawns that support hidden mesh guns
            /// </summary>
            [JsonProperty("hasgunmesh")]
            public bool HasGunMesh { get; set; }
            /// <summary>
            /// Object Name
            /// </summary>
            [JsonProperty("gunname")]
            public string GunName { get; set; }
            /// <summary>
            /// If this gun can only be used via imports - this is for DLC weapons that are for some reason loaded in a startup file and thus will always be in memory
            /// </summary>
            [JsonProperty("importonly")]
            public bool ImportOnly { get; set; }

            [JsonIgnore]
            public bool IsUsable { get; set; } = true;
            public GunInfo() { }
            public GunInfo(ExportEntry export)
            {
                ParseGun(export);
                GunName = export.ObjectName;
                PackageFileName = Path.GetFileName(export.FileRef.FilePath);
                PackageName = export.ParentName;
                SourceUIndex = export.UIndex;
            }

            private void ParseGun(ExportEntry classExport)
            {
                var uClass = ObjectBinary.From<UClass>(classExport);
                var defaults = classExport.FileRef.GetUExport(uClass.Defaults);
                var props = defaults.GetProperties();

                var mesh = props.GetProp<ObjectProperty>("Mesh");
                if (mesh?.ResolveToEntry(classExport.FileRef) is ExportEntry meshExp)
                {
                    var meshProp = meshExp.GetProperty<ObjectProperty>("SkeletalMesh");
                    HasGunMesh = meshProp != null;
                }

                if (classExport.InheritsFrom("SFXHeavyWeapon"))
                {
                    WeaponClassification = EWeaponClassification.HeavyWeapon;
                }
                else if (classExport.InheritsFrom("SFXWeapon_AssaultRifle"))
                {
                    WeaponClassification = EWeaponClassification.AssaultRifle;
                }
                else if (classExport.InheritsFrom("SFXWeapon_HeavyPistol"))
                {
                    WeaponClassification = EWeaponClassification.Pistol;
                }
                else if (classExport.InheritsFrom("SFXWeapon_AutoPistol"))
                {
                    WeaponClassification = EWeaponClassification.SMG;
                }
                else if (classExport.InheritsFrom("SFXWeapon_Shotgun"))
                {
                    WeaponClassification = EWeaponClassification.Shotgun;
                }
                else if (classExport.InheritsFrom("SFXWeapon_SniperRifle"))
                {
                    WeaponClassification = EWeaponClassification.SniperRifle;
                }
                else
                {
                    Debugger.Break();
                }
            }
        }

        public static List<GunInfo> GetAllowedWeaponsForLoadout(ExportEntry export)
        {
            List<GunInfo> guns = new();
            var objName = export.ObjectName.Name;
            if (objName.Contains("SecurityMech"))
            {
                // Others crash the game? AssaultRifle def does
                guns.AddRange(AvailableWeapons.Where(x => x.WeaponClassification == GunInfo.EWeaponClassification.SMG));
            }
            else
            {
                // All guns on the table
                guns.AddRange(AvailableWeapons);
            }

            return guns;
        }

        public static IEntry PortWeaponIntoPackage(IMEPackage targetPackage, GunInfo gunInfo)
        {
            var sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage(gunInfo.PackageFileName);
            if (sourcePackage != null)
            {
                var sourceExport = sourcePackage.GetUExport(gunInfo.SourceUIndex);

                if (!sourceExport.InheritsFrom("SFXWeapon") || sourceExport.IsDefaultObject)
                {
                    throw new Exception("Wrong setup!");
                }

                if (sourceExport.Parent != null && sourceExport.Parent.ClassName != "Package")
                {
                    throw new Exception("Cannot port weapon - parent object is not Package!");
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

                void errorOccuredCB(string s)
                {
                    Debugger.Break();
                }

                IEntry newEntry = null;
                if (gunInfo.ImportOnly)
                {
                    Debug.WriteLine($"ImportOnly in file {targetPackage.FilePath}");
                    newEntry = PackageTools.CreateImportForClass(sourceExport, targetPackage);
                }
                else
                {
                    Dictionary<IEntry, IEntry> crossPCCObjectMap = new Dictionary<IEntry, IEntry>(); // Not sure what this is used for these days. SHould probably just be part of the method
                    var relinkResults = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceExport, targetPackage,
                        newParent, true, out newEntry, crossPCCObjectMap, errorOccuredCB);

                    if (relinkResults.Any())
                    {
                        Debugger.Break();
                    }
                }

                return newEntry;
            }
            else
            {
                Debug.WriteLine($"Package for gun porting not found: {gunInfo.PackageFileName}");
            }
        return null; // No package was found
        }

        // This can probably be changed later
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == "SFXLoadoutData"
                                                                                        && !export.ObjectName.Name.Contains("HeavyWeaponMech") // Not actually sure we can't randomize this one
                                                                                        && !export.ObjectName.Name.Contains("BOS_Reaper") // Don't randomize the final boss cause it'd really make him stupid
                                                                                        && export.GetProperty<ArrayProperty<ObjectProperty>>("Weapons") != null;

        internal static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var guns = export.GetProperty<ArrayProperty<ObjectProperty>>("Weapons");
            if (guns.Count == 1) //Randomizing multiple guns could be difficult and I'm not sure enemies ever change their weapons.
            {
                var gun = guns[0];
                if (gun.Value == 0) return false; // Null entry in weapons list
                var allowedGuns = GetAllowedWeaponsForLoadout(export);
                if (allowedGuns.Any())
                {
                    var randomNewGun = allowedGuns.RandomElementByWeight(x => x.Weight);
                    if (randomNewGun.GunName != gun.ResolveToEntry(export.FileRef).ObjectName)
                    {
                        var gunInfo = randomNewGun;
                        Log.Information($@"Changing gun {export.ObjectName} => {randomNewGun.GunName}");
                        // It's a different gun.

                        // See if we need to port this in
                        var fullName = gunInfo.PackageName + "." + randomNewGun.GunName;
                        var repoint = export.FileRef.Imports.FirstOrDefault(x => x.FullPath == fullName) as IEntry;
                        if (repoint == null)
                        {
                            repoint = export.FileRef.Exports.FirstOrDefault(x => x.FullPath == fullName) as IEntry;
                        }

                        if (repoint != null)
                        {
                            // Gun does not need ported in
                            gun.Value = repoint.UIndex;
                        }
                        else
                        {
                            // Gun needs ported in
                            var newEntry = PortWeaponIntoPackage(export.FileRef, randomNewGun);
                            gun.Value = newEntry.UIndex;
                        }

                        export.WriteProperty(guns);
                        var pName = Path.GetFileName(export.FileRef.FilePath);

                        // If this is not a master or localization file (which are often used for imports) 
                        // Change the number around so it will work across packages.
                        // May need disabled if game becomes unstable.

                        // We check if less than 10 as it's very unlikely there will be more than 10 loadouts in a non-persistent package
                        // if it's > 10 it's likely already a memory-changed item by MER
                        if (export.indexValue < 10 && !PackageTools.IsPersistentPackage(pName) && !PackageTools.IsLocalizationPackage(pName))
                        {
                            export.ObjectName = new NameReference(export.ObjectName, ThreadSafeRandom.Next(2000));
                        }
                    }
                }
            }

            return false;
        }
    }

}
