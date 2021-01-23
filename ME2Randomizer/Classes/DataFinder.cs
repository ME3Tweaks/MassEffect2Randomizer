using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using MassEffectRandomizer;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Enemy;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes
{
    class DataFinder
    {
        private MainWindow mainWindow;
        private BackgroundWorker dataworker;

        public DataFinder(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            dataworker = new BackgroundWorker();

            dataworker.DoWork += FindDatapads;
            dataworker.RunWorkerCompleted += ResetUI;

            mainWindow.ShowProgressPanel = true;
            dataworker.RunWorkerAsync();
        }

        private void ResetUI(object sender, RunWorkerCompletedEventArgs e)
        {
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.CurrentProgressValue = 0;
            mainWindow.ShowProgressPanel = false;
            mainWindow.CurrentOperationText = "Data finder done";
        }

        private void FindDatapads(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values.ToList();
            mainWindow.CurrentOperationText = "Scanning for stuff";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            var startupFileCache = GetGlobalCache();

            
            ConcurrentDictionary<string, EnemyPowerChanger.PowerInfo> mapping = new ConcurrentDictionary<string, EnemyPowerChanger.PowerInfo>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (file) =>
              {
                  mainWindow.CurrentOperationText = $"Scanning for stuff [{numdone}/{numtodo}]";
                  Interlocked.Increment(ref numdone);

                  var package = MEPackageHandler.OpenMEPackage(file);
                  if (package.FindExport("SeekFreeShaderCache") != null)
                      return; // don't check these
                  var powers = package.Exports.Where(x => x.InheritsFrom("SFXPower") && !x.IsDefaultObject);
                  foreach (var skm in powers.Where(x => !mapping.ContainsKey(x.InstancedFullPath)))
                  {
                      // See if power is fully defined in package?
                      var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
                      var importDependencies = dependencies.OfType<ImportEntry>().ToList();
                      var usable = CheckImports(importDependencies, package, startupFileCache, out var missingImport);
                      if (usable)
                      {
                          Debug.WriteLine($"Usable power: {skm.InstancedFullPath} in {package.FilePath}");

                          mapping[skm.InstancedFullPath] = new EnemyPowerChanger.PowerInfo(skm);

                      }
                      else
                      {
                          //Debug.WriteLine($"Not usable power: {skm.InstancedFullPath} in {package.FilePath}");
                      }
                  }
                  mainWindow.CurrentProgressValue = numdone;
              });

            foreach (var v in mapping)
            {
                Debug.WriteLine($"{v.Key}\t{v.Value}");
            }

            // Coagulate stuff
            //Dictionary<string, int> counts = new Dictionary<string, int>();
            //foreach (var v in listM)
            //{
            //    foreach (var k in v.Value)
            //    {
            //        int existingC = 0;
            //        counts.TryGetValue(k, out existingC);
            //        existingC++;
            //        counts[k] = existingC;
            //    }
            //}

            //foreach (var count in counts.OrderBy(x => x.Key))
            //{
            //    Debug.WriteLine($"{count.Key}\t\t\t{count.Value}");
            //}
        }

        private static PackageCache GetGlobalCache()
        {
            var cache = new PackageCache();
            cache.GetCachedPackage("Core.pcc");
            cache.GetCachedPackage("SFXGame.pcc");
            cache.GetCachedPackage("Startup_INT.pcc");
            cache.GetCachedPackage("Engine.pcc");
            cache.GetCachedPackage("WwiseAudio.pcc");
            cache.GetCachedPackage("SFXOnlineFoundation.pcc");
            cache.GetCachedPackage("PlotManagerMap.pcc");
            cache.GetCachedPackage("GFxUI.pcc");
            return cache;
        }

        /// <summary>
        /// Checks to see if the listed imports can be reliably resolved as being in memory via their parents and localizations.
        /// </summary>
        /// <param name="imports"></param>
        private static bool CheckImports(List<ImportEntry> imports, IMEPackage package, PackageCache globalCache, out ImportEntry unresolvableEntry)
        {
            // Force into memory
            //var importExtras = EntryImporter.GetPossibleAssociatedFiles(package);
            PackageCache lpc = new PackageCache();
            //foreach (var ie in importExtras)
            //{
            //    lpc.GetCachedPackage(ie);
            //}

            // Enumerate and resolve all imports.
            bool canBeUsed = true;
            foreach (var import in imports)
            {
                if (import.InstancedFullPath == "BioVFX_Z_TEXTURES.Generic.Glass_Shards_Norm ")
                    continue; // this is native for some reason

                //Debug.Write($@"Resolving {import.FullPath}...");
                var export = ResolveImport(import, globalCache, lpc);
                if (export != null)
                {
                    //Debug.WriteLine($@" OK");
                }
                else if (UnrealObjectInfo.IsAKnownNativeClass(import))
                {
                    // Debug.WriteLine($@" OK, in native");
                }
                else
                {
                    lpc.ReleasePackages();
                    unresolvableEntry = import;
                    return false;
                    Debug.WriteLine($@" {import.FullPath} UNRESOLVABLE!");
                    //Debugger.Break();
                }
            }
            lpc.ReleasePackages();
            unresolvableEntry = null;
            return true;
        }

        public static ExportEntry ResolveImport(ImportEntry entry, PackageCache globalCache, PackageCache localCache)
        {
            var entryFullPath = entry.InstancedFullPath;


            string containingDirectory = Path.GetDirectoryName(entry.FileRef.FilePath);
            var filesToCheck = new List<string>();
            CaseInsensitiveDictionary<string> gameFiles = MELoadedFiles.GetFilesLoadedInGame(entry.Game);

            string upkOrPcc = entry.Game == MEGame.ME1 ? ".upk" : ".pcc";
            // Check if there is package that has this name. This works for things like resolving SFXPawn_Banshee
            bool addPackageFile = gameFiles.TryGetValue(entry.ObjectName + upkOrPcc, out var efxPath) && !filesToCheck.Contains(efxPath);

            // Let's see if there is same-named top level package folder file. This will resolve class imports from SFXGame, Engine, etc.
            IEntry p = entry.Parent;
            if (p != null)
            {
                while (p.Parent != null)
                {
                    p = p.Parent;
                }

                if (p.ClassName == "Package")
                {
                    if (gameFiles.TryGetValue($"{p.ObjectName}{upkOrPcc}", out var efPath) && !filesToCheck.Contains(efxPath))
                    {
                        filesToCheck.Add(Path.GetFileName(efPath));
                    }
                    else if (entry.Game == MEGame.ME1)
                    {
                        if (gameFiles.TryGetValue(p.ObjectName + ".u", out var path) && !filesToCheck.Contains(efxPath))
                        {
                            filesToCheck.Add(Path.GetFileName(path));
                        }
                    }
                }
            }

            // 
            filesToCheck.Add(entry.PackageFile + upkOrPcc);

            if (addPackageFile)
            {
                filesToCheck.Add(Path.GetFileName(efxPath));
            }



            //add related files that will be loaded at the same time (eg. for BioD_Nor_310, check BioD_Nor_310_LOC_INT, BioD_Nor, and BioP_Nor)
            filesToCheck.AddRange(EntryImporter.GetPossibleAssociatedFiles(entry.FileRef));



            if (entry.Game == MEGame.ME3)
            {
                // Look in BIOP_MP_Common. This is not a 'safe' file but it is always loaded in MP mode and will be commonly referenced by MP files
                if (gameFiles.TryGetValue("BIOP_MP_COMMON.pcc", out var efPath))
                {
                    filesToCheck.Add(Path.GetFileName(efPath));
                }
            }


            //add base definition files that are always loaded (Core, Engine, etc.)
            foreach (var fileName in EntryImporter.FilesSafeToImportFrom(entry.Game))
            {
                if (gameFiles.TryGetValue(fileName, out var efPath))
                {
                    filesToCheck.Add(Path.GetFileName(efPath));
                }
            }

            //add startup files (always loaded)
            IEnumerable<string> startups;
            if (entry.Game == MEGame.ME2)
            {
                startups = gameFiles.Keys.Where(x => x.Contains("Startup_", StringComparison.InvariantCultureIgnoreCase) && x.Contains("_INT", StringComparison.InvariantCultureIgnoreCase)); //me2 this will unfortunately include the main startup file
            }
            else
            {
                startups = gameFiles.Keys.Where(x => x.Contains("Startup_", StringComparison.InvariantCultureIgnoreCase)); //me2 this will unfortunately include the main startup file
            }

            filesToCheck = filesToCheck.Distinct().ToList();

            foreach (var fileName in filesToCheck.Concat(startups.Select(x => Path.GetFileName(gameFiles[x]))))
            {
                //if (gameFiles.TryGetValue(fileName, out var fullgamepath) && File.Exists(fullgamepath))
                //{
                var export = containsImportedExport(fileName);
                if (export != null)
                {
                    return export;
                }
                //}

                //Try local.
                //                var localPath = Path.Combine(containingDirectory, fileName);
                //              if (!localPath.Equals(fullgamepath, StringComparison.InvariantCultureIgnoreCase) && File.Exists(localPath))
                //            {
                //var export = containsImportedExport(fileName);
                //if (export != null)
                //{
                //    return export;
                //}
                //          }
            }
            return null;

            //Perform check and lookup
            ExportEntry containsImportedExport(string packagePath)
            {
                //Debug.WriteLine($"Checking file {packagePath} for {entryFullPath}");
                var package = localCache.GetCachedPackage(packagePath, false);
                package ??= globalCache.GetCachedPackage(packagePath, false);
                var packName = Path.GetFileNameWithoutExtension(packagePath);
                var packageParts = entryFullPath.Split('.').ToList();
                if (packageParts.Count > 1 && packName == packageParts[0])
                {
                    packageParts.RemoveAt(0);
                    entryFullPath = string.Join(".", packageParts);
                }
                else if (packName == packageParts[0])
                {
                    //it's literally the file itself (an imported package like SFXGame)
                    return package.Exports.FirstOrDefault(x => x.idxLink == 0); //this will be at top of the tree
                }

                return package?.FindExport(entryFullPath);
            }
        }
    }
}
