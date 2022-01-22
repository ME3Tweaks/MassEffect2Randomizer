using System.Linq;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Shared;

namespace Randomizer.Randomizers.Game3.ExportTypes
{
    class RBioMorphFace
    {
        private static RandomizationOption henchFaceOption = new RandomizationOption() { SliderValue = .3f };

        //private static string[] SquadmateMorphHeadPaths =
        //{
        //    "BIOG_Hench_FAC.HMM.hench_wilson",
        //    "BIOG_Hench_FAC.HMM.hench_leadingman"
        //};

        public static bool RandomizeSquadmateFaces(GameTarget target, RandomizationOption option)
        {
            //var henchFiles = MERFileSystem.LoadedFiles.Where(x => x.Key.StartsWith("BioH_")
            //                                                      || x.Key.StartsWith("BioP_ProCer")
            //                                                      || x.Key.StartsWith("BioD_ProCer")
            //                                                      || x.Key == "BioD_EndGm1_110ROMJacob.pcc");
            //foreach (var h in henchFiles)
            //{
            //    var hPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, h.Key));
            //    foreach (var smhp in SquadmateMorphHeadPaths)
            //    {
            //        var mf = hPackage.FindExport(smhp);
            //        if (mf != null)
            //        {
            //            RSharedBioMorphHead.RandomizeInternal(mf, henchFaceOption);

            //        }
            //    }
            //    MERFileSystem.SavePackage(hPackage);
            //}
            //return true;
            return true;
        }

        private static bool CanRandomizeNonHench(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioMorphFace";
        public static bool RandomizeExportNonHench(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            //if (!CanRandomizeNonHench(export)) return false;
            RSharedBioMorphHead.RandomizeInternal(export, option);
            return true;
        }
    }
}
