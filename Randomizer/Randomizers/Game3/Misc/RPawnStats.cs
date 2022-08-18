using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;
using WinCopies.Util;

namespace Randomizer.Randomizers.Game3.Misc
{
    internal class RPawnStats
    {
        internal const string HEALTH_OPTION = "PAWNSTAT_HEALTH";
        internal const string MOVEMENTSPEED_OPTION = "PAWNSTAT_MOVEMENTSPEED";
        internal const string SHIELD_OPTION = "PAWNSTAT_SHIELD";
        internal const string EVASION_OPTION = "PAWNCUSTACTION_EVASION";
        internal const string MELEE_OPTION = "PAWNCUSTACTION_MELEE";

        /// <summary>
        /// If dynamic resources have been prepared
        /// </summary>
        private static bool Prepared;

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            if (!HasOneSubOptionSelected(option))
                return false; // Don't do any work if nothing is selected.

            var scriptText = MEREmbedded.GetEmbeddedTextAsset("Scripts.EnemyStats.SFXAI_Core_Initialize.uc");
            var sfxgame = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "SFXGame.pcc"));

            scriptText = SubOptionSubstitute(scriptText, option, MOVEMENTSPEED_OPTION, "COMBATSPEEDRANDOMIZER",
                "CombatSpeedRandomizer.uc");
            scriptText = SubOptionSubstitute(scriptText, option, HEALTH_OPTION, "HEALTHRANDOMIZER",
                "HealthRandomizer.uc");
            scriptText = SubOptionSubstitute(scriptText, option, SHIELD_OPTION, "SHIELDRANDOMIZER",
                "ShieldRandomizer.uc");

            if (option.HasSubOptionSelected(EVASION_OPTION) || option.HasSubOptionSelected(MELEE_OPTION))
            {
                PrepareCustomActions(target);
                if (option.HasSubOptionSelected(MELEE_OPTION))
                {
                    PatchMeleeToSyncPartner(sfxgame);
                }
            }

            scriptText = SubOptionSubstitute(scriptText, option, EVASION_OPTION, "EVASIONRANDOMIZER",
                generateCAAssignmentScriptText, 54, 4); // 54 is first CA evasion
            scriptText = SubOptionSubstitute(scriptText, option, MELEE_OPTION, "MELEERANDOMIZER",
                generateCAAssignmentScriptText, 133, 3); // 133 is first CA melee

            ScriptTools.InstallScriptTextToExport(sfxgame.FindExport("SFXAI_Core.Initialize"), scriptText,
                "SFXAI_Core.Initialize() Pawn Stat Randomizer", MERCaches.GlobalCommonLookupCache);

            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        /// <summary>
        /// Makes it so melee attacks pass in the sync partner, so it can be used to sync. this allows things like player heavy melee to be used
        /// </summary>
        /// <param name="sfxgame"></param>
        private static void PatchMeleeToSyncPartner(IMEPackage sfxgame)
        {
            var script = "public function DoMeleeAttack() { LastCombatActionTime = WorldInfo.GameTimeSeconds; MyBP.StartCustomAction(133, Pawn(FireTarget)); }";
            ScriptTools.InstallScriptTextToExport(sfxgame.FindExport("SFXAI_Core.DoMeleeAttack"),script,"StandardMeleeSyncPatch", null);
        }

        private static string generateCAAssignmentScriptText(int startingCA, int numConsecutiveCAs)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < numConsecutiveCAs; i++)
            {
                var caIndex = i + startingCA;
                sb.AppendLine($"if (MyBP.CustomActionClasses[{caIndex}] != None)");
                sb.AppendLine("{");
                var caArray = getArrayForCA(caIndex);
                sb.AppendLine($"randIndex = Rand({caArray.Count});");
                for (int j = 0; j < caArray.Count; j++)
                {
                    // Line for if statement
                    if (j > 0)
                        sb.Append("else "); // Else if
                    sb.AppendLine($"if (randIndex == {j}) {{MyBP.CustomActionClasses[{caIndex}] = Class(Class'SFXEngine'.static.LoadSeekFreeObjectBlocking(\"{caArray[j].EntryPath}\", Class'Class'));}}");
                }

                sb.AppendLine("}");
            }
            Debug.WriteLine(sb.ToString());
            return sb.ToString();
        }

        private static List<SeekFreeInfo> getArrayForCA(int caIndex)
        {
            if (caIndex == 54) return _evadeLeftActions;
            if (caIndex == 55) return _evadeRightActions;
            if (caIndex == 56) return _evadeForwardActions;
            if (caIndex == 57) return _evadeBackwardsActions;
            if (caIndex == 133) return _punchActions; // Standard Melee
            if (caIndex == 134) return _punchActions; // Standard Melee Alt
            if (caIndex == 135) return _syncMeleeActions; // Sync actions

            return null;
        }


        private static List<SeekFreeInfo> _evadeRightActions = new();
        private static List<SeekFreeInfo> _evadeLeftActions = new();
        private static List<SeekFreeInfo> _evadeForwardActions = new();
        private static List<SeekFreeInfo> _evadeBackwardsActions = new();
        private static List<SeekFreeInfo> _punchActions = new();
        private static List<SeekFreeInfo> _syncMeleeActions = new(); // Actions usable for sync melee (banshee, brute, etc)

        private static List<SeekFreeInfo> _deathActions = new(); // Not used in MER, but in kismet
        private static List<SeekFreeInfo> _climbUpActions = new(); // Not used in MER, but in kismet
        private static List<SeekFreeInfo> _climbDownActions = new(); // Not used in MER, but in kismet


        /// <summary>
        /// Initialization of randomizer - inventory actions we can use
        /// </summary>
        /// <param name="target"></param>
        internal static void PrepareCustomActions(GameTarget target)
        {
            if (Prepared)
                return;
            // Extract custom actions packages
            var assets = MEREmbedded.ListEmbeddedAssets("Binary", $"Packages.{target.Game}.CustomActions", false);
            foreach (var asset in assets)
            {
                var filename = MEREmbedded.GetFilenameFromAssetName(asset);
                var assetStream = MEREmbedded.GetEmbeddedPackage(target.Game, $"CustomActions.{filename}");
                var customActionPackage = MEPackageHandler.OpenMEPackageFromStream(assetStream);

                SeekFreeInfo info = null;
                foreach (var e in customActionPackage.Exports.Where(x => x.ClassName == "Class"))
                {
                    if (e.HasParent)
                    {
                        switch (e.ParentName)
                        {
                            case "EvadeLeftActions":
                                info = new SeekFreeInfo(e, filename);
                                _evadeLeftActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "EvadeRightActions":
                                info = new SeekFreeInfo(e, filename);
                                _evadeRightActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "EvadeForwardsActions":
                                info = new SeekFreeInfo(e, filename);
                                _evadeForwardActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "EvadeBackwardsActions":
                                info = new SeekFreeInfo(e, filename);
                                _evadeBackwardsActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "PunchActions":
                                info = new SeekFreeInfo(e, filename);
                                _punchActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "SyncMeleeActions":
                                info = new SeekFreeInfo(e, filename);
                                _syncMeleeActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "ClimbUpActions":
                                info = new SeekFreeInfo(e, filename);
                                _climbUpActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "ClimbDownActions":
                                info = new SeekFreeInfo(e, filename);
                                _climbDownActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                            case "DeathActions":
                                info = new SeekFreeInfo(e, filename);
                                _deathActions.Add(info);
                                CoalescedHandler.AddDynamicLoadMappingEntry(info);
                                break;
                        }
                    }
                }

                // This will essentially count as an extraction. Directly copying would be faster technically...
                MERFileSystem.SavePackage(customActionPackage, true, forcedFileName: filename);
            }

            Prepared = true;
        }

        private static bool HasOneSubOptionSelected(RandomizationOption option)
        {
            if (option.HasSubOptionSelected(HEALTH_OPTION)) return true;
            if (option.HasSubOptionSelected(MOVEMENTSPEED_OPTION)) return true;
            if (option.HasSubOptionSelected(SHIELD_OPTION)) return true;
            if (option.HasSubOptionSelected(EVASION_OPTION)) return true;
            if (option.HasSubOptionSelected(MELEE_OPTION)) return true;
            return false;
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

        /// <summary>
        /// Replaces part of the script with another using the information provided.
        /// </summary>
        /// <param name="currentScriptText"></param>
        /// <param name="option"></param>
        /// <param name="optionKey"></param>
        /// <param name="stringId"></param>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        private static string SubOptionSubstitute(string currentScriptText, RandomizationOption option, string optionKey, string stringId, Func<int, int, string> getTextDelegate, int startingCA, int numConsecutiveCAs)
        {
            if (option.HasSubOptionSelected(optionKey))
            {
                return currentScriptText.Replace($"%{stringId}%", getTextDelegate(startingCA, numConsecutiveCAs));
            }

            // Blank it out
            return currentScriptText.Replace($"%{stringId}%", "");
        }

        public static void ResetClass()
        {
            _evadeBackwardsActions.Clear();
            _evadeForwardActions.Clear();
            _evadeRightActions.Clear();
            _evadeLeftActions.Clear();
            _punchActions.Clear();
            _syncMeleeActions.Clear();
            _climbDownActions.Clear();
            _climbUpActions.Clear();
            _deathActions.Clear();
            Prepared = false;
        }
    }
}
