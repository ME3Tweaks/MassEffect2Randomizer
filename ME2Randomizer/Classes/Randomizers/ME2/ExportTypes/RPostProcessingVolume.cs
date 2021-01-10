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
                RandBool(random, settings, "bEnableDOF", random.Next(3) == 0);
                RandBool(random, settings, "bEnableFilmic", random.Next(3) == 0);
                RandBool(random, settings, "bEnableBloom", random.Next(3) == 0);
                RandFloat(random, settings, "Bloom_Scale", 0, 0.4f, random.Next(3) == 0);
                RandFloat(random, settings, "DOF_BlurKernelSize", 0, 20f, random.Next(3) == 0);
                RandFloat(random, settings, "DOF_MaxNearBlurAmount", 0, 0.2f, random.Next(3) == 0);
                RandFloat(random, settings, "DOF_MaxFarBlurAmount", 0.1f, 0.5f, random.Next(3) == 0);
                RandFloat(random, settings, "DOF_InnerFocusRadius", 0, 2000f, random.Next(3) == 0);
                RandFloat(random, settings, "DOF_FocusDistance", 0, 400f, random.Next(3) == 0);
                RandFloat(random, settings, "Scene_Desaturation", 0, 0.5f, random.Next(3) == 0);
                RandFloat(random, settings, "Scene_InterpolationDuration", 0, 2f, random.Next(3) == 0);
                RandVector(random, settings, "Scene_Highlights", 0, 2, random.Next(3) == 0);
                RandVector(random, settings, "Scene_Midtones", 0, 6f, random.Next(3) == 0);
                RandVector(random, settings, "Scene_Shadows", 0, .4f, random.Next(3) == 0);
                export.WriteProperty(settings);
                return true;
            }
            return false;
        }

        private static void RandVector(Random random, StructProperty settings, string propname, float min, float max, bool createIfMissing)
        {
            var prop = settings.GetProp<StructProperty>(propname);
            if (prop == null && createIfMissing)
            {
                var propCollection = new PropertyCollection();
                propCollection.Add(new FloatProperty(0, "X"));
                propCollection.Add(new FloatProperty(0, "Y"));
                propCollection.Add(new FloatProperty(0, "Z"));
                prop = new StructProperty("Vector", propCollection, propname, true);
                settings.Properties.Add(prop);
            }
            if (prop != null)
            {
                prop.GetProp<FloatProperty>("X").Value = random.NextFloat(min, max);
                prop.GetProp<FloatProperty>("Y").Value = random.NextFloat(min, max);
                prop.GetProp<FloatProperty>("Z").Value = random.NextFloat(min, max);
            }
        }

        private static void RandFloat(Random random, StructProperty settings, string propname, float min, float max, bool createIfMissing)
        {
            var prop = settings.GetProp<FloatProperty>(propname);
            if (prop == null && createIfMissing)
            {
                prop = new FloatProperty(0, propname);
                settings.Properties.Add(prop);
            }
            if (prop != null) prop.Value = random.NextFloat(min, max);
        }

        private static void RandBool(Random random, StructProperty settings, string propname, bool createIfMissing)
        {
            var prop = settings.GetProp<BoolProperty>(propname);
            if (prop == null && createIfMissing)
            {
                prop = new BoolProperty(false, propname);
                settings.Properties.Add(prop);
            }
            if (prop != null) prop.Value = random.Next(2) == 1;
        }
    }
}