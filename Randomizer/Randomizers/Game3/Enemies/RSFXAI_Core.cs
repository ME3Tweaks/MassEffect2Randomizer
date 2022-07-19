using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game3.ExportTypes;
using Randomizer.Randomizers.Game3.Levels;
using Randomizer.Randomizers.Shared;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Enemies
{
    internal class RSFXAI_Core
    {
        /// <summary>
        /// Makes enemies use the BerserkCommand instead of DefaultCommand
        /// </summary>
        /// <param name="target"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool SetupBerserkerAI(GameTarget target, RandomizationOption option)
        {
            var scriptText = MEREmbedded.GetEmbeddedTextAsset("Scripts.Enemies.BerserkRandomizer.uc");
            scriptText = scriptText.Replace("%BERSERKCHANCE%", option.SliderValue.ToString(CultureInfo.InvariantCulture));

            ScriptTools.InstallScriptTextToPackage(target, "SFXGame.pcc", "SFXAI_Core.BeginDefaultCommand", scriptText, "BerserkRandomizer.uc", true, null);
            return true;
        }
    }
}
