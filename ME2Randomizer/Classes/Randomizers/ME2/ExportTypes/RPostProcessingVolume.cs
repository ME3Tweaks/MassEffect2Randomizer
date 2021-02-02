using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RPostProcessingVolume
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"PostProcessVolume";
        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var settings = export.GetProperty<StructProperty>("Settings");
            if (settings != null)
            {
                Log.Information($@"Randomizing PostProcessingVolume settings {export.UIndex}");
                // randomize the options
                RProperty.RandBool(settings.Properties, "bEnableDOF", ThreadSafeRandom.Next(3) == 0);
                RProperty.RandBool(settings.Properties, "bEnableFilmic", ThreadSafeRandom.Next(3) == 0);
                RProperty.RandBool(settings.Properties, "bEnableBloom", ThreadSafeRandom.Next(3) == 0);
                RProperty.RandFloat(settings.Properties, "Bloom_Scale", 0, 0.4f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandFloat(settings.Properties, "DOF_BlurKernelSize", 0, 20f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandFloat(settings.Properties, "DOF_MaxNearBlurAmount", 0, 0.2f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandFloat(settings.Properties, "DOF_MaxFarBlurAmount", 0.1f, 0.5f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandFloat(settings.Properties, "DOF_InnerFocusRadius", 0, 2000f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandFloat(settings.Properties, "DOF_FocusDistance", 0, 400f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandFloat(settings.Properties, "Scene_Desaturation", 0, 0.5f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandFloat(settings.Properties, "Scene_InterpolationDuration", 0, 2f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandVector(settings.Properties, "Scene_Highlights", 0, 2, ThreadSafeRandom.Next(8) == 0);
                RProperty.RandVector(settings.Properties, "Scene_Midtones", 0, 6f, ThreadSafeRandom.Next(5) == 0);
                RProperty.RandVector(settings.Properties, "Scene_Shadows", 0, .4f, ThreadSafeRandom.Next(8) == 0);
                export.WriteProperty(settings);
                return true;
            }
            return false;
        }
    }
}