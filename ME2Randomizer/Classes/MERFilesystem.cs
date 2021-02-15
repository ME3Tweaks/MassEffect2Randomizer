using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
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

        public static void InitMERFS(bool loadTLK)
        {
            // Is there reason to do this here...?
            ReloadLoadedFiles();

            var dlcmodPath = Path.Combine(MEDirectories.GetDefaultGamePath(Game), "BioGame", "DLC", $"DLC_MOD_{Game}Randomizer");
            if (Directory.Exists(dlcmodPath)) Utilities.DeleteFilesAndFoldersRecursively(dlcmodPath); //Nukes the DLC folder


            dlcModPath = dlcmodPath;
            CreateRandomizerDLCMod(dlcmodPath);
            DLCModCookedPath = Path.Combine(dlcModPath, Game == MEGame.ME2 ? "CookedPC" : "CookedPCConsole"); // Must be changed for ME3


            ReloadLoadedFiles();
            CoalescedHandler.StartHandler();
            if (loadTLK)
            {
                TLKHandler.StartHandler();
            }
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
        }

        public static CaseInsensitiveDictionary<string> LoadedFiles { get; private set; }
        public static void ReloadLoadedFiles()
        {
            var loadedFiles = MELoadedFiles.GetAllGameFiles(MEDirectories.GetDefaultGamePath(Game), Game, true);
            LoadedFiles = new CaseInsensitiveDictionary<string>();
            foreach (var lf in loadedFiles)
            {
                LoadedFiles[Path.GetFileName(lf)] = lf;
            }
        }

        public static string GetPackageFile(string packagename)
        {
            if (LoadedFiles == null)
            {
                Debug.WriteLine("Calling GetPackageFile() without LoadedFiles! Populating now, but this should be fixed!");
                ReloadLoadedFiles();
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
            return result; // can return null
        }

        /// <summary>
        /// Saves an open package, if it is modified. Saves it to the correct location.
        /// </summary>
        /// <param name="package"></param>
        public static void SavePackage(IMEPackage package)
        {
            if (package.IsModified)
            {
                Log.Information($"Saving package {Path.GetFileName(package.FilePath)}");
                if (!alwaysBasegameFiles.Contains(Path.GetFileName(package.FilePath), StringComparer.InvariantCultureIgnoreCase))
                {
                    var fname = Path.GetFileName(package.FilePath);
                    var packageNewPath = Path.Combine(DLCModCookedPath, fname);
                    package.Save(packageNewPath, true);
                }
                else
                {
                    package.Save(compress: true);
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
            Utilities.ExtractInternalFileToMemory($"starterkit.{Game.ToString().ToLower()}starterkit.zip", false, zipMemory);
            using ZipArchive archive = new ZipArchive(zipMemory);
            archive.ExtractToDirectory(dlcpath);
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
    }
}
