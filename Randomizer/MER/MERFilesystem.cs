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
        public static readonly string[] filesToSkip = { "RefShaderCache-PC-D3D-SM3.upk", "RefShaderCache-PC-D3D-SM5.upk", "IpDrv.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "GFxUI.pcc" };
        /// <summary>
        /// TODO: CHANGE TO NOT USE EXTENSIONS
        /// </summary>
        public static readonly string[] alwaysBasegameFiles = { "Startup_INT.pcc", "Engine.pcc", "GameFramework.pcc", "SFXGame.pcc", "EntryMenu.pcc", "BIOG_Male_Player_C.pcc" };
#elif __GAME2__
        /// <summary>
        /// List of games this build supports
        /// </summary>
        public static MEGame[] Games = new[] { MEGame.ME2, MEGame.LE2 };
        public static readonly string[] filesToSkip = { "RefShaderCache-PC-D3D-SM3.upk", "RefShaderCache-PC-D3D-SM5.upk", "IpDrv.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "GFxUI.pcc" };
        public static readonly string[] alwaysBasegameFiles = { "Startup_INT.pcc", "Engine.pcc", "GameFramework.pcc", "SFXGame.pcc", "EntryMenu.pcc", "BIOG_Male_Player_C.pcc" };
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


            ReloadLoadedFiles(options.RandomizationTarget);

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
            MERFileSystem.SavePackage(startupPackage, true);

            ThreadSafeDLCStartupPackage.AddStartupPackage($"Startup_DLC_MOD_{target.Game}Randomizer");
        }

        /// <summary>
        /// Fetches the DLC mod component's startup package
        /// </summary>
        /// <returns></returns>
        public static IMEPackage GetStartupPackage(GameTarget target)
        {
            var startupDestName = $"Startup_DLC_MOD_{target.Game}Randomizer_INT.pcc";
            return MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(MERUtilities.GetEmbeddedStaticFilesBinaryFile(startupDestName)), startupDestName);
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
        /// <param name="package"></param>
        public static void SavePackage(IMEPackage package, bool forceSave = false)
        {
            if (package.IsModified || forceSave)
            {
                if (!alwaysBasegameFiles.Contains(Path.GetFileName(package.FilePath), StringComparer.InvariantCultureIgnoreCase))
                {
                    var fname = Path.GetFileName(package.FilePath);
                    var packageNewPath = Path.Combine(DLCModCookedPath, fname);
                    lock (openSavePackageSyncObj)
                    {
                        MERLog.Information($"Saving package {Path.GetFileName(package.FilePath)} => {packageNewPath}");
                        package.Save(packageNewPath, true);
                    }
                }
                else
                {
                    MERLog.Information($"Saving package {Path.GetFileName(package.FilePath)} => {package.FilePath}");
                    lock (openSavePackageSyncObj)
                    {
                        package.Save(compress: true);
                    }
                }
            }
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
    }
}
