using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Serilog;

namespace Randomizer.Randomizers.Levels
{
    class REdenPrime
    {
        private static void RandomizeEdenPrime(GameTarget target, RandomizationOption option)
        {
            MERLog.Information("Randomizing Eden Prime");

            var p = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\PRO\DSG\BIOA_PRO10_08_DSG.SFM"));
            MERLog.Information("Applying sovereign drawscale pre-randomization modifications");
            p.GetUExport(5640).Data = MERUtilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.SovereignInterpTrackFloatDrawScale_5640_PRO08DSG.bin");
            p.GetUExport(5643).Data = MERUtilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.SovereignInterpTrackMove_5643_PRO08DSG.bin");

            ExportEntry drawScaleExport = p.GetUExport(5640);
            var floatTrack = drawScaleExport.GetProperty<StructProperty>("FloatTrack");
            {
                var points = floatTrack?.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var s = points[i];
                        var outVal = s.GetProp<FloatProperty>("OutVal");
                        if (outVal != null)
                        {
                            outVal.Value = ThreadSafeRandom.NextFloat(-15, 95);
                        }
                    }
                }
            }

            drawScaleExport.WriteProperty(floatTrack);

            ExportEntry movementExport = p.GetUExport(5643);
            var props = movementExport.GetProperties();
            var posTrack = props.GetProp<StructProperty>("PosTrack");
            if (posTrack != null)
            {
                var points = posTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    for (int i = 1; i < 5; i++)
                    {
                        var s = points[i];
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            x.Value += ThreadSafeRandom.NextFloat(-3000, 3000);
                            y.Value += ThreadSafeRandom.NextFloat(-3000, 3000);
                            z.Value = ThreadSafeRandom.NextFloat(-106400, 392000);
                        }
                    }
                }
            }

            movementExport.WriteProperties(props);
            //p.save();
        }

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            RandomizeEdenPrime(target, option);
            return true;
        }
    }
}
