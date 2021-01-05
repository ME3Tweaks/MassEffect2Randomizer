using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MassEffectRandomizer.Classes;
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
            UsingDLCModFS = usingDlcModFS;
            if (UsingDLCModFS)
            {
                dlcModPath = CreateRandomizerDLCMod();
                DLCModCookedPath = Path.Combine(dlcModPath, Game == MEGame.ME2 ? "CookedPC" : "CookedPCConsole"); // Must be changed for ME3
            }
            else
            {
                // Todo: Delete any existing randomizer mod that is installed.
            }
        }



        private static CaseInsensitiveDictionary<string> LoadedFiles { get; set; }
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
            return result;
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
        private static string CreateRandomizerDLCMod()
        {
            var dlcpath = Path.Combine(MEDirectories.GetDefaultGamePath(Game), "BioGame", "DLC", $"DLC_MOD_{Game}Randomizer");
            if (Directory.Exists(dlcpath)) Utilities.DeleteFilesAndFoldersRecursively(dlcpath);
            Directory.CreateDirectory(dlcpath);

            MemoryStream zipMemory = new MemoryStream();
            Utilities.ExtractInternalFileToMemory("starterkit.dlcmodcomponents.zip", false, zipMemory);
            using ZipArchive archive = new ZipArchive(zipMemory);
            archive.ExtractToDirectory(dlcpath);
            return dlcpath;
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
    }
}
