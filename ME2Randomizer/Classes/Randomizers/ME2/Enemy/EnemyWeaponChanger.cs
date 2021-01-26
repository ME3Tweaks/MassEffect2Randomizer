using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
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
        public static ConcurrentDictionary<string, bool> LoadoutSupportsVisibleMapping;
        public static List<GunInfo> AllAvailableWeapons;
        public static List<GunInfo> VisibleAvailableWeapons;
        public static void LoadGuns()
        {
            if (AllAvailableWeapons == null)
            {
                string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("weaponloadoutrules.json");
                LoadoutSupportsVisibleMapping = JsonConvert.DeserializeObject<ConcurrentDictionary<string, bool>>(fileContents);

                fileContents = Utilities.GetEmbeddedStaticFilesTextFile("weaponlistme2.json");
                var allGuns = JsonConvert.DeserializeObject<List<GunInfo>>(fileContents).ToList();
                AllAvailableWeapons = new List<GunInfo>();
                VisibleAvailableWeapons = new List<GunInfo>();
                foreach (var g in allGuns)
                {
                    var gf = MERFileSystem.GetPackageFile(g.PackageFileName);
                    if (g.IsCorrectedPackage || (gf != null && File.Exists(gf)))
                    {
                        // debug force
                        if (g.GunName.Contains("Prae"))
                        {
                            AllAvailableWeapons.Add(g);
                            if (g.HasGunMesh)
                            {
                                VisibleAvailableWeapons.Add(g);
                            }
                        }
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
            LoadGuns();
            return true;
        }

        [DebuggerDisplay("GunInfo for {GunName} in {PackageFileName}")]
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
            [JsonIgnore]
            public long PackageFileSize { get; set; }
            /// <summary>
            /// If this is a DLC weapon that must be loaded into memory in order to be used (due to immovable shader cache)
            /// </summary>
            [JsonProperty("requiresstartuppackage")]
            public bool RequiresStartupPackage { get; set; }

            public GunInfo() { }
            public GunInfo(ExportEntry export, bool isCorrected)
            {
                ParseGun(export);
                GunName = export.ObjectName;
                PackageFileName = Path.GetFileName(export.FileRef.FilePath);
                PackageName = export.ParentName;
                SourceUIndex = export.UIndex;
                PackageFileSize = isCorrected ? 0 : new FileInfo(export.FileRef.FilePath).Length;
                IsCorrectedPackage = isCorrected;
            }

            /// <summary>
            /// If the file this is sourced from is stored in the randomizer executable and not the game
            /// </summary>
            [JsonProperty("iscorrectedpackage")]
            public bool IsCorrectedPackage { get; set; }

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

                var hasShaderCache = classExport.FileRef.FindExport("SeekFreeShaderCache") != null;
                RequiresStartupPackage = hasShaderCache && !classExport.FileRef.FilePath.Contains("Startup", StringComparison.InvariantCultureIgnoreCase);
                ImportOnly = hasShaderCache;
            }
        }

        public static List<GunInfo> GetAllowedWeaponsForLoadout(ExportEntry export)
        {
            List<GunInfo> guns = new();
            var objName = export.ObjectName.Name;
            if (objName.Contains("SecurityMech"))
            {
                // Others crash the game? AssaultRifle def does
                guns.AddRange(VisibleAvailableWeapons.Where(x => x.WeaponClassification == GunInfo.EWeaponClassification.SMG));
            }
            else if (LoadoutSupportsVisibleMapping.TryGetValue(export.FullPath, out var supportsVisibleWeapons))
            {
                // We use FullPath instead of instanced as the loadout number may change across randomization
                if (supportsVisibleWeapons)
                {
                    guns.AddRange(VisibleAvailableWeapons);
                }
                else
                {
                    guns.AddRange(AllAvailableWeapons);
                }
            }
            else
            {
                // Only allow visible guns
                guns.AddRange(VisibleAvailableWeapons);
            }

            return guns;
        }

        public static IEntry PortWeaponIntoPackage(IMEPackage targetPackage, GunInfo gunInfo)
        {
            IMEPackage sourcePackage;
            if (gunInfo.IsCorrectedPackage)
            {
                var sourceData = Utilities.GetEmbeddedStaticFilesBinaryFile("correctedloadouts.weapons." + gunInfo.PackageFileName);
                sourcePackage = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(sourceData));
            }
            else
            {
                sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage(gunInfo.PackageFileName);
            }
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
                    if (gunInfo.RequiresStartupPackage)
                    {
                        ThreadSafeDLCStartupPackage.AddStartupPackage(Path.GetFileNameWithoutExtension(gunInfo.PackageFileName));
                    }

                    newEntry = PackageTools.CreateImportForClass(sourceExport, targetPackage, newParent);
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
                        var repoint = export.FileRef.FindImport(fullName) as IEntry;
                        if (repoint == null)
                        {
                            repoint = export.FileRef.FindExport(fullName);
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
