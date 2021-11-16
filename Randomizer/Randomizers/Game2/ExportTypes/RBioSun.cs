using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.Misc;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    class RBioSun
    {
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && (exp.ClassName == "BioSunFlareComponent" || exp.ClassName == "BioSunFlareStreakComponent" || exp.ClassName == "BioSunActor");

        public static bool PerformRandomization(ExportEntry export,RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            MERLog.Information($"{export.FileRef.FilePath}\t{export.FullPath}");
            var props = export.GetProperties();
            if (export.ClassName == "BioSunFlareComponent" || export.ClassName == "BioSunFlareStreakComponent")
            {
                var tint = props.GetProp<StructProperty>("FlareTint");
                if (tint != null)
                {
                    RStructs.RandomizeTint( tint, false);
                }
                RProperty.RandFloat( props, "Intensity", 0.0001f, 100f, false);
                RProperty.RandFloat( props, "BrightPercent", 0.0001f, 0.1f, false);
                RProperty.RandFloat( props, "Scale", 0.05f, 3f, false);
            }
            else if (export.ClassName == "BioSunActor")
            {
                var tint = props.GetProp<StructProperty>("SunTint");
                if (tint != null)
                {
                    RStructs.RandomizeTint( tint, false);
                }
            }

            export.WriteProperties(props);
            return true;
        }
    }
}
