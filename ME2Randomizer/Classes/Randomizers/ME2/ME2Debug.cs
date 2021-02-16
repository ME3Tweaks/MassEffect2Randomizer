using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;

namespace ME2Randomizer.Classes.Randomizers.ME2
{
    class ME2Debug
    {
        public static void CheckImportsWithPersistence(object? sender, DoWorkEventArgs doWorkEventArgs)
        {
            var file = @"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\BioP_JnkKgA.pcc";
            //var file = @"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\BioP_JnkKgA.pcc";
            var persistP = MEPackageHandler.OpenMEPackage(file);
            var importableObjects = EntryImporter.GetAllReferencesOfExport(persistP.FindExport("TheWorld.PersistentLevel"), true);
            Debug.WriteLine($"Persistent Referenced objects: {importableObjects.Count}. {importableObjects.Count(x => x is ImportEntry)} imports, {importableObjects.Count(x => x is ExportEntry)} exports");
            Debug.WriteLine($"Persistent package does not reference: {(persistP.ImportCount + persistP.ExportCount) - importableObjects.Count}. {persistP.ImportCount - importableObjects.Count(x => x is ImportEntry)} imports, {persistP.ExportCount - importableObjects.Count(x => x is ExportEntry)} exports:");

            var droppedImports = persistP.Imports.Except(importableObjects);
            var droppedExports = persistP.Exports.Except(importableObjects);

            foreach (var imp in droppedImports)
            {
                Debug.WriteLine($" >> Dropped import: {imp.UIndex} {imp.InstancedFullPath}");
            }
            foreach (var exp in droppedExports)
            {
                Debug.WriteLine($" >> Dropped export: {exp.UIndex} {exp.InstancedFullPath}");
            }


        }
    }
}
