using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Dialogue;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.ExportTypes;
using Randomizer.Randomizers.Game2.TLK;
using Randomizer.Randomizers.Utility;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game2.Levels
{
    public static class Citadel
    {

        #region ENDORSEMENTS
        // Endorsement line is 2.1ish seconds long.
        // BOTH LISTS MUST BE THE SAME LENGTH AND HAVE IDENTICAL TLK STRS!
        private static List<(string packageName, int uindex)> EndorsementCandidatesFemale = new List<(string packageName, int uindex)>()
        {
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3408), // Sorry. Don't remember, don't care.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3403), // I knew this was a mistake
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3421), // People die. I don't have time for this crap.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3503), // Geth, pirates, mercenary scum

            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 796), // Cheap touristy crap
            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 798), // HEY EVERYONE THIS STORE DISCRIMINATES AGAINST THE POOR

            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", 2693), // Are you naturally this bitchy or is it just me

            ("BioD_Nor_101Cockpit_LOC_INT.pcc", 3675 ), // What is this high school

            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", 6895), // Free drinks on me
            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", 7034), // Sectors of space

            ("BioD_PrsCvA_104bLastCheckIn_LOC_INT.pcc", 240), // Go to hell

        };

        private static List<(string packageName, int uindex)> EndorsementCandidatesMale = new List<(string packageName, int uindex)>()
        {
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3432), // Sorry. Don't remember, don't care.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3427), // I knew this was a mistake
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3445), // People die. I don't have time for this crap.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3523), // Geth, pirates, mercenary scum

            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 811), // Cheap touristy crap
            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 813), // HEY EVERYONE THIS STORE DISCRIMINATES AGAINST THE POOR

            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", 2710), // Are you naturally this bitchy or is it just me

            ("BioD_Nor_101Cockpit_LOC_INT.pcc", 3712 ), // What is this high school

            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", 6902), // Free drinks on me
            ("BioD_OmgHub_500DenVIP_LOC_INT.pcc", 7113), // Sectors of space

            ("BioD_PrsCvA_104bLastCheckIn_LOC_INT.pcc", 244), // Go to hell


        };



        private static void RandomizeEndorsements()
        {
            var cache = new MERPackageCache();
            List<int> pickedIndices = new List<int>();

            RandomizeEndorsementLine(@"BioD_CitHub_240Vendors_LOC_INT.pcc", 808, 793, 26, 7, cache, pickedIndices); //sirta, i think?
            RandomizeEndorsementLine(@"BioD_CitHub_300UpperWing_LOC_INT.pcc", 3539, 3514, 105, 18, cache, pickedIndices); // gun turian
            RandomizeEndorsementLine(@"BioD_CitHub_420LowerSouth_LOC_INT.pcc", 1979, 1963, 43, 8, cache, pickedIndices); //biotic
            RandomizeEndorsementLine(@"BioD_CitHub_420LowerSouth_LOC_INT.pcc", 2038, 2022, 44, 11, cache, pickedIndices); //omni
        }

        private static void RandomizeEndorsementLine(string packageName, int maleUIndex, int femaleUIndex, int conversationUIndex, int replyIdx, MERPackageCache cache, List<int> pickedIndices)
        {
            var package = cache.GetCachedPackage(packageName);

            var conversation = package.GetUExport(conversationUIndex);
            var replies = conversation.GetProperty<ArrayProperty<StructProperty>>("m_ReplyList");

            var endorsementLineMale = package.GetUExport(maleUIndex);
            var endorsementLineFemale = package.GetUExport(femaleUIndex);

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
            var sourceExport = sourcePackage.GetUExport(maleReplacement.uindex);
            WwiseTools.RepointWwiseStream(sourceExport, endorsementLineMale);

            // Female
            sourcePackage = cache.GetCachedPackage(femaleReplacement.packageName);
            sourceExport = sourcePackage.GetUExport(femaleReplacement.uindex);
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
                TLKHandler.ReplaceStringByRepoint(oldTlkId, newTlkId);
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
            RandomizeEndorsements();
            RandomizeThaneInterrogation(target);
            RandomizeCouncilConvo(target);
            RandomizeShepDance(target);
            return true;
        }

        private static void RandomizeShepDance(GameTarget target)
        {
            var loungeF = MERFileSystem.GetPackageFile(target, "BioD_CitHub_310Lounge.pcc");
            if (loungeF != null)
            {
                var loungeP = MEPackageHandler.OpenMEPackage(loungeF);
                var sequence = loungeP.GetUExport(2583);

                MERPackageCache cache = new MERPackageCache();
                List<InterpTools.InterpData> interpDatas = new List<InterpTools.InterpData>();
                var interp1 = loungeP.GetUExport(2532);

                // Make 2 additional dance options by cloning the interp and the data tree
                var interp2 = MERSeqTools.CloneBasicSequenceObject(interp1);
                var interp3 = MERSeqTools.CloneBasicSequenceObject(interp1);


                // Clone the interp data for attaching to 2 and 3
                var interpData1 = loungeP.GetUExport(698);
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
                KismetHelper.CreateOutputLink(loungeP.GetUExport(449), "Finished", interp2, 2);
                KismetHelper.CreateOutputLink(loungeP.GetUExport(449), "Finished", interp3, 2);


                // Link up the random choice it makes
                var randSw = MERSeqTools.InstallRandomSwitchIntoSequence(sequence, 3);
                KismetHelper.CreateOutputLink(randSw, "Link 1", interp1);
                KismetHelper.CreateOutputLink(randSw, "Link 2", interp2);
                KismetHelper.CreateOutputLink(randSw, "Link 3", interp3);

                // Break the original output to start the interp, repoint it's output to the switch instead
                var sgm = loungeP.GetUExport(475); //set gesture mode
                KismetHelper.RemoveOutputLinks(sgm);
                KismetHelper.CreateOutputLink(sgm, "Done", loungeP.GetUExport(451));
                KismetHelper.CreateOutputLink(sgm, "Done", randSw);

                // Now install the dances
                foreach (var id in interpDatas)
                {
                    var danceTrack = id.InterpGroups[0].Tracks[0];
                    OmegaHub.InstallShepardDanceGesture(danceTrack.Export, cache);
                }

                MERFileSystem.SavePackage(loungeP);
            }
        }

        private static void RandomizeCouncilConvo(GameTarget target)
        {
            var embassyF = MERFileSystem.GetPackageFile(target, "BioD_CitHub_Embassy_LOC_INT.pcc");
            if (embassyF != null)
            {
                var embassyInt = MEPackageHandler.OpenMEPackage(embassyF);
                MERPackageCache cache = new MERPackageCache();

                var convoE = embassyInt.GetUExport(94);
                RandomizeCouncilConvoSingle(convoE, cache);

                MERFileSystem.SavePackage(embassyInt);
            }
        }

        private static string[] councilKeywords = new[] { "Dancing", "Angry", "Cursing", "Fearful", "ROM", "Drunk" };
        private static string[] councilAnimPackages = new[] { "BIOG_HMF_AM_A" }; //Towny

        private static void RandomizeCouncilConvoSingle(ExportEntry convoE, MERPackageCache cache)
        {
            var convoInfo = new ConversationExtended(convoE);
            convoInfo.LoadConversation(detailedLoad: true);

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

                            if (actorToFind.Value.Name.EndsWith("_council"))
                            {
                                var gestures = RBioEvtSysTrackGesture.GetGestures(gestTrack.Export);
                                if (gestures != null)
                                {
                                    for (int i = 0; i < gestures.Count; i++)
                                    {
                                        var newGesture = RBioEvtSysTrackGesture.InstallRandomFilteredGestureAsset(gestTrack.Export.FileRef, 5, councilKeywords, OmegaHub.notDanceKeywords, councilAnimPackages, true, cache);
                                        gestures[i] = newGesture;
                                    }

                                    RBioEvtSysTrackGesture.WriteGestures(gestTrack.Export, gestures);
                                }

                                var defaultPose = RBioEvtSysTrackGesture.GetDefaultPose(gestTrack.Export);
                                if (defaultPose != null)
                                {
                                    var newPose = RBioEvtSysTrackGesture.InstallRandomFilteredGestureAsset(gestTrack.Export.FileRef, 5, councilKeywords, OmegaHub.notDanceKeywords, councilAnimPackages, true, cache);
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
                var citHubTP = MEPackageHandler.OpenMEPackage(citHubTF);

                var newMdl = PackageTools.PortExportIntoPackage(citHubTP, lockedUpAsset.BodyAsset.GetAsset(target));
                citHubTP.GetUExport(670).WriteProperty(new ObjectProperty(newMdl, "SkeletalMesh"));
                if (!lockedUpAsset.KeepHead)
                {
                    citHubTP.GetUExport(671).RemoveProperty("SkeletalMesh");
                }
                if (lockedUpAsset.RemoveMaterials)
                {
                    citHubTP.GetUExport(670).RemoveProperty("Materials");
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

                var newMdl = PackageTools.PortExportIntoPackage(citHubTP, lawyerAsset.BodyAsset.GetAsset(target));
                citHubTP.GetUExport(822).WriteProperty(new ObjectProperty(newMdl, "SkeletalMesh"));
                if (!lawyerAsset.KeepHead)
                {
                    citHubTP.GetUExport(823).RemoveProperty("SkeletalMesh");
                }
                if (lawyerAsset.RemoveMaterials)
                {
                    citHubTP.GetUExport(822).RemoveProperty("Materials");
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
        };
    }
}