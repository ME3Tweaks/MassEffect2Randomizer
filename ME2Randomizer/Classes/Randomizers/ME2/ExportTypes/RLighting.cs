using ME2Randomizer.Classes.Randomizers.ME2.Misc;
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
    /// <summary>
    /// Handles randomizing things such as PointLight, Spotlight, etc
    /// </summary>
    class RLighting
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject &&
            (export.ClassName == @"SpotLightComponent" ||
             export.ClassName == @"PointLightComponent" ||
             export.ClassName == @"DirectionalLightComponent" ||
             export.ClassName == @"SkyLightComponent");

        public static bool RandomizeExport(ExportEntry export,RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            Log.Information($@"Randomizing light {export.UIndex}");
            var lc = export.GetProperty<StructProperty>("LightColor");
            if (lc == null)
            {
                // create
                var pc = new PropertyCollection();
                pc.Add(new ByteProperty(255, "B"));
                pc.Add(new ByteProperty(255, "G"));
                pc.Add(new ByteProperty(255, "R"));
                pc.Add(new ByteProperty(0, "A"));

                lc = new StructProperty("Color", pc, "LightColor", true);
            }

            RStructs.RandomizeColor( lc, false);
            export.WriteProperty(lc);
            return true;
        }
    }
}
