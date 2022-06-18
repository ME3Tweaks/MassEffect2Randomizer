using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers.Game3.FirstRun
{
    public class Inventory
    {
        public static void PerformInventory(GameTarget target, string storagePath)
        {
            var inventoriedPowersF = Path.Combine(storagePath, "InventoriedPowers.pcc");
            var inventoriedWeaponsF = Path.Combine(storagePath, "InventoriedWeapons.pcc");
            MEPackageHandler.CreateAndSavePackage(inventoriedPowersF, target.Game);
            MEPackageHandler.CreateAndSavePackage(inventoriedWeaponsF, target.Game);

            using var ip = MEPackageHandler.OpenMEPackage(inventoriedPowersF);
            using var iw = MEPackageHandler.OpenMEPackage(inventoriedWeaponsF);

            var inventoriedPowerList = new SortedSet<string>();
            var inventoriedWeaponList = new SortedSet<string>();

            var usedFiles = MELoadedFiles.GetFilesLoadedInGame(target.Game, gameRootOverride: target.TargetPath);

            var gc = new PackageCache();
            foreach (var f in usedFiles)
            {
                using var package = MEPackageHandler.OpenMEPackage(f.Value);
                var localCache = new PackageCache();
                // Inventory Weapons
                foreach (var exp in package.Exports.Where(x => !x.IsDefaultObject && x.InheritsFrom("SFXWeapon")))
                {
                    if (inventoriedWeaponList.Contains(exp.InstancedFullPath))
                        continue;

                    Debug.WriteLine($"Inventoried weapon {exp.InstancedFullPath}");
                    EntryExporter.ExportExportToPackage(exp, iw, out var newEntry, gc, localCache);
                    inventoriedWeaponList.Add(exp.InstancedFullPath);
                }

                // Inventory Powers
                foreach (var exp in package.Exports.Where(x => !x.IsDefaultObject && x.InheritsFrom("SFXPowerCustomAction")))
                {
                    if (inventoriedPowerList.Contains(exp.InstancedFullPath))
                        continue;

                    Debug.WriteLine($"Inventoried power {exp.InstancedFullPath}");
                    EntryExporter.ExportExportToPackage(exp, ip, out var newEntry, gc, localCache);
                    inventoriedPowerList.Add(exp.InstancedFullPath);
                }
            }

            ip.Save();
            iw.Save();
        }
    }
}
