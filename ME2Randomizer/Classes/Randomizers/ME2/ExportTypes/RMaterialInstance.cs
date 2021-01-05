using System;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    public class RMaterialInstance
    {
        private static bool CanRandomize(ExportEntry export) => export.ClassName == @"MaterialInstanceConstant";
        public static bool RandomizeExport(ExportEntry material, RandomizationOption option, Random random)
        {
            if (!CanRandomize(material)) return false;
            var props = material.GetProperties();

            {
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                if (vectors != null)
                {
                    foreach (var vector in vectors)
                    {
                        var pc = vector.GetProp<StructProperty>("ParameterValue");
                        if (pc != null)
                        {
                            RStructs.RandomizeTint(random, pc, false);
                        }
                    }
                }

                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                if (scalars != null)
                {
                    for (int i = 0; i < scalars.Count; i++)
                    {
                        var scalar = scalars[i];
                        var parameter = scalar.GetProp<NameProperty>("ParameterName");
                        var currentValue = scalar.GetProp<FloatProperty>("ParameterValue");
                        if (currentValue > 1)
                        {
                            scalar.GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0, currentValue * 1.3);
                        }
                        else
                        {
                            //Debug.WriteLine("Randomizing parameter " + scalar.GetProp<NameProperty>("ParameterName"));
                            scalar.GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0, 1);
                        }
                    }

                    //foreach (var scalar in vectors)
                    //{
                    //    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                    //    RandomizeTint(random, paramValue, false);
                    //}
                }
            }
            material.WriteProperties(props);
            return true;
        }
    }
}
