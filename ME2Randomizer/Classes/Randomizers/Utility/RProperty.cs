using ME3ExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class RProperty
    {
        public static void RandVector(Random random, PropertyCollection props, string propname, float min, float max, bool createIfMissing)
        {
            var prop = props.GetProp<StructProperty>(propname);
            if (prop == null && createIfMissing)
            {
                var propCollection = new PropertyCollection();
                propCollection.Add(new FloatProperty(0, "X"));
                propCollection.Add(new FloatProperty(0, "Y"));
                propCollection.Add(new FloatProperty(0, "Z"));
                prop = new StructProperty("Vector", propCollection, propname, true);
                props.Add(prop);
            }
            if (prop != null)
            {
                prop.GetProp<FloatProperty>("X").Value = random.NextFloat(min, max);
                prop.GetProp<FloatProperty>("Y").Value = random.NextFloat(min, max);
                prop.GetProp<FloatProperty>("Z").Value = random.NextFloat(min, max);
            }
        }

        public static void RandFloat(Random random, PropertyCollection props, string propname, float min, float max, bool createIfMissing)
        {
            var prop = props.GetProp<FloatProperty>(propname);
            if (prop == null && createIfMissing)
            {
                prop = new FloatProperty(0, propname);
                props.Add(prop);
            }
            if (prop != null) prop.Value = random.NextFloat(min, max);
        }

        public static void RandBool(Random random, PropertyCollection props, string propname, bool createIfMissing)
        {
            var prop = props.GetProp<BoolProperty>(propname);
            if (prop == null && createIfMissing)
            {
                prop = new BoolProperty(false, propname);
                props.Add(prop);
            }
            if (prop != null) prop.Value = random.Next(2) == 1;
        }
    }
}
