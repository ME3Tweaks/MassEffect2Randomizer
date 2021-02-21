using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioMorphFace
    {
        private static RandomizationOption henchFaceOption = new RandomizationOption() { SliderValue = .3f };
        public static bool RandomizeSquadmateFaces(RandomizationOption option)
        {
            var henchFiles = MERFileSystem.LoadedFiles.Where(x => x.Key.StartsWith("BioH_"));
            foreach (var h in henchFiles)
            {
                var hPackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(h.Key));

                var morphFaces = hPackage.Exports.Where(x => x.ClassName == "BioMorphFace");
                foreach (var mf in morphFaces)
                {
                    RandomizeExport(mf, henchFaceOption);
                }

                MERFileSystem.SavePackage(hPackage);
            }
            return true;
        }

        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioMorphFace" && !export.FileRef.FilePath.Contains("BioH_");
        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
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
            return true;
        }
    }
}
