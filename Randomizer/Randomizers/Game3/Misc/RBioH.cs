using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game3.Misc
{
    internal class RBioH
    {
        private static List<string> tempNameChanges;

        //
        private static string[] MERSpecialHenchFilesCombat =
        {
            "BioH_Anderson_00.pcc"
        };

        public static bool RandomizeBioH(GameTarget target, RandomizationOption option)
        {
            //var henchDecooks = new List<ObjectDecookInfo>()
            //{
            //    new ObjectDecookInfo()
            //    {
            //        SourceFileName = "BioD_ProEar_200.pcc",
            //        SeekFreeInfo = new SeekFreeInfo()
            //        {
            //            EntryPath = "SFXGameContent.SFXPawn_Anderson",
            //            SeekFreePackage = "SFXPawn_Anderson"
            //        }
            //    }
            //};
            //MERDecooker.DecookObjectsToPackages(target, option, henchDecooks, "Decooking henchmen", true);

            option.CurrentOperation = "Randomizing henchmen";
            tempNameChanges = new List<string>();

            // Extract custom henchmen files
            var merCustomHenchmenFiles = MEREmbedded.ExtractEmbeddedBinaryFolder($"Binary.Packages.{target.Game}.Henchmen");

            // Inventory BioH
            var biohFiles = MERFileSystem.LoadedFiles.Where(x =>
                x.Key.StartsWith("BioH", StringComparison.CurrentCultureIgnoreCase)
                && !x.Key.Contains("CitSim", StringComparison.InvariantCultureIgnoreCase)
                && !x.Key.Contains("Exp3", StringComparison.InvariantCultureIgnoreCase)
                && !x.Key.Contains("Nyreen", StringComparison.InvariantCultureIgnoreCase) // Sorry but bioware messed up your files good
                && !x.Key.Equals("BioH_SelectGUI.pcc", StringComparison.InvariantCultureIgnoreCase)
                && !MERSpecialHenchFilesCombat.Contains(x.Key, StringComparer.InvariantCultureIgnoreCase)
                ).ToList();



            // Get No-Loc files.
            var foundOutfits = biohFiles.Where(x => x.Key.GetUnrealLocalization() == MELocalization.None).ToList();

            // Split to combat/non-combat
            var foundOutfitsCombat = foundOutfits.Where(x => !x.Key.Contains("Explore", StringComparison.InvariantCultureIgnoreCase) && !x.Key.Contains("_NC", StringComparison.InvariantCultureIgnoreCase)).ToList();
            var foundOutfitsExplore = foundOutfits.Where(x => x.Key.Contains("Explore", StringComparison.InvariantCultureIgnoreCase) && !x.Key.Contains("_NC", StringComparison.InvariantCultureIgnoreCase)).ToList();
            var foundOutfitsNonCombat = foundOutfits.Where(x => x.Key.Contains("_NC", StringComparison.InvariantCultureIgnoreCase)).ToList();

            var specialOutfitsCombat = MERFileSystem.LoadedFiles.Where(x => MERSpecialHenchFilesCombat.Contains(x.Key, StringComparer.InvariantCultureIgnoreCase)).ToList();


            var newOutfitsCombat = new List<KeyValuePair<string, string>>();
            var newOutfitsExplore = new List<KeyValuePair<string, string>>();
            var newOutfitsNonCombat = new List<KeyValuePair<string, string>>();

            // Key -> Key for found -> new
            var remappingCombat = new Dictionary<string, string>();
            var remappingExplore = new Dictionary<string, string>();
            var remappingNonCombat = new Dictionary<string, string>();

            // The remapped keys 
            List<string> combatKeys = foundOutfitsCombat.Select(x => x.Key).ToList();
            List<string> exploreKeys = foundOutfitsExplore.Select(x => x.Key).ToList();
            List<string> nonCombatKeys = foundOutfitsNonCombat.Select(x => x.Key).ToList();

            // Add MER special outfits ONLY to the found outfits, which are the source files
            foundOutfitsCombat.Add(new KeyValuePair<string, string>("BioH_Anderson_00.pcc", Path.Combine(MERFileSystem.DLCModCookedPath, "BioH_Anderson_00.pcc")));
            // Add Citadel Armax files only to found outfits
            // Inventory BioH
            var citsimBioHFiles = MERFileSystem.LoadedFiles.Where(x =>
                x.Key.StartsWith("BioH", StringComparison.CurrentCultureIgnoreCase)
                && x.Key.Contains("CitSim", StringComparison.InvariantCultureIgnoreCase)
                && !x.Key.Contains("Exp3", StringComparison.InvariantCultureIgnoreCase)
                && !x.Key.Contains("Wrex", StringComparison.InvariantCultureIgnoreCase) // sorry matey but you use conditional
                && !x.Key.Contains("Nyreen", StringComparison.InvariantCultureIgnoreCase) // Sorry but bioware messed up your files good
                && !x.Key.Equals("BioH_SelectGUI.pcc", StringComparison.InvariantCultureIgnoreCase)
                && !MERSpecialHenchFilesCombat.Contains(x.Key, StringComparer.InvariantCultureIgnoreCase)
                && x.Key.GetUnrealLocalization() == MELocalization.None
            ).ToList();
            foundOutfitsCombat.AddRange(citsimBioHFiles);
            foundOutfitsCombat.Add(new KeyValuePair<string, string>("BioH_Anderson_00.pcc", Path.Combine(MERFileSystem.DLCModCookedPath, "BioH_Anderson_00.pcc")));



            foundOutfitsCombat.Shuffle();

            combatKeys.Shuffle();
            while (combatKeys.Any())
            {
                var key = combatKeys.PullFirstItem();
                remappingCombat[foundOutfitsCombat.PullFirstItem().Key] = key;
            }

            exploreKeys.Shuffle();
            while (exploreKeys.Any())
            {
                var key = exploreKeys.PullFirstItem();
                remappingExplore[foundOutfitsExplore.PullFirstItem().Key] = key;
            }

            nonCombatKeys.Shuffle();
            while (nonCombatKeys.Any())
            {
                var key = nonCombatKeys.PullFirstItem();
                remappingNonCombat[foundOutfitsNonCombat.PullFirstItem().Key] = key;
            }

            option.ProgressValue = 0;
            option.ProgressMax = remappingCombat.Count + remappingNonCombat.Count + remappingExplore.Count;
            option.ProgressIndeterminate = false;
            foreach (var v in remappingCombat)
            {
                SwapHenchFiles(target, v.Key, v.Value, true);
                option.ProgressValue++;
            }

            foreach (var v in remappingExplore)
            {
                SwapHenchFiles(target, v.Key, v.Value, true);
                option.ProgressValue++;
            }

            foreach (var v in remappingNonCombat)
            {
                SwapHenchFiles(target, v.Key, v.Value, false);
                option.ProgressValue++;
            }

            // We have to finalize name changes so lookups in MERFS don't return our modified packages
            FinalizeNameChanges();
            return true;
        }

        private static void FinalizeNameChanges()
        {
            foreach (var v in tempNameChanges)
            {
                var outDir = Directory.GetParent(v).FullName;
                var finalNameBase = Path.GetFileNameWithoutExtension(v);
                finalNameBase = finalNameBase.Substring(0, finalNameBase.Length - 4); // remove _TMP
                var finalName = Path.Combine(outDir, finalNameBase + ".pcc");
                File.Move(v, finalName);
            }
        }

        /// <summary>
        /// Swaps a BioH file and updates it to work in place of the original. Saves to a TMP File which is renamed after all swaps have been completed to avoid MERFS loading the wrong files
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sourceFile"></param>
        /// <param name="destFile"></param>
        /// <param name="requiresHandshakeUpdate"></param>
        private static void SwapHenchFiles(GameTarget target, string sourceFile, string destFile, bool requiresHandshakeUpdate)
        {
            Debug.WriteLine($"NonCombat file {sourceFile} -> {destFile}");
            var newhenchname = Path.GetFileNameWithoutExtension(destFile).Substring(5); // BioH_
            newhenchname = newhenchname.Substring(0, newhenchname.IndexOf("_")).ToLower();

            Debug.WriteLine($"New henchname: {newhenchname}");

            var destFileTempName = Path.Combine(MERFileSystem.DLCModCookedPath,
                Path.GetFileNameWithoutExtension(destFile) + "_TMP.pcc");
            var henchPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, sourceFile));


            var levelActors = henchPackage.GetLevelBinary().Actors.Where(x => x > 0)
                .Select(x => henchPackage.GetUExport(x)).ToList();

            var pawn = levelActors.FirstOrDefault(x => x.ClassName == "SFXStuntActor"); // NonCombat
            pawn ??= levelActors.FirstOrDefault(x => x.IsA("SFXPawn")); // Combat, Explore

            // Update the tag
            pawn.WriteProperty(new NameProperty($"hench_{newhenchname}", "Tag"));
            var sfxUseModule = henchPackage.Exports.FirstOrDefault(x => x.idxLink == pawn.UIndex && x.ClassName == "SFXSimpleUseModule");
            if (sfxUseModule != null)
            {
                sfxUseModule.WriteProperty(new BoolProperty(true, "m_bTargetable"));
            }
            // We must update the kismet so it properly does a handshake

            // 1. Change the name for polling and remote event.
            int nameIdx = 0;
            for (int i = 0; i < henchPackage.Names.Count; i++)
            {
                if (henchPackage.Names[i].StartsWith("RE_Poll_BioH_", StringComparison.InvariantCultureIgnoreCase))
                {
                    henchPackage.replaceName(i, $"RE_Poll_BioH_{newhenchname.UpperFirst()}_Visible");
                }
                if (henchPackage.Names[i].StartsWith("re_BioH_", StringComparison.InvariantCultureIgnoreCase))
                {
                    henchPackage.replaceName(i, $"re_BioH_{newhenchname.UpperFirst()}_Visible");
                }
            }

            // 2. Update the plot conditional for inParty to match.
            var pmCheckState = henchPackage.Exports.FirstOrDefault(x => x.ClassName == "BioSeqAct_PMCheckState");
            if (pmCheckState != null)
            {
                pmCheckState.WriteProperty(new IntProperty(GetHenchInPartyIndex(newhenchname), "m_nIndex"));
            }
            else
            {
                pmCheckState ??= henchPackage.Exports.FirstOrDefault(x => x.ClassName == "BioSeqAct_PMCheckConditional"); // WREX
                if (pmCheckState != null)
                {
                    pmCheckState.WriteProperty(new IntProperty(GetHenchInPartyConditionalIndex(newhenchname), "m_nIndex"));
                }
                else
                {
                    MERLog.Information($"Skipping non-conditionalized squad member: {sourceFile}");
                }
            }

            // 3. Update the handshake transition
            pmCheckState = henchPackage.Exports.FirstOrDefault(x => x.ClassName == "BioSeqAct_PMExecuteTransition");
            //pmCheckState ??= henchPackage.Exports.FirstOrDefault(x => x.ClassName == "BioSeqAct_PMCheckConditional"); // WREX
            if (pmCheckState != null)
                pmCheckState.WriteProperty(new IntProperty(GetHenchInPartyIndexHandshake(newhenchname), "m_nIndex"));
            else
                MERLog.Information($"Skipping non-conditionalized transition squad member: {sourceFile}");

            // 4. Handle special Nyreen and Aria cases
            // The incoming package will have the actual filename.
            if (henchPackage.FileNameNoExtension.StartsWith("BioH_Nyreen") || henchPackage.FileNameNoExtension.StartsWith("BioH_Aria"))
            {
                // Nyreen and Aria uses a 'Set Bool' for some reason.
                pmCheckState = henchPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.BioSeqVar_StoryManagerBool_1");
                if (pmCheckState != null)
                    pmCheckState.WriteProperty(new IntProperty(GetHenchInPartyIndexHandshake(newhenchname), "m_nIndex"));
            }


            // 5. Update the FaceFX asset so people like garrus can move their mouth.
            var destPawnBeingReplacedPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, destFile));
            var faceFx = destPawnBeingReplacedPackage.Exports.Where(x => x.ClassName == "FaceFXAsset").ToList();
            if (faceFx.Count > 1)
                faceFx.RemoveAt(0); // Wrex.

            EntryExporter.ExportExportToPackage(faceFx[0], henchPackage, out var newFaceFX);

            var faceFxLinkCandidates =
                henchPackage.Exports.Where(x => x.ClassName == "SFXModule_Conversation").ToList();
            foreach (var faceFxLinkCandidate in faceFxLinkCandidates)
            {
                var prop = faceFxLinkCandidate.GetProperty<ObjectProperty>("m_pDefaultFaceFXAsset");
                if (prop != null)
                {
                    prop.Value = newFaceFX.UIndex;
                    faceFxLinkCandidate.WriteProperty(prop);
                }
            }

            tempNameChanges.Add(destFileTempName);
            MERFileSystem.SavePackage(henchPackage, forceSave: true, forcedFileName: destFileTempName);

            // 6. Rename localizations as they contain things like the FaceFX stuff.
            var locFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith(Path.GetFileNameWithoutExtension(sourceFile) + "_LOC")).ToList();

            foreach (var locFile in locFiles)
            {
                var destLocFile = Path.Combine(MERFileSystem.DLCModCookedPath, $"{Path.GetFileNameWithoutExtension(destFile)}_LOC_{locFile.GetUnrealLocalization()}_TMP.pcc");
                Debug.WriteLine($"Moving LOC file {MERFileSystem.LoadedFiles[locFile]} -> {destLocFile}");
                File.Copy(MERFileSystem.LoadedFiles[locFile], destLocFile);
                tempNameChanges.Add(destLocFile);
            }
        }

        private static int GetHenchInPartyConditionalIndex(string newhenchname)
        {
            return newhenchname switch
            {
                "liara" => 3,
                "kaidan" => 4,
                "ashley" => 5,
                "garrus" => 6,
                "edi" => 7,
                "prothean" => 8,
                "marine" => 32,
                "tali" => 184,
                "aria" => 2758,
                "nyreen" => 2760,
                "wrex" => 3325, // THIS IS A CONDITIONAL!
                _ => throw new Exception($"Invalid party member {newhenchname}")
            };
        }

        private static int GetHenchInPartyIndex(string newhenchname)
        {
            return newhenchname switch
            {
                "liara" => 17663,
                "kaidan" => 17664,
                "ashley" => 17665,
                "garrus" => 17666,
                "edi" => 17667,
                "prothean" => 17668,
                "marine" => 17692,
                "tali" => 17836,
                "aria" => 22834,
                "nyreen" => 22835,
                "wrex" => 3325, // THIS IS A CONDITIONAL!
                _ => throw new Exception($"Invalid party member {newhenchname}")
            };
        }

        private static int GetHenchInPartyIndexHandshake(string newhenchname)
        {
            return newhenchname switch
            {
                "liara" => 19,
                "kaidan" => 20,
                "ashley" => 21,
                "garrus" => 22,
                "edi" => 23,
                "prothean" => 24,
                "marine" => 68,
                "tali" => 249,
                "aria" => 4960,
                "nyreen" => 4961,
                "wrex" => 6049, // is this right?
                _ => throw new Exception($"Invalid party member {newhenchname}")
            };
        }

        public static void ResetClass()
        {
            tempNameChanges = null;
        }
    }
}
