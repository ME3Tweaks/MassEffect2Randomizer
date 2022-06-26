using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Microsoft.WindowsAPICodePack.Win32Native.NamedPipe;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Misc
{
    internal class REnemyPowers
    {
        /// <summary>
        /// Mapping of export to the loaded text value - this is loaded on init
        /// </summary>
        private static Dictionary<string, string> PatchMapping;


        /// <summary>
        /// Run once per file - determine if file has any AIs that need updated.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="option"></param>
        /// <param name="package"></param>
        public static bool PerFileAIChanger(GameTarget target, IMEPackage package, RandomizationOption option)
        {
            foreach (var v in PatchMapping.Keys)
            {
                var exp = package.FindExport(v);
                if (exp != null)
                {
                    ScriptTools.InstallScriptTextToExport(exp, PatchMapping[v], $"AI patch - {v}");
                }
            }

            return true;
        }

        public static bool Init(GameTarget target, RandomizationOption option)
        {
            // Inventory the AI patches
            PatchMapping = new Dictionary<string, string>();
            var scriptAssets = MERUtilities.ListEmbeddedAssets("Text", "Scripts.EnemyPowersAI");
            var cutOffPosition = scriptAssets[0].IndexOf("EnemyPowersAI") + "EnemyPowersAI".Length + 1;

            foreach (var asset in scriptAssets)
            {
                PatchMapping[Path.GetFileNameWithoutExtension(asset.Substring(cutOffPosition))] = new StreamReader(MERUtilities.GetEmbeddedAssetByFullPath(asset)).ReadToEnd();
            }

            // Install the script that randomly gives powers on SFXPowerManager load.
            ScriptTools.InstallScriptToPackage(target, "SFXGame.pcc", "SFXPowerManager.InitializePowerList", "PawnPowerRandomizer.uc", false, true);
            return true;
        }
    }
}
