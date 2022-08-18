using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;
using WinCopies.Util;

namespace Randomizer.Randomizers.Game3.Misc
{
    internal class RPawnEvasionowers
    {
        public const string OPTION_NEW = "NEW";
        public const string OPTION_VANILLA = "PATCHED";
        public const string OPTION_PORTED = "PORTED";

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
                    ScriptTools.InstallScriptTextToExport(exp, PatchMapping[v], $"AI patch - {v}", MERCaches.GlobalCommonLookupCache);
                }
            }

            return true;
        }

        public static bool Init(GameTarget target, RandomizationOption option)
        {
            if (!HasOneOptionSelected(option))
                return false;
            // Inventory the AI patches
            PatchMapping = new Dictionary<string, string>();
            var scriptAssets = MEREmbedded.ListEmbeddedAssets("Text", "Scripts.EnemyPowersAI");
            var cutOffPosition = scriptAssets[0].IndexOf("EnemyPowersAI") + "EnemyPowersAI".Length + 1;

            foreach (var asset in scriptAssets)
            {
                PatchMapping[Path.GetFileNameWithoutExtension(asset.Substring(cutOffPosition))] = new StreamReader(MEREmbedded.GetEmbeddedAsset("FULLPATH", asset, false, true)).ReadToEnd();
            }

            var sfxgame = RSFXGameGeneric.GetSFXGame(target);

            // Set percent to allow pawns to use powers.
            sfxgame.FindExport("Default__BioPawn").WriteProperty(new FloatProperty(0.75f, "m_fPowerUsePercent"));

            // Extract our custom powers.
            List<string> powerEntryPaths = new List<string>();
            List<string> extractedPackages = new List<string>();
            if (option.HasSubOptionSelected(OPTION_NEW))
            {
                extractedPackages.AddRange(MEREmbedded.ExtractEmbeddedBinaryFolder($"Packages.{target.Game}.PowerRandomizer.New"));
                powerEntryPaths.AddRange(MEREmbedded.GetEmbeddedTextAsset("Scripts.EnemyPowersSFXGame.NewPowersList.txt").SplitToLines());
            }
            if (option.HasSubOptionSelected(OPTION_PORTED))
            {
                extractedPackages.AddRange(MEREmbedded.ExtractEmbeddedBinaryFolder($"Packages.{target.Game}.PowerRandomizer.Ported"));
                powerEntryPaths.AddRange(MEREmbedded.GetEmbeddedTextAsset("Scripts.EnemyPowersSFXGame.PortedPowersList.txt").SplitToLines());
            }
            if (option.HasSubOptionSelected(OPTION_VANILLA))
            {
                // Vanilla includes patched since the end user wouldn't notice.
                // Vanilla items will start with SFXGameContent - these should not be added to seek free
                extractedPackages.AddRange(MEREmbedded.ExtractEmbeddedBinaryFolder($"Packages.{target.Game}.PowerRandomizer.Patched"));
                powerEntryPaths.AddRange(MEREmbedded.GetEmbeddedTextAsset("Scripts.EnemyPowersSFXGame.PatchedPowersList.txt").SplitToLines());
            }

            var seekFreeInfos = new List<SeekFreeInfo>();
            // Generate SeekFree entries.
            foreach (var p in extractedPackages)
            {
                bool found = false;
                var package = MEPackageHandler.OpenMEPackage(p);
                foreach (var path in powerEntryPaths)
                {
                    if (path.StartsWith("SFXGameContent"))
                        continue; // Don't add to seek free, it will already exist

                    if (package.FindExport(path) != null)
                    {
                        found = true;
                        seekFreeInfos.Add(new SeekFreeInfo()
                        {
                            SeekFreePackage = Path.GetFileNameWithoutExtension(p),
                            EntryPath = path
                        });
                        break;
                    }
                }

                if (!found)
                {
                    var mgcp = package.FindExport("MERGameContentPowers");
                    if (mgcp == null)
                    {
                        Debugger.Break();
                    }
                    else
                    {
                        foreach (var c in package.Exports.Where(x => x.idxLink == mgcp.UIndex && x.IsClass))
                        {
                            Debug.WriteLine(c.InstancedFullPath);
                        }
                    }
                    //Debug.WriteLine($"We have a power file that does not have an entry defined for it: {p}");
                }
            }

            foreach (var v in seekFreeInfos)
            {
                CoalescedHandler.AddDynamicLoadMappingEntry(v);
            }

            // Install the script that randomly gives powers on SFXPowerManager load.
            string initializePowerListScript = MEREmbedded.GetEmbeddedTextAsset("Scripts.EnemyPowersSFXGame.InitializePowerList.uc");

            initializePowerListScript = initializePowerListScript.Replace("%NUMPOWERSINPOOL%", powerEntryPaths.Count.ToString()); // Random Number

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < powerEntryPaths.Count; i++)
            {
                var pep = powerEntryPaths[i];
                sb.AppendLine($"case {i}:");
                sb.AppendLine($"powerSeekFreeName = \"{pep}\";");
                if (powerEntryPaths.Contains("Ammo"))
                {
                    sb.AppendLine("isAmmoPower = true;");
                }

                sb.AppendLine("break;");
            }


            // Install the switch statements
            initializePowerListScript = initializePowerListScript.Replace("%POWERSLIST%", sb.ToString());


            ScriptTools.InstallScriptTextToExport(sfxgame.FindExport("SFXPowerManager.InitializePowerList"), initializePowerListScript, "InitializePowerList", MERCaches.GlobalCommonLookupCache);

            // Add script to promote power usage.
            ScriptTools.InstallScriptToPackage(sfxgame, "SFXPowerCustomAction.ShouldUsePowerOnShields", "EnemyPowersSFXGame.ShouldUsePowerOnShields.uc", false, false, cache: MERCaches.GlobalCommonLookupCache);



#if DEBUG
            MERDebug.InstallDebugScript(sfxgame, "SFXAI_Core.ChooseAttack", false);
            MERDebug.InstallDebugScript(sfxgame, "SFXAI_Core.Attack", false);
#endif
            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        private static bool HasOneOptionSelected(RandomizationOption option)
        {
            // Add options here
            if (option.HasSubOptionSelected(OPTION_NEW)) return true;
            if (option.HasSubOptionSelected(OPTION_PORTED)) return true;
            if (option.HasSubOptionSelected(OPTION_VANILLA)) return true;
            return false;
        }
    }
}
