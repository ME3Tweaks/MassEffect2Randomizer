using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.SharpDX;
using LegendaryExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class RHolograms
    {
        private const int option_avina = 2;
        private const int option_hologram = 1;

        private static bool CanRandomize(ExportEntry export, out int option)
        {
            option = 0;
            if (export.IsDefaultObject) return false;
            if (export.ClassName == "Material")
            {
                if (export.ObjectName.Name == "VI_ARM_NKD_Master_Mat")
                {
                    option = option_avina;
                    return true;
                }
            }
            else if (export.ClassName == "MaterialInstanceConstant" && export.ObjectName.Name.StartsWith("Holo"))
            {
                option = option_hologram;
                return true;
            }

            return false;
        }

        public static bool RandomizeExport(ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp, out var coption)) return false;
            switch (coption)
            {
                case option_hologram:
                    //RMaterialInstance.RandomizeExport(exp, option);
                    return true;
                case option_avina:
                    RandomizeVIMaterial(exp);
                    return true;
            }

            Debug.WriteLine("Hologram randomizer is set up wrong!");
            return false;
        }

        private static void RandomizeVIMaterial(ExportEntry exp)
        {
            var data = exp.Data;
            //RandomizeRGBA(data, 0x70C, false);
            RandomizeRGBA(data, 0x72C, false);
            MERLog.Information($@"Randomized VI material {exp.InstancedFullPath} in {exp.FileRef.FilePath}");
            exp.Data = data;
        }

        /// <summary>
        /// Randomizes the RGBA values starting at the listed offset. Returns the data 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public static Vector4 RandomizeRGBA(byte[] data, int startingOffset, bool randomizeAlpha)
        {
            float totalColor = 0;
            totalColor += BitConverter.ToSingle(data, startingOffset);
            totalColor += BitConverter.ToSingle(data, startingOffset + 4);
            totalColor += BitConverter.ToSingle(data, startingOffset + 8);

            // Randomize the color orders to help ensure we get a more random distribution
            List<int> randomOrderChooser = new List<int>();
            randomOrderChooser.Add(startingOffset);
            randomOrderChooser.Add(startingOffset + 4);
            randomOrderChooser.Add(startingOffset + 8);
            randomOrderChooser.Shuffle();

            byte[] colord = new byte[16];
            for (int i = 0; i < randomOrderChooser.Count; i++)
            {
                float amt;
                if (i == randomOrderChooser.Count - 1)
                {
                    amt = totalColor;
                }
                else
                {
                    amt = ThreadSafeRandom.NextFloat(0, totalColor);
                }
                data.OverwriteRange(randomOrderChooser[i], BitConverter.GetBytes(amt));
                colord.OverwriteRange(randomOrderChooser[i] - startingOffset, BitConverter.GetBytes(amt));
                totalColor -= amt;
            }

            if (randomizeAlpha)
            {
                var amt = ThreadSafeRandom.NextFloat(0, 1);
                data.OverwriteRange(startingOffset + 12, BitConverter.GetBytes(amt));
                colord.OverwriteRange(12, BitConverter.GetBytes(amt));
            }
            else
            {
                colord.OverwriteRange(12, data.Slice(startingOffset + 12, 4));
            }


            return new Vector4()
            {
                W = BitConverter.ToSingle(colord, 0),
                X = BitConverter.ToSingle(colord, 4),
                Y = BitConverter.ToSingle(colord, 8),
                Z = BitConverter.ToSingle(colord, 12),
            };
        }
    }
}
