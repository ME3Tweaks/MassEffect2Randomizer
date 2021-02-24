using ME2Randomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using System.Diagnostics;
using System.IO;

namespace ME2Randomizer.DebugTools
{
    class ME2Debug
    {
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
    }
}
