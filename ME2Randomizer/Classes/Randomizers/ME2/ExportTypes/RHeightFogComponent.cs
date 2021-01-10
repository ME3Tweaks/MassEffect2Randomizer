using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class RHeightFogComponent
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"HeightFogComponent";
        public static bool RandomizeExport(ExportEntry export, Random random, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var properties = export.GetProperties();
            var lightColor = properties.GetProp<StructProperty>("LightColor");
            if (lightColor != null)
            {
                lightColor.GetProp<ByteProperty>("R").Value = (byte)random.Next(256);
                lightColor.GetProp<ByteProperty>("G").Value = (byte)random.Next(256);
                lightColor.GetProp<ByteProperty>("B").Value = (byte)random.Next(256);

                var density = properties.GetProp<FloatProperty>("Density");
                if (density != null)
                {
                    var thicknessRandomizer = random.NextFloat(-density * .03, density * 1.15);
                    density.Value = density + thicknessRandomizer;
                }

                export.WriteProperties(properties);
                return true;
            }
            return false;
        }
    }
}
