namespace Randomizer.Randomizers.Game2.ExportTypes
{
    class RHeightFogComponent
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"HeightFogComponent";
        public static bool RandomizeExport(ExportEntry export,RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var properties = export.GetProperties();
            var lightColor = properties.GetProp<StructProperty>("LightColor");
            if (lightColor != null)
            {
                lightColor.GetProp<ByteProperty>("R").Value = (byte)ThreadSafeRandom.Next(256);
                lightColor.GetProp<ByteProperty>("G").Value = (byte)ThreadSafeRandom.Next(256);
                lightColor.GetProp<ByteProperty>("B").Value = (byte)ThreadSafeRandom.Next(256);

                var density = properties.GetProp<FloatProperty>("Density");
                if (density != null)
                {
                    var thicknessRandomizer = ThreadSafeRandom.NextFloat(-density * .03, density * 1.15);
                    density.Value = density + thicknessRandomizer;
                }

                //Debug.WriteLine($"Updating fog {export.InstancedFullPath} in {export.FileRef.FilePath}");
                export.WriteProperties(properties);
                return true;
            }
            return false;
        }
    }
}
