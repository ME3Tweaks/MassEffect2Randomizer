using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Enemy
{
    class EnemyWeaponChanger
    {
        internal class GunInfo
        {
            public string PackageName { get; set; } = "SFXGameContent_Inventory";
            public string SourcePackage { get; set; }
            public int SourceUIndex { get; set; }
            public float Weight { get; set; } = 1.0f;
            public string Class { get; set; }
        }

        private static Dictionary<string, GunInfo> PackageMapping = new Dictionary<string, GunInfo>()
        {
            // Heavy weapons
            {"SFXHeavyWeapon_FlameThrower", new GunInfo(){ SourcePackage = "BioD_OmgGrA_202Wave2.pcc", SourceUIndex = 453, Class="AssaultRifle"}},
            {"SFXHeavyWeapon_FreezeGun", new GunInfo(){ SourcePackage = "SFXHeavyWeapon_FreezeGun.pcc", SourceUIndex = 35, Class="AssaultRifle"}},
            {"SFXHeavyWeapon_MissileLauncher", new GunInfo(){ SourcePackage = "SFXHeavyWeapon_MissileLauncher.pcc", SourceUIndex = 9, Class="AssaultRifle"}},
            {"SFXHeavyWeapon_NukeLauncher", new GunInfo(){ SourcePackage = "SFXHeavyWeapon_NukeLauncher.pcc", SourceUIndex = 28, Weight=0.3f, Class="AssaultRifle"}},
            {"SFXHeavyWeapon_ParticleBeam", new GunInfo(){ SourcePackage = "SFXHeavyWeapon_ParticleBeam.pcc", SourceUIndex = 82, Class="AssaultRifle"}},
            {"SFXHeavyWeapon_GrenadeLauncher", new GunInfo(){ SourcePackage = "SFXHeavyWeapon_GrenadeLauncher.pcc", SourceUIndex = 5, Class="AssaultRifle"}},

            // Assault Rifles
            {"SFXWeapon_GethPulseRifle", new GunInfo(){ SourcePackage = "SFXWeapon_AssaultRifles.pcc", SourceUIndex = 53, Class="AssaultRifle"}},
            {"SFXWeapon_AssaultRifle", new GunInfo(){ SourcePackage = "SFXWeapon_AssaultRifles.pcc", SourceUIndex = 33, Class="AssaultRifle"}},
            {"SFXWeapon_MachineGun", new GunInfo(){ SourcePackage = "SFXWeapon_AssaultRifles.pcc", SourceUIndex = 66, Class="AssaultRifle"}},
            {"SFXWeapon_Needler", new GunInfo(){ SourcePackage = "SFXWeapon_AssaultRifles.pcc", SourceUIndex = 96, Class="AssaultRifle"}},
            //{"SFXWeapon_GethMiniGun", new GunInfo(){ SourcePackage = "BioD_BlbGtl_205Evacuation.pcc", SourceUIndex = 47, Weight=0.5f}}, // Gun has some stuff in Imports so it crashes game


            // Pistols
            {"SFXWeapon_HandCannon", new GunInfo(){ SourcePackage = "SFXWeapon_HeavyPistols.pcc", SourceUIndex = 36, Class="Pistol"}},
            {"SFXWeapon_HeavyPistol", new GunInfo(){ SourcePackage = "SFXWeapon_HeavyPistols.pcc", SourceUIndex = 16, Class="Pistol"}},
            
            // AutoPistols (SMG)
            {"SFXWeapon_AutoPistol", new GunInfo(){ SourcePackage = "SFXWeapon_AutoPistols.pcc", SourceUIndex = 16, Class="SMG"}},
            {"SFXWeapon_SMG", new GunInfo(){ SourcePackage = "SFXWeapon_AutoPistols.pcc", SourceUIndex = 38, Class="AssaultRifle"}},

            // Snipers
            {"SFXWeapon_SniperRifles", new GunInfo(){ SourcePackage = "SFXWeapon_SniperRifles.pcc", SourceUIndex = 20, Class="SniperRifle"}},
            {"SFXWeapon_AntiMatRifle", new GunInfo(){ SourcePackage = "SFXWeapon_SniperRifles.pcc", SourceUIndex = 41, Class="SniperRifle"}},
            {"SFXWeapon_MassCannon", new GunInfo(){ SourcePackage = "SFXWeapon_SniperRifles.pcc", SourceUIndex = 55, Class="SniperRifle"}},

            // Shotguns
            {"SFXWeapon_HeavyShotgun", new GunInfo(){ SourcePackage = "SFXWeapon_Shotguns.pcc", SourceUIndex = 81, Class="Shotgun"}},
            {"SFXWeapon_Shotgun", new GunInfo(){ SourcePackage = "SFXWeapon_Shotguns.pcc", SourceUIndex = 47, Class="Shotgun"}},
            {"SFXWeapon_FlakGun", new GunInfo(){ SourcePackage = "SFXWeapon_Shotguns.pcc", SourceUIndex = 68, Class="Shotgun"}},

            // Collector guns (not in cooked single package)
            //{"SFXWeapon_CollectorNeedler", new GunInfo(){ SourcePackage = "BioP_HorCr1.pcc", SourceUIndex = 387}}, //?
            //{"SFXWeapon_CollectorRifle", new GunInfo(){ SourcePackage = "BioP_HorCr1.pcc", SourceUIndex = 389}}, //?


            // DLC GUNS---- (Are not usable due to shader cache)
            //SFXGameContentDLC_Pistol.SFXWeapon_LaserPistol  79 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MCR_01\CookedPC\SFXWeapon_LaserPistol.pcc
            //SFXGameContentDLC_Desert.SFXWeapon_DesertAssaultRifle   41 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MCR_01\CookedPC\SFXWeapon_DesertAssaultRifle.pcc
            //SFXGameContentDLC_PRE_Cerberus.SFXWeapon_CerberusShotgun    53 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_PRE_Cerberus\CookedPC\SFXWeapon_CerberusShotgun.pcc
            //SFXGameContentDLC_CER_Arc.SFXHeavyWeapon_ArcProjector   403 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_CER_Arc\CookedPC\SFXHeavyWeapon_ArcProjector.pcc
            //SFXGameContentDLC_MCR_02.SFXWeapon_GethShotgun  210 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MCR_01\CookedPC\SFXWeapon_GethShotgun.pcc
            //SFXGameContentDLC_HEN_VT.SFXHeavyWeapon_FlameThrower_Player 139 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_HEN_VT\CookedPC\SFXHeavyWeapon_FlameThrower_Player.pcc
            //SFXGameContentDLC_HEN_MT.SFXWeapon_TacticalMachinePistol    26 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_HEN_MT\CookedPC\SFXWeapon_TMP.pcc
            //SFXGameContentDLC_PRE_Collectors.SFXWeapon_CollectorAssaultRifle_Player 44 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_PRE_Collectors\CookedPC\SFXPlayerWeapons_Collectors.pcc
            //SFXGameContentDLC_PRE_Gamestop.SFXHeavyWeapon_Blackstorm    76 B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_PRE_Gamestop\CookedPC\SFXHeavyWeapon_Blackstorm.pcc
        };

        public static List<KeyValuePair<string, GunInfo>> GetAllowedWeaponsForLoadout(ExportEntry export)
        {
            List<KeyValuePair<string, GunInfo>> str = new();
            var objName = export.ObjectName.Name;
            if (objName.Contains("SecurityMech"))
            {
                // Others crash the game? AssaultRifle def does
                str.AddRange(PackageMapping.Where(x => x.Value.Class == "SMG"));
            }
            else
            {
                // All guns on the table
                str.AddRange(PackageMapping);
            }

            return str;
        }

        public static ExportEntry PortWeaponIntoPackage(IMEPackage targetPackage, string weaponName)
        {
            var portingInfo = PackageMapping[weaponName];
            var sourcePackage = NonSharedPackageCache.GetCachedPackage(portingInfo.SourcePackage);
            if (sourcePackage != null)
            {
                var sourceExport = sourcePackage.GetUExport(portingInfo.SourceUIndex);

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

                void errorOccuredCB(string s)
                {
                    Debugger.Break();
                }

                Dictionary<IEntry, IEntry> crossPCCObjectMap = new Dictionary<IEntry, IEntry>(); // Not sure what this is used for these days. SHould probably just be part of the method
                var relinkResults = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceExport, targetPackage,
                    newParent, true, out IEntry newEntry, crossPCCObjectMap, errorOccuredCB);

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
                                                                                        && !export.ObjectName.Name.Contains("HeavyWeaponMech")
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
                    var randomNewGun = allowedGuns.RandomElementByWeight(x => x.Value.Weight);
                    if (randomNewGun.Key != gun.ResolveToEntry(export.FileRef).ObjectName)
                    {
                        var gunInfo = randomNewGun.Value;
                        Log.Information($@"Changing gun {export.ObjectName} => {randomNewGun.Key}");
                        // It's a different gun.

                        // See if we need to port this in
                        var fullName = gunInfo.PackageName + "." + randomNewGun.Key;
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
                            var exp = PortWeaponIntoPackage(export.FileRef, randomNewGun.Key);
                            gun.Value = exp.UIndex;
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
