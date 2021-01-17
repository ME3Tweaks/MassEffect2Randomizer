using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME3ExplorerCore.Packages;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class FreedomsProgress
    {
        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            SetVeetorFootage();
            return true;
        }

        private static void SetVeetorFootage()
        {
            var moviedata = RTextureMovie.GetTextureMovieAssetBinary("Veetor.size_mer.bik");
            var veetorFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioD_ProFre_501Veetor")).ToList();
            foreach (var v in veetorFiles)
            {
                Log.Information($@"Setting veetor footage in {v}");
                var mpackage = MERFileSystem.GetPackageFile(v);
                var package = MEPackageHandler.OpenMEPackage(mpackage);
                var veetorExport = package.Exports.FirstOrDefault(x => x.ObjectName == "ProFre_501_VeetorFootage");

                if (veetorExport != null)
                {
                    RTextureMovie.RandomizeExportDirect(veetorExport, null, moviedata);
                }
                MERFileSystem.SavePackage(package);
            }
        }
    }
}
