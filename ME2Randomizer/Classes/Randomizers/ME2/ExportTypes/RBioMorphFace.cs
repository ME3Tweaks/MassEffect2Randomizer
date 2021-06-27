using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioMorphFace
    {
        private static RandomizationOption henchFaceOption = new RandomizationOption() { SliderValue = .3f };

        private static string[] SquadmateMorphHeadPaths =
        {
            "BIOG_Hench_FAC.HMM.hench_wilson",
            "BIOG_Hench_FAC.HMM.hench_leadingman"
        };

        public static bool RandomizeSquadmateFaces(RandomizationOption option)
        {
            var henchFiles = MERFileSystem.LoadedFiles.Where(x => x.Key.StartsWith("BioH_")
                                                                  || x.Key.StartsWith("BioP_ProCer")
                                                                  || x.Key.StartsWith("BioD_ProCer")
                                                                  || x.Key == "BioD_EndGm1_110ROMJacob.pcc");
            foreach (var h in henchFiles)
            {
                var hPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(h.Key));
                foreach (var smhp in SquadmateMorphHeadPaths)
                {
                    var mf = hPackage.FindExport(smhp);
                    if (mf != null)
                    {
                        RandomizeInternal(mf, henchFaceOption);

                    }
                }
                MERFileSystem.SavePackage(hPackage);
            }
            return true;
        }

        private static bool CanRandomizeNonHench(ExportEntry export) => !export.IsDefaultObject 
                                                                && export.ClassName == @"BioMorphFace" 
                                                                && !export.ObjectName.Name.Contains("hench_leadingman") 
                                                                && !export.ObjectName.Name.Contains("hench_wilson");
        public static bool RandomizeExportNonHench(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomizeNonHench(export)) return false;
            RandomizeInternal(export, option);
            return true;
        }

        /// <summary>
        /// Randomizes the export. Does not check CanRandomize()
        /// </summary>
        /// <param name="export"></param>
        /// <param name="option"></param>
        private static void RandomizeInternal(ExportEntry export, RandomizationOption option)
        {
            var props = export.GetProperties();
            ArrayProperty<StructProperty> m_aMorphFeatures = props.GetProp<ArrayProperty<StructProperty>>("m_aMorphFeatures");
            if (m_aMorphFeatures != null)
            {
                foreach (StructProperty morphFeature in m_aMorphFeatures)
                {
                    FloatProperty offset = morphFeature.GetProp<FloatProperty>("Offset");
                    if (offset != null)
                    {
                        //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.FullPath + " offset");
                        offset.Value = offset.Value * ThreadSafeRandom.NextFloat(1 - (option.SliderValue / 3), 1 + (option.SliderValue / 3));
                    }
                }
            }

            ArrayProperty<StructProperty> m_aFinalSkeleton = props.GetProp<ArrayProperty<StructProperty>>("m_aFinalSkeleton");
            if (m_aFinalSkeleton != null)
            {
                foreach (StructProperty offsetBonePos in m_aFinalSkeleton)
                {
                    StructProperty vPos = offsetBonePos.GetProp<StructProperty>("vPos");
                    if (vPos != null)
                    {
                        //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.FullPath + " vPos");
                        FloatProperty x = vPos.GetProp<FloatProperty>("X");
                        FloatProperty y = vPos.GetProp<FloatProperty>("Y");
                        FloatProperty z = vPos.GetProp<FloatProperty>("Z");
                        x.Value = x.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                        y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                        z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - (option.SliderValue / .85), 1 + (option.SliderValue / .85));
                    }
                }
            }

            export.WriteProperties(props);
        }
    }
}
