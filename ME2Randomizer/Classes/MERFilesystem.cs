using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using ALOTInstallerCore;
using ALOTInstallerCore.Helpers;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Packages;
using Serilog;

namespace ME2Randomizer.Classes
{
    public class MERFileSystem
    {
#if __ME2__
        public static MEGame Game => MEGame.ME2;
        public static readonly string[] filesToSkip = { "RefShaderCache-PC-D3D-SM3.upk", "IpDrv.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "GFxUI.pcc" };
        public static readonly string[] alwaysBasegameFiles = { "Startup_INT.pcc", "Engine.pcc", "GameFramework.pcc", "SFXGame.pcc", "EntryMenu.pcc", "BIOG_Male_Player_C.pcc" };
# elif __ME3__
        public static MEGame Game => MEGame.ME3;
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
            ReloadLoadedFiles();

            dlcModPath = GetDLCModPath();
            if (options.Reroll && Directory.Exists(dlcModPath)) Utilities.DeleteFilesAndFoldersRecursively(dlcModPath); //Nukes the DLC folder

            // Re-extract even if we are on re-roll
            CreateRandomizerDLCMod(dlcModPath);
            Locations.GetTarget(Game).InstallBinkBypass();
            DLCModCookedPath = Path.Combine(dlcModPath, Game == MEGame.ME2 ? "CookedPC" : "CookedPCConsole"); // Must be changed for ME3


            ReloadLoadedFiles();
            CoalescedHandler.StartHandler();
            if (useTlk)
            {
                TLKHandler.StartHandler();
            }
        }

        private static object openSavePackageSyncObj = new object();

        /// <summary>
        /// Opens packages in a memory safe fashion using a lock.
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
        public static void InstallStartupPackage()
        {
            if (installedStartupPackage)
                return;
            installedStartupPackage = true;
            var startupPackage = GetStartupPackage();
            MERFileSystem.SavePackage(startupPackage, true);

            ThreadSafeDLCStartupPackage.AddStartupPackage($"Startup_DLC_MOD_{MERFileSystem.Game}Randomizer");
        }

        /// <summary>
        /// Fetches the DLC mod component's startup package
        /// </summary>
        /// <returns></returns>
        public static IMEPackage GetStartupPackage()
        {
            var startupDestName = $"Startup_DLC_MOD_{MERFileSystem.Game}Randomizer_INT.pcc";
            return MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(MERUtilities.GetEmbeddedStaticFilesBinaryFile(startupDestName)), startupDestName);
        }

        public static void Finalize(OptionsPackage selectedOptions)
        {
            var metacmmFile = Path.Combine(dlcModPath, @"_metacmm.txt");
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            var metacmm = new MetaCMM()
            {
                InstalledBy = $"Mass Effect 2 Randomizer {version}",
                ModName = "Mass Effect 2 Randomization",
                Version = version.ToString(),
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
            File.WriteAllText(metacmmFile, metacmm.ToCMMText());
            GlobalCache = null;
        }

        public static ME3ExplorerCore.Misc.CaseInsensitiveDictionary<string> LoadedFiles { get; private set; }
        public static void ReloadLoadedFiles()
        {
            var loadedFiles = MELoadedFiles.GetAllGameFiles(MEDirectories.GetDefaultGamePath(Game), Game, true);
            LoadedFiles = new ME3ExplorerCore.Misc.CaseInsensitiveDictionary<string>();
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
        public static string GetPackageFile(string packagename)
        {
            if (LoadedFiles == null)
            {
                Log.Warning("Calling GetPackageFile() without LoadedFiles! Populating now, but this should be fixed!");
                ReloadLoadedFiles();
            }
            bool packageFile = ME3ExplorerCore.Helpers.StringExtensions.RepresentsPackageFilePath(packagename);
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
            if (!retFile)
            {
                Log.Warning($"Could not find package file: {packagename}! Loaded files count: {LoadedFiles.Count}");
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
        private static void CreateRandomizerDLCMod(string dlcpath)
        {
            Directory.CreateDirectory(dlcpath);

            MemoryStream zipMemory = new MemoryStream();
            MERUtilities.ExtractInternalFileToMemory($"starterkit.{Game.ToString().ToLower()}starterkit.zip", false, zipMemory);
            using ZipArchive archive = new ZipArchive(zipMemory);
            archive.ExtractToDirectory(dlcpath);
        }

        private static MERPackageCache GlobalCache;
        private static bool installedStartupPackage;

        /// <summary>
        /// Gets the global cache of files that can be used for looking up imports
        /// </summary>
        /// <returns></returns>
        public static MERPackageCache GetGlobalCache()
        {
            if (GlobalCache == null)
            {
                GlobalCache = new MERPackageCache();
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
        public static string GetSpecificFile(string relativeSubPath)
        {
            return Path.Combine(MEDirectories.GetDefaultGamePath(Game), relativeSubPath);

        }

        /// <summary>
        /// Gets the path to the TFC used by MER. MER uses 2 TFCs - one is in the basegame 'Textures_MER_PreDLCLoad.tfc', the other being in the DLC mod (or basegame, if DLC mod is not used)
        /// </summary>
        /// <returns></returns>
        public static string GetTFCPath(bool postLoadTFC)
        {
            if (postLoadTFC)
            {
                return Path.Combine(DLCModCookedPath, $"Textures_DLC_MOD_{Game}Randomizer.tfc");
            }

            // TFC that can be used safely before load
            return Path.Combine(MEDirectories.GetCookedPath(Game), @"Textures_MER_PreDLCLoad.tfc");
        }

        /// <summary>
        /// Gets the path of the DLC mod component. Does not check if it exists.
        /// </summary>
        /// <returns></returns>
        public static string GetDLCModPath()
        {
            return Path.Combine(MEDirectories.GetDLCPath(Game), $"DLC_MOD_{Game}Randomizer");
        }
    }
}
