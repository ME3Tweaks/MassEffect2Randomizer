using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game1.Misc
{
    class SFXGame
    {
        /// <summary>
        /// Turns on friendly collateral
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool TurnOnFriendlyFire(GameTarget target, RandomizationOption option)
        {
            var sfxgame = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(target, "SFXGame"));
            
            // Disable config reading of the property
            var friendlyCollateralProp = sfxgame.FindExport("BioActorBehavior.AllowFriendlyCollateral");
            var propFlags = friendlyCollateralProp.GetPropertyFlags();
            if (propFlags != null)
            {
                propFlags &= ~UnrealFlags.EPropertyFlags.Config;
                friendlyCollateralProp.SetPropertyFlags(propFlags.Value);
            }

            // Set property to true
            var bioActorBehavior = sfxgame.FindExport("Default__BioActorBehavior");
            bioActorBehavior.WriteProperty(new BoolProperty(true, "AllowFriendlyCollateral"));

            MERFileSystem.SavePackage(sfxgame);
            return true;
        }
    }
}
