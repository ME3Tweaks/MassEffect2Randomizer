using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Dialogue;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Objects;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.ExportTypes;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game2.Levels
{
    public static class Citadel
    {

        #region ENDORSEMENTS
        // Endorsement line is 2.1ish seconds long.
        // BOTH LISTS MUST BE THE SAME LENGTH AND HAVE IDENTICAL TLK STRS!
        private static List<(string packageName, string ifp)> EndorsementCandidatesFemale = new()
        {
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_rpplot_giver5_d_S.en_us_player_f_cithub_rpplot_giver5_d_00322109_f_wav"), // Sorry. Don't remember, don't care.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_rpplot_giver5_d_S.en_us_player_f_cithub_rpplot_giver5_d_00290444_f_wav"), // I knew this was a mistake
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_rpplot_giver5_d_S.en_us_player_f_cithub_rpplot_giver5_d_00322128_f_wav"), // People die. I don't have time for this crap.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_vend_sniper_d_S.en_us_player_f_cithub_vend_sniper_d_00253670_f_wav"), // Geth, pirates, mercenary scum

            ("BioD_CitHub_240Vendors_LOC_INT.pcc", "cithub_vend_decor_d_S.en_us_player_f_cithub_vend_decor_d_00252286_f_wav"), // Cheap touristy crap
            ("BioD_CitHub_240Vendors_LOC_INT.pcc", "cithub_vend_decor_d_S.en_us_player_f_cithub_vend_decor_d_00252288_f_wav"), // HEY EVERYONE THIS STORE DISCRIMINATES AGAINST THE POOR

            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", "procer_vixen_briefing_d_S.en_us_player_f_procer_vixen_briefing_d_00218765_f_wav"), // Are you naturally this bitchy or is it just me
            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", "procer_leading_briefing_d_S.en_us_player_f_procer_leading_briefing_d_00263469_f_wav"), // Cerberus gave me my body back

            ("BioD_Nor_101Cockpit_LOC_INT.pcc", "norjk_starter_h_S.en_us_player_f_norjk_starter_h_00275990_f_wav" ), // What is this high school

            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", "omgmwl_buy_drinks_d_S.en_us_player_f_omgmwl_buy_drinks_d_00236782_f_wav"), // Free drinks on me
            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", "omgmwl_daughter01_d_S.en_us_player_f_omgmwl_daughter01_d_00234320_f_wav"), // Sectors of space

            ("BioD_PrsCvA_104bLastCheckIn_LOC_INT.pcc", "prscva_double_cross_d_S.en_us_player_f_prscva_double_cross_d_00186607_f_wav"), // Go to hell

            ("BioD_KroHub_110GruntLoyalty_LOC_INT.pcc", "krokgl_shamantent_d_S.en_us_player_f_krokgl_shamantent_d_00270395_f_wav"), // I don't need to listen to any more of this
        };

        private static List<(string packageName, string ifp)> EndorsementCandidatesMale = new()
        {
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_rpplot_giver5_d_S.en_us_player_m_cithub_rpplot_giver5_d_00322109_m_wav"), // Sorry. Don't remember, don't care.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_rpplot_giver5_d_S.en_us_player_m_cithub_rpplot_giver5_d_00290444_m_wav"), // I knew this was a mistake
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_rpplot_giver5_d_S.en_us_player_m_cithub_rpplot_giver5_d_00322128_m_wav"), // People die. I don't have time for this crap.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_vend_sniper_d_S.en_us_cithub_vend_sniper_cithub_vend_sniper_d_00252897_m_wav"), // Geth, pirates, mercenary scum

            ("BioD_CitHub_240Vendors_LOC_INT.pcc", "cithub_vend_decor_d_S.en_us_player_m_cithub_vend_decor_d_00252286_m_wav"), // Cheap touristy crap
            ("BioD_CitHub_240Vendors_LOC_INT.pcc", "cithub_vend_decor_d_S.en_us_player_m_cithub_vend_decor_d_00252288_m_wav"), // HEY EVERYONE THIS STORE DISCRIMINATES AGAINST THE POOR

            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", "procer_vixen_briefing_d_S.en_us_player_m_procer_vixen_briefing_d_00218765_m_wav"), // Are you naturally this bitchy or is it just me
            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", "procer_leading_briefing_d_S.en_us_player_m_procer_leading_briefing_d_00263469_m_wav"), // Cerberus gave me my body back

            ("BioD_Nor_101Cockpit_LOC_INT.pcc", "norjk_starter_h_S.en_us_player_m_norjk_starter_h_00275990_m_wav" ), // What is this high school

            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", "omgmwl_buy_drinks_d_S.en_us_player_m_omgmwl_buy_drinks_d_00236782_m_wav"), // Free drinks on me
            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", "omgmwl_daughter01_d_S.en_us_player_m_omgmwl_daughter01_d_00234320_m_wav"), // Sectors of space

            ("BioD_PrsCvA_104bLastCheckIn_LOC_INT.pcc", "prscva_double_cross_d_S.en_us_player_m_prscva_double_cross_d_00186607_m_wav"), // Go to hell

            ("BioD_KroHub_110GruntLoyalty_LOC_INT.pcc","krokgl_shamantent_d_S.en_us_player_m_krokgl_shamantent_d_00270395_m_wav"), // I don't need to listen to any more of this

        };



        private static void RandomizeEndorsements(GameTarget target)
        {
            var cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, false);
            List<int> pickedIndices = new List<int>();

            RandomizeEndorsementLine(@"BioD_CitHub_240Vendors_LOC_INT.pcc", "cithub_vend_decor_d_S.en_us_player_m_cithub_vend_decor_d_00252280_m_wav", "cithub_vend_decor_d_S.en_us_player_f_cithub_vend_decor_d_00252280_f_wav", "cithub_vend_decor_d_D.cithub_vend_decor_d_dlg", 7, cache, pickedIndices); //sirta, i think?
            RandomizeEndorsementLine(@"BioD_CitHub_300UpperWing_LOC_INT.pcc", "cithub_vend_sniper_d_S.en_us_player_m_cithub_vend_sniper_d_00253684_m_wav", "cithub_vend_sniper_d_S.en_us_player_f_cithub_vend_sniper_d_00253684_f_wav", "cithub_vend_sniper_d_d.cithub_vend_sniper_d_dlg", 18, cache, pickedIndices); // gun turian
            RandomizeEndorsementLine(@"BioD_CitHub_420LowerSouth_LOC_INT.pcc", "cithub_vend_biotic_d_S.en_us_player_m_cithub_vend_biotic_d_00249952_m_wav", "cithub_vend_biotic_d_S.en_us_player_f_cithub_vend_biotic_d_00249952_f_wav", "cithub_vend_biotic_d_D.cithub_vend_biotic_d_dlg", 8, cache, pickedIndices); //biotic
            RandomizeEndorsementLine(@"BioD_CitHub_420LowerSouth_LOC_INT.pcc", "cithub_vend_omni_d_S.en_us_player_m_cithub_vend_omni_d_00253591_m_wav", "cithub_vend_omni_d_S.en_us_player_f_cithub_vend_omni_d_00253591_f_wav", "cithub_vend_omni_d_D.cithub_vend_omni_d_dlg", 11, cache, pickedIndices); //omni
        }

        private static void RandomizeEndorsementLine(string packageName, string maleConvIFP, string femaleConvIFP, string conversationIFP, int replyIdx, MERPackageCache cache, List<int> pickedIndices)
        {
            var package = cache.GetCachedPackage(packageName);

            var conversation = package.FindExport(conversationIFP);
            var replies = conversation.GetProperty<ArrayProperty<StructProperty>>("m_ReplyList");

            var endorsementLineMale = package.FindExport(maleConvIFP);
            var endorsementLineFemale = package.FindExport(femaleConvIFP);

            var replacementIndex = EndorsementCandidatesMale.RandomIndex();
            while (pickedIndices.Contains(replacementIndex))
            {
                replacementIndex = EndorsementCandidatesMale.RandomIndex(); // repick
            }

            pickedIndices.Add(replacementIndex); // do not use this line again
            var maleReplacement = EndorsementCandidatesMale[replacementIndex];
            var femaleReplacement = EndorsementCandidatesFemale[replacementIndex];

            // Male
            var sourcePackage = cache.GetCachedPackage(maleReplacement.packageName);
            var sourceExport = sourcePackage.FindExport(maleReplacement.ifp);
            WwiseTools.RepointWwiseStream(sourceExport, endorsementLineMale);

            // Female
            sourcePackage = cache.GetCachedPackage(femaleReplacement.packageName);
            sourceExport = sourcePackage.FindExport(femaleReplacement.ifp);
            WwiseTools.RepointWwiseStream(sourceExport, endorsementLineFemale);

            // Update the TLK reference. Not sure how this works with FaceFX honestly (or if it does at all...)

            var newTlkId = WwiseTools.ExtractTLKIdFromExportName(sourceExport);
            if (newTlkId > 0)
            {
                // Conversations srTExt seems to be used to tell what line to play. Not sure how.
                // We can't change this or it will break the audio

                var replyStruct = replies[replyIdx];
                var uiStr = replyStruct.Properties.GetProp<StringRefProperty>("srText");
                var oldTlkId = uiStr.Value;
                //uiStr.Value = newTlkId;
                //conversation.WriteProperty(replies);

                // We need to also update the FaceFX IDs to point to this new line, I guess?
                //UpdateFaceFXIDs(conversation, oldTlkId.ToString(), newTlkId.ToString());

                // Just overwrite the TLK string instead
                TLKBuilder.ReplaceStringByRepoint(oldTlkId, newTlkId);
            }

            MERFileSystem.SavePackage(package);
        }

        // Works, but doesn't actually due to how FaceFX looks crap up

        //private static void UpdateFaceFXIDs(ExportEntry conversation, string oldTlkId, string newTlkId)
        //{
        //    var mFaceSets = conversation.GetProperty<ArrayProperty<ObjectProperty>>("m_aMaleFaceSets");
        //    var fFaceSets = conversation.GetProperty<ArrayProperty<ObjectProperty>>("m_aFemaleFaceSets");

        //    var allFaceSets = mFaceSets.Select(x => x.ResolveToEntry(conversation.FileRef) as ExportEntry).ToList();
        //    allFaceSets.AddRange(fFaceSets.Select(x => x.ResolveToEntry(conversation.FileRef) as ExportEntry));

        //    foreach (var fs in allFaceSets)
        //    {
        //        UpdateSingleFaceFXLine(fs, oldTlkId, newTlkId); //They're strings in the code
        //    }
        //}

        //private static void UpdateSingleFaceFXLine(ExportEntry animSet, string oldId, string newTlkId)
        //{
        //    var facefxas = ObjectBinary.From<FaceFXAnimSet>(animSet);
        //    foreach (var fxl in facefxas.Lines)
        //    {
        //        if (fxl.ID == oldId)
        //        {
        //            // No idea if this matters at all...
        //            fxl.ID = newTlkId;
        //        }
        //    }
        //}

        #endregion

        internal static bool PerformRandomization(GameTarget target, RandomizationOption notUsed)
        {
            //RandomizeEndorsements(target);
            //RandomizeThaneInterrogation(target);

            //RandomizeCouncilConvo(target);

            // Implement once gesture system is reimplemented
            //RandomizeShepDance(target);
            InstallPackingHeat(target);

            // MakeAdsCreepy(target);
            return true;
        }

        private static void MakeAdsCreepy(GameTarget target)
        {
            #region Level 28 Top Floor
            {
                var topFloorF = MERFileSystem.GetPackageFile(target, @"BioD_CitHub_300UpperWing.pcc");
                var topFloorP = MERFileSystem.OpenMEPackage(topFloorF);

                var sequence = topFloorP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Talk_objects");
                var player = MERSeqTools.CreatePlayerObject(sequence, true);
                var adPawn =
                    topFloorP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Talk_objects.SeqVar_Object_11");

                var creepySeq = MERSeqTools.InstallMakeItCreepySingle(player, adPawn);

                // Change all the ads to point to our creepyseq
                var baseIFP = "TheWorld.PersistentLevel.Main_Sequence.Talk_objects.SeqAct_SetObject_";
                for (int i = 0; i < 5; i++)
                {
                    var ifp = baseIFP + i;
                    var setObj = topFloorP.FindExport(ifp);
                    SeqTools.ChangeOutlink(setObj, 0, 0, creepySeq.UIndex);
                }

                // CreepySeq -> Start Conversation
                KismetHelper.CreateOutputLink(creepySeq, "Out",
                    topFloorP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Talk_objects.SFXSeqAct_StartConversation_8"));

                MERFileSystem.SavePackage(topFloorP);
            }
            #endregion

            #region Level 26 Bottom Floor ELCOR

            {
                var bottomFloorF = MERFileSystem.GetPackageFile(target, @"BioD_CitHub_400LowerWing.pcc");
                var bottomFloorP = MERFileSystem.OpenMEPackage(bottomFloorF);

                var sequence = bottomFloorP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Tracking_sign");
                var player = MERSeqTools.CreatePlayerObject(sequence, true);

                // get actors
                var modify =
                    bottomFloorP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Tracking_sign.BioSeqAct_ModifyPropertyPawn_1");
                var pawnsToChange = SeqTools.GetVariableLinksOfNode(modify).SelectMany(x => x.LinkedNodes)
                    .OfType<ExportEntry>().ToList();

                ExportEntry lastCreepySeq = null;
                foreach (var p in pawnsToChange)
                {
                    var creepySeq = MERSeqTools.InstallMakeItCreepySingle(player, p);
                    if (lastCreepySeq == null)
                    {
                        // Point to first creepyseq
                        SeqTools.ChangeOutlink(modify, 0, 1, creepySeq.UIndex);
                    }
                    else
                    {
                        KismetHelper.CreateOutputLink(lastCreepySeq, "Out", creepySeq);
                    }

                    lastCreepySeq = creepySeq;

                }

                // CreepySeq -> Start Conversation
                KismetHelper.CreateOutputLink(lastCreepySeq, "Out",
                    bottomFloorP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Tracking_sign.SFXSeqAct_StartConversation_0"));
                MERFileSystem.SavePackage(bottomFloorP);

            }
            #endregion


            #region Level 26 Bottom Floor SOUTH MULTI
            {
                var lowSouthF = MERFileSystem.GetPackageFile(target, @"BioD_CitHub_420LowerSouth.pcc");
                var lowSouthP = MERFileSystem.OpenMEPackage(lowSouthF);

                var sequence = lowSouthP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Low_talking_ad");
                var player = MERSeqTools.CreatePlayerObject(sequence, true);
                var adPawn = lowSouthP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Low_talking_ad.SeqVar_Object_1");

                var creepySeq = MERSeqTools.InstallMakeItCreepySingle(player, adPawn);

                // Change all the ads to point to our creepyseq
                var baseIFP = "TheWorld.PersistentLevel.Main_Sequence.Low_talking_ad.SeqAct_SetObject_";
                for (int i = 0; i < 5; i++)
                {
                    var ifp = baseIFP + i;
                    var setObj = lowSouthP.FindExport(ifp);
                    SeqTools.ChangeOutlink(setObj, 0, 0, creepySeq.UIndex);
                }

                // CreepySeq -> Start Conversation
                KismetHelper.CreateOutputLink(creepySeq, "Out",
                    lowSouthP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Low_talking_ad.SFXSeqAct_StartConversation_0"));

                MERFileSystem.SavePackage(lowSouthP);
            }
            #endregion


            #region Level 27 MID MULTI
            {
                var mainRoomF = MERFileSystem.GetPackageFile(target, @"BioD_CitHub_200MainRoom.pcc");
                var mainRoomP = MERFileSystem.OpenMEPackage(mainRoomF);

                var sequence = mainRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.ads.Mid_Ad_logic");
                var player = MERSeqTools.CreatePlayerObject(sequence, true);
                var adPawn = mainRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.ads.Mid_Ad_logic.SeqVar_Object_1");

                var creepySeq = MERSeqTools.InstallMakeItCreepySingle(player, adPawn);

                // Change all the basic ads to point to our creepyseq
                var baseIFP = "TheWorld.PersistentLevel.Main_Sequence.ads.Mid_Ad_logic.SeqAct_SetObject_";
                for (int i = 0; i < 5; i++)
                {
                    var ifp = baseIFP + i;
                    int idx = 0;
                    if (i == 0)
                    {
                        idx = 2; // Use different output link
                    }
                    else if (i == 4)
                    {
                        ifp = "TheWorld.PersistentLevel.Main_Sequence.ads.Mid_Ad_logic.BioSeqAct_ModifyPropertyPawn_10"; // We use same logic but different IFP as it's branched
                    }
                    var setObj = mainRoomP.FindExport(ifp);
                    SeqTools.ChangeOutlink(setObj, 0, idx, creepySeq.UIndex);
                }

                // CreepySeq -> Start Conversation
                KismetHelper.CreateOutputLink(creepySeq, "Out", mainRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.ads.Mid_Ad_logic.SFXSeqAct_StartConversation_7"));

                MERFileSystem.SavePackage(mainRoomP);
            }
            #endregion
        }

        private static void InstallPackingHeat(GameTarget target)
        {
            // Files required for modification:
            // BioD_CitHub.pcc -> Adds a hackjob reverse triggerstream to stream out the 311PackingHeat file
            // BioD_CitHub_300UpperWing.pcc -> When this file is loaded in, PackingHeat is streamed in via a console command on the LevelLoaded event
            //        -> It also triggers a remote event when the reporter conversation wraps up to trigger logic in 311PackingHeat
            // BioD_CitHub_311PackingHeat.pcc -> New package file, has all the logic for packing heat
            //        -> Localizations for SFXVocalizationBanks

            #region BioD_Cithub.pcc

            {
                var biodCitHubF = MERFileSystem.GetPackageFile(target, "BioD_CitHub.pcc");
                if (biodCitHubF != null)
                {
                    var biodCitHubP = MERFileSystem.OpenMEPackage(biodCitHubF);

                    // Port the trigger volume
                    var mainSeq = biodCitHubP.FindExport("TheWorld.PersistentLevel.Main_Sequence");
                    var fabP = MEPackageHandler.OpenMEPackageFromStream(
                        MEREmbedded.GetEmbeddedPackage(target.Game, "SeqPrefabs.PackingHeat.pcc"), "PackingHeat.pcc");
                    var brSeq = fabP.FindExport("TheWorld.PersistentLevel.Main_Sequence.BloodRageVolume");
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, brSeq,
                        biodCitHubP, mainSeq, true, new RelinkerOptionsPackage(), out var newSeq);
                    KismetHelper.AddObjectToSequence(newSeq as ExportEntry, mainSeq);

                    // This will have been ported in by the sequence
                    var trigVol = fabP.FindExport("TheWorld.PersistentLevel.BioTriggerVolume_14");
                    fabP.AddToLevelActorsIfNotThere(trigVol);

                    MERFileSystem.SavePackage(biodCitHubP);
                }
            }

            #endregion

            #region BioD_CitHub_300UpperWing.pcc
            var biodCitHubUWF = MERFileSystem.GetPackageFile(target, "BioD_CitHub_300UpperWing.pcc");
            if (biodCitHubUWF != null)
            {
                var biodCitHubUWP = MERFileSystem.OpenMEPackage(biodCitHubUWF);

                // Stream the file in when the package loads
                var levelLoaded = biodCitHubUWP.FindExport("TheWorld.PersistentLevel.Main_Sequence.RP_Plot_5_Logic.SeqEvent_LevelLoaded_0");
                var rp5Seq = biodCitHubUWP.FindExport("TheWorld.PersistentLevel.Main_Sequence.RP_Plot_5_Logic");
                var cc = MERSeqTools.CreateConsoleCommandObject(rp5Seq, "streamlevelin BioD_CitHub_311PackingHeat");
                KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", cc);

                // Signal the remote event when the conversation logic finishes
                var saveGame = biodCitHubUWP.FindExport("TheWorld.PersistentLevel.Main_Sequence.RP_Plot_5_Logic.SFXSeqAct_SaveGame_0");
                var brc = MERSeqTools.CreateActivateRemoteEvent(rp5Seq, "BloodRageCheck");
                KismetHelper.CreateOutputLink(saveGame, "Out", brc);
                MERFileSystem.SavePackage(biodCitHubUWP);
            }

            #endregion

            #region BioD_CitHub_311PackingHeat.pcc
            // Install the entire package set
            MEREmbedded.ExtractEmbeddedBinaryFolder("Packages.LE2.PackingHeat");
            #endregion
        }

        private static void RandomizeShepDance(GameTarget target)
        {
            var loungeF = MERFileSystem.GetPackageFile(target, "BioD_CitHub_310Lounge.pcc");
            if (loungeF != null)
            {
                var loungeP = MEPackageHandler.OpenMEPackage(loungeF);
                var sequence = loungeP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Dancing_Shepard");

                MERPackageCache cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true);
                List<InterpTools.InterpData> interpDatas = new List<InterpTools.InterpData>();
                var interp1 = loungeP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Dancing_Shepard.SeqAct_Interp_1");

                // Make 2 additional dance options by cloning the interp and the data tree
                var interp2 = MERSeqTools.CloneBasicSequenceObject(interp1);
                var interp3 = MERSeqTools.CloneBasicSequenceObject(interp1);


                // Clone the interp data for attaching to 2 and 3
                var interpData1 = loungeP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Dancing_Shepard.InterpData_1");
                var interpData2 = EntryCloner.CloneTree(interpData1);
                var interpData3 = EntryCloner.CloneTree(interpData2);
                KismetHelper.AddObjectToSequence(interpData2, sequence);
                KismetHelper.AddObjectToSequence(interpData3, sequence);

                // Load ID for randomization
                interpDatas.Add(new InterpTools.InterpData(interpData1));
                interpDatas.Add(new InterpTools.InterpData(interpData2));
                interpDatas.Add(new InterpTools.InterpData(interpData3));


                // Chance the data for interp2/3
                var id2 = SeqTools.GetVariableLinksOfNode(interp2);
                id2[0].LinkedNodes[0] = interpData2;
                SeqTools.WriteVariableLinksToNode(interp2, id2);

                var id3 = SeqTools.GetVariableLinksOfNode(interp3);
                id3[0].LinkedNodes[0] = interpData3;
                SeqTools.WriteVariableLinksToNode(interp3, id3);

                // Add additional finished states for fadetoblack when done
                KismetHelper.CreateOutputLink(loungeP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Dancing_Shepard.BioSeqAct_BlackScreen_1"), "Finished", interp2, 2);
                KismetHelper.CreateOutputLink(loungeP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Dancing_Shepard.BioSeqAct_BlackScreen_1"), "Finished", interp3, 2);


                // Link up the random choice it makes
                var randSw = MERSeqTools.InstallRandomSwitchIntoSequence(target, sequence, 3);
                KismetHelper.CreateOutputLink(randSw, "Link 1", interp1);
                KismetHelper.CreateOutputLink(randSw, "Link 2", interp2);
                KismetHelper.CreateOutputLink(randSw, "Link 3", interp3);

                // Break the original output to start the interp, repoint it's output to the switch instead
                var sgm = loungeP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Dancing_Shepard.BioSeqAct_SetGestureMode_3"); //set gesture mode
                KismetHelper.RemoveOutputLinks(sgm);
                KismetHelper.CreateOutputLink(sgm, "Done", loungeP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Dancing_Shepard.BioSeqAct_BlackScreen_3"));
                KismetHelper.CreateOutputLink(sgm, "Done", randSw);

                // Now install the dances
                foreach (var id in interpDatas)
                {
                    var danceTrack = id.InterpGroups[0].Tracks[0];
                    OmegaHub.InstallShepardDanceGesture(target, danceTrack.Export, cache);
                }

                MERFileSystem.SavePackage(loungeP);
            }
        }

        private static void RandomizeCouncilConvo(GameTarget target)
        {
            var langs = GameLanguage.GetLanguagesForGame(target.Game);
            foreach (var lang in langs)
            {
                // Modify each localized version
                var embassyF = MERFileSystem.GetPackageFile(target, $"BioD_CitHub_Embassy_LOC_{lang.FileCode}.pcc");
                if (embassyF != null)
                {
                    var embassyInt = MEPackageHandler.OpenMEPackage(embassyF);
                    MERPackageCache cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true);

                    var convoE = embassyInt.FindExport("citprs_council_d_D.citprs_council_d_dlg");
                    RandomizeCouncilConvoSingle(target, convoE, cache);

                    MERFileSystem.SavePackage(embassyInt);
                }
            }
        }

        private static string[] councilKeywords = new[] { "Dancing", "Angry", "Cursing", "Fearful", "ROM", "Drunk" };
        private static string[] councilAnimPackages = new[] { "BIOG_HMF_AM_A" }; //Towny

        private static void RandomizeCouncilConvoSingle(GameTarget target, ExportEntry convoE, MERPackageCache cache)
        {
            var convoInfo = new ConversationExtended(convoE);
            convoInfo.LoadConversation(detailedParse: true);

            //var turianIdx = convoInfo.Speakers.First(x => x.SpeakerName == "citprs_turian_council").SpeakerID;
            //var salarianIdx = convoInfo.Speakers.First(x => x.SpeakerName == "citprs_salarian_council").SpeakerID;
            //var asariIdx = convoInfo.Speakers.First(x => x.SpeakerName == "citprs_asari_council").SpeakerID;

            //var turianEnabled = ThreadSafeRandom.Next(2) == 0;
            //var salarianEnabled = ThreadSafeRandom.Next(2) == 0;
            //var asariEnabled = (!turianEnabled || !salarianEnabled) || ThreadSafeRandom.Next(2) == 0; // if one or none of the other councilors is modified, force asari on

            foreach (var entryNode in convoInfo.EntryList)
            {
                var interpData = entryNode.Interpdata;
                if (interpData != null)
                {
                    var conversationData = new InterpTools.InterpData(interpData);
                    var gestureTracks = conversationData.InterpGroups.FirstOrDefault(x => x.GroupName == "Conversation")?.Tracks.Where(x => x.Export.ClassName == "BioEvtSysTrackGesture").ToList();
                    if (gestureTracks != null)
                    {
                        foreach (var gestTrack in gestureTracks)
                        {
                            var actorToFind = gestTrack.Export.GetProperty<NameProperty>("m_nmFindActor");

                            //if (actorToFind.Value.Name.EndsWith("_council"))
                            {
                                var gestures = RBioEvtSysTrackGesture.GetSysTrackGestures(gestTrack.Export);
                                if (gestures != null)
                                {
                                    for (int i = 0; i < gestures.Count; i++)
                                    {
                                        var newGesture = GestureManager.InstallRandomFilteredGestureAsset(target, gestTrack.Export.FileRef, 5, filterKeywords: councilKeywords, blacklistedKeywords: OmegaHub.notDanceKeywords, mainPackagesAllowed: councilAnimPackages, includeSpecial: true);
                                        gestures[i] = newGesture;
                                    }

                                    RBioEvtSysTrackGesture.WriteSysTrackGestures(gestTrack.Export, gestures);
                                }

                                var defaultPose = RBioEvtSysTrackGesture.GetDefaultPose(gestTrack.Export);
                                if (defaultPose != null)
                                {
                                    var newPose = GestureManager.InstallRandomFilteredGestureAsset(target, gestTrack.Export.FileRef, 5, filterKeywords: councilKeywords, blacklistedKeywords: OmegaHub.notDanceKeywords, mainPackagesAllowed: councilAnimPackages, includeSpecial: true);
                                    if (newPose != null)
                                    {
                                        RBioEvtSysTrackGesture.WriteDefaultPose(gestTrack.Export, newPose);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }




        private static void RandomizeThaneInterrogation(GameTarget target)
        {
            // The main dickhead guy
            var lockedUpAsset = BodyModels.RandomElement();
            var citHubTF = MERFileSystem.GetPackageFile(target, "BioD_CitHub_200Dialogue.pcc");
            if (citHubTF != null)
            {
                var citHubTP = MERFileSystem.OpenMEPackage(citHubTF);
                var citAslInterrogateSKM = citHubTP.FindExport("TheWorld.PersistentLevel.BioPawn_4.SkeletalMeshComponent_1733");

                var newMdl = PackageTools.PortExportIntoPackage(target, citHubTP, lockedUpAsset.BodyAsset.GetAsset(target));
                citAslInterrogateSKM.WriteProperty(new ObjectProperty(newMdl, "SkeletalMesh"));
                if (!lockedUpAsset.KeepHead)
                {
                    citHubTP.FindExport("TheWorld.PersistentLevel.BioPawn_4.SkeletalMeshComponent_68").RemoveProperty("SkeletalMesh");
                }
                if (lockedUpAsset.RemoveMaterials)
                {
                    citAslInterrogateSKM.RemoveProperty("Materials");
                }
                MERFileSystem.SavePackage(citHubTP);
            }


            // Laywer
            var citHubASLTF = MERFileSystem.GetPackageFile(target, "BioD_CitHub_220AsL.pcc");
            var lawyerAsset = BodyModels.RandomElement();
            if (lawyerAsset == lockedUpAsset)
            {
                lawyerAsset = BodyModels.RandomElement(); // Give a slight better chance to make them have different outfits
            }
            if (citHubASLTF != null)
            {
                var citHubTP = MEPackageHandler.OpenMEPackage(citHubASLTF);

                var citAslLawyerSKM = citHubTP.FindExport("TheWorld.PersistentLevel.BioPawn_8.SkeletalMeshComponent_304");
                var newMdl = PackageTools.PortExportIntoPackage(target, citHubTP, lawyerAsset.BodyAsset.GetAsset(target));
                citAslLawyerSKM.WriteProperty(new ObjectProperty(newMdl, "SkeletalMesh"));
                if (!lawyerAsset.KeepHead)
                {
                    citHubTP.FindExport("TheWorld.PersistentLevel.BioPawn_8.SkeletalMeshComponent_72").RemoveProperty("SkeletalMesh");
                }
                if (lawyerAsset.RemoveMaterials)
                {
                    citAslLawyerSKM.RemoveProperty("Materials");
                }
                MERFileSystem.SavePackage(citHubTP);
            }
        }

        private static IlliumHub.DancerSource[] BodyModels = new[]
        {
            // WREX
            new IlliumHub.DancerSource()
            {
                KeepHead = true,
                BodyAsset = new IlliumHub.AssetSource()
                {
                    PackageFile = "BioD_KroHub_100MainHub.pcc",
                    AssetPath = "BIOG_KRO_ARM_HVY_R.HVYc.KRO_ARM_HVYc_MDL"
                },
            },
            // Scion
            new IlliumHub.DancerSource()
            {
                KeepHead = false,
                RemoveMaterials = true,
                BodyAsset = new IlliumHub.AssetSource()
                {
                    PackageFile = "BioP_RprGtA.pcc",
                    AssetPath = "BIOG_SCI_ARM_NKD_R.NKDa.SCI_ARM_NKDa_MDL"
                },
            },
            // Vorcha
            new IlliumHub.DancerSource()
            {
                KeepHead = true,
                RemoveMaterials = true,
                BodyAsset = new IlliumHub.AssetSource()
                {
                    PackageFile = "BioD_N7BldInv2_100.pcc",
                    AssetPath = "BIOG_ALN_TRO_NKD_R.NKDa.ALN_TRO_NKDa_MDL"
                }
            },
            // Rat Monkey thing
            new IlliumHub.DancerSource()
            {
                KeepHead = false,
                RemoveMaterials = true,
                BodyAsset = new IlliumHub.AssetSource()
                {
                    PackageFile = "BioP_KroHub.pcc",
                    AssetPath = "BIOG_AMB_MON_NKD_R.NKDa.AMB_MON_NKDa_MDL"
                }
            }
        };
    }
}