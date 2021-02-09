using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioPawn
    {
        /// <summary>
        /// Is this a non-player biopawn
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        private static bool CanRandomize(ExportEntry export)
        {
            if (!export.IsDefaultObject && !export.IsClass && export.IsA("BioPawn"))
            {
                // BioPawn instance
                var props = export.GetProperties();
                var aic = props.GetProp<ObjectProperty>("AIController");
                if (aic == null || (aic.ResolveToEntry(export.FileRef) is IEntry e && (e.ObjectName == "SFXAI_Ambient" || e.ObjectName == "SFXAI_None")))
                {
                    return true;
                }

                var ambient = export.GetProperty<BoolProperty>("bAmbientCreature");
                if (ambient != null && ambient)
                {
                    return true; // Combat pawns cannot be modified
                }
            }
            return false;
        }

        public static bool RandomizePawnSize(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            Log.Information($"[{Path.GetFileNameWithoutExtension(export.FileRef.FilePath)}] Randomizing pawn size for " + export.UIndex + ": " + export.InstancedFullPath);
            var existingSize = export.GetProperty<StructProperty>("DrawScale3D");
            CFVector3 d3d = existingSize == null ? new CFVector3() { X = 1, Y = 1, Z = 1 } : CFVector3.FromStructProperty(existingSize, "X", "Y", "Z");

            d3d.X *= ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
            d3d.Y *= ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
            d3d.Z *= ThreadSafeRandom.NextFloat(1 - option.SliderValue, 1 + option.SliderValue);
            export.WriteProperty(d3d.ToStructProperty("X", "Y", "Z", "DrawScale3D", true));
            return true;
        }
    }
}
