using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers.Shared
{
    /// <summary>
    /// Eye (non Illusive man) randomizer
    /// </summary>
    class RSharedEyes
    {
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ClassName == "MaterialInstanceConstant" && exp.ObjectName != "HMM_HED_EYEillusiveman_MAT_1a" && exp.ObjectName.Name.Contains("_EYE");
        public static bool RandomizeExport(GameTarget target, ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            //Log.Information("Randomizing eye color");
            RSharedMaterialInstance.RandomizeExport(exp, null);
            return true;
        }
    }
}
