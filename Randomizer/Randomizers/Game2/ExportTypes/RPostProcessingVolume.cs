using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    class RPostProcessingVolume
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"PostProcessVolume";
        public static bool RandomizeExport(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var settings = export.GetProperty<StructProperty>("Settings");
            if (settings != null)
            {
                MERLog.Information($@"Randomizing PostProcessingVolume settings {export.UIndex}");
                // randomize the options
                PropertyTools.RandBool(settings.Properties, "bEnableDOF", ThreadSafeRandom.Next(3) == 0);
                PropertyTools.RandBool(settings.Properties, "bEnableFilmic", ThreadSafeRandom.Next(3) == 0);
                PropertyTools.RandBool(settings.Properties, "bEnableBloom", ThreadSafeRandom.Next(3) == 0);
                PropertyTools.RandFloat(settings.Properties, "Bloom_Scale", 0, 0.4f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandFloat(settings.Properties, "DOF_BlurKernelSize", 0, 20f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandFloat(settings.Properties, "DOF_MaxNearBlurAmount", 0, 0.2f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandFloat(settings.Properties, "DOF_MaxFarBlurAmount", 0.1f, 0.5f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandFloat(settings.Properties, "DOF_InnerFocusRadius", 0, 2000f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandFloat(settings.Properties, "DOF_FocusDistance", 0, 400f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandFloat(settings.Properties, "Scene_Desaturation", 0, 0.5f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandFloat(settings.Properties, "Scene_InterpolationDuration", 0, 2f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandVector(settings.Properties, "Scene_Highlights", 0, 2, ThreadSafeRandom.Next(8) == 0);
                PropertyTools.RandVector(settings.Properties, "Scene_Midtones", 0, 6f, ThreadSafeRandom.Next(5) == 0);
                PropertyTools.RandVector(settings.Properties, "Scene_Shadows", 0, .4f, ThreadSafeRandom.Next(8) == 0);
                export.WriteProperty(settings);
                return true;
            }
            return false;
        }
    }
}