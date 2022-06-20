using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using Randomizer.Randomizers;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.MER
{
    /// <summary>
    /// Filesystem class for Mass Effect Randomizer programs. It is conditionally compiled for Game 1/2/3 and can be accessed as the authority on which game the build is for.
    /// </summary>
    public class MERFileSystem
    {
#if __GAME1__
        /// <summary>
        /// List of games this build supports
        /// </summary>
        public static MEGame[] Games = new[] { MEGame.ME1, MEGame.LE1 };
        public static readonly string[] filesToSkip = { "Core", "PlotManagerMap", "RefShaderCache-PC-D3D-SM3", "RefShaderCache-PC-D3D-SM5", "IpDrv", "WwiseAudio", "SFXOnlineFoundation", "GFxUI" };
        public static readonly string[] alwaysBasegameFiles = { "Startup", "Engine", "GameFramework", "SFXGame", "EntryMenu", "BIOG_Male_Player_C", "BIOC_Materials", "SFXStrategicAI" };
#elif __GAME2__
        /// <summary>
        /// List of games this build supports
        /// </summary>
        public static MEGame[] Games = new[] { MEGame.ME2, MEGame.LE2 };
        public static readonly string[] filesToSkip = { "RefShaderCache-PC-D3D-SM3.upk", "RefShaderCache-PC-D3D-SM5.upk", "IpDrv.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "GFxUI.pcc" };
        public static readonly string[] alwaysBasegameFiles = { "Startup_INT.pcc", "Engine.pcc", "GameFramework.pcc", "SFXGame.pcc", "EntryMenu.pcc", "BIOG_Male_Player_C.pcc" };
#elif __GAME3__
        /// <summary>
        /// List of games this build supports
        /// </summary>
        public static MEGame[] Games = new[] { MEGame.LE3 };
        public static readonly string[] filesToSkip = { "RefShaderCache-PC-D3D-SM3.upk", "RefShaderCache-PC-D3D-SM5.upk", "IpDrv.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "GFxUI.pcc" };
#endif


        private static string dlcModPath { get; set; }
        /// <summary>
        /// The DLC mod's cookedpc path.
        /// </summary>
        public static string DLCModCookedPath { get; private set; }

        public static void InitMERFS(OptionsPackage options)
        {
            var useTlk = options.SelectedOptions.Any(x => x.RequiresTLK);
            installedStartupPackage = false;
            ReloadLoadedFiles(options.RandomizationTarget);

            dlcModPath = GetDLCModPath(options.RandomizationTarget);
            if (options.Reroll && Directory.Exists(dlcModPath))
                MUtilities.DeleteFilesAndFoldersRecursively(dlcModPath); //Nukes the DLC folder

            // Re-extract even if we are on re-roll
            CreateRandomizerDLCMod(options.RandomizationTarget, dlcModPath);
            options.RandomizationTarget.InstallBinkBypass();
            DLCModCookedPath = Path.Combine(dlcModPath, options.RandomizationTarget.Game.CookedDirName());

            // ME1 Randomizer does not use this feature
#if !__GAME1__
            if (options.RandomizationTarget.Game.IsGame2() || options.RandomizationTarget.Game.IsGame3())
            {
                CoalescedHandler.StartHandler();
            }
#endif

            if (useTlk)
            {
                TLKBuilder.StartHandler(options.RandomizationTarget);
            }

#if __GAME3__
            // LE3 version will always install a startup package
            // nInstallStartupPackage(options.RandomizationTarget);
#endif

            ReloadLoadedFiles(options.RandomizationTarget);
        }

        private static object openSavePackageSyncObj = new object();

        /// <summary>
        /// Opens packages in a memory safe fashion using a lock. Takes the full path of the package.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="forceLoadFromDisk"></param>
        /// <param name="quickload"></param>
        /// <returns></returns>
        public static IMEPackage OpenMEPackage(string path, bool forceLoadFromDisk = false, bool quickload = false)
        {
            return MEPackageHandler.OpenMEPackage(path, forceLoadFromDisk: forceLoadFromDisk, quickLoad: quickload, diskIOSyncLock: openSavePackageSyncObj);
        }

        /// <summary>
        /// Installs the DLC mod's startup package and adds it to the startup ini files
        /// </summary>
        public static void InstallStartupPackage(GameTarget target)
        {
            if (installedStartupPackage)
                return;
            installedStartupPackage = true;
            var startupPackage = GetStartupPackage(target);
            var savedStartupPackage = MERFileSystem.SavePackage(startupPackage, true);
            ThreadSafeDLCStartupPackage.AddStartupPackage(Path.GetFileNameWithoutExtension(savedStartupPackage));
            // EntryImporter.AddUserSafeToImportFromFile(target.Game, Path.GetFileName(startupPackage.FilePath));

            // We have to re-open package so it knows how to properly inventory the path of the package for class info.
            var packageForInventory = MEPackageHandler.OpenMEPackage(savedStartupPackage);
            foreach (var startupClass in packageForInventory.Exports.Where(x => x.ClassName == @"Class"))
            {
                MERUtilities.InventoryCustomClass(startupClass);
            }
        }

        /// <summary>
        /// Fetches the DLC mod component's startup package - FROM EXE
        /// </summary>
        /// <returns></returns>
        public static IMEPackage GetStartupPackage(GameTarget target)
        {
            var startupDestName = $"Startup_MOD_{target.Game}Randomizer.pcc";
            return MEPackageHandler.OpenMEPackageFromStream(MERUtilities.GetEmbeddedPackage(target.Game, startupDestName), startupDestName);
        }

        public static void Finalize(OptionsPackage selectedOptions)
        {
            var metacmmFile = Path.Combine(dlcModPath, @"_metacmm.txt");
            var metacmm = new MetaCMM()
            {
                ModName = $"{MERUtilities.GetGameUIName(selectedOptions.RandomizationTarget.Game.IsOTGame())} Randomization",
                Version = MLibraryConsumer.GetAppVersion().ToString(),
            };
            var allOptions = new List<string>();
            foreach (var option in selectedOptions.SelectedOptions)
            {
                allOptions.Add(option.HumanName);
                if (option.SubOptions != null)
                {
                    foreach (var suboption in option.SubOptions)
                    {
                        if (suboption.OptionIsSelected)
                        {
                            allOptions.Add($"{option.HumanName}: {suboption.HumanName}");
                        }
                    }
                }
            }

            metacmm.OptionsSelectedAtInstallTime.AddRange(allOptions);

#if __GAME1__
            var installedBy = $"Mass Effect Randomizer {MLibraryConsumer.GetAppVersion()}";
#elif __GAME2__
            var installedBy = $"Mass Effect 2 Randomizer {MLibraryConsumer.GetAppVersion()}";
#elif __GAME3__
            var installedBy = $"Mass Effect 3 Randomizer {MLibraryConsumer.GetAppVersion()}";
#endif

            metacmm.WriteMetaCMM(metacmmFile, installedBy);
            GlobalCache = null;

            EntryImporter.ClearUserSafeToImportFromFiles(selectedOptions.RandomizationTarget.Game);
        }

        public static CaseInsensitiveDictionary<string> LoadedFiles { get; private set; }
        public static void ReloadLoadedFiles(GameTarget target)
        {
            var loadedFiles = MELoadedFiles.GetAllGameFiles(target.TargetPath, target.Game, true);
            LoadedFiles = new LegendaryExplorerCore.Misc.CaseInsensitiveDictionary<string>();
            foreach (var lf in loadedFiles)
            {
                LoadedFiles[Path.GetFileName(lf)] = lf;
            }
        }

        /// <summary>
        /// Gets a package file from the MER filesystem, checking in the DLC mod folder first, then the original game files. Returns null if a package is not found in the laoded files list or the DLC mod.
        /// </summary>
        /// <param name="packagename"></param>
        /// <returns></returns>
        public static string GetPackageFile(GameTarget target, string packagename, bool MERLogIfNotFound = true)
        {
            if (LoadedFiles == null)
            {
                MERLog.Warning("Calling GetPackageFile() without LoadedFiles! Populating now, but this should be fixed!");
                ReloadLoadedFiles(target);
            }

            bool packageFile = packagename.RepresentsPackageFilePath();
            if (packageFile && DLCModCookedPath != null)
            {
                // Check if the package is already in the mod folder
                var packageName = Path.GetFileName(packagename);
                var dlcModVersion = Path.Combine(DLCModCookedPath, packageName);
                if (File.Exists(dlcModVersion))
                {
                    return dlcModVersion;
                }
            }

            var retFile = LoadedFiles.TryGetValue(packagename, out var result);
            if (!retFile && MERLogIfNotFound)
            {
                MERLog.Warning($"Could not find package file: {packagename}! Loaded files count: {LoadedFiles.Count}");
            }
            return result; // can return null
        }

        /// <summary>
        /// Saves an open package, if it is modified. Saves it to the correct location.
        /// </summary>
        /// <param name="package">Package to save</param>
        /// <returns>The path the package was saved to. Can be null if package was not saved</returns>
        public static string SavePackage(IMEPackage package, bool forceSave = false, string forcedFileName = null)
        {
            if (package.IsModified || forceSave)
            {
                var packageNameNoLocalization = forcedFileName != null ? Path.GetFileNameWithoutExtension(forcedFileName) : package.FileNameNoExtension;
                if (package.Localization != MELocalization.None)
                {
                    packageNameNoLocalization = packageNameNoLocalization.Substring(0, packageNameNoLocalization.LastIndexOf("_", StringComparison.InvariantCultureIgnoreCase));
                }

                // Todo: This might need to check for things like Startup_ESN!
                if (!EntryImporter.FilesSafeToImportFrom(package.Game).Contains(packageNameNoLocalization, StringComparer.InvariantCultureIgnoreCase))
                {
                    var fname = Path.GetFileName(forcedFileName ?? package.FilePath);
                    var packageNewPath = Path.Combine(DLCModCookedPath, fname);
                    lock (openSavePackageSyncObj)
                    {
                        MERLog.Information($"Saving DLC package {Path.GetFileName(package.FilePath)} => {packageNewPath}");
                        package.Save(packageNewPath, true);
                        return packageNewPath;
                    }
                }
                else
                {
                    MERLog.Information($"Saving basegame package {Path.GetFileName(package.FilePath)} => {package.FilePath}");
                    lock (openSavePackageSyncObj)
                    {
                        package.Save(compress: true);
                        return package.FilePath;
                    }
                }
            }

            return null; // Package was not saved
        }

        /// <summary>
        /// Creates the DLC_MOD_RANDOMIZER folder
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private static void CreateRandomizerDLCMod(GameTarget target, string dlcpath)
        {
            Directory.CreateDirectory(dlcpath);
            var zipMemory = MERUtilities.GetEmbeddedAsset("StarterKit", $"{target.Game.ToString().ToLower()}starterkit.zip");
            using ZipArchive archive = new ZipArchive(zipMemory);
            archive.ExtractToDirectory(dlcpath);
        }

        private static MERPackageCache GlobalCache;
        private static bool installedStartupPackage;

        /// <summary>
        /// Gets the global cache of files that can be used for looking up imports
        /// </summary>
        /// <returns></returns>
        public static MERPackageCache GetGlobalCache(GameTarget target)
        {
            if (GlobalCache == null)
            {
                Debug.WriteLine("Loading global cache");
                GlobalCache = new MERPackageCache(target);
                GlobalCache.GetCachedPackage("Core.pcc");
                GlobalCache.GetCachedPackage("SFXGame.pcc");
                GlobalCache.GetCachedPackage("Startup_INT.pcc");
                GlobalCache.GetCachedPackage("Engine.pcc");
                GlobalCache.GetCachedPackage("WwiseAudio.pcc");
                GlobalCache.GetCachedPackage("SFXOnlineFoundation.pcc");
                GlobalCache.GetCachedPackage("PlotManagerMap.pcc");
                GlobalCache.GetCachedPackage("GFxUI.pcc");
            }

            return GlobalCache;
        }

        /// <summary>
        /// Gets a specific file from the game, bypassing the MERFS system.
        /// </summary>
        /// <param name="relativeSubPath"></param>
        /// <returns></returns>
        public static string GetSpecificFile(GameTarget target, string relativeSubPath)
        {
            return Path.Combine(target.TargetPath, relativeSubPath);

        }

        /// <summary>
        /// Gets the path to the TFC used by MER. MER uses 2 TFCs - one is in the basegame 'Textures_MER_PreDLCLoad.tfc', the other being in the DLC mod (or basegame, if DLC mod is not used)
        /// </summary>
        /// <returns></returns>
        public static string GetTFCPath(GameTarget target, bool postLoadTFC)
        {
            if (postLoadTFC)
            {
                return Path.Combine(DLCModCookedPath, $"Textures_DLC_MOD_{target.Game}Randomizer.tfc");
            }

            // TFC that can be used safely before load
            return Path.Combine(M3Directories.GetCookedPath(target), @"Textures_MER_PreDLCLoad.tfc");
        }

        /// <summary>
        /// Gets the path of the DLC mod component. Does not check if it exists. Returns null if the game cannot be found.
        /// </summary>
        /// <returns></returns>
        public static string GetDLCModPath(GameTarget target)
        {
            var dlcPath = MEDirectories.GetDLCPath(target.Game, target.TargetPath);
            if (dlcPath != null)
            {
                return Path.Combine(dlcPath, $"DLC_MOD_{target.Game}Randomizer");
            }
            return null;
        }

        /// <summary>
        /// Opens a package without loading the exports table. Do not cache this package.
        /// </summary>
        /// <param name="packagePath">The path to the package to open</param>
        /// <returns></returns>
        public static IMEPackage OpenMEPackageTablesOnly(string packagePath)
        {
            var package = MEPackageHandler.UnsafePartialLoad(packagePath, x => false);
            return package;
        }
    }
}
