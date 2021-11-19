using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.Misc
{

    /// <summary>
    /// Illusive Man Eye randomizer
    /// </summary>
    class RIllusiveEyes
    {
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ClassName == "MaterialInstanceConstant" && exp.ObjectName == "HMM_HED_EYEillusiveman_MAT_1a";
        public static bool RandomizeExport(GameTarget target, ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            MERLog.Information($"Randomizing illusive eye color in {exp.FileRef.FilePath}");
            var props = exp.GetProperties();

            //eye color
            var emisVector = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues").First(x => x.GetProp<NameProperty>("ParameterName").Value.Name == "Emis_Color").GetProp<StructProperty>("ParameterValue");
            //tint is float based
            StructTools.RandomizeTint(emisVector, false);

            var emisScalar = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues").First(x => x.GetProp<NameProperty>("ParameterName").Value.Name == "Emis_Scalar").GetProp<FloatProperty>("ParameterValue");
            emisScalar.Value = 3; //very vibrant
            exp.WriteProperties(props);
            return true;
        }
    }
}