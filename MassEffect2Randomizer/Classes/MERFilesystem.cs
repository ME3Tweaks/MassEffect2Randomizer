using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Explorer.Packages;

namespace MassEffectRandomizer.Classes
{
    public class MERFileSystem
    {
        private string gameroot;
        private string dlcModPath;
        private string dlcModCookedPath;
        private bool UsingDLCModFS;

        public MERFileSystem(string gameroot, bool usingDlcModFs)
        {
            this.gameroot = gameroot;
            this.UsingDLCModFS = usingDlcModFs;
            if (UsingDLCModFS)
            {
                dlcModPath = CreateRandomizerDLCMod();
                dlcModCookedPath = Path.Combine(dlcModPath, "CookedPC");
            }
        }

        public static readonly string[] filesToSkip = { "RefShaderCache-PC-D3D-SM3.upk", "IpDrv.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "GFxUI.pcc" };

        public static readonly string[] alwaysBasegameFiles = { "Startup_INT.pcc", "Engine.pcc", "GameFramework.pcc", "SFXGame.pcc", "EntryMenu.pcc" };

        /// <summary>
        /// Fetches file from CookedPC (ME2 specific)
        /// </summary>
        /// <param name="basefilename"></param>
        /// <returns></returns>
        public string GetBasegameFile(string basefilename)
        {
            return GetGameFile("BIOGame\\CookedPC\\" + basefilename);
        }

        public string GetGameFile(string subpath)
        {
            bool packageFile = subpath.RepresentsPackageFilePath();
            if (packageFile && UsingDLCModFS)
            {
                var packageName = Path.GetFileName(subpath);
                var dlcModVersion = Path.Combine(dlcModCookedPath, packageName);
                if (File.Exists(dlcModVersion))
                {
                    return dlcModVersion;
                }
            }

            return Path.Combine(gameroot, subpath);
        }

        public void SavePackage(IMEPackage package)
        {
            if (UsingDLCModFS && !alwaysBasegameFiles.Contains(Path.GetFileName(package.FilePath), StringComparer.InvariantCultureIgnoreCase))
            {
                var fname = Path.GetFileName(package.FilePath);
                var packageNewPath = Path.Combine(dlcModCookedPath, fname);
                package.save(packageNewPath);
            }
            else
            {
                package.save();
            }
        }

        public string CreateRandomizerDLCMod()
        {
            var dlcpath = Path.Combine(gameroot, "CookedPC", "DLC", "DLC_MOD_ME2Randomizer");
            if (Directory.Exists(dlcpath)) Utilities.DeleteFilesAndFoldersRecursively(dlcpath);
            Directory.CreateDirectory(dlcpath);

            MemoryStream zipMemory = new MemoryStream();
            Utilities.ExtractInternalFileToMemory("starterkit.dlcmodcomponents.zip", false, zipMemory);
            using ZipArchive archive = new ZipArchive(zipMemory);
            archive.ExtractToDirectory(dlcpath);
            return dlcpath;
        }
    }
}
