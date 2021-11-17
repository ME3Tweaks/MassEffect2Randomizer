using System.Linq;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.ExportTypes;

namespace Randomizer.Randomizers.Game2.Misc
{
    class SizeSixteens
    {

        public static bool InstallSSChanges(GameTarget target, RandomizationOption option)
        {
            // Freedoms progress
            SetVeetorFootage(target);
            return true;
        }
        
        private static void SetVeetorFootage(GameTarget target)
        {
            var moviedata = RTextureMovie.GetTextureMovieAssetBinary(target.Game, "Veetor.size_mer.bik");
            var veetorFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioD_ProFre_501Veetor")).ToList();
            foreach (var v in veetorFiles)
            {
                MERLog.Information($@"Setting veetor footage in {v}");
                var mpackage = MERFileSystem.GetPackageFile(target, v);
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
