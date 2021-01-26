using System.IO;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RInterpTrackMove
    {

        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"InterpTrackMove" && ThreadSafeRandom.Next(1) == 0;
        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            Log.Information($"[{Path.GetFileNameWithoutExtension(export.FileRef.FilePath)}] Randomizing movement interpolations for " + export.UIndex + ": " + export.InstancedFullPath);
            var props = export.GetProperties();
            var posTrack = props.GetProp<StructProperty>("PosTrack");
            if (posTrack != null)
            {
                var points = posTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            x.Value = x.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                            y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                            z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                        }
                    }
                }
            }

            var eulerTrack = props.GetProp<StructProperty>("EulerTrack");
            if (eulerTrack != null)
            {
                var points = eulerTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            if (x.Value != 0)
                            {
                                x.Value = x.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                            }
                            else
                            {
                                x.Value = ThreadSafeRandom.NextFloat(0, ThreadSafeRandom.NextFloat(-1000 * option.SliderValue, 1000 * option.SliderValue));
                            }

                            if (y.Value != 0)
                            {
                                y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                            }
                            else
                            {
                                y.Value = ThreadSafeRandom.NextFloat(0, ThreadSafeRandom.NextFloat(-1000 * option.SliderValue, 1000 * option.SliderValue));
                            }

                            if (z.Value != 0)
                            {
                                z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
                            }
                            else
                            {
                                z.Value = ThreadSafeRandom.NextFloat(0, ThreadSafeRandom.NextFloat(-1000 * option.SliderValue, 1000 * option.SliderValue));
                            }
                        }
                    }
                }
            }

            export.WriteProperties(props);
            return true;
        }
    }
}
