using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;

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

        /// <summary>
        /// If the MERFS system is using the DLC Mod filesystem
        /// </summary>
        public static bool UsingDLCModFS { get; private set; }

        private static string dlcModPath { get; set; }
        /// <summary>
        /// The DLC mod's cookedpc path.
        /// </summary>
        public static string DLCModCookedPath { get; private set; }

        public static void InitMERFS(bool usingDlcModFS)
        {
            // Is there reason to do this here...?
            ReloadLoadedFiles();
            UsingDLCModFS = usingDlcModFS;

            var dlcmodPath = Path.Combine(MEDirectories.GetDefaultGamePath(Game), "BioGame", "DLC", $"DLC_MOD_{Game}Randomizer");
            if (Directory.Exists(dlcmodPath)) Utilities.DeleteFilesAndFoldersRecursively(dlcmodPath); //Nukes the DLC folder

            if (UsingDLCModFS)
            {
                dlcModPath = dlcmodPath;
                CreateRandomizerDLCMod(dlcmodPath);
                DLCModCookedPath = Path.Combine(dlcModPath, Game == MEGame.ME2 ? "CookedPC" : "CookedPCConsole"); // Must be changed for ME3
            }
            else
            {
                dlcModPath = null; // do not set this var
            }
            ReloadLoadedFiles();
            CoalescedHandler.StartHandler(UsingDLCModFS);
            TLKHandler.StartHandler(UsingDLCModFS);
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
            bool packageFile = packagename.RepresentsPackageFilePath();
            if (packageFile && UsingDLCModFS)
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
        /// Saves an open package.
        /// </summary>
        /// <param name="package"></param>
        public static void SavePackage(IMEPackage package)
        {
            if (package.IsModified)
            {
                if (UsingDLCModFS && !alwaysBasegameFiles.Contains(Path.GetFileName(package.FilePath), StringComparer.InvariantCultureIgnoreCase))
                {
                    var fname = Path.GetFileName(package.FilePath);
                    var packageNewPath = Path.Combine(DLCModCookedPath, fname);
                    package.Save(packageNewPath, false);
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
                if (UsingDLCModFS)
                {
                    return Path.Combine(DLCModCookedPath, $"Textures_DLC_MOD_{Game}Randomizer.tfc");
                }
                else
                {
                    return Path.Combine(MEDirectories.GetCookedPath(Game), $"Textures_{Game}Randomizer.tfc");
                }
            }

            // TFC that can be used safely before load
            return Path.Combine(MEDirectories.GetCookedPath(Game), @"Textures_MER_PreDLCLoad.tfc");
        }
    }
}
