using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Enemy;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using Newtonsoft.Json;

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

            dataworker.DoWork += FindPortableGuns;
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

        private void FindPortableGuns(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x =>
                //x.Contains("blackstorm", StringComparison.InvariantCultureIgnoreCase)
                ////x.Contains("ReaperCombat")
                // )
                .ToList();
            mainWindow.CurrentOperationText = "Scanning for stuff";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            var startupFileCache = GetGlobalCache();

            // Maps instanced full path to list of instances
            ConcurrentDictionary<string, List<EnemyWeaponChanger.GunInfo>> mapping = new ConcurrentDictionary<string, List<EnemyWeaponChanger.GunInfo>>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
            {
                mainWindow.CurrentOperationText = $"Scanning for stuff [{numdone}/{numtodo}]";
                Interlocked.Increment(ref numdone);

                var package = MEPackageHandler.OpenMEPackage(file);
                var sfxweapons = package.Exports.Where(x => x.InheritsFrom("SFXWeapon") && x.IsClass && !x.IsDefaultObject);
                foreach (var skm in sfxweapons)
                {
                    BuildGunInfo(skm, mapping, package, startupFileCache, false);
                }
                mainWindow.CurrentProgressValue = numdone;
            });

            // Corrected, embedded guns that required file coalescing for portability
            var correctedGuns = Utilities.ListStaticAssets("binary.correctedloadouts.weapons");
            foreach (var cg in correctedGuns)
            {
                var pData = Utilities.GetEmbeddedStaticFile(cg, true);
                var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(pData), Utilities.GetFilenameFromAssetName(cg)); //just any path
                var sfxweapons = package.Exports.Where(x => x.InheritsFrom("SFXWeapon") && x.IsClass && !x.IsDefaultObject);
                foreach (var skm in sfxweapons)
                {
                    BuildGunInfo(skm, mapping, package, startupFileCache, true);
                }
            }
            // PERFORM REDUCE OPERATION
            
            // Order by not needing startup, then by filesize so we can have smallest files loaded
            foreach (var gunL in mapping)
            {
                gunL.Value.ReplaceAll(gunL.Value.OrderBy(x => x.RequiresStartupPackage).ThenBy(x => x.PackageFileSize).ToList());
            }

            // Build power info list
            var reducedGunInfos = mapping.Select(x => x.Value[0]);

            var jsonList = JsonConvert.SerializeObject(reducedGunInfos, Formatting.Indented);
            File.WriteAllText(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\weaponlistme2.json", jsonList);

            UnusableGuns.RemoveAll(x => reducedGunInfos.Any(y => y.GunName == x.Key));
            File.WriteAllLines(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\unusableguns.json", UnusableGuns.Keys.ToList());

        }

        private static ConcurrentDictionary<string, string> UnusableGuns = new ConcurrentDictionary<string, string>();

        private void BuildGunInfo(ExportEntry skm, ConcurrentDictionary<string, List<EnemyWeaponChanger.GunInfo>> mapping, IMEPackage package, PackageCache startupFileCache, bool isCorrectedPackage)
        {
            // See if power is fully defined in package?
            var classInfo = ObjectBinary.From<UClass>(skm);
            if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                return; // This class cannot be used as a power, it is abstract

            var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
            var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            var usable = CheckImports(importDependencies, package, startupFileCache, out var missingImport);
            if (usable)
            {
                var pi = new EnemyWeaponChanger.GunInfo(skm, isCorrectedPackage);
                if ((pi.RequiresStartupPackage && !pi.PackageFileName.StartsWith("SFX")) 
                    || pi.GunName.Contains("Player")
                    || pi.GunName.Contains("AsteroidRocketLauncher")
                    || pi.GunName.Contains("VehicleRocketLauncher")
                )
                {
                    // We do not allow startup files that have levels
                    pi.IsUsable = false;
                }
                if (pi.IsUsable)
                {
                    Debug.WriteLine($"Usable sfxweapon: {skm.InstancedFullPath} in {Path.GetFileName(package.FilePath)}");
                    if (!mapping.TryGetValue(skm.InstancedFullPath, out var instanceList))
                    {
                        instanceList = new List<EnemyWeaponChanger.GunInfo>();
                        mapping[skm.InstancedFullPath] = instanceList;
                    }

                    instanceList.Add(pi);
                }
            }
            else
            {
                Debug.WriteLine($"Not usable weapon: {skm.InstancedFullPath} in {Path.GetFileName(package.FilePath)}, missing import {missingImport.FullPath}");
                if (mapping.ContainsKey(skm.InstancedFullPath))
                {
                    UnusableGuns.Remove(skm.InstancedFullPath, out _);
                }
                else
                {
                    UnusableGuns[skm.InstancedFullPath] = missingImport.FullPath;
                }
            }
        }

        private void BuildVisibleLoadoutsMap(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x => x.Contains("SFXWeapon") || x.Contains("BioP"))
                .ToList();
            mainWindow.CurrentOperationText = "Scanning for stuff";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            var startupFileCache = GetGlobalCache();

            // Maps instanced full path to list of instances
            // Loadout full path -> caller supports visible weapons
            ConcurrentDictionary<string, bool> loadoutmap = new ConcurrentDictionary<string, bool>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
            {
                mainWindow.CurrentOperationText = $"Scanning for stuff [{numdone}/{numtodo}]";
                Interlocked.Increment(ref numdone);

                var package = MEPackageHandler.OpenMEPackage(file);
                var pawnClasses = package.Exports.Where(x => x.InheritsFrom("SFXPawn") && x.IsClass && !x.IsDefaultObject);
                foreach (var skm in pawnClasses.Where(x => !loadoutmap.ContainsKey(x.InstancedFullPath)))
                {
                    // See if power is fully defined in package?
                    var classInfo = ObjectBinary.From<UClass>(skm);
                    if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                        continue; // This class cannot be used as a power, it is abstract

                    var defaults = package.GetUExport(classInfo.Defaults);

                    var supportsVisibleWeapons = defaults.GetProperty<BoolProperty>("bSupportsVisibleWeapons")?.Value ?? true;

                    // Get loadout
                    var actorTypeObj = defaults.GetProperty<ObjectProperty>("ActorType");
                    if (actorTypeObj != null && actorTypeObj.Value > 0)
                    {
                        var actorType = package.GetUExport(actorTypeObj.Value);
                        var loadoutObj = actorType.GetProperty<ObjectProperty>("Loadout");
                        if (loadoutObj != null && loadoutObj.Value > 0)
                        {
                            var loadout = package.GetUExport(loadoutObj.Value);
                            if (loadout.GetProperty<ArrayProperty<ObjectProperty>>("Weapons") != null)
                            {
                                loadoutmap[loadout.InstancedFullPath] = supportsVisibleWeapons;
                            }
                        }
                    }

                    // mark
                }
                mainWindow.CurrentProgressValue = numdone;
            });

            // PERFORM REDUCE OPERATION

            // Count the number of times a file is referenced for a power
            //Dictionary<string, int> fileUsages = new Dictionary<string, int>();
            //foreach (var powerPair in mapping)
            //{
            //    foreach (var powerInfo in powerPair.Value)
            //    {
            //        if (!fileUsages.TryGetValue(powerInfo.PackageFileName, out var fileUsageInt))
            //        {
            //            fileUsages[powerInfo.PackageFileName] = 1;
            //        }
            //        else
            //        {
            //            fileUsages[powerInfo.PackageFileName] = fileUsageInt + 1;
            //        }
            //    }
            //}

            // Sort file usages by count, highest to lowest
            //var gunListSS = fileUsages.Select(x => (x.Key, x.Value)).ToList();
            //gunListSS = gunListSS.OrderByDescending(x => x.Value).ToList();
            //var gunList = gunListSS.Select(x => x.Key).ToList(); // Drop the tuple part, we don't care about it

            //// Build power info list
            //var reducedGunInfos = new List<EnemyWeaponChanger.GunInfo>();

            //foreach (var gunFile in gunList)
            //{
            //    // Get powers that are in this file
            //    var items = mapping.Where(x => x.Value.Any(x => x.PackageFileName == gunFile)).ToList();
            //    foreach (var item in items)
            //    {
            //        reducedGunInfos.Add(item.Value.FirstOrDefault(x => x.PackageFileName == gunFile));
            //        mapping.Remove(item.Key, out var removedItem);
            //    }
            //}

            var jsonList = JsonConvert.SerializeObject(loadoutmap, Formatting.Indented);
            File.WriteAllText(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\weaponloadoutrules.json", jsonList);


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

        private void FindPortablePowers(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x => x.Contains("SFXPower") || x.Contains("BioP"))
                .ToList();
            mainWindow.CurrentOperationText = "Scanning for stuff";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            var startupFileCache = GetGlobalCache();

            // Maps instanced full path to list of instances
            ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>> mapping = new ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
              {
                  mainWindow.CurrentOperationText = $"Scanning for stuff [{numdone}/{numtodo}]";
                  Interlocked.Increment(ref numdone);

                  var package = MEPackageHandler.OpenMEPackage(file);
                  if (package.FindExport("SeekFreeShaderCache") != null)
                  {
                      mainWindow.CurrentProgressValue = numdone;
                      return; // don't check these
                  }
                  var powers = package.Exports.Where(x => x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject);
                  foreach (var skm in powers.Where(x => !mapping.ContainsKey(x.InstancedFullPath)))
                  {
                      // See if power is fully defined in package?
                      var classInfo = ObjectBinary.From<UClass>(skm);
                      if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                          continue; // This class cannot be used as a power, it is abstract

                      var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
                      var importDependencies = dependencies.OfType<ImportEntry>().ToList();
                      var usable = CheckImports(importDependencies, package, startupFileCache, out var missingImport);
                      if (usable)
                      {
                          var pi = new EnemyPowerChanger.PowerInfo(skm);

                          if (pi.IsUsable)
                          {
                              Debug.WriteLine($"Usable power: {skm.InstancedFullPath} in {package.FilePath}");
                              if (!mapping.TryGetValue(skm.InstancedFullPath, out var instanceList))
                              {
                                  instanceList = new List<EnemyPowerChanger.PowerInfo>();
                                  mapping[skm.InstancedFullPath] = instanceList;
                              }

                              instanceList.Add(pi);
                          }
                      }
                      else
                      {
                          //Debug.WriteLine($"Not usable power: {skm.InstancedFullPath} in {package.FilePath}");
                      }
                  }
                  mainWindow.CurrentProgressValue = numdone;
              });

            // PERFORM REDUCE OPERATION

            // Count the number of times a file is referenced for a power
            Dictionary<string, int> fileUsages = new Dictionary<string, int>();
            foreach (var powerPair in mapping)
            {
                foreach (var powerInfo in powerPair.Value)
                {
                    if (!fileUsages.TryGetValue(powerInfo.PackageFileName, out var fileUsageInt))
                    {
                        fileUsages[powerInfo.PackageFileName] = 1;
                    }
                    else
                    {
                        fileUsages[powerInfo.PackageFileName] = fileUsageInt + 1;
                    }
                }
            }

            // Sort file usages by count, highest to lowest
            var powerListSS = fileUsages.Select(x => (x.Key, x.Value)).ToList();
            powerListSS = powerListSS.OrderByDescending(x => x.Value).ToList();
            var powerList = powerListSS.Select(x => x.Key).ToList(); // Drop the tuple part, we don't care about it

            // Build power info list
            List<EnemyPowerChanger.PowerInfo> reducedPowerInfos = new List<EnemyPowerChanger.PowerInfo>();

            foreach (var powerFile in powerList)
            {
                // Get powers that are in this file
                var items = mapping.Where(x => x.Value.Any(x => x.PackageFileName == powerFile)).ToList();
                foreach (var item in items)
                {
                    reducedPowerInfos.Add(item.Value.FirstOrDefault(x => x.PackageFileName == powerFile));
                    mapping.Remove(item.Key, out var removedItem);
                }
            }

            var jsonList = JsonConvert.SerializeObject(reducedPowerInfos);
            File.WriteAllText(@"C:\users\public\powerlistme2.json", jsonList);


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
                if (import.InstancedFullPath == "BioVFX_Z_TEXTURES.Generic.Glass_Shards_Norm")
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
