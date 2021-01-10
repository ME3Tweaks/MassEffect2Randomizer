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
        public static bool RandomizeExport(ExportEntry export, Random random, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var settings = export.GetProperty<StructProperty>("Settings");
            if (settings != null)
            {
                Log.Information($@"Randomizing PostProcessingVolume settings {export.UIndex}");
                // randomize the options
                RProperty.RandBool(random, settings.Properties, "bEnableDOF", random.Next(3) == 0);
                RProperty.RandBool(random, settings.Properties, "bEnableFilmic", random.Next(3) == 0);
                RProperty.RandBool(random, settings.Properties, "bEnableBloom", random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "Bloom_Scale", 0, 0.4f, random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "DOF_BlurKernelSize", 0, 20f, random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "DOF_MaxNearBlurAmount", 0, 0.2f, random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "DOF_MaxFarBlurAmount", 0.1f, 0.5f, random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "DOF_InnerFocusRadius", 0, 2000f, random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "DOF_FocusDistance", 0, 400f, random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "Scene_Desaturation", 0, 0.5f, random.Next(3) == 0);
                RProperty.RandFloat(random, settings.Properties, "Scene_InterpolationDuration", 0, 2f, random.Next(3) == 0);
                RProperty.RandVector(random, settings.Properties, "Scene_Highlights", 0, 2, random.Next(3) == 0);
                RProperty.RandVector(random, settings.Properties, "Scene_Midtones", 0, 6f, random.Next(3) == 0);
                RProperty.RandVector(random, settings.Properties, "Scene_Shadows", 0, .4f, random.Next(3) == 0);
                export.WriteProperty(settings);
                return true;
            }
            return false;
        }
    }
}