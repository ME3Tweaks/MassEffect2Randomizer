using LegendaryExplorerCore.Unreal;
using System.Collections.Generic;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Packages;
using NameProperty = LegendaryExplorerCore.Unreal.NameProperty;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class ScalarParameter
    {
        public static List<ScalarParameter> GetScalarParameters(ExportEntry export)
        {
            var vectors = export.GetProperty<ArrayProperty<StructProperty>>("ScalarParameterValues") ?? export.GetProperty<ArrayProperty<StructProperty>>("ScalarParameters");
            return vectors?.Select(FromStruct).ToList();
        }

        public static void WriteScalarParameters(ExportEntry export, List<ScalarParameter> parameters, string paramName = "ScalarParameterValues")
        {
            var arr = new ArrayProperty<StructProperty>(paramName);
            arr.AddRange(parameters.Select(x => x.ToStruct()));
            export.WriteProperty(arr);
        }

        public static ScalarParameter FromStruct(StructProperty sp)
        {
            return new ScalarParameter
            {
                Property = sp,
                ParameterName = sp.GetProp<NameProperty>("ParameterName")?.Value,
                ParameterValue = sp.GetProp<FloatProperty>(sp.StructType == "SMAScalarParameter" ? "Parameter" : "ParameterValue").Value,
                Group = sp.GetProp<NameProperty>("Group")
            };
        }

        public StructProperty ToStruct()
        {
            PropertyCollection props = new PropertyCollection();
            props.Add(new NameProperty(ParameterName, "ParameterName"));

            if (Property.StructType == "SMAScalarParameter")
            {
                props.Add(new FloatProperty(ParameterValue, "Parameter"));
                props.Add(Group);
                return new StructProperty("SMAScalarParameter", props);
            }
            else
            {
                props.Add(new FloatProperty(ParameterValue, "ParameterValue"));
                props.Add(RStructs.ToFourPartIntStruct("Guid", true, 0, 0, 0, 0,
                    "A", "B", "C", "D", "ExpressionGUID"));
                return new StructProperty("ScalarParameterValue", props);
            }
        }
        public float ParameterValue { get; set; }

        public string ParameterName { get; set; }

        /// <summary>
        /// SMAScalarParameter only
        /// </summary>
        public NameProperty Group { get; set; }

        public StructProperty Property { get; set; }
    }

    class VectorParameter
    {
        public static List<VectorParameter> GetVectorParameters(ExportEntry export)
        {
            var vectors = export.GetProperty<ArrayProperty<StructProperty>>("VectorParameterValues") ?? export.GetProperty<ArrayProperty<StructProperty>>("VectorParameters");
            return vectors?.Select(FromStruct).ToList();
        }

        public static void WriteVectorParameters(ExportEntry export, List<VectorParameter> parameters, string paramName = "VectorParameterValues")
        {
            var arr = new ArrayProperty<StructProperty>(paramName);
            arr.AddRange(parameters.Select(x => x.ToStruct()));
            export.WriteProperty(arr);
        }

        public static VectorParameter FromStruct(StructProperty sp)
        {
            return new VectorParameter
            {
                Property = sp,
                ParameterName = sp.GetProp<NameProperty>("ParameterName")?.Value,
                ParameterValue = RStructs.FromLinearColorStructProperty(sp.GetProp<StructProperty>(sp.StructType == "SMAVectorParameter" ? "Parameter" : "ParameterValue")),
                Group = sp.GetProp<NameProperty>("Group")
            };
        }

        public StructProperty ToStruct()
        {
            PropertyCollection props = new PropertyCollection();
            props.Add(new NameProperty(ParameterName, "ParameterName"));

            if (Property.StructType == "SMAVectorParameter")
            {
                props.Add(RStructs.ToFourPartFloatStruct("LinearColor", true, ParameterValue.W, ParameterValue.X, ParameterValue.Y, ParameterValue.Z,
                    "R", "G", "B", "A", "Parameter"));
                props.Add(Group);
                return new StructProperty("SMAVectorParameter", props);
            }
            else
            {
                props.Add(RStructs.ToFourPartFloatStruct("LinearColor", true, ParameterValue.W, ParameterValue.X, ParameterValue.Y, ParameterValue.Z,
                    "R", "G", "B", "A", "ParameterValue"));
                props.Add(RStructs.ToFourPartIntStruct("Guid", true, 0, 0, 0, 0,
                    "A", "B", "C", "D", "ExpressionGUID"));
                return new StructProperty("VectorParameterValue", props);
            }
        }
        public CFVector4 ParameterValue { get; set; }

        public string ParameterName { get; set; }

        /// <summary>
        /// SMAVectorParameter only
        /// </summary>
        public NameProperty Group { get; set; }

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
