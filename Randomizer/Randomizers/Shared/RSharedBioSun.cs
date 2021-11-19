using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Shared
{
    class RSharedBioSun
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
                    StructTools.RandomizeTint( tint, false);
                }
                PropertyTools.RandFloat( props, "Intensity", 0.0001f, 100f, false);
                PropertyTools.RandFloat( props, "BrightPercent", 0.0001f, 0.1f, false);
                PropertyTools.RandFloat( props, "Scale", 0.05f, 3f, false);
            }
            else if (export.ClassName == "BioSunActor")
            {
                var tint = props.GetProp<StructProperty>("SunTint");
                if (tint != null)
                {
                    StructTools.RandomizeTint( tint, false);
                }
            }

            export.WriteProperties(props);
            return true;
        }
    }
}
