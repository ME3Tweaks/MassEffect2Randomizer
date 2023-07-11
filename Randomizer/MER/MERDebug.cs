using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Microsoft.WindowsAPICodePack.PortableDevices.CommandSystem.Object;
using Newtonsoft.Json;
using Randomizer.Randomizers;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;
using WinCopies.Diagnostics;
using WinCopies.Util;

namespace Randomizer.MER
{
    public class MERDebug
    {
        [Conditional("DEBUG")]
        public static void InstallDebugScript(GameTarget target, string packagename, string scriptName)
        {
            Debug.WriteLine($"Installing debug script {scriptName}");
            ScriptTools.InstallScriptToPackage(target, packagename, scriptName, "Debug." + scriptName + ".uc", false,
                true);
        }

        [Conditional("DEBUG")]
        public static void InstallDebugScript(IMEPackage package, string scriptName, bool saveOnFinish)
        {
            Debug.WriteLine($"Installing debug script {scriptName}");
            ScriptTools.InstallScriptToPackage(package, scriptName, "Debug." + scriptName + ".uc", false, saveOnFinish);
        }

        public static void BuildPowersBank(object sender, DoWorkEventArgs e)
        {
#if DEBUG && __GAME2__

            var option = e.Argument as RandomizationOption;

            option.ProgressValue = 0;
            option.CurrentOperation = "Building powers package";
            option.ProgressIndeterminate = false;

            //var allIFPs =
            //    File.ReadAllLines(
            //        @"C:\Users\mgame\source\repos\ME3Tweaks\MassEffectRandomizerShared\Randomizer\Randomizers\Game2\Dev\PowerIFPs.txt");

            // SETUP STAGE 1
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.LE2, true, false);

            using var powerBank = MEPackageHandler.CreateAndOpenPackage(@"C:\users\public\AllPowers.pcc",
                MEGame.LE2);

            List<string> alreadyDonePowers = new List<string>();

            int done = 0;
            option.ProgressMax = files.Count;
            foreach (var f in files)
            {
                Interlocked.Increment(ref done);
                option.ProgressValue = done;
                using var package = MEPackageHandler.OpenMEPackage(f.Value);
                var powers = package.Exports.Where(x =>
                    x.IsClass && !x.IsDefaultObject && x.InheritsFrom(@"SFXPower"));
                foreach (var skm in powers.Where(x => !alreadyDonePowers.Contains(x.InstancedFullPath)))
                {
                    // Add to power bank file
                    BuildPowerInfo(skm, powerBank, false);
                    option.CurrentOperation = $"Building powers package ({alreadyDonePowers.Count} powers done)";
                    alreadyDonePowers.Add(skm.InstancedFullPath);
                }
            }

            foreach (var v in alreadyDonePowers)
            {
                Debug.WriteLine(v);
            }

            powerBank.Save();
            Debug.WriteLine("DONE");
            return;
            //mainWindow.CurrentOperationText = "Finding portable powers (stage 1)";
            //int numdone = 0;
            //int numtodo = files.Count;

            //mainWindow.ProgressBarIndeterminate = false;
            //mainWindow.ProgressBar_Bottom_Max = files.Count();
            //mainWindow.CurrentProgressValue = 0;

            //// PREP WORK
            //var startupFileCache = MERFileSystem.GetGlobalCache();

            //// Maps instanced full path to list of instances
            //ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>> mapping =
            //    new ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>>();

            //// STAGE 1====================

            //// Corrected, embedded powers that required file coalescing for portability or other corrections in order to work on enemies
            //var correctedPowers = MERUtilities.ListStaticAssets("binary.correctedloadouts.powers");
            //foreach (var cg in correctedPowers)
            //{
            //    var pData = MERUtilities.GetEmbeddedStaticFile(cg, true);
            //    var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(pData),
            //        MERUtilities.GetFilenameFromAssetName(cg)); //just any path
            //    var sfxPowers =
            //        package.Exports.Where(x => x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject);
            //    foreach (var sfxPow in sfxPowers)
            //    {
            //        BuildPowerInfo(sfxPow, mapping, package, startupFileCache, null, true);
            //    }
            //}

            //Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
            //{
            //    mainWindow.CurrentOperationText = $"Finding portable powers (stage 1) [{numdone}/{numtodo}]";
            //    Interlocked.Increment(ref numdone);
            //    MERPackageCache localCache = new MERPackageCache();
            //    var package = MEPackageHandler.OpenMEPackage(file);
            //    var powers = package.Exports.Where(x => x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject);
            //    foreach (var skm in powers.Where(x => !mapping.ContainsKey(x.InstancedFullPath)))
            //    {
            //        // See if power is fully defined in package?
            //        var classInfo = ObjectBinary.From<UClass>(skm);
            //        if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
            //            continue; // This class cannot be used as a power, it is abstract

            //        var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
            //        var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            //        var usable = CheckImports(importDependencies, package, startupFileCache, localCache,
            //            out var missingImport);
            //        if (usable)
            //        {
            //            var pi = new EnemyPowerChanger.PowerInfo(skm, false);

            //            if (pi.IsUsable)
            //            {
            //                Debug.WriteLine($"Usable power: {skm.InstancedFullPath} in {package.FilePath}");
            //                if (!mapping.TryGetValue(skm.InstancedFullPath, out var instanceList))
            //                {
            //                    instanceList = new List<EnemyPowerChanger.PowerInfo>();
            //                    mapping[skm.InstancedFullPath] = instanceList;
            //                }

            //                instanceList.Add(pi);
            //            }
            //        }
            //        else
            //        {
            //            //Debug.WriteLine($"Not usable power: {skm.InstancedFullPath} in {package.FilePath}");
            //        }
            //    }

            //    mainWindow.CurrentProgressValue = numdone;
            //});

            //// PHASE 2

            //files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
            //    .Where(x => x.Contains("BioD")
            //                || x.Contains("BioP"))
            //    .ToList();
            //mainWindow.CurrentOperationText = "Finding portable powers (Stage 2)";
            //numdone = 0;
            //numtodo = files.Count;

            //mainWindow.ProgressBarIndeterminate = false;
            //mainWindow.ProgressBar_Bottom_Max = files.Count();
            //mainWindow.CurrentProgressValue = 0;

            //Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
            //{
            //    mainWindow.CurrentOperationText = $"Finding portable powers (Stage 2) [{numdone}/{numtodo}]";
            //    Interlocked.Increment(ref numdone);
            //    if (!file.Contains("BioD"))
            //        return; // BioD only
            //    MERPackageCache localCache = new MERPackageCache();
            //    var package = MEPackageHandler.OpenMEPackage(file);
            //    var powers = package.Exports.Where(x =>
            //        x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject &&
            //        !mapping.ContainsKey(x.InstancedFullPath));
            //    foreach (var skm in powers.Where(x => !mapping.ContainsKey(x.InstancedFullPath)))
            //    {
            //        // See if power is fully defined in package?
            //        var classInfo = ObjectBinary.From<UClass>(skm);
            //        if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
            //            continue; // This class cannot be used as a power, it is abstract

            //        var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
            //        var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            //        var usable = CheckImports(importDependencies, package, startupFileCache, localCache,
            //            out var missingImport);
            //        if (usable)
            //        {
            //            var pi = new EnemyPowerChanger.PowerInfo(skm, false);

            //            if (pi.IsUsable)
            //            {
            //                Debug.WriteLine($"Usable power: {skm.InstancedFullPath} in {package.FilePath}");
            //                if (!mapping.TryGetValue(skm.InstancedFullPath, out var instanceList))
            //                {
            //                    instanceList = new List<EnemyPowerChanger.PowerInfo>();
            //                    mapping[skm.InstancedFullPath] = instanceList;
            //                }

            //                instanceList.Add(pi);
            //            }
            //        }
            //        else
            //        {
            //            //Debug.WriteLine($"Not usable power: {skm.InstancedFullPath} in {package.FilePath}");
            //        }
            //    }

            //    mainWindow.CurrentProgressValue = numdone;
            //});


            //// PERFORM REDUCE OPERATION

            //// Count the number of times a file is referenced for a power
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

            //// Sort file usages by count, highest to lowest
            //var powerListSS = fileUsages.Select(x => (x.Key, x.Value)).ToList();
            //powerListSS = powerListSS.OrderByDescending(x => x.Value).ToList();
            //var powerList = powerListSS.Select(x => x.Key).ToList(); // Drop the tuple part, we don't care about it

            // Build power info list
            //List<EnemyPowerChanger.PowerInfo> powerInfos = new List<EnemyPowerChanger.PowerInfo>();

            //foreach (var powerFile in powerList)
            //{
            //    // Get powers that are in this file
            //    var items = mapping.Where(x => x.Value.Any(x => x.PackageFileName == powerFile)).ToList();
            //    foreach (var item in items)
            //    {
            //        powerInfos.Add(item.Value.FirstOrDefault(x => x.PackageFileName == powerFile));
            //        mapping.Remove(item.Key, out var removedItem);
            //    }
            //}
            //var jsonList = JsonConvert.SerializeObject(powerInfos, Formatting.Indented);
            //File.WriteAllText(@"B:\UserProfile\source\repos\ME2Randomizer\Randomizer\Randomizers\Game2\Assets\Text\powerlistle2.json",
            //    jsonList);


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
#endif
        }

        [Conditional("DEBUG")]
        public static void BuildPowerInfo(ExportEntry powerExport, IMEPackage powerBank, bool isCorrectedPackage)
        {
#if __GAME2__
            // See if power is fully defined in package?
            var classInfo = ObjectBinary.From<UClass>(powerExport);
            if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                return; // This class cannot be used as a power, it is abstract

            // var dependencies = EntryImporter.GetAllReferencesOfExport(powerExport);
            //var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            //var usable = CheckImports(importDependencies, package, startupFileCache, localCache, out var missingImport);
            //if (usable)
            {
                //if (powerExport.ObjectName.Name == "SFXPower_Flashbang_NPC")
                //    Debugger.Break();
                var pi = new Randomizer.Randomizers.Game2.Enemy.EnemyPowerChanger.PowerInfo(powerExport, isCorrectedPackage);
                //if ((pi.RequiresStartupPackage && !pi.PackageFileName.StartsWith("SFX")))
                //{
                //    // We do not allow startup files that have levels
                //    pi.IsUsable = false;
                //}

                //if (pi.IsUsable)
                //{
                Debug.WriteLine(
                    $"Usable sfxpower being ported {powerExport.InstancedFullPath} in {Path.GetFileName(powerExport.FileRef.FilePath)}");
                //if (!mapping.TryGetValue(powerExport.InstancedFullPath, out var instanceList))
                //{
                //    instanceList = new List<EnemyPowerChanger.PowerInfo>();
                //    mapping[powerExport.InstancedFullPath] = instanceList;
                //}

                //instanceList.Add(pi);
                EntryExporter.ExportExportToPackage(powerExport, powerBank, out _);
                //}
                //else
                //{
                //   Debug.WriteLine($"Denied power {pi.PowerName}");
                //}
            }
            //else
            //{
            //    Debug.WriteLine($"Not usable power: {powerExport.InstancedFullPath} in {Path.GetFileName(package.FilePath)}, missing import {missingImport.FullPath}");
            //    if (mapping.ContainsKey(powerExport.InstancedFullPath))
            //    {
            //        UnusablePowers.Remove(powerExport.InstancedFullPath, out _);
            //    }
            //    else
            //    {
            //        UnusablePowers[powerExport.InstancedFullPath] = missingImport.FullPath;
            //    }
            //}
#endif
        }

        public static void DebugPrintActorNames(object sender, DoWorkEventArgs doWorkEventArgs)
        {
#if DEBUG
            var option = doWorkEventArgs.Argument as RandomizationOption;

            option.ProgressValue = 0;
            option.CurrentOperation = "Finding actor names";
            option.ProgressIndeterminate = false;
            SortedSet<string> actorTypeNames = new SortedSet<string>();

#if __GAME1__
            var game = MEGame.LE1;
            var files = MELoadedFiles.GetFilesLoadedInGame(game, true, false).Values
                //.Where(x =>
                //                    !x.Contains("_LOC_")
                //&& x.Contains(@"CitHub", StringComparison.InvariantCultureIgnoreCase)
                //)
                //.OrderBy(x => x.Contains("_LOC_"))
                .ToList();
            option.ProgressMax = files.Count;

            TLKBuilder.StartHandler(new GameTarget(game, MEDirectories.GetDefaultGamePath(game), false));
            foreach (var f in files)
            {
                option.IncrementProgressValue();
                var p = MEPackageHandler.UnsafePartialLoad(f,
                    x => !x.IsDefaultObject && x.IsA("BioPawn"));
                foreach (var exp in p.Exports.Where(x => x.IsDataLoaded()))
                {

                    if (exp.IsA("BioPawn"))
                    {
                        var tag = exp.GetProperty<NameProperty>("Tag");
                        if (tag != null)
                        {
                            actorTypeNames.Add(tag.Value.Instanced);
                        }
                    }
                }
            }
#endif
#if __GAME3__
            var game = MEGame.LE3;
            var files = MELoadedFiles.GetFilesLoadedInGame(game, true, false).Values
                //.Where(x =>
                //                    !x.Contains("_LOC_")
                //&& x.Contains(@"CitHub", StringComparison.InvariantCultureIgnoreCase)
                //)
                //.OrderBy(x => x.Contains("_LOC_"))
                .ToList();

            // PackageName -> GesturePackage
            int i = 0;
            TLKBuilder.StartHandler(new GameTarget(game, MEDirectories.GetDefaultGamePath(game), false));
            foreach (var f in files)
            {
                i++;
                var p = MEPackageHandler.UnsafePartialLoad(f,
                    x => !x.IsDefaultObject &&
                         (x.ClassName == "SFXSimpleUseModule" || x.ClassName == "SFXModule_AimAssistTarget"));
                foreach (var exp in p.Exports.Where(x =>
                             !x.IsDefaultObject && (x.ClassName == "SFXSimpleUseModule" ||
                                                    x.ClassName == "SFXModule_AimAssistTarget")))
                {
                    if (exp.Parent.ClassName == "SFXPointOfInterest")
                        continue; // 
                    var displayNameVal = exp.GetProperty<StringRefProperty>("m_SrGameName");
                    if (displayNameVal != null)
                    {
                        var displayName = TLKBuilder.TLKLookupByLang(displayNameVal.Value, MELocalization.INT);
                        actorTypeNames.Add($"{displayNameVal.Value}: {displayName}");
                    }
                    else
                    {
                        // actorTypeNames.Add(exp.ObjectName.Instanced);
                    }
                }

            }
#endif
            //foreach (var atn in actorTypeNames)
            //{
            //    Debug.WriteLine(atn);
            //}
#endif
        }

        public static void FindRTPCNames(object sender, DoWorkEventArgs doWorkEventArgs)
        {
#if DEBUG
            var option = doWorkEventArgs.Argument as RandomizationOption;
            var game = MEGame.LE3;
            var target = new GameTarget(game, MEDirectories.GetDefaultGamePath(game), true);
            var loadedFiles = MELoadedFiles.GetFilesLoadedInGame(game);
            SortedSet<string> rtpcStrings = new SortedSet<string>();

            option.ProgressMax = loadedFiles.Count;
            option.ProgressValue = 0;
            option.CurrentOperation = "Finding RTPCs";
            option.ProgressIndeterminate = false;
            foreach (var v in loadedFiles)
            {
                using var package = MEPackageHandler.UnsafePartialLoad(v.Value, x => x.ClassName == "SFXModule_Audio");
                foreach (var am in package.Exports.Where(x => x.ClassName == "SFXModule_Audio"))
                {
                    var rtpcs = am.GetProperty<ArrayProperty<StructProperty>>("RTPCs");
                    if (rtpcs != null)
                    {
                        foreach (var rtpc in rtpcs)
                        {
                            rtpcStrings.Add(
                                $"{rtpc.GetProp<StrProperty>("RTPCName").Value}: {rtpc.GetProp<FloatProperty>("RTPCValue").Value}");
                        }
                    }
                }

                option.IncrementProgressValue();
            }

            foreach (var v in rtpcStrings)
            {
                Debug.WriteLine(v);
            }
#endif
        }

        /// <summary>
        /// Attempts to build full packages (still ForcedExport unfortunately)
        /// </summary>
        /// <param name="target"></param>
        public static void DecookGame(object sender, DoWorkEventArgs doWorkEventArgs)
        {
#if DEBUG
            var option = doWorkEventArgs.Argument as RandomizationOption;
            var game = MEGame.LE3;
            var loadedFiles = MELoadedFiles.GetFilesLoadedInGame(game);
            var outputDirectory = @"B:\DecookedGames\LE3";

            // Step 1: Find all top level package exports
            SortedSet<string> topLevelPackages = new SortedSet<string>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, IMEPackage> fileToTablesOnlyPackage = new Dictionary<string, IMEPackage>();
            option.CurrentOperation = "Loading all package summaries";
            option.ProgressMax = loadedFiles.Count;
            option.ProgressValue = 0;
            foreach (var f in loadedFiles)
            {
                if (f.Key.StartsWith("BIOG_"))
                    continue; // These are already cooked seek free

                var file = MERFileSystem.OpenMEPackageTablesOnly(f.Value);
                foreach (var exp in file.Exports.Where(x =>
                             !x.IsDefaultObject && x.ClassName == "Package" && x.idxLink == 0))
                {
                    // Don't add blanks
                    if (file.Exports.Any(x => x.idxLink == exp.UIndex))
                    {
                        topLevelPackages.Add(exp.ObjectName);
                    }
                }

                fileToTablesOnlyPackage[f.Key] = file;
                option.ProgressValue++;
            }

            //File.WriteAllLines(@"B:\DecookedGames\LE3TopLevelPackages.txt", topLevelPackages.ToList());
            //foreach (var v in topLevelPackages)
            //{
            //    Debug.WriteLine(v);
            //}

            Debug.WriteLine("Beginning decook");

            // var decookPackagesList = File.ReadAllLines(@"B:\DecookedGames\LE3TopLevelPackages.txt");

            using var
                globalCache =
                    MERCaches.GlobalCommonLookupCache; // This way of accessing will not work in release builds.

            option.ProgressValue = 0;
            option.ProgressMax = topLevelPackages.Count;
            option.CurrentOperation = "Decooking packages";
            Parallel.ForEach(topLevelPackages, new ParallelOptions() { MaxDegreeOfParallelism = 6 }, tlp =>
            {
                Debug.WriteLine($"Decooking {tlp}");
                if (tlp.Contains("..") || tlp.Contains('\\'))
                {
                    Debug.WriteLine($"Skipping path-named package {tlp}");
                    option.IncrementProgressValue();
                    return;
                }

                var topLevelNameBase = $"{tlp}."; // Add . to denote separator
                var decookedPackagePath = Path.Combine(outputDirectory, tlp + ".pcc");
                MEPackageHandler.CreateAndSavePackage(decookedPackagePath, game);
                var decookedPackage = MEPackageHandler.OpenMEPackage(decookedPackagePath);
                foreach (var tableOnlyPackage in fileToTablesOnlyPackage)
                {
                    if (tableOnlyPackage.Value.Exports.Any(x =>
                            x.InstancedFullPath.StartsWith(topLevelNameBase,
                                StringComparison.InvariantCultureIgnoreCase) &&
                            decookedPackage.FindEntry(x.InstancedFullPath) == null))
                    {
                        var package = MEPackageHandler.OpenMEPackage(tableOnlyPackage.Value.FilePath);
                        using PackageCache localCache = new PackageCache();
                        foreach (var itemToPort in package.Exports.Where(x =>
                                     x.InstancedFullPath.StartsWith(topLevelNameBase,
                                         StringComparison.InvariantCultureIgnoreCase)).ToList())
                        {
                            EntryExporter.ExportExportToPackage(itemToPort, decookedPackage, out var _, globalCache,
                                localCache);
                        }
                    }
                }

                decookedPackage.Save();
                option.IncrementProgressValue();
            });
            fileToTablesOnlyPackage = null;
#endif
        }

        public static void BuildGestureFiles(object? sender, DoWorkEventArgs e)
        {
#if DEBUG && __GAME2__
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.LE2, true, false).Values
                //.Where(x =>
                //                    !x.Contains("_LOC_")
                //&& x.Contains(@"CitHub", StringComparison.InvariantCultureIgnoreCase)
                //)
                .OrderBy(x => x.Contains("_LOC_"))
                .ToList();
            RandomizationOption option = (RandomizationOption)e.Argument;
            var game = MEGame.LE2;
            var target = new GameTarget(game, MEDirectories.GetDefaultGamePath(game), true);
            GestureManager.Init(target, false);


            // PackageName -> GesturePackage
            var gestureSaveF =
                @"C:\Users\mgame\source\repos\ME2Randomizer\Randomizer\Randomizers\Game3\Assets\Binary\Packages\LE3\Gestures";

            option.CurrentOperation = "Finding gesture animations";
            option.ProgressMax = files.Count;
            option.ProgressValue = 0;
            option.ProgressIndeterminate = false;

            using MERPackageCache cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true)
            {
                CacheMaxSize = 10
            };

            using PackageCache gestureCache = new PackageCache();

            foreach (var f in files)
            {
                option.ProgressValue++;
                var p = MEPackageHandler.OpenMEPackage(f);
                var gesturesPackageExports = p.Exports.Where(
                        x => x.idxLink == 0 && x.ClassName == "Package"
                                            && GestureManager.IsGestureGroupPackage(x.InstancedFullPath))
                    .Select(x => x.UIndex).ToList();

                // Get list of exports under these packages
                foreach (var exp in p.Exports.Where(x =>
                             gesturesPackageExports.Contains(x.idxLink) &&
                             (x.ClassName == "AnimSet" || x.ClassName == "AnimSequence")))
                {
                    var destFile = Path.Combine(gestureSaveF, exp.Parent.ObjectName.Name + ".pcc");
                    IMEPackage gestPackage = gestureCache.GetCachedPackage(destFile, false);

                    if (gestPackage == null)
                    {
                        MEPackageHandler.CreateAndSavePackage(destFile, game);
                        gestPackage = MEPackageHandler.OpenMEPackage(destFile);
                        gestureCache.InsertIntoCache(gestPackage);
                    }

                    EntryExporter.ExportExportToPackage(exp, gestPackage, out var _, MERCaches.GlobalCommonLookupCache,
                        cache);
                }
            }

            foreach (var v in gestureCache.Cache)
            {
                v.Value.Save();
            }

            MERCaches.Cleanup();
#endif
        }

        public static void BuildHenchPowers(object sender, DoWorkEventArgs e)
        {
            bool evolutions = false;
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.LE2, true, false).Values
                .Where(x =>
                    x.Contains(@"BioD_ArvLvl1.pcc") ||
                    (
                    x.Contains("BioH_")
                    && x.Contains("_00")
                    && !x.Contains(@"_END_")
                )
                    && !x.Contains(@"_LOC_"))
                //&& x.Contains(@"CitHub", StringComparison.InvariantCultureIgnoreCase)
                //)
                .ToList();
            var squadmatePowerIFPs = new List<string>();

            foreach (var file in files)
            {
                Debug.WriteLine(file);
            }

            foreach (var file in files)
            {
                using var package = MEPackageHandler.OpenMEPackage(file);
                var loadouts = package.Exports.Where(x => x.ClassName == @"SFXPlayerSquadLoadoutData");
                foreach (var loadout in loadouts)
                {
                    var powers = loadout.GetProperty<ArrayProperty<ObjectProperty>>(@"Powers");
                    foreach (var powerObj in powers)
                    {
                        var power = powerObj.ResolveToEntry(package) as ExportEntry;

                        if (evolutions)
                        {
                            power = power.GetDefaults();
                            var ePower1 =
                                power.GetProperty<ObjectProperty>(@"EvolvedPowerClass1")
                                    ?.ResolveToEntry(package) as ExportEntry;
                            var ePower2 =
                                power.GetProperty<ObjectProperty>(@"EvolvedPowerClass2")
                                    ?.ResolveToEntry(package) as ExportEntry;

                            if (ePower1 != null)
                            {
                                squadmatePowerIFPs.Add(ePower1.InstancedFullPath);
                            }

                            if (ePower2 != null)
                            {
                                squadmatePowerIFPs.Add(ePower2.InstancedFullPath);
                            }
                        }
                        else
                        {
                            if (power.ObjectName.Instanced.Contains(@"Loyalty"))
                                continue; // We don't do these
                            squadmatePowerIFPs.Add(power.InstancedFullPath);
                        }
                    }
                }
            }

            foreach (var sPower in squadmatePowerIFPs)
            {
                Debug.WriteLine(sPower);
            }
        }

        public static void BuildTFCs(object sender, DoWorkEventArgs e)
        {
#if __GAME2
            LE2Textures.BuildPremadeTFC();
#endif
        }
    }
}