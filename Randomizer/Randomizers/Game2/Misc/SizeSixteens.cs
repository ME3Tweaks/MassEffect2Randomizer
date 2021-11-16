using System.Linq;
using LegendaryExplorerCore.Packages;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.ExportTypes;

namespace Randomizer.Randomizers.Game2.Misc
{
    class SizeSixteens
    {

        public static bool InstallSSChanges(RandomizationOption option)
        {
            // Freedoms progress
            SetVeetorFootage();
            return true;
        }
        
        private static void SetVeetorFootage()
        {
            var moviedata = RTextureMovie.GetTextureMovieAssetBinary("Veetor.size_mer.bik");
            var veetorFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioD_ProFre_501Veetor")).ToList();
            foreach (var v in veetorFiles)
            {
                MERLog.Information($@"Setting veetor footage in {v}");
                var mpackage = MERFileSystem.GetPackageFile(v);
                var package = MEPackageHandler.OpenMEPackage(mpackage);
                var veetorExport = package.FindExport("BioVFX_Env_Hologram.ProFre_501_VeetorFootage");
                if (veetorExport != null)
                {
                    RTextureMovie.RandomizeExportDirect(veetorExport, null, moviedata);
                }
                MERFileSystem.SavePackage(package);
            }
        }
    }
}
