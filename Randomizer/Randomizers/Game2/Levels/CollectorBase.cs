using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.Enemy;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game2.Levels
{
    class CollectorBase
    {
        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            //RandomizeFlyerSpawnPawns(target);
            //AutomatePlatforming400(target, option);
            //MakeTubesSectionHarder(target);

            //RandomizeTheLongWalk(target, option);
            MichaelBayifyFinalFight(target, option);
            // InstallBorger(target); // Change to new texture system
            return true;
        }

        private static void MichaelBayifyFinalFight(GameTarget target, RandomizationOption option)
        {
            var reaperFightP = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_EndGm2_430ReaperCombat.pcc"));
            var sequenceSupportPackage = MEPackageHandler.OpenMEPackageFromStream(MEREmbedded.GetEmbeddedPackage(target.Game, "SeqPrefabs.SuicideMission.pcc"), "SuicideMission.pcc");

            var fireWeaponAts = reaperFightP.Exports.Where(x => x.ClassName == @"SFXSeqAct_FireWeaponAt").ToList();
            var matchingInputIdxs = new List<int>(new[] { 0 });

            foreach (var fwa in fireWeaponAts)
            {
                var seqObjs = SeqTools.GetAllSequenceElements(fwa).OfType<ExportEntry>();
                var inboundLinks = SeqTools.FindOutboundConnectionsToNode(fwa, seqObjs, matchingInputIdxs);
                if (inboundLinks.Count != 1)
                {
                    Debugger.Break();
                    continue; // Something is wrong!!
                }


                var newSeq = MERSeqTools.InstallSequenceChained(sequenceSupportPackage.FindExport("ReaperWeaponHandler"), reaperFightP, SeqTools.GetParentSequence(fwa), fwa, 0);

                var source = inboundLinks[0];
                var outlinks = SeqTools.GetOutboundLinksOfNode(source);
                foreach (var v in outlinks)
                {
                    foreach (var o in v)
                    {
                        if (o.LinkedOp == fwa && o.InputLinkIdx == 0)
                        {
                            o.LinkedOp = newSeq; // Repoint output link from FWA to our new sequnce instead
                        }
                    }
                }
                SeqTools.WriteOutboundLinksToNode(source, outlinks);

            }


            MERFileSystem.SavePackage(reaperFightP);
        }

        private static void MakeTubesSectionHarder(GameTarget target)
        {
            var preReaperF = MERFileSystem.GetPackageFile(target, "BioD_EndGm2_420CombatZone.pcc");
            if (preReaperF != null && File.Exists(preReaperF))
            {
                var preReaperP = MEPackageHandler.OpenMEPackage(preReaperF);

                // Open tubes on kills to start the attack (post platforms)----------------------
                var seq = preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.PostPlatform_CombatFiller");

                var attackSw = MERSeqTools.InstallRandomSwitchIntoSequence(target, seq, 2); //50% chance

                // killed squad member -> squad still exists to 50/50 sw
                KismetHelper.CreateOutputLink(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.PostPlatform_CombatFiller.SequenceReference_3"), "SquadStillExists", attackSw);

                // 50/50 to just try to do reaper attack
                KismetHelper.CreateOutputLink(attackSw, "Link 1", preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.PostPlatform_CombatFiller.SeqAct_ActivateRemoteEvent_9"));

                // Automate the platforms one after another
                KismetHelper.RemoveAllLinks(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform01.SeqEvent_Death_2")); //B Plat01 Death
                KismetHelper.RemoveAllLinks(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqEvent_Death_2")); //B Plat02 Death

                // Sub automate - Remove attack completion gate inputs ----
                KismetHelper.RemoveAllLinks(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqEvent_RemoteEvent_6")); //Plat03 Attack complete
                KismetHelper.RemoveAllLinks(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqEvent_RemoteEvent_6")); //Plat02 Attack complete

                //// Sub automate - Remove activate input into gate
                var cmb2activated = preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqEvent_SequenceActivated_0");
                var cmb3activated = preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqEvent_SequenceActivated_2");

                KismetHelper.RemoveAllLinks(cmb2activated);
                KismetHelper.CreateOutputLink(cmb2activated, "Out", preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.BioSeqAct_PMCheckState_10"));

                // Delay the start of platform 3 by 4 seconds to give player a bit more time to handle first two platforms
                // Player will likely have decent weapons by now so they will be better than my testing for sure
                KismetHelper.RemoveAllLinks(cmb3activated);
                var newDelay = EntryCloner.CloneEntry(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqAct_Delay_7"));
                newDelay.WriteProperty(new FloatProperty(4, "Duration"));
                KismetHelper.AddObjectToSequence(newDelay, preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03"), true);

                KismetHelper.CreateOutputLink(cmb3activated, "Out", newDelay);
                KismetHelper.CreateOutputLink(newDelay, "Finished", preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.BioSeqAct_PMCheckState_0"));
                //                preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqAct_Gate_0").RemoveProperty("bOpen"); // Plat03 gate - forces gate open so when reaper attack fires it passes through
                //              preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqAct_Gate_2").RemoveProperty("bOpen"); // Plat02 gate - forces gate open so when reaper attack fires it passes through


                // There is no end to Plat03 behavior until tubes are dead
                KismetHelper.CreateOutputLink(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform01.SeqAct_Interp_4"), "Completed", preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform01.SeqAct_FinishSequence_1")); // Interp completed to Complete in Plat01
                KismetHelper.CreateOutputLink(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqAct_Interp_4"), "Completed", preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqAct_FinishSequence_1")); // Interp completed to Complete in Plat02

                // if possession fails continue the possession loop on plat3 to end of pre-reaper combat
                KismetHelper.CreateOutputLink(preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SFXSeqAct_CollectorPossess_6"), "Failed", preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqAct_Delay_7"));




                MERFileSystem.SavePackage(preReaperP);
            }
        }

        private static void GateTubesAttack(GameTarget target)
        {
            // This doesn't actually work like expected, it seems gate doesn't store input value

            // Adds a gate to the tubes attack to ensure it doesn't fire while the previous attack is running still.
            var tubesF = MERFileSystem.GetPackageFile(target, "BioD_EndGm2_425ReaperTubes.pcc");
            if (tubesF != null && File.Exists(tubesF))
            {
                var tubesP = MEPackageHandler.OpenMEPackage(tubesF);

                // Clone a gate
                var gateToClone = tubesP.GetUExport(1316);
                var seq = tubesP.GetUExport(1496);
                var newGate = EntryCloner.CloneEntry(gateToClone);
                newGate.RemoveProperty("bOpen"); // Make it open by default.
                KismetHelper.AddObjectToSequence(newGate, seq, true);

                // Hook up the 'START REAPER ATTACK' to the gate, remove it's existing output.
                var sraEvent = tubesP.GetUExport(1455);
                KismetHelper.RemoveOutputLinks(sraEvent);
                KismetHelper.CreateOutputLink(sraEvent, "Out", newGate, 0); // 0 = in, which means fire or queue for fire

                // Hook up the ending of the attack to the gate for 'open' so the gate can be passed through.
                var delay = tubesP.GetUExport(1273);
                KismetHelper.CreateOutputLink(tubesP.GetUExport(103), "Out", delay); // Attack finished (CameraShake_Intimidate) to 2s delay
                KismetHelper.RemoveAllLinks(delay);
                KismetHelper.CreateOutputLink(delay, "Finished", newGate, 1); //2s Delay to open gate

                // Make the gate automatically close itself on pass through, and configure output of gate to next item.
                KismetHelper.CreateOutputLink(newGate, "Out", newGate, 2); // Hook from Out to Close
                KismetHelper.CreateOutputLink(newGate, "Out", tubesP.GetUExport(1340), 0); // Hook from Out to Log (bypass the delay, we are repurposing it)

                MERFileSystem.SavePackage(tubesP);
            }
        }

        private static void RandomizeStuntHench(GameTarget target)
        {
            var shFile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioP_EndGm_StuntHench.pcc"));

            var originalTriggerStreams = shFile.Exports.Where(x => x.ClassName == @"BioTriggerStream").ToList();
            var triggerStreamProps = originalTriggerStreams.Select(x => x.GetProperty<ArrayProperty<StructProperty>>("StreamingStates")).ToList(); // These are the original streaming states in same order as original
            triggerStreamProps.Shuffle();

            for (int i = 0; i < originalTriggerStreams.Count; i++)
            {
                var oTrigStream = originalTriggerStreams[i];
                var newPropSet = triggerStreamProps.PullFirstItem();
                var trigStreams = oTrigStream.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
                trigStreams[1].Properties.AddOrReplaceProp(newPropSet[1].Properties.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames"));
                trigStreams[2].Properties.AddOrReplaceProp(newPropSet[2].Properties.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames"));
                oTrigStream.WriteProperty(trigStreams);
            }

            MERFileSystem.SavePackage(shFile);
        }

        private static void AddMoreFlyers(GameTarget target)
        {
            string[] files =
            {
                "BioD_EndGm2_120TheHives.pcc",
                //"BioD_EndGm2_430ReaperCombat.pcc"
            };

            foreach (var f in files)
            {
                var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, f));
                List<ExportEntry> spawnFlyerSequences = new List<ExportEntry>(); // The list of sequence activated events named 'Spawn Flyer'
                foreach (var exp in package.Exports.Where(x => x.ClassName == "SeqEvent_SequenceActivated" && x.GetProperty<StrProperty>("InputLabel")?.Value is "Spawn Flyer").Select(x => SeqTools.GetParentSequence(x)).ToList())
                {
                    CloneFullFlyerSequence(exp);
                }
                MERFileSystem.SavePackage(package);
            }
        }

        private static void CloneFullFlyerSequence(ExportEntry seqRef)
        {
            var containingSeq = SeqTools.GetParentSequence(seqRef);

            List<ExportEntry> allObjs = new List<ExportEntry>();


        }

        private static void RandomizeFlyerSpawnPawns(GameTarget target)
        {
            string[] files =
            {
                "BioD_EndGm2_420CombatZone.pcc",
                "BioD_EndGm2_430ReaperCombat.pcc"
            };
            ChangeFlyersInFiles(target, files);

            // Install names
            TLKBuilder.ReplaceString(7892160, "Indoctrinated Krogan"); //Garm update
            TLKBuilder.ReplaceString(7892161, "Enthralled Batarian"); //Batarian Commando update
            //TLKBuilder.ReplaceString(7892162, "Collected Human"); //Batarian Commando update
        }

        private static void ChangeFlyersInFiles(GameTarget target, string[] files)
        {
            foreach (var f in files)
            {
                var fPath = MERFileSystem.GetPackageFile(target, f);
                if (fPath != null && File.Exists(fPath))
                {
                    var package = MEPackageHandler.OpenMEPackage(fPath);
                    GenericRandomizeFlyerSpawns(target, package, 3);
                    MERFileSystem.SavePackage(package);
                }
            }
        }

        private static void GenericRandomizeFlyerSpawns(GameTarget target, IMEPackage package, int maxNumNewEnemies, EPortablePawnClassification minClassification = EPortablePawnClassification.Mook, EPortablePawnClassification maxClassification = EPortablePawnClassification.Boss)
        {
            var flyerPrepSequences = package.Exports.Where(x => x.ClassName == "Sequence" && x.GetProperty<StrProperty>("ObjName") is StrProperty objName && objName == "REF_SpawnPrep_Flyer").ToList();

            foreach (var flySeq in flyerPrepSequences)
            {
                var objectsInSeq = KismetHelper.GetSequenceObjects(flySeq).OfType<ExportEntry>().ToList();
                var preGate = objectsInSeq.FirstOrDefault(x => x.ClassName == "SeqAct_Gate"); // should not be null
                var outbound = SeqTools.GetOutboundLinksOfNode(preGate);
                var aifactoryObj = outbound[0][0].LinkedOp as ExportEntry; //First out on first link. Should point to AIFactory assuming these are all duplicated flyers

                var availablePawns = PawnPorting.PortablePawns.Where(x => x.Classification >= minClassification && x.Classification <= maxClassification).ToList();
                if (availablePawns.Any())
                {
                    // We can add new pawns to install
                    List<PortablePawn> newPawnsInThisSeq = new List<PortablePawn>();
                    int numEnemies = 1; // 1 is the original amount.
                    List<ExportEntry> aiFactories = new List<ExportEntry>();
                    aiFactories.Add(aifactoryObj); // the original one
                    for (int i = 0; i < maxNumNewEnemies; i++)
                    {
                        var randPawn = availablePawns.RandomElement();
                        if (!newPawnsInThisSeq.Contains(randPawn))
                        {
                            numEnemies++;
                            PawnPorting.PortPawnIntoPackage(target, randPawn, package);
                            newPawnsInThisSeq.Add(randPawn);
                            // Clone the ai factory sequence object and add it to the sequence
                            var newAiFactorySeqObj = EntryCloner.CloneTree(aifactoryObj);
                            aiFactories.Add(newAiFactorySeqObj);
                            KismetHelper.AddObjectToSequence(newAiFactorySeqObj, flySeq, false);

                            // Update the backing factory object
                            var backingFactory = newAiFactorySeqObj.GetProperty<ObjectProperty>("Factory").ResolveToEntry(package) as ExportEntry;
                            backingFactory.WriteProperty(new ObjectProperty(package.FindExport(randPawn.ChallengeTypeFullPath), "ActorType"));
                            var collection = backingFactory.GetProperty<ArrayProperty<ObjectProperty>>("ActorResourceCollection");
                            collection.Clear();
                            foreach (var asset in randPawn.AssetPaths)
                            {
                                collection.Add(new ObjectProperty(package.FindExport(asset)));
                            }
                            backingFactory.WriteProperty(collection);
                        }
                    }

                    if (newPawnsInThisSeq.Any())
                    {
                        // install the switch
                        var randSw = MERSeqTools.InstallRandomSwitchIntoSequence(target, flySeq, numEnemies);
                        outbound[0][0].LinkedOp = randSw;
                        SeqTools.WriteOutboundLinksToNode(preGate, outbound);

                        // Hook up switch to ai factories
                        for (int i = 0; i < numEnemies; i++)
                        {
                            var linkDesc = $"Link {i + 1}";
                            KismetHelper.CreateOutputLink(randSw, linkDesc, aiFactories[i]); // switches are indexed at 1
                        }
                    }
                }
            }
        }

        private static void InstallBorger(GameTarget target)
        {
            var endGame3F = MERFileSystem.GetPackageFile(target, "BioP_EndGm3.pcc");
            if (endGame3F != null && File.Exists(endGame3F))
            {
                var biopEndGm3 = MEPackageHandler.OpenMEPackage(endGame3F);

                var packageBin = MEREmbedded.GetEmbeddedPackage(target.Game, "Delux2go_Edmonton_Burger.pcc");
                var burgerPackage = MEPackageHandler.OpenMEPackageFromStream(packageBin);

                // 1. Add the burger package
                var burgerMDL = PackageTools.PortExportIntoPackage(target, biopEndGm3, burgerPackage.FindExport("Edmonton_Burger_Delux2go.Burger_MDL"));

                // 2. Link up the textures
                TextureHandler.RandomizeExport(target, biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Diff"), null);
                TextureHandler.RandomizeExport(target, biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Norm"), null);
                TextureHandler.RandomizeExport(target, biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Spec"), null);

                // 3. Convert the collector base into lunch or possibly early dinner
                // It's early dinner cause that thing will keep you full all night long
                biopEndGm3.GetUExport(11276).WriteProperty(new ObjectProperty(burgerMDL.UIndex, "SkeletalMesh"));
                biopEndGm3.GetUExport(11282).WriteProperty(new ObjectProperty(burgerMDL.UIndex, "SkeletalMesh"));
                MERFileSystem.SavePackage(biopEndGm3);
            }
        }


        private static void AutomatePlatforming400(GameTarget target, RandomizationOption option)
        {
            var platformControllerF = MERFileSystem.GetPackageFile(target, "BioD_EndGm2_420CombatZone.pcc");
            if (platformControllerF != null)
            {
                var platformController = MEPackageHandler.OpenMEPackage(platformControllerF);

                // Remove completion state from squad kills as we won't be using that mechanism
                KismetHelper.RemoveOutputLinks(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform01.SeqAct_Log_2")); //A Platform 01
                KismetHelper.RemoveOutputLinks(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform02.SeqAct_Log_2")); //A Platform 02
                KismetHelper.RemoveOutputLinks(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform03.SeqAct_Log_2")); //A Platform 03
                KismetHelper.RemoveOutputLinks(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform0405.SeqAct_Log_2")); //A Platform 0405 (together)
                                                                                                                                                                   // there's final platform with the controls on it. we don't touch it

                // Install delays and hook them up to the complection states
                InstallPlatformAutomation(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform01.SeqEvent_SequenceActivated_0"), platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform01.SeqAct_FinishSequence_1"), 1); //01 to 02
                InstallPlatformAutomation(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform02.SeqEvent_SequenceActivated_0"), platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform02.SeqAct_FinishSequence_1"), 2); //02 to 03
                InstallPlatformAutomation(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform03.SeqEvent_SequenceActivated_0"), platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform03.SeqAct_FinishSequence_1"), 3); //03 to 0405
                InstallPlatformAutomation(platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform0405.SeqEvent_SequenceActivated_0"), platformController.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform0405.SeqAct_FinishSequence_0"), 4); //0405 to 06

                MERFileSystem.SavePackage(platformController);
            }
        }

        private static void InstallPlatformAutomation(ExportEntry seqActivated, ExportEntry finishSeq, int platIdx)
        {
            var seq = seqActivated.GetProperty<ObjectProperty>("ParentSequence").ResolveToEntry(seqActivated.FileRef) as ExportEntry;

            // Clone a delay object, set timer on it
            var delay = MERSeqTools.AddDelay(seq, ThreadSafeRandom.NextFloat(3f, 10f - platIdx));

            // Point start to delay
            KismetHelper.CreateOutputLink(seqActivated, "Out", delay);

            // Point delay to finish
            KismetHelper.CreateOutputLink(delay, "Finished", finishSeq);
        }


        private static void RandomizeTheLongWalk(GameTarget target, RandomizationOption option)
        {
            var prelongwalkfile = MERFileSystem.GetPackageFile(target, "BioD_EndGm2_200Factory.pcc");
            if (prelongwalkfile != null)
            {
                // Pre-long walk selection
                var package = MEPackageHandler.OpenMEPackage(prelongwalkfile);
                var bioticTeamSeq = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B");

                var activated = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.SeqEvent_SequenceActivated_0");
                KismetHelper.RemoveAllLinks(activated);

                // install new logic
                var randSwitch = MERSeqTools.InstallRandomSwitchIntoSequence(target, bioticTeamSeq, 13); // don't include theif or veteran as dlc might not be installed
                KismetHelper.CreateOutputLink(activated, "Out", randSwitch);

                // Outputs of random choice
                KismetHelper.CreateOutputLink(randSwitch, "Link 1", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_9")); //thane
                KismetHelper.CreateOutputLink(randSwitch, "Link 2", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_8")); //jack
                KismetHelper.CreateOutputLink(randSwitch, "Link 3", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_2")); //garrus
                KismetHelper.CreateOutputLink(randSwitch, "Link 4", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_13")); //legion
                KismetHelper.CreateOutputLink(randSwitch, "Link 5", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_6")); //grunt
                KismetHelper.CreateOutputLink(randSwitch, "Link 6", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_0")); //jacob
                KismetHelper.CreateOutputLink(randSwitch, "Link 7", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_7")); //samara
                KismetHelper.CreateOutputLink(randSwitch, "Link 8", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_4")); //mordin
                KismetHelper.CreateOutputLink(randSwitch, "Link 9", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_3")); //tali
                KismetHelper.CreateOutputLink(randSwitch, "Link 10", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_15")); //morinth
                KismetHelper.CreateOutputLink(randSwitch, "Link 11", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_19")); //miranda

                // kasumi
                KismetHelper.CreateOutputLink(randSwitch, "Link 12", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_10")); //kasumi

                // zaeed
                KismetHelper.CreateOutputLink(randSwitch, "Link 13", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_5")); //zaeed

                MERFileSystem.SavePackage(package);
            }

            var biodEndGm2F = MERFileSystem.GetPackageFile(target, "BioD_EndGm2.pcc");
            if (biodEndGm2F != null)
            {
                var package = MEPackageHandler.OpenMEPackage(biodEndGm2F);
                var ts = package.FindExport("TheWorld.PersistentLevel.BioTriggerStream_0");
                var ss = ts.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
                // Make walk4 remain loaded while walk5 is active as enemeis may not yet be cleared out
                var conclusion = ss[8];
                var visibleNames = conclusion.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames");
                if (!visibleNames.Any(x => x.Value == "BioD_EndGm2_300Walk04"))
                {
                    // This has pawns as part of the level so we must make sure it doesn't disappear or player will just see enemies disappear
                    visibleNames.Add(new NameProperty("BioD_EndGm2_300Walk04"));
                }

                ts.WriteProperty(ss);
                MERFileSystem.SavePackage(package);
            }

            var longwalkfile = MERFileSystem.GetPackageFile(target, "BioD_EndGm2_300LongWalk.pcc");
            if (longwalkfile != null)
            {
                // automate TLW
                var package = MEPackageHandler.OpenMEPackage(longwalkfile);
                var seq = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State");
                var stopWalking = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqEvent_SequenceActivated_2");

                // The auto walk delay on Stop Walking
                var delay = MERSeqTools.AddDelay(seq, ThreadSafeRandom.NextFloat(2, 7)); // How long to hold position
                KismetHelper.CreateOutputLink(delay, "Finished", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.BioSeqAct_ResurrectHenchman_0"));
                KismetHelper.CreateOutputLink(stopWalking, "Out", delay);

                // Do not allow targeting the escort
                package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqVar_Bool_8").WriteProperty(new IntProperty(0, "bValue")); // stopped walking
                package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqVar_Bool_14").WriteProperty(new IntProperty(0, "bValue")); // loading from save - we will auto start
                KismetHelper.CreateOutputLink(package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqAct_Toggle_2"), "Out", delay); // post loaded from save init

                // Do not enable autosaves, cause it makes it easy to cheese this area. Bypass the 'savegame' item
                KismetHelper.RemoveOutputLinks(package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.BioSeqAct_ResurrectHenchman_0"));
                KismetHelper.CreateOutputLink(package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.BioSeqAct_ResurrectHenchman_0"), "Out", package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqAct_Gate_2"));


                // Pick a random henchman to go on a date with
                //var determineEscortLog = package.GetUExport(1118);
                //var spawnSeq = package.GetUExport(1598);

                //// disconnect old logic
                //KismetHelper.RemoveAllLinks(determineEscortLog);

                // install new logic
                /*var randSwitch = SeqTools.InstallRandomSwitchIntoSequence(spawnSeq, 12); // don't include theif or veteran as dlc might not be installed
                KismetHelper.CreateOutputLink(determineEscortLog, "Out", randSwitch);


                // Outputs of random choice



                KismetHelper.CreateOutputLink(randSwitch, "Link 1", package.GetUExport(1599)); //thane
                KismetHelper.CreateOutputLink(randSwitch, "Link 2", package.GetUExport(1601)); //jack
                KismetHelper.CreateOutputLink(randSwitch, "Link 3", package.GetUExport(1603)); //garrus
                KismetHelper.CreateOutputLink(randSwitch, "Link 4", package.GetUExport(1605)); //legion
                KismetHelper.CreateOutputLink(randSwitch, "Link 5", package.GetUExport(1607)); //grunt
                KismetHelper.CreateOutputLink(randSwitch, "Link 6", package.GetUExport(1609)); //jacob
                KismetHelper.CreateOutputLink(randSwitch, "Link 7", package.GetUExport(1611)); //samara
                KismetHelper.CreateOutputLink(randSwitch, "Link 8", package.GetUExport(1613)); //mordin
                KismetHelper.CreateOutputLink(randSwitch, "Link 9", package.GetUExport(1615)); //tali
                KismetHelper.CreateOutputLink(randSwitch, "Link 10", package.GetUExport(1619)); //morinth
                KismetHelper.CreateOutputLink(randSwitch, "Link 11", package.GetUExport(1624)); //miranda
                */



                MERFileSystem.SavePackage(package);
            }

            //randomize long walk lengths. 
            // IFP map
            var endwalkexportmap = new Dictionary<string, string>()
            {
                {"BioD_EndGm2_300Walk01", "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_01.SeqAct_Interp_1"},
                {"BioD_EndGm2_300Walk02", "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_02.SeqAct_Interp_1"},
                {"BioD_EndGm2_300Walk03", "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_03.SeqAct_Interp_2"},
                {"BioD_EndGm2_300Walk04", "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_04.SeqAct_Interp_0"},
                {"BioD_EndGm2_300Walk05", "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_05.SeqAct_Interp_4"}
            };

            foreach (var map in endwalkexportmap)
            {
                var file = MERFileSystem.GetPackageFile(target, map.Key + ".pcc");
                if (file != null)
                {
                    var package = MEPackageHandler.OpenMEPackage(file);
                    var export = package.FindExport(map.Value);
                    export.WriteProperty(new FloatProperty(ThreadSafeRandom.NextFloat(.5, 2.5), "PlayRate"));
                    MERFileSystem.SavePackage(package);
                }
            }

            /*foreach (var f in files)
            {
                var package = MEPackageHandler.OpenMEPackage(f);
                var animExports = package.Exports.Where(x => x.ClassName == "InterpTrackAnimControl");
                foreach (var anim in animExports)
                {
                    var animseqs = anim.GetProperty<ArrayProperty<StructProperty>>("AnimSeqs");
                    if (animseqs != null)
                    {
                        foreach (var animseq in animseqs)
                        {
                            var seqname = animseq.GetProp<NameProperty>("AnimSeqName").Value.Name;
                            if (seqname.StartsWith("Walk_"))
                            {
                                var playrate = animseq.GetProp<FloatProperty>("AnimPlayRate");
                                var oldrate = playrate.Value;
                                if (oldrate != 1) Debugger.Break();
                                playrate.Value = ThreadSafeRandom.NextFloat(.2, 6);
                                var data = anim.Parent.Parent as ExportEntry;
                                var len = data.GetProperty<FloatProperty>("InterpLength");
                                len.Value = len.Value * playrate; //this might need to be changed if its not 1
                                data.WriteProperty(len);
                            }
                        }
                    }
                    anim.WriteProperty(animseqs);
                }
                SavePackage(package);
            }*/
        }
    }
}
