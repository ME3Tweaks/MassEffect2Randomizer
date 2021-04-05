using System.ComponentModel;
using ME2Randomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.DebugTools
{
    class ME2Debug
    {
        public static void GetExportsInPersistentThatAreAlsoInSub()
        {
            var dlcModPath = Path.Combine(MEDirectories.GetDefaultGamePath(MERFileSystem.Game), "BioGame", "DLC", $"DLC_MOD_{MERFileSystem.Game}Randomizer", "CookedPC");

            var oldPersistentFile = MEPackageHandler.OpenMEPackage(Path.Combine(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC", "BioP_JnkKgA.pcc"));
            var newPersistentFile = MEPackageHandler.OpenMEPackage(Path.Combine(dlcModPath, "BioP_JnkKgA.pcc"));

            var oldExpList = oldPersistentFile.Exports.Select(x => x.InstancedFullPath);
            var newExpList = newPersistentFile.Exports.Select(x => x.InstancedFullPath);

            var newExports = newExpList.Except(oldExpList).ToList();

            var subFile = MEPackageHandler.OpenMEPackage(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\BioD_JnkKgA_300Labs.pcc");
            var subExports = subFile.Exports.Select(x => x.InstancedFullPath);

            var sameExports = subExports.Intersect(newExports).ToList();

            foreach (var v in sameExports)
            {
                Debug.WriteLine(v);
            }
        }

        public static void TestPropertiesInBinaryAssets()
        {
            var dlcModPath = @"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\binary";
            ReferenceCheckPackage rcp = new ReferenceCheckPackage();
            bool checkCanceled = false;
            EntryChecker.CheckReferences(rcp, dlcModPath, EntryChecker.NonLocalizedStringConveter, x => Debug.WriteLine(x));

            var cookedPC = ME2Directory.CookedPCPath;
            var sourcePackages = Directory.GetFiles(cookedPC, "SFX*.pcc", SearchOption.TopDirectoryOnly).Select(x => MEPackageHandler.OpenMEPackage(x)).ToList();
            sourcePackages.AddRange(Directory.GetFiles(cookedPC, "Bio*.pcc", SearchOption.TopDirectoryOnly).Select(x => MEPackageHandler.OpenMEPackage(x)));


            foreach (var s in rcp.GetInfoWarnings())
            {
                Debug.WriteLine($"INFO: {s.Message}");
            }

            foreach (var s in rcp.GetSignificantIssues())
            {
                Debug.WriteLine($"SIGNIFICANT: {s.Message}");
                if (s.Entry is ExportEntry exp)
                {
                    ExportEntry sourceToPull = null;
                    foreach (var sp in sourcePackages)
                    {
                        sourceToPull = sp.FindExport(exp.InstancedFullPath);
                        if (sourceToPull != null)
                            break;
                    }
                    if (sourceToPull != null)
                    {
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, sourceToPull, exp.FileRef, exp, true, out _);
                        Debug.WriteLine(" > REPLACED");
                    }
                }
            }

            foreach (var s in rcp.GetBlockingErrors())
            {
                Debug.WriteLine($"BLOCKING: {s.Message}");
            }

            var packagesToSave = rcp.GetSignificantIssues().Where(x => x.Entry != null && x.Entry.FileRef.IsModified).Select(x => x.Entry.FileRef).Distinct().ToList();
            foreach (var p in packagesToSave)
            {
                p.Save();
            }
        }

        public static void TestPropertiesInMERFS()
        {
            var dlcModPath = Path.Combine(MEDirectories.GetDefaultGamePath(MERFileSystem.Game), "BioGame", "DLC", $"DLC_MOD_{MERFileSystem.Game}Randomizer", "CookedPC");
            ReferenceCheckPackage rcp = new ReferenceCheckPackage();
            bool checkCanceled = false;
            EntryChecker.CheckReferences(rcp, dlcModPath, EntryChecker.NonLocalizedStringConveter, x => Debug.WriteLine(x));

            foreach (var s in rcp.GetInfoWarnings())
            {
                Debug.WriteLine($"INFO: {s}");
            }

            foreach (var s in rcp.GetSignificantIssues())
            {
                Debug.WriteLine($"SIGNIFICANT: {s}");
            }

            foreach (var s in rcp.GetBlockingErrors())
            {
                Debug.WriteLine($"BLOCKING: {s}");
            }
        }

        public static void TestAllImportsInMERFS()
        {
            var dlcModPath = Path.Combine(MEDirectories.GetDefaultGamePath(MERFileSystem.Game), "BioGame", "DLC", $"DLC_MOD_{MERFileSystem.Game}Randomizer", "CookedPC");

            var packages = Directory.GetFiles(dlcModPath);

            var globalCache = MERFileSystem.GetGlobalCache();
            //var globalP = Path.Combine(dlcModPath, "BioP_Global.pcc");
            //if (File.Exists(globalP))
            //{
            //    // This is used for animation lookups.
            //    globalCache.InsertIntoCache(MEPackageHandler.OpenMEPackage(globalP));
            //}

            foreach (var p in packages)
            {
                if (p.RepresentsPackageFilePath())
                {
                    MERPackageCache localCache = new MERPackageCache();
                    Debug.WriteLine($"Checking package file {p}");
                    var pack = MEPackageHandler.OpenMEPackage(p);
                    foreach (var imp in pack.Imports)
                    {
                        if (imp.InstancedFullPath.StartsWith("Core.") || imp.InstancedFullPath.StartsWith("Engine."))
                            continue; // These have some natives are always in same file.
                        if (imp.InstancedFullPath == "BioVFX_Z_TEXTURES.Generic.Glass_Shards_Norm" && MERFileSystem.Game == MEGame.ME2)
                            continue; // This is... native for some reason?
                        var resolvedExp = EntryImporter.ResolveImport(imp, globalCache, localCache);
                        if (resolvedExp == null)
                        {
                            Debug.WriteLine($"Could not resolve import: {imp.InstancedFullPath}");
                        }
                    }
                }
            }
            Debug.WriteLine("Done checking imports");
        }

        public static void BuildStartupPackage()
        {
            var weaponAnims = MEPackageHandler.OpenMEPackage(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\binary\WeaponAnims.pcc");
            var existingSUF = MEPackageHandler.OpenMEPackage(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_PRE_General\CookedPC\Startup_PRE_General_int.pcc");

            var startupFile = @"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\binary\Startup_DLC_MOD_ME2Randomizer_INT.pcc";
            MEPackageHandler.CreateAndSavePackage(startupFile, MERFileSystem.Game);
            var startupPackage = MEPackageHandler.OpenMEPackage(startupFile);

            var objReferencer = existingSUF.FindExport("ObjectReferencer_0");
            objReferencer.WriteProperty(new ArrayProperty<ObjectProperty>("ReferencedObjects")); // prevent porting other stuff
            var sObjRefencer = EntryImporter.ImportExport(startupPackage, objReferencer, 0, true);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, weaponAnims.FindExport("WeaponAnimData"), startupPackage, null, true, out _);
            var referencedObjects = new ArrayProperty<ObjectProperty>("ReferencedObjects");
            referencedObjects.AddRange(startupPackage.Exports.Where(x => x.ClassName == "AnimSet").Select(x => new ObjectProperty(x.UIndex)));


            sObjRefencer.WriteProperty(referencedObjects);
            startupPackage.Save(compress: true);
            Debug.WriteLine("Done");
        }

        /// <summary>
        /// Determines which imports and exports are not referenced and will be dropped when the file loads
        /// </summary>
        public static void CheckImportsWithPersistence()
        {
            var file = @"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\BioD_CitHub_300UpperWing_LOC_INT.pcc";
            //var file = @"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\BioP_JnkKgA.pcc";
            var persistP = MEPackageHandler.OpenMEPackage(file);
            var persistentLevel = persistP.FindExport("TheWorld.PersistentLevel");
            var objectReferencer = persistP.Exports.FirstOrDefault(x => x.idxLink == 0 && x.ClassName == "ObjectReferencer");

            if (persistentLevel == null && objectReferencer == null)
                Debugger.Break();
            var importableObjects = EntryImporter.GetAllReferencesOfExport(persistentLevel ?? objectReferencer, true);
            Debug.WriteLine($"Referenced objects: {importableObjects.Count}. {importableObjects.Count(x => x is ImportEntry)} imports, {importableObjects.Count(x => x is ExportEntry)} exports");
            Debug.WriteLine($"Unreferenced objects: {(persistP.ImportCount + persistP.ExportCount) - importableObjects.Count}. {persistP.ImportCount - importableObjects.Count(x => x is ImportEntry)} imports, {persistP.ExportCount - importableObjects.Count(x => x is ExportEntry)} exports:");

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
