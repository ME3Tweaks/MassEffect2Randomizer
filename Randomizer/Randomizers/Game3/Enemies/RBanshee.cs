using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers.Game3.Enemies
{
    internal class RBanshee
    {
        public const string OPTION_REVERSE_SIDE_MIXIN = "REVERSESIDE";
        public const string OPTION_JUMPDIST_MIXIN = "JUMPDIST";
        public const string OPTION_IGNOREINVALIDPATHING_MIXIN = "IGNOREINVALIDPATHING";

        private static bool HasOneOptionSelected(RandomizationOption option)
        {
            if (option.HasSubOptionSelected(OPTION_JUMPDIST_MIXIN)) return true;
            if (option.HasSubOptionSelected(OPTION_REVERSE_SIDE_MIXIN)) return true;
            if (option.HasSubOptionSelected(OPTION_IGNOREINVALIDPATHING_MIXIN)) return true;
            return false;
        }

        public static bool RandomizeBanshee(GameTarget target, IMEPackage package, RandomizationOption option)
        {
            // Package doesn't contain the banshee
            if (package.FindExport("SFXGameContent.SFXPawn_Banshee") == null)
                return false;
            if (!HasOneOptionSelected(option))
                return false;

            // Todo: Implement script installs here

            return true;
        }
    }
}