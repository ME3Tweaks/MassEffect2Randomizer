using ME3ExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.SharpDX;
using NameProperty = ME3ExplorerCore.Unreal.NameProperty;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class VectorParameter
    {
        public static List<VectorParameter> GetVectorParameters(ExportEntry export)
        {
            var vectors = export.GetProperty<ArrayProperty<StructProperty>>("VectorParameterValues");
            return vectors?.Select(FromStruct).ToList();
        }

        public static void WriteVectorParameters(ExportEntry export, List<VectorParameter> parameters)
        {
            var arr = new ArrayProperty<StructProperty>("VectorParameterValues");
            arr.AddRange(parameters.Select(x => x.ToStruct()));
            export.WriteProperty(arr);
        }

        public static VectorParameter FromStruct(StructProperty sp)
        {
            VectorParameter vp = new VectorParameter
            {
                Property = sp,
                ParameterName = sp.GetProp<NameProperty>("ParameterName")?.Value,
                ParameterValue = RStructs.FromLinearColorStructProperty(sp.GetProp<StructProperty>("ParameterValue"))
            };
            //vp.ExpressionGuid = FGuid.FromStruct(sp.GetProp<StructProperty>("ExpressionGUID"));

            return vp;
        }

        public StructProperty ToStruct()
        {
            PropertyCollection props = new PropertyCollection();
            props.Add(new NameProperty(ParameterName, "ParameterName"));
            props.Add(RStructs.ToFourPartFloatStruct("LinearColor", true, ParameterValue.W, ParameterValue.X, ParameterValue.Y, ParameterValue.Z,
                "R", "G", "B", "A", "ParameterValue"));
            props.Add(RStructs.ToFourPartIntStruct("LinearColor", true, 0,0,0,0,
                "A", "B", "C", "D", "ExpressionGUID"));

            // Do we add a None?
            return new StructProperty("VectorParameterValue", props);
        }
        public Vector4 ParameterValue { get; set; }

        public string ParameterName { get; set; }

        public StructProperty Property { get; set; }
    }

    class RProperty
    {
        public static void RandVector(PropertyCollection props, string propname, float min, float max, bool createIfMissing)
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
                prop.GetProp<FloatProperty>("X").Value = ThreadSafeRandom.NextFloat(min, max);
                prop.GetProp<FloatProperty>("Y").Value = ThreadSafeRandom.NextFloat(min, max);
                prop.GetProp<FloatProperty>("Z").Value = ThreadSafeRandom.NextFloat(min, max);
            }
        }

        public static void RandFloat(PropertyCollection props, string propname, float min, float max, bool createIfMissing)
        {
            var prop = props.GetProp<FloatProperty>(propname);
            if (prop == null && createIfMissing)
            {
                prop = new FloatProperty(0, propname);
                props.Add(prop);
            }
            if (prop != null) prop.Value = ThreadSafeRandom.NextFloat(min, max);
        }

        public static void RandBool(PropertyCollection props, string propname, bool createIfMissing)
        {
            var prop = props.GetProp<BoolProperty>(propname);
            if (prop == null && createIfMissing)
            {
                prop = new BoolProperty(false, propname);
                props.Add(prop);
            }
            if (prop != null) prop.Value = ThreadSafeRandom.Next(2) == 1;
        }
    }
}
