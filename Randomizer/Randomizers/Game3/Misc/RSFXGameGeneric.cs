using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.UnrealScript.Lexing;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Misc
{
    /// <summary>
    /// Contains generic misc randomizer for SFXGame
    /// </summary>
    internal class RSFXGameGeneric
    {
        /// <summary>
        /// Fetches SFXGame.pcc package
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static IMEPackage GetSFXGame(GameTarget target)
        {
            var sfxgame = MERFileSystem.GetPackageFile(target, "SFXGame.pcc");
            if (sfxgame != null && File.Exists(sfxgame))
            {
                return MEPackageHandler.OpenMEPackage(sfxgame);
            }

            return null;
        }

        /// <summary>
        /// Allows enemy weapons to penetrate cover and walls
        /// </summary>
        /// <param name="target"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool AllowEnemyWeaponPenetration(GameTarget target, RandomizationOption option)
        {
            ScriptTools.InstallScriptToPackage(target, "SFXGame.pcc", "SFXWeapon.CalcWeaponFire", "SFXGameGeneric.SFXWeapon.CalcWeaponFire.uc", false, true);
            return true;
        }
    }
}
