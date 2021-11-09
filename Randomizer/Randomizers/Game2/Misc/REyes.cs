using Randomizer.Randomizers.Game2.ExportTypes;

namespace Randomizer.Randomizers.Game2.Misc
{

    /// <summary>
    /// Illusive Man Eye randomizer
    /// </summary>
    class RIllusiveEyes
    {
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ClassName == "MaterialInstanceConstant" && exp.ObjectName == "HMM_HED_EYEillusiveman_MAT_1a";
        public static bool RandomizeExport(ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            MERLog.Information($"Randomizing illusive eye color in {exp.FileRef.FilePath}");
            var props = exp.GetProperties();

            //eye color
            var emisVector = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues").First(x => x.GetProp<NameProperty>("ParameterName").Value.Name == "Emis_Color").GetProp<StructProperty>("ParameterValue");
            //tint is float based
            RStructs.RandomizeTint(emisVector, false);

            var emisScalar = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues").First(x => x.GetProp<NameProperty>("ParameterName").Value.Name == "Emis_Scalar").GetProp<FloatProperty>("ParameterValue");
            emisScalar.Value = 3; //very vibrant
            exp.WriteProperties(props);
            return true;
        }
    }

    /// <summary>
    /// Eye (non Illusive man) randomizer
    /// </summary>
    class REyes
    {
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ClassName == "MaterialInstanceConstant" && exp.ObjectName != "HMM_HED_EYEillusiveman_MAT_1a" && exp.ObjectName.Name.Contains("_EYE");
        public static bool RandomizeExport(ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            //Log.Information("Randomizing eye color");
            RMaterialInstance.RandomizeExport(exp, null);
            return true;
        }
    }
}