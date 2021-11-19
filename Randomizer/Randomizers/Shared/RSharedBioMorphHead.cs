using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace Randomizer.Randomizers.Shared
{
    /// <summary>
    /// Shared code for randomizing the morph head. The can randomize and randomize entry points are
    /// defined per game as the criteria is different per game.
    /// </summary>
    class RSharedBioMorphHead
    {
        /// <summary>
        /// Randomizes the export. Does not check CanRandomize()
        /// </summary>
        /// <param name="export"></param>
        /// <param name="option"></param>
        public static void RandomizeInternal(ExportEntry export, RandomizationOption option)
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
