using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.SharpDX;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    public static class RStructs
    {
        public static StructProperty ToVectorStructProperty(this Vector3 vector, string propName = null)
        {
            var pc = new PropertyCollection();
            pc.Add(new FloatProperty(vector.X, "X"));
            pc.Add(new FloatProperty(vector.Y, "Y"));
            pc.Add(new FloatProperty(vector.Z, "Z"));
            return new StructProperty("Vector", pc, propName, true);
        }

        public static void RandomizeColor(StructProperty color, bool randomizeAlpha)
        {
            var a = color.GetProp<ByteProperty>("A");
            var r = color.GetProp<ByteProperty>("R");
            var g = color.GetProp<ByteProperty>("G");
            var b = color.GetProp<ByteProperty>("B");

            int totalcolorValue = r.Value + g.Value + b.Value;

            //Randomizing hte pick order will ensure we get a random more-dominant first color (but only sometimes).
            //e.g. if e went in R G B order red would always have a chance at a higher value than the last picked item
            var randomOrderChooser = new List<ByteProperty>();
            randomOrderChooser.Add(r);
            randomOrderChooser.Add(g);
            randomOrderChooser.Add(b);
            randomOrderChooser.Shuffle();

            randomOrderChooser[0].Value = (byte)ThreadSafeRandom.Next(0, Math.Min(totalcolorValue, 256));
            totalcolorValue -= randomOrderChooser[0].Value;

            randomOrderChooser[1].Value = (byte)ThreadSafeRandom.Next(0, Math.Min(totalcolorValue, 256));
            totalcolorValue -= randomOrderChooser[1].Value;

            randomOrderChooser[2].Value = (byte)totalcolorValue;
            if (randomizeAlpha)
            {
                a.Value = (byte)ThreadSafeRandom.Next(0, 256);
            }
        }

        public static void RandomizeTint(StructProperty tint, bool randomizeAlpha)
        {
            var a = tint.GetProp<FloatProperty>("A");
            var r = tint.GetProp<FloatProperty>("R");
            var g = tint.GetProp<FloatProperty>("G");
            var b = tint.GetProp<FloatProperty>("B");

            float totalTintValue = r + g + b;

            //Randomizing hte pick order will ensure we get a random more-dominant first color (but only sometimes).
            //e.g. if e went in R G B order red would always have a chance at a higher value than the last picked item
            List<FloatProperty> randomOrderChooser = new List<FloatProperty>();
            randomOrderChooser.Add(r);
            randomOrderChooser.Add(g);
            randomOrderChooser.Add(b);
            randomOrderChooser.Shuffle();

            randomOrderChooser[0].Value = ThreadSafeRandom.NextFloat(0, totalTintValue);
            totalTintValue -= randomOrderChooser[0].Value;

            randomOrderChooser[1].Value = ThreadSafeRandom.NextFloat(0, totalTintValue);
            totalTintValue -= randomOrderChooser[1].Value;

            randomOrderChooser[2].Value = totalTintValue;
            if (randomizeAlpha)
            {
                a.Value = ThreadSafeRandom.NextFloat(0, 1);
            }
        }
    }
}
