using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
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

        private static MERPackageCache globalFileLibCache;
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
                    ScriptTools.InstallScriptTextToExport(exp, PatchMapping[v], $"AI patch - {v}", globalFileLibCache);
                }
            }

            return true;
        }

        public static bool Init(GameTarget target, RandomizationOption option)
        {
            globalFileLibCache = new MERPackageCache(target);
            // Inventory the AI patches
            PatchMapping = new Dictionary<string, string>();
            var scriptAssets = MERUtilities.ListEmbeddedAssets("Text", "Scripts.EnemyPowersAI");
            var cutOffPosition = scriptAssets[0].IndexOf("EnemyPowersAI") + "EnemyPowersAI".Length + 1;

            foreach (var asset in scriptAssets)
            {
                PatchMapping[Path.GetFileNameWithoutExtension(asset.Substring(cutOffPosition))] = new StreamReader(MERUtilities.GetEmbeddedAssetByFullPath(asset)).ReadToEnd();
            }

            // Install the script that randomly gives powers on SFXPowerManager load.
            var sfxgame = ScriptTools.InstallScriptToPackage(target, "SFXGame.pcc", "SFXPowerManager.InitializePowerList", "PawnPowerRandomizer.uc", false, false, cache: MERCaches.LookupCache);
            ScriptTools.InstallScriptToPackage(sfxgame, "SFXPowerCustomAction.ShouldUsePowerOnShields", "EnemyPowersSFXGame.ShouldUsePowerOnShields.uc", false, false, cache: MERCaches.LookupCache);

            // Set percent to allow pawns to use powers.
            sfxgame.FindExport("Default__BioPawn").WriteProperty(new FloatProperty(0.75f, "m_fPowerUsePercent"));
            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public static void CleanupClass()
        {
            globalFileLibCache.Dispose();
            globalFileLibCache = null;
        }
    }
}
