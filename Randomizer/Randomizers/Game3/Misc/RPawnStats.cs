using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Misc
{
    internal class RPawnStats
    {
        private const string HEALTH_OPTION = "PAWNSTAT_HEALTH";
        private const string MOVEMENTSPEED_OPTION = "PAWNSTAT_MOVEMENTSPEED";

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            var scriptText = MEREmbedded.GetEmbeddedTextAsset("Scripts.EnemyStats.SFXAI_Core_Initialize.uc");

            var sfxgame = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "SFXSame.pcc"));

            scriptText = SubOptionSubstitute(scriptText, option, MOVEMENTSPEED_OPTION, "COMBATSPEEDRANDOMIZER",
                "CombatSpeedRandomizer.uc");
            scriptText = SubOptionSubstitute(scriptText, option, HEALTH_OPTION, "HEALTHRANDOMIZER",
                "HealthRandomizer.uc");

            ScriptTools.InstallScriptTextToExport(sfxgame.FindExport("SFXAI_Core.Initialize"), scriptText,
                "SFXAI_Core.Initialize() Pawn Stat Randomizer", MERCaches.GlobalCommonLookupCache);
            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        /// <summary>
        /// Replaces part of the script with another using the information provided.
        /// </summary>
        /// <param name="currentScriptText"></param>
        /// <param name="option"></param>
        /// <param name="optionKey"></param>
        /// <param name="stringId"></param>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        private static string SubOptionSubstitute(string currentScriptText, RandomizationOption option, string optionKey, string stringId, string scriptName)
        {
            if (option.HasSubOptionSelected(optionKey))
            {
                var replString = MEREmbedded.GetEmbeddedTextAsset($"Scripts.EnemyStats.{scriptName}");
                return currentScriptText.Replace($"%{stringId}%", replString);
            }
         
            // Blank it out
            return currentScriptText.Replace($"%{stringId}%", "");
        }
    }
}
