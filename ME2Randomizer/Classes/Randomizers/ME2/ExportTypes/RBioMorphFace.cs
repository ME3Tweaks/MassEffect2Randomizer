using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioMorphFace : IExportRandomizer
    {
        private bool CanRandomize(ExportEntry export) => export.ClassName == @"BioMorphFace";
        public bool RandomizeExport(ExportEntry export, RandomizationOption option, Random random)
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
                        offset.Value = offset.Value * random.NextFloat(1 - (option.SliderValue / 3), 1 + (option.SliderValue / 3));
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
                        x.Value = x.Value * random.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                        y.Value = y.Value * random.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                        z.Value = z.Value * random.NextFloat(1 - (option.SliderValue / .85), 1 + (option.SliderValue / .85));
                    }
                }
            }

            export.WriteProperties(props);

            // Todo: Somehow move this to another export or something.
            //if (mainWindow.RANDSETTING_PAWN_CLOWNMODE)
            //{
            //    var materialoverride = props.GetProp<ObjectProperty>("m_oMaterialOverrides");
            //    if (materialoverride != null)
            //    {
            //        var overrides = export.FileRef.GetUExport(materialoverride.Value);
            //        RandomizeMaterialOverride(overrides, random);
            //    }
            //}
            return true;
        }
    }
}
