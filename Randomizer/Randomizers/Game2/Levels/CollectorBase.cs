using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
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
            using var sequenceSupportPackage = MEPackageHandler.OpenMEPackageFromStream(
                MEREmbedded.GetEmbeddedPackage(target.Game, "SeqPrefabs.SuicideMission.pcc"), "SuicideMission.pcc");

            // Hives are the segment before The Long Walk (pipes)
            //UpdateHives1(target, sequenceSupportPackage); // First combat
            //UpdateHives2(target, sequenceSupportPackage); // First possessed enemy, long open area
            //UpdateHives3(target, sequenceSupportPackage); // Olympic high jump sprint
            //UpdateHives4(target, sequenceSupportPackage); // Run to the final button

            // The Long Walk
            //RandomlyChooseTeams(target, option);
            //AutomateTheLongWalk(target, option);
            //UpdateSpawnsLongWalk(target, sequenceSupportPackage);


            // Platforming and Final Battles
            // UpdatePreFinalBattle(target, sequenceSupportPackage);
            UpdateFinalBattle(target, sequenceSupportPackage);

            // Post-CollectorBase
            // UpdatePostCollectorBase(target);

            // RandomizeFlyerSpawnPawns(target);

            UpdateLevelStreaming(target);
            return true;
        }

        private static void UpdateLevelStreaming(GameTarget target)
        {
            // Fix streaming states for all of long walk
            var biodEndGm2F = MERFileSystem.GetPackageFile(target, "BioD_EndGm2.pcc");
            if (biodEndGm2F != null)
            {
                var package = MERFileSystem.OpenMEPackage(biodEndGm2F);

                //
                {
                    var ts = package.FindExport("TheWorld.PersistentLevel.BioTriggerStream_0");
                    var ss = ts.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");

                    // Find all phase states and ensure all previous walk phases are also loaded and visible.
                    // This prevents previous enemies from despawning 
                    foreach (var state in ss)
                    {
                        var stateName = state.Properties.GetProp<NameProperty>("StateName")?.Value.Name;
                        if (stateName == null || stateName == "None")
                            continue; // Don't care

                        int phaseNum = 0;
                        if (stateName == "SS_WALKCONCLUSION")
                        {
                            // Ensure previous states are set too
                            phaseNum = 5;
                        }
                        else
                        {
                            var phasePos = stateName.IndexOf("PHASE", StringComparison.InvariantCultureIgnoreCase);
                            if (phasePos == -1)
                            {
                                continue; // We don't care about these
                            }

                            phaseNum = int.Parse(stateName.Substring(phasePos + 5));
                        }

                        // Ensure previous states are kept
                        var visibleChunks = state.GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames");
                        var baseName = "BioD_EndGm2_300Walk0";
                        while (phaseNum > 1)
                        {
                            if (visibleChunks.All(x => x.Value != baseName + phaseNum))
                            {
                                // This has pawns as part of the level so we must make sure it doesn't disappear or player will just see enemies disappear
                                visibleChunks.Add(new NameProperty(baseName + phaseNum));
                            }

                            phaseNum--;
                        }
                    }

                    ts.WriteProperty(ss);
                }
                MERFileSystem.SavePackage(package);
            }
        }

        private static void UpdateSpawnsLongWalk(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            UpdateLongWalk2(target, sequenceSupportPackage); // Ahead of the second stopping point
            UpdateLongWalk3(target, sequenceSupportPackage); // At the third stopping point
            UpdateLongWalk5(target, sequenceSupportPackage); // The final run
        }

        /// <summary>
        /// First enemy encounter in TLW
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sequenceSupportPackage"></param>
        private static void UpdateLongWalk2(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_300Walk02.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            string[] allowedPawns = new[] { "MERChar_Enemies.ChargingHusk", "BioChar_Animals.Combat.ELT_Spider" }; // Swarm the player

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.CombatEncounter_FlyerRaid.SeqAct_Delay_1",
                allowedPawns, 6,
                "TheWorld.PersistentLevel.Main_Sequence.CombatEncounter_FlyerRaid.SeqVar_Object_36",
                1500f, sequenceSupportPackage);

            // Remove playpen manipulations

            MERFileSystem.SavePackage(package);
        }

        /// <summary>
        /// First enemy encounter in TLW
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sequenceSupportPackage"></param>
        private static void UpdateLongWalk3(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_300Walk03.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            string[] allowedPawns = new[] { "MERChar_EndGm2.SuicideBomination" }; // Special sequence for spawning will kill these as they fly over and these pawns have timed detonators

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_03.BioSeqAct_PMExecuteTransition_4",
                allowedPawns, 3,
                "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_03.SeqVar_Object_4",
                400f, sequenceSupportPackage, spawnSeqName: "EndGm2RandomEnemySpawnSuicide");

            // Remove playpen manipulations

            MERFileSystem.SavePackage(package);
        }

        private static void UpdateLongWalk5(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            // Run for your lives
            string file = "BioD_EndGm2_300Walk05.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            string[] allowedPawns = new[] { "MERChar_Enemies.ChargingHusk", "MERChar_EndGm2.Bomination" }; // Swarm the player

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_05.BioSeqAct_PMExecuteTransition_2",
                allowedPawns, 12,
                MERSeqTools.CreateNewSquadObject(SeqTools.GetParentSequence(package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_05.BioSeqAct_PMExecuteTransition_2"))).InstancedFullPath,
                900f, sequenceSupportPackage);

            // Remove playpen manipulations

            MERFileSystem.SavePackage(package);
        }

        #region Hives

        private static void UpdateHives1(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_120TheHives.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            string[] allowedPawns = new[] { "MERChar_Enemies.ChargingHusk" }; // We only use husks in the early area
            //string[] allowedPawns = new[] { "MERChar_EndGm2.Bomination" }; // We only use husks in the early area

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.Hives120_Combat_Respawn.Seq_FlyIn_LowerSection.SeqEvent_SequenceActivated_0",
                allowedPawns, 10,
                "TheWorld.PersistentLevel.Main_Sequence.Hives120_Combat_Respawn.Seq_FlyIn_LowerSection.SeqVar_Object_40",
                1500f, sequenceSupportPackage);

            // Remove playpen manipulations
            KismetHelper.RemoveAllLinks(
                package.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Hives120_Playpen_Manipulators.SeqEvent_Touch_0"));
            KismetHelper.RemoveAllLinks(
                package.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Hives120_Playpen_Manipulators.SeqEvent_Touch_1"));
            KismetHelper.RemoveAllLinks(
                package.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Hives120_Playpen_Manipulators.SeqEvent_Touch_3"));

            MERFileSystem.SavePackage(package);
        }

        private static void UpdateHives2(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_140CoverCorridor.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            // Originally was Varren
            // Varren turn into land sharks for some reason
            string[] allowedPawns = new[] { "MERChar_Enemies.GethDestroyerSpawnable" };

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Combat_Respawn.SeqAct_Gate_0", allowedPawns, 2,
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Combat_Respawn.SeqVar_Object_36", 1300f,
                sequenceSupportPackage);

            // Remove playpen manipulations
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Playpen_Manipulators.SeqEvent_Touch_0"));
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Playpen_Manipulators.SeqEvent_Touch_1"));
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Playpen_Manipulators.SeqEvent_Touch_4"));
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Playpen_Manipulators.SeqEvent_Touch_9"));
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Playpen_Manipulators.SeqEvent_Touch_3"));

            MERFileSystem.SavePackage(package);
        }

        private static void UpdateHives3(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_160HoneyCombs.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            string[]
                allowedPawns = new[]
                {
                    "BioChar_Animals.Combat.ELT_Spider"
                }; // Guys that blow up are spawned close to the player to make them panic

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.HoneyCombs160_Combat_Respawn_B.SeqAct_Gate_1", allowedPawns, 4,
                "TheWorld.PersistentLevel.Main_Sequence.HoneyCombs160_Combat_Respawn_B.SeqVar_Object_4", 700f,
                sequenceSupportPackage);

            // Remove playpen manipulations
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.HoneyCombs160_Playpen_Manipulators.SeqEvent_Touch_0"));
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.HoneyCombs160_Playpen_Manipulators.SeqEvent_Touch_1"));

            MERFileSystem.SavePackage(package);
        }

        private static void UpdateHives4(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_180FactoryEntryB.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            string[]
                allowedPawns = new[]
                {
                    "BioChar_Collectors.ELT_Scion"
                }; // A few scions for the final area as it will force player to run for it

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.FactoryEntry180_Combat_RespawnB.SeqAct_Gate_0", allowedPawns, 2,
                "TheWorld.PersistentLevel.Main_Sequence.FactoryEntry180_Combat_RespawnB.SeqVar_Object_36", 3000f,
                sequenceSupportPackage, minSpawnDistance: 1600f);

            // Remove playpen manipulations
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.FactoryEntry180_Playpen_Manipulators.SeqEvent_Touch_6"));
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.FactoryEntry180_Playpen_Manipulators.SeqEvent_Touch_7"));
            KismetHelper.RemoveAllLinks(package.FindExport(
                "TheWorld.PersistentLevel.Main_Sequence.FactoryEntry180_Playpen_Manipulators.SeqEvent_Touch_8"));

            MERFileSystem.SavePackage(package);
        }

        #endregion

        #region The Long Walk

        private static void RandomlyChooseTeams(GameTarget target, RandomizationOption option)
        {
            var files = new string[] { "BioD_EndGm1_310Huddle.pcc", "BioD_EndGm2_200Factory.pcc" };

            foreach (var file in files)
            {
                var teamSelectFile = MERFileSystem.GetPackageFile(target, file);
                var package = MERFileSystem.OpenMEPackage(teamSelectFile);

                var teams = package.Exports.Where(x => x.ClassName == "Sequence" && x.ObjectName.Instanced.StartsWith("Select_Team_")).ToList();

                foreach (var teamSeq in teams)
                {
                    var seqObjects = SeqTools.GetAllSequenceElements(teamSeq).OfType<ExportEntry>().ToList();
                    /*
                    if (false)
                    {
                        // CHoiceGUI is native doesn't actually set lstData... sigh
    
                        var showChoiceGui = seqObjects.FirstOrDefault(x => x.ClassName == "BioSeqAct_ShowChoiceGUI");
                        var seqInt = package.FindExport(teamSeq.InstancedFullPath + ".SeqVar_Int_0");
                        var choiceData = package.FindExport(teamSeq.InstancedFullPath + ".BioSeqVar_ChoiceGUIData_0");
                        var nextNode = MERSeqTools.GetNextNode(showChoiceGui, 0);
                        nextNode = MERSeqTools.GetNextNode(nextNode, 0);
    
                        // Add rand selector
                        var randIndexSelector = SequenceObjectCreator.CreateSequenceObject(package,
                            "MERSeqAct_GetRandomChoiceGUIIndex", MERCaches.GlobalCommonLookupCache);
                        KismetHelper.AddObjectToSequence(randIndexSelector, teamSeq);
                        KismetHelper.CreateVariableLink(randIndexSelector, "ChosenIndex", seqInt);
                        KismetHelper.CreateVariableLink(randIndexSelector, "ChoiceGUIData", choiceData);
                        KismetHelper.CreateOutputLink(randIndexSelector, "Out", nextNode); // Point to after CloseChoiceGUI
    
                        // Repoint to rand selector
                        var outboundNodes = SeqTools.FindOutboundConnectionsToNode(showChoiceGui, seqObjects);
                        foreach (var outboundNode in outboundNodes)
                        {
                            var outboundLinks = SeqTools.GetOutboundLinksOfNode(outboundNode);
                            foreach (var outLink in outboundLinks)
                            {
                                foreach (var linkedOp in outLink)
                                {
                                    if (linkedOp.LinkedOp == showChoiceGui)
                                    {
                                        linkedOp.LinkedOp = randIndexSelector;
                                        linkedOp.InputLinkIdx = 0;
                                    }
                                }
                            }
    
                            SeqTools.WriteOutboundLinksToNode(outboundNode, outboundLinks);
                        }
                    }
                    */


                    var chosenIndex = package.FindExport(teamSeq.InstancedFullPath + ".SeqVar_Int_0");

                    // Count the number of options added

                    // 1. Create the counter
                    var inputInt = MERSeqTools.CreateInt(teamSeq, 0);

                    // Output to count


                    // Create our rand list container and replace ShowGUI logic with it
                    var hackDelay =
                        MERSeqTools.AddDelay(teamSeq,
                            0.01f); // This is a hack so kismet logic runs in order. Otherwise this would require 
                    // significantly rearchitecting the sequence

                    // Add rand selector
                    var randIndexContainer = SequenceObjectCreator.CreateSequenceObject(package,
                        "MERSeqAct_RandIntList", MERCaches.GlobalCommonLookupCache);
                    KismetHelper.AddObjectToSequence(randIndexContainer, teamSeq);
                    KismetHelper.CreateVariableLink(randIndexContainer, "Input", inputInt);
                    KismetHelper.CreateVariableLink(randIndexContainer, "Result", chosenIndex);

                    // Add logic to add each valid choice index to our random int list
                    foreach (var seqObj in seqObjects.Where(x => x.ClassName == "BioSeqAct_AddChoiceGUIElement"))
                    {
                        var choiceIdLink = SeqTools.GetVariableLinksOfNode(seqObj)[8];
                        ExportEntry choiceIdInt;
                        if (choiceIdLink.LinkedNodes.Count == 0)
                        {
                            // This is choice 0 which is default which is why this is null
                            choiceIdInt = MERSeqTools.CreateInt(teamSeq, 0);
                        }
                        else
                        {
                            choiceIdInt = SeqTools.GetVariableLinksOfNode(seqObj)[8].LinkedNodes[0] as ExportEntry;
                            if (choiceIdInt == null)
                                Debugger.Break();
                        }

                        var setInt = MERSeqTools.CreateSetInt(teamSeq, inputInt, choiceIdInt);
                        KismetHelper.CreateOutputLink(seqObj, "Success", setInt);
                        KismetHelper.CreateOutputLink(setInt, "Out", randIndexContainer);
                    }

                    // Get the node after showchoicegui and closegui - we will output to that to skip the UI
                    var showChoiceGui = seqObjects.FirstOrDefault(x => x.ClassName == "BioSeqAct_ShowChoiceGUI");
                    var nextNode = MERSeqTools.GetNextNode(showChoiceGui, 0);
                    nextNode = MERSeqTools.GetNextNode(nextNode, 0);
                    KismetHelper.CreateOutputLink(randIndexContainer, "SetValue",
                        nextNode); // Point to after CloseChoiceGUI

                    // Repoint from ShowGUI to rand selector
                    var outboundNodes = SeqTools.FindOutboundConnectionsToNode(showChoiceGui, seqObjects);
                    foreach (var outboundNode in outboundNodes)
                    {
                        var outboundLinks = SeqTools.GetOutboundLinksOfNode(outboundNode);
                        foreach (var outLink in outboundLinks)
                        {
                            foreach (var linkedOp in outLink)
                            {
                                if (linkedOp.LinkedOp == showChoiceGui)
                                {
                                    linkedOp.LinkedOp = hackDelay;
                                    linkedOp.InputLinkIdx = 0; // SetValue
                                }
                            }
                        }

                        SeqTools.WriteOutboundLinksToNode(outboundNode, outboundLinks);
                    }

                    KismetHelper.CreateOutputLink(hackDelay, "Finished", randIndexContainer, 2); // SetOutput
                }


                MERFileSystem.SavePackage(package);
            }
        }

        private static void AutomateTheLongWalk(GameTarget target, RandomizationOption option)
        {
            var longwalkfile = MERFileSystem.GetPackageFile(target, "BioD_EndGm2_300LongWalk.pcc");
            if (longwalkfile != null)
            {
                // automate TLW
                var package = MEPackageHandler.OpenMEPackage(longwalkfile);
                var seq = package.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State");
                var stopWalking =
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqEvent_SequenceActivated_2");

                // The auto walk delay on Stop Walking
                var delay = MERSeqTools.AddDelay(seq, ThreadSafeRandom.NextFloat(2, 7)); // How long to hold position
                KismetHelper.CreateOutputLink(delay, "Finished",
                            package.FindExport(
                                "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.BioSeqAct_ResurrectHenchman_0"));
                KismetHelper.CreateOutputLink(stopWalking, "Out", delay);

                // Do not allow targeting the escort
                package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqVar_Bool_8")
                    .WriteProperty(new IntProperty(0, "bValue")); // stopped walking
                package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqVar_Bool_14")
                    .WriteProperty(new IntProperty(0, "bValue")); // loading from save - we will auto start
                KismetHelper.CreateOutputLink(
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqAct_Toggle_2"),
                    "Out", delay); // post loaded from save init

                // Do not enable autosaves, cause it makes it easy to cheese this area. Bypass the 'savegame' item
                KismetHelper.RemoveOutputLinks(package.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.BioSeqAct_ResurrectHenchman_0"));
                KismetHelper.CreateOutputLink(
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.BioSeqAct_ResurrectHenchman_0"),
                    "Out",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.SeqAct_Gate_2"));


                // Damage henchmen outside of the bubble
                var hench2Vfx = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.Damage_if_too_Far.HenchB_VFX_OnRange");
                var hench1Vfx = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.LongWalk_Controller.Control_Walking_State.Damage_if_too_Far.HenchA_VFX_OnRange");
                var damage1 = SequenceObjectCreator.CreateSequenceObject(package, "MERSeqAct_CauseDamageVocal", MERCaches.GlobalCommonLookupCache);
                var damage2 = SequenceObjectCreator.CreateSequenceObject(package, "MERSeqAct_CauseDamageVocal", MERCaches.GlobalCommonLookupCache);
                KismetHelper.AddObjectsToSequence(SeqTools.GetParentSequence(hench1Vfx), true, damage1, damage2);

                damage1.WriteProperty(new ObjectProperty(package.FindEntry("SFXGame.SFXDamageType_Environmental"), "DamageType"));
                damage2.WriteProperty(new ObjectProperty(package.FindEntry("SFXGame.SFXDamageType_Environmental"), "DamageType"));
                damage1.WriteProperty(new IntProperty(7, "DamageAmount"));
                damage2.WriteProperty(new IntProperty(7, "DamageAmount"));

                KismetHelper.CreateOutputLink(hench1Vfx, "Took Damage", damage1);
                KismetHelper.CreateOutputLink(hench2Vfx, "Took Damage", damage2);

                MERFileSystem.SavePackage(package);
            }

            //randomize long walk lengths.
            // IFP map
            var endwalkexportmap = new Dictionary<string, string>()
            {
                {
                    "BioD_EndGm2_300Walk01",
                    "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_01.SeqAct_Interp_1"
                },
                {
                    "BioD_EndGm2_300Walk02",
                    "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_02.SeqAct_Interp_1"
                },
                {
                    "BioD_EndGm2_300Walk03",
                    "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_03.SeqAct_Interp_2"
                },
                {
                    "BioD_EndGm2_300Walk04",
                    "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_04.SeqAct_Interp_0"
                },
                {
                    "BioD_EndGm2_300Walk05",
                    "TheWorld.PersistentLevel.Main_Sequence.Henchmen_Patrol_Track_05.SeqAct_Interp_4"
                }
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
        }

        #endregion

        #region Platforming and Final Fights


        private static float DESTROYER_PLAYRATE = 2;

        /// <summary>
        /// Adds a fog that gradually fades in as you damage the reaper, making it harder to see
        /// </summary>
        /// <param name="finalFightPackage"></param>
        /// <param name="sequenceSupportPackage"></param>
        private static void InstallAtmosphereHandler(IMEPackage finalFightPackage, IMEPackage sequenceSupportPackage)
        {
            var scalar =
                finalFightPackage.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.BioSeqAct_ScalarMathUnit_0");
            var healthPercent =
                finalFightPackage.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SeqVar_Int_1");
            var atmoHandler = MERSeqTools.InstallSequenceStandalone(
                sequenceSupportPackage.FindExport("AtmosphereHandler"), finalFightPackage,
                SeqTools.GetParentSequence(scalar));

            KismetHelper.CreateOutputLink(scalar, "Out", atmoHandler);
            KismetHelper.CreateVariableLink(atmoHandler, "HealthPercent", healthPercent);

            // Add the fog
            // Maybe this should be in CombatZone as is 430 streamed out?
            var fog = sequenceSupportPackage.FindExport("HeightFog_0");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, fog,
                finalFightPackage, finalFightPackage.GetLevel(), true, new RelinkerOptionsPackage(), out var newEntry);
            finalFightPackage.AddToLevelActorsIfNotThere(newEntry as ExportEntry);
        }


        private static void AddRandomSpawnsToFinalFight(GameTarget target, IMEPackage reaperFightPackage, IMEPackage sequenceSupportPackage)
        {
            // Logic here
            string[] allowedPawns = new[] { "MERChar_Enemies.ChargingHusk", "MERChar_EndGm2.Bomination" }; // We only use husks in the early area

            var squad1 = MERSeqTools.CreateNewSquadObject(reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop"), "LoopSquad");
            AddRandomSpawns(target, reaperFightPackage,
                "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.SeqAct_Gate_3",
                allowedPawns, 6,
                squad1.InstancedFullPath,
                1500f, sequenceSupportPackage);

            var squad2 = MERSeqTools.CreateNewSquadObject(reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop"), "LoopSquad");
            AddRandomSpawns(target, reaperFightPackage,
                "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.SeqAct_Gate_3",
                allowedPawns, 2,
                squad1.InstancedFullPath,
                1500f, sequenceSupportPackage);
        }

        private static void PlatformDestroyer(GameTarget target, IMEPackage reaperFightPackage, IMEPackage sequenceSupportPackage)
        {
            var sequences = reaperFightPackage.Exports.Where(x =>
                x.ClassName == "Sequence" && x.ObjectName.Instanced.StartsWith("Destroy_Platform_")).ToList();
            foreach (var sequence in sequences)
            {
                var seqObjs = SeqTools.GetAllSequenceElements(sequence).OfType<ExportEntry>().ToList();
                // Update interps
                foreach (var seqObj in seqObjs.Where(x => x.ClassName == "SeqAct_Interp"))
                {
                    bool setGesture = false;
                    // 1. Set gesture speed
                    var interpData = new InterpTools.InterpData(MERSeqTools.GetInterpData(seqObj));
                    foreach (var group in interpData.InterpGroups.Where(x => x.GroupName == "ProtoReaper"))
                    {
                        foreach (var track in group.Tracks.Where(x =>
                                     x.TrackTitle != null && x.TrackTitle.StartsWith("Gesture")))
                        {
                            setGesture = true;
                            var gestures = track.Export.GetProperty<ArrayProperty<StructProperty>>(@"m_aGestures");
                            foreach (var g in gestures)
                            {
                                g.Properties.AddOrReplaceProp(new FloatProperty(DESTROYER_PLAYRATE, "fPlayRate"));
                            }

                            track.Export.WriteProperty(gestures);
                        }
                    }


                    // 2. If there is gesture speed, also set playrate of whole track (this skips platform destroy animation)
                    //if (setGesture)
                    {
                        seqObj.WriteProperty(new FloatProperty(DESTROYER_PLAYRATE, "PlayRate"));
                    }
                }

                // Set ragdolls on pawns standing on platforms as they are destroyed
                var checkIfInVolume = seqObjs.FirstOrDefault(x => x.ClassName == "BioSeqAct_CheckIfInVolume");
                var attachEffect =
                    seqObjs.FirstOrDefault(x =>
                        x.ObjectName.Instanced ==
                        "BioSeqAct_AttachVisualEffect_0"); // This is very specific and depends on compile order of file!

                KismetHelper.RemoveOutputLinks(checkIfInVolume); // remove teleport and get nearest point logic

                var signalRagdolled = MERSeqTools.CreateActivateRemoteEvent(sequence, "PlayerEnteredRagdoll");


                var newSeq = MERSeqTools.InstallSequenceChained(sequenceSupportPackage.FindExport("RagdollIntoAir"),
                    reaperFightPackage, sequence, signalRagdolled, 0);
                KismetHelper.CreateOutputLink(checkIfInVolume, "In Volume", newSeq); // Connect input
                KismetHelper.CreateVariableLink(newSeq, "Pawn", MERSeqTools.CreatePlayerObject(sequence, true));
                KismetHelper.CreateOutputLink(signalRagdolled, "Out", attachEffect); // Finish chain
            }

            // Make reaper blow up platforms much earlier

            var destroyPlatformA = reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_A");
            var destroyPlatformB = reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_B");
            var destroyPlatformC = reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_C");

            // Create the remote event signalers
            var destroyBSignalEvent = MERSeqTools.CreateActivateRemoteEvent(destroyPlatformA, "DestroyPlatformB");
            var destroyCSignalEvent = MERSeqTools.CreateActivateRemoteEvent(destroyPlatformB, "DestroyPlatformC");

            // A -> B
            var retractLog = reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_A.SeqAct_Log_3");
            KismetHelper.RemoveOutputLinks(retractLog); // Interp and voc
            KismetHelper.CreateOutputLink(retractLog, "Out", destroyBSignalEvent);
            KismetHelper.CreateOutputLink(retractLog, "Out", // This is the gate that needs opened so that this logic can pass through once completed. Links to 'Open'
                reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_A.SeqAct_Gate_2"), 1);

            // B -> C
            // Logic is different here
            // Wipes output of 'Completed' and points to our logic
            var interp = reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_B.SeqAct_Interp_3");
            var links = SeqTools.GetOutboundLinksOfNode(interp);
            links[0].Clear();
            links[0].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = destroyCSignalEvent });
            // Opens the passthrough gate
            links[0].Add(new SeqTools.OutboundLink() { InputLinkIdx = 1, LinkedOp = reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_B.SeqAct_Gate_2") });
            SeqTools.WriteOutboundLinksToNode(interp, links);


            // These are receivers from the previous platform destroy to trigger the next animation
            var destroyBEvent = MERSeqTools.CreateSeqEventRemoteActivated(destroyPlatformB, "DestroyPlatformB");
            var destroyCEvent = MERSeqTools.CreateSeqEventRemoteActivated(destroyPlatformC, "DestroyPlatformC");

            // Events close the input gates but don't use them
            KismetHelper.CreateOutputLink(destroyBEvent, "Out", reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_B.SeqAct_Gate_4"), 2);
            KismetHelper.CreateOutputLink(destroyCEvent, "Out", reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_C.SeqAct_Gate_4"), 2);

            // B Platform and reaper plays
            KismetHelper.CreateOutputLink(destroyBEvent, "Out", reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_B.SeqAct_Interp_3"));
            KismetHelper.CreateOutputLink(destroyBEvent, "Out", reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_B.SeqAct_Interp_2"));

            // C Platform and reaper plays
            KismetHelper.CreateOutputLink(destroyCEvent, "Out", reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_C.SeqAct_Interp_3"));
            KismetHelper.CreateOutputLink(destroyCEvent, "Out", reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_C.SeqAct_Interp_2"));

            // Close the gates that reaper uses to normally trigger platforms B and C because we only care about A.
            reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SeqAct_Gate_2").WriteProperty(new BoolProperty(false, "bOpen"));
            reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SeqAct_Gate_4").WriteProperty(new BoolProperty(false, "bOpen"));

            // Make the time after he retracts not a million years
            reaperFightPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.SeqVar_Float_6").WriteProperty(new FloatProperty(1, "FloatValue"));

            // Suicide bombing during this sequence
            string[] allowedPawns = new[] { "MERChar_EndGm2.SuicideBomination" }; // Special sequence for spawning will kill these as they fly over and these pawns have timed detonators

            // Coming up
            AddRandomSpawns(target, reaperFightPackage, "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_A.SeqAct_Gate_4",
                allowedPawns, 2, MERSeqTools.CreateNewSquadObject(destroyPlatformA).InstancedFullPath, 500f, sequenceSupportPackage, spawnSeqName: "EndGm2RandomEnemySpawnSuicide");

            // Destroying B
            AddRandomSpawns(target, reaperFightPackage, destroyBEvent.InstancedFullPath,
                allowedPawns, 2, MERSeqTools.CreateNewSquadObject(destroyPlatformB).InstancedFullPath, 500f, sequenceSupportPackage, spawnSeqName: "EndGm2RandomEnemySpawnSuicide");


            if (false)
            {
                // Old code
                var firstAppearance = reaperFightPackage.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.First_Appearance");
                var destPlatformA = reaperFightPackage.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_A");
                var destPlatformB = reaperFightPackage.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_B");
                var destPlatformC = reaperFightPackage.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Reaper_Attack_Loop.Destroy_Platform_C");


                var outFirstAppearance = SeqTools.GetOutboundLinksOfNode(firstAppearance);
                outFirstAppearance[0][0].LinkedOp = destPlatformA;
                SeqTools.WriteOutboundLinksToNode(firstAppearance, outFirstAppearance);

                var outA = SeqTools.GetOutboundLinksOfNode(destPlatformA);
                outA[0][0].LinkedOp = destPlatformB;
                SeqTools.WriteOutboundLinksToNode(destPlatformA, outA);

                var outB = SeqTools.GetOutboundLinksOfNode(destPlatformB);
                outB[0][0].LinkedOp = destPlatformC;
                SeqTools.WriteOutboundLinksToNode(destPlatformB, outB);
            }

            // Add shepard ragdoll recovery to work around... fun issues with ragdoll
            MERSeqTools.InstallSequenceStandalone(sequenceSupportPackage.FindExport("ShepardRagdollRecovery"),
                reaperFightPackage, null);


            // Do not save, calling method will handle it
        }

        /// <summary>
        /// Makes Reaper destroy platforms and use different weapons, more HP
        /// </summary>
        /// <param name="reaperFightP"></param>
        private static void MichaelBayifyFinalFight(GameTarget target, IMEPackage reaperFightP, IMEPackage sequenceSupportPackage)
        {
            // Give more HP since squadmates do a lot more now
            var loadout = reaperFightP.FindExport("BioChar_Loadouts.Collector.BOS_Reaper");
            var shields = loadout.GetProperty<ArrayProperty<StructProperty>>("ShieldLoadouts");
            // 12000 -> 17000
            shields[0].GetProp<StructProperty>("MaxShields").GetProp<FloatProperty>("X").Value = 17000;
            shields[0].GetProp<StructProperty>("MaxShields").GetProp<FloatProperty>("Y").Value = 17000;
            loadout.WriteProperty(shields);

            // Add reaper weapon handler
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


                var newSeq = MERSeqTools.InstallSequenceChained(
                    sequenceSupportPackage.FindExport("ReaperWeaponHandler"), reaperFightP,
                    SeqTools.GetParentSequence(fwa), fwa, 0);

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

                // Add attack during idle step
                var log = seqObjs.FirstOrDefault(x => x.ClassName == "SeqAct_Log" && x.GetProperty<ArrayProperty<StrProperty>>("m_aObjComment") is ArrayProperty<StrProperty> comments && comments.Count == 1 && comments[0].Value.Contains("Starting Idle"));
                if (log == null)
                {
                    continue;
                }

                // Link to extra firing handler
                var firingHandler = MERSeqTools.InstallSequenceChained(sequenceSupportPackage.FindExport("ReaperExtraWeaponFiringController"), reaperFightP, SeqTools.GetParentSequence(fwa), newSeq, 0);
                KismetHelper.CreateVariableLink(firingHandler, "Reaper", MERSeqTools.CreateFindObject(SeqTools.GetParentSequence(firingHandler), "BOSS_ProtoReaper"));
                KismetHelper.CreateOutputLink(log, "Out", firingHandler);
            }

            AddRandomSpawnsToFinalFight(target, reaperFightP, sequenceSupportPackage);
            PlatformDestroyer(target, reaperFightP, sequenceSupportPackage);
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
                KismetHelper.CreateOutputLink(
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.PostPlatform_CombatFiller.SequenceReference_3"),
                    "SquadStillExists", attackSw);

                // 50/50 to just try to do reaper attack
                KismetHelper.CreateOutputLink(attackSw, "Link 1",
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.PostPlatform_CombatFiller.SeqAct_ActivateRemoteEvent_9"));

                // Automate the platforms one after another
                KismetHelper.RemoveAllLinks(preReaperP.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform01.SeqEvent_Death_2")); //B Plat01 Death
                KismetHelper.RemoveAllLinks(preReaperP.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqEvent_Death_2")); //B Plat02 Death

                // Sub automate - Remove attack completion gate inputs ----
                KismetHelper.RemoveAllLinks(preReaperP.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqEvent_RemoteEvent_6")); //Plat03 Attack complete
                KismetHelper.RemoveAllLinks(preReaperP.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqEvent_RemoteEvent_6")); //Plat02 Attack complete

                //// Sub automate - Remove activate input into gate
                var cmb2activated =
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqEvent_SequenceActivated_0");
                var cmb3activated =
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqEvent_SequenceActivated_2");

                KismetHelper.RemoveAllLinks(cmb2activated);
                KismetHelper.CreateOutputLink(cmb2activated, "Out",
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.BioSeqAct_PMCheckState_10"));

                // Delay the start of platform 3 by 4 seconds to give player a bit more time to handle first two platforms
                // Player will likely have decent weapons by now so they will be better than my testing for sure
                KismetHelper.RemoveAllLinks(cmb3activated);
                var newDelay = EntryCloner.CloneEntry(preReaperP.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqAct_Delay_7"));
                newDelay.WriteProperty(new FloatProperty(4, "Duration"));
                KismetHelper.AddObjectToSequence(newDelay,
                    preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03"), true);

                KismetHelper.CreateOutputLink(cmb3activated, "Out", newDelay);
                KismetHelper.CreateOutputLink(newDelay, "Finished",
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.BioSeqAct_PMCheckState_0"));
                //                preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqAct_Gate_0").RemoveProperty("bOpen"); // Plat03 gate - forces gate open so when reaper attack fires it passes through
                //              preReaperP.FindExport("TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqAct_Gate_2").RemoveProperty("bOpen"); // Plat02 gate - forces gate open so when reaper attack fires it passes through


                // There is no end to Plat03 behavior until tubes are dead
                KismetHelper.CreateOutputLink(
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform01.SeqAct_Interp_4"),
                    "Completed",
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform01.SeqAct_FinishSequence_1")); // Interp completed to Complete in Plat01
                KismetHelper.CreateOutputLink(
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqAct_Interp_4"),
                    "Completed",
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform02.SeqAct_FinishSequence_1")); // Interp completed to Complete in Plat02

                // if possession fails continue the possession loop on plat3 to end of pre-reaper combat
                KismetHelper.CreateOutputLink(
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SFXSeqAct_CollectorPossess_6"),
                    "Failed",
                    preReaperP.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.CombatB_IncomingPlatform03.SeqAct_Delay_7"));




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
                KismetHelper.CreateOutputLink(sraEvent, "Out", newGate,
                    0); // 0 = in, which means fire or queue for fire

                // Hook up the ending of the attack to the gate for 'open' so the gate can be passed through.
                var delay = tubesP.GetUExport(1273);
                KismetHelper.CreateOutputLink(tubesP.GetUExport(103), "Out",
                    delay); // Attack finished (CameraShake_Intimidate) to 2s delay
                KismetHelper.RemoveAllLinks(delay);
                KismetHelper.CreateOutputLink(delay, "Finished", newGate, 1); //2s Delay to open gate

                // Make the gate automatically close itself on pass through, and configure output of gate to next item.
                KismetHelper.CreateOutputLink(newGate, "Out", newGate, 2); // Hook from Out to Close
                KismetHelper.CreateOutputLink(newGate, "Out", tubesP.GetUExport(1340),
                    0); // Hook from Out to Log (bypass the delay, we are repurposing it)

                MERFileSystem.SavePackage(tubesP);
            }
        }

        private static void UpdatePreFinalBattle(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_420CombatZone.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            AutomatePlatforming400(package);
            UnconstipatePathing(package);
            RandomizePreReaperSpawns(target, package);

            MERFileSystem.SavePackage(package);

        }

        private static void UpdateFinalBattle(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_430ReaperCombat.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            MichaelBayifyFinalFight(target, package, sequenceSupportPackage);
            InstallAtmosphereHandler(package, sequenceSupportPackage);
            RandomizePickups(target, package, sequenceSupportPackage);
            MarkDownedSquadmatesDead(package, sequenceSupportPackage);
            ImproveSquadmateAI(package, sequenceSupportPackage);
            MERFileSystem.SavePackage(package);
        }

        private static void RandomizePickups(GameTarget target, IMEPackage package, IMEPackage sequenceSupportPackage)
        {
            // Item to branch from
            var gate = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Add_CollectorPossessed_To_Combat.SequenceReference_4.Sequence_2445.SeqAct_Gate_0");
            var sequence = SeqTools.GetParentSequence(gate);
            KismetHelper.RemoveOutputLinks(gate);

            // The original option
            var awardAmmo = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Add_CollectorPossessed_To_Combat.SequenceReference_4.Sequence_2445.SFXSeqAct_AwardResource_0");

            // The end result
            var toggleHidden = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Add_CollectorPossessed_To_Combat.SequenceReference_4.Sequence_2445.SeqAct_ToggleHidden_2");

            // New option
            var awardMedigel = SequenceObjectCreator.CreateSequenceObject(package, "SFXSeqAct_AwardResource", MERCaches.GlobalCommonLookupCache);
            awardMedigel.WriteProperty(new EnumProperty("MEDIGEL_TREASURE", "ETreasureType", package.Game, "TreasureType"));
            KismetHelper.AddObjectToSequence(awardMedigel, sequence);
            KismetHelper.CreateVariableLink(awardMedigel, "Amount", MERSeqTools.CreateInt(sequence, 2));
            KismetHelper.CreateOutputLink(awardMedigel, "Out", toggleHidden);

            // Install branching
            var randSwitch = MERSeqTools.InstallRandomSwitchIntoSequence(target, sequence, 2);
            KismetHelper.CreateOutputLink(randSwitch, "Link 1", awardAmmo);
            KismetHelper.CreateOutputLink(randSwitch, "Link 2", awardMedigel);

            KismetHelper.CreateOutputLink(gate, "Out", randSwitch);

        }

        private static void MarkDownedSquadmatesDead(IMEPackage package, IMEPackage sequenceSupportPackage)
        {
            var trigger = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SeqAct_Log_7");
            var end = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SequenceReference_20");
            KismetHelper.RemoveOutputLinks(trigger);

            var mark = SequenceObjectCreator.CreateSequenceObject(package, "MERSeqAct_MarkDownedSquadmatesUnloyal", MERCaches.GlobalCommonLookupCache);
            KismetHelper.AddObjectToSequence(mark, SeqTools.GetParentSequence(trigger));
            KismetHelper.CreateOutputLink(trigger, "Out", mark);
            KismetHelper.CreateOutputLink(mark, "Out", end);
        }

        private static void ImproveSquadmateAI(IMEPackage package, IMEPackage sequenceSupportPackage)
        {
            // Installs HenchTargeting sequence to the Main_Sequence
            MERSeqTools.InstallSequenceStandalone(sequenceSupportPackage.FindExport("HenchTargetting"), package);
            var trigger = package.FindExport("TheWorld.PersistentLevel.Main_Sequence.Reaper_Combat_Handler.SEQ_Initialize_Reaper_Combat.BioSeqAct_TurnTowards_1");
            var remoteEvent = MERSeqTools.CreateActivateRemoteEvent(SeqTools.GetParentSequence(trigger), "SetupFinalBattleHench"); // Trigger HenchTargetting
            KismetHelper.CreateOutputLink(trigger, "Out", remoteEvent);

            // Change the tag of a gunship so HenchTargetting can find it
            var eyeLeft = package.FindExport("TheWorld.PersistentLevel.SFXPawn_Targetable_Gunship_6");
            eyeLeft.WriteProperty(new NameProperty("HenchTargetAid", "Tag"));
        }


        private static void RandomizePreReaperSpawns(GameTarget target, IMEPackage preReaperFightP)
        {
            GenericRandomizeAIFactorySpawns(target, preReaperFightP, new [] { ""});

            // Install names
            //TLKBuilder.ReplaceString(7892160, "Indoctrinated Krogan"); //Garm update
            //TLKBuilder.ReplaceString(7892161, "Enthralled Batarian"); //Batarian Commando update
                                                                      //TLKBuilder.ReplaceString(7892162, "Collected Human"); //Batarian Commando update
        }

        /// <summary>
        /// Removes bBlocked from all MantleMarkers in the file
        /// </summary>
        /// <param name="reaper420Pathing"></param>
        private static void UnconstipatePathing(IMEPackage reaper420Pathing)
        {
            // Pathing in final battle is total ass
            // It's no wonder your squadmates do nothing

            // For some reason all the MantleMarkers have bBlocked = true which is preventing 
            // AI from being able to move around, and IDK why
            // This removes them from the final boss area as well as the platforming before it (400 Platforming)

            foreach (var mm in reaper420Pathing.Exports.Where(x => x.ClassName == "MantleMarker"))
            {
                mm.RemoveProperty("bBlocked"); // Get rid of it
            }
        }

        /// <summary>
        /// Changes all AIFactory spawns in the given file
        /// </summary>
        /// <param name="target"></param>
        /// <param name="package"></param>
        /// <param name="maxNumNewEnemies"></param>
        /// <param name="minClassification"></param>
        /// <param name="maxClassification"></param>
        private static void GenericRandomizeAIFactorySpawns(GameTarget target, IMEPackage package, string[] allowedPawns)
        {
            var aiFactories = package.Exports.Where(x => x.ClassName == "SFXSeqAct_AIFactory").ToList();

            foreach (var aiFactory in aiFactories)
            {
                var seq = SeqTools.GetParentSequence(aiFactory);
                var sequenceObjects = KismetHelper.GetSequenceObjects(seq).OfType<ExportEntry>().ToList();

                // Create the assignment object
                var aiFactoryAssignment = SequenceObjectCreator.CreateSequenceObject(package, "MERSeqAct_AssignAIFactoryData", MERCaches.GlobalCommonLookupCache);
                aiFactoryAssignment.WriteProperty(new ObjectProperty(aiFactory, "Factory"));
                var pawnTypeList = MERSeqTools.CreatePawnList(target, seq, allowedPawns);
                KismetHelper.CreateVariableLink(aiFactoryAssignment, "PawnTypes", pawnTypeList);

                // Repoint incoming to spawn to this node instead
                MERSeqTools.RedirectInboundLinks(aiFactory, aiFactoryAssignment);

                // Create outlink to continue spawn
                KismetHelper.CreateOutputLink(aiFactoryAssignment, "Out", aiFactory);
            }
        }

        private static void AutomatePlatforming400(IMEPackage platformController)
        {
            // Remove completion state from squad kills as we won't be using that mechanism
            KismetHelper.RemoveOutputLinks(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform01.SeqAct_Log_2")); //A Platform 01
            KismetHelper.RemoveOutputLinks(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform02.SeqAct_Log_2")); //A Platform 02
            KismetHelper.RemoveOutputLinks(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform03.SeqAct_Log_2")); //A Platform 03
            KismetHelper.RemoveOutputLinks(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform0405.SeqAct_Log_2")); //A Platform 0405 (together)
                                                                                                          // there's final platform with the controls on it. we don't touch it

            // Install delays and hook them up to the complection states
            InstallPlatformAutomation(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform01.SeqEvent_SequenceActivated_0"),
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform01.SeqAct_FinishSequence_1"),
                1); //01 to 02
            InstallPlatformAutomation(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform02.SeqEvent_SequenceActivated_0"),
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform02.SeqAct_FinishSequence_1"),
                2); //02 to 03
            InstallPlatformAutomation(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform03.SeqEvent_SequenceActivated_0"),
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform03.SeqAct_FinishSequence_1"),
                3); //03 to 0405
            InstallPlatformAutomation(
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform0405.SeqEvent_SequenceActivated_0"),
                platformController.FindExport(
                    "TheWorld.PersistentLevel.Main_Sequence.CombatA_IncomingPlatform0405.SeqAct_FinishSequence_0"),
                4); //0405 to 06
        }

        private static void InstallPlatformAutomation(ExportEntry seqActivated, ExportEntry finishSeq, int platIdx)
        {
            var seq =
                seqActivated.GetProperty<ObjectProperty>("ParentSequence")
                    .ResolveToEntry(seqActivated.FileRef) as ExportEntry;

            // Clone a delay object, set timer on it
            var delay = MERSeqTools.AddDelay(seq, ThreadSafeRandom.NextFloat(3f, 10f - platIdx));

            // Point start to delay
            KismetHelper.CreateOutputLink(seqActivated, "Out", delay);

            // Point delay to finish
            KismetHelper.CreateOutputLink(delay, "Finished", finishSeq);
        }
        #endregion

        #region EndGm3 Post-Final Fight

        private static void UpdatePostCollectorBase(GameTarget target)
        {
            InstallBorger(target);
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
                var burgerMDL = PackageTools.PortExportIntoPackage(target, biopEndGm3,
                    burgerPackage.FindExport("Edmonton_Burger_Delux2go.Burger_MDL"));

                // 2. Link up the textures
                TextureHandler.RandomizeExport(target,
                    biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Diff"), null);
                TextureHandler.RandomizeExport(target,
                    biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Norm"), null);
                TextureHandler.RandomizeExport(target,
                    biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Spec"), null);

                // 3. Convert the collector base into lunch or possibly early dinner
                // It's early dinner cause that thing will keep you full all night long
                biopEndGm3.GetUExport(11276).WriteProperty(new ObjectProperty(burgerMDL.UIndex, "SkeletalMesh"));
                biopEndGm3.GetUExport(11282).WriteProperty(new ObjectProperty(burgerMDL.UIndex, "SkeletalMesh"));
                MERFileSystem.SavePackage(biopEndGm3);
            }
        }


        #endregion

        #region Utility

        private static void AddRandomSpawns(GameTarget target, IMEPackage package, string hookupIFP,
            string[] allowedPawns, int numToSpawn, string squadObjectIFP, float radius,
            IMEPackage sequenceSupportPackage,
            float minSpawnDistance = 0.0f, string spawnSeqName = "EndGm2RandomEnemySpawn")
        {

            if (minSpawnDistance > radius)
            {
                Debugger.Break(); // Cannot have swapped radiuses
            }

            var trigger = package.FindExport(hookupIFP);
            var outputName = SeqTools.GetOutlinkNames(trigger)[0];
            var sequence = SeqTools.GetParentSequence(trigger);

            List<ExportEntry> bioPawnTypes = new List<ExportEntry>();
            var pawnTypeList = MERSeqTools.CreatePawnList(target, sequence, allowedPawns);
            var endGm2RandomEnemySpawn = sequenceSupportPackage.FindExport(spawnSeqName);
            //var squad = squadObjectIFP != null ? package.FindExport(squadObjectIFP) : MERSeqTools.CreateSquadObject(sequence);
            var squad = package.FindExport(squadObjectIFP);
            var squadActor = squad.GetProperty<ObjectProperty>("ObjValue").ResolveToExport(package);
            squadActor.RemoveProperty("PlayPenVolumes"); // Allow free roam

            for (int i = 0; i < numToSpawn; i++)
            {
                var delay = MERSeqTools.AddDelay(sequence, ThreadSafeRandom.NextFloat(numToSpawn));
                KismetHelper.CreateOutputLink(trigger, outputName, delay);
                var spawnSeq = MERSeqTools.InstallSequenceStandalone(endGm2RandomEnemySpawn, package, sequence);
                KismetHelper.CreateOutputLink(delay, "Finished", spawnSeq);
                KismetHelper.CreateVariableLink(spawnSeq, "PawnTypes", pawnTypeList);
                KismetHelper.CreateVariableLink(spawnSeq, "Squad", squad);
                KismetHelper.CreateVariableLink(spawnSeq, "Radius", MERSeqTools.CreateFloat(sequence, radius));
                KismetHelper.CreateVariableLink(spawnSeq, "Spawned Flyer", MERSeqTools.CreateObject(sequence, null));

                if (minSpawnDistance > 0f)
                {
                    KismetHelper.CreateVariableLink(spawnSeq, "MinRadius",
                        MERSeqTools.CreateFloat(sequence, minSpawnDistance));
                }
            }
        }

        #endregion

        #region Old or unused
        // Code here was either from ME2R and was changed/cut or was developed and was cut due to bugs/time
        // These are not used but kept here for reference for someone, someday, maybe
        private static void RandomizeStuntHench(GameTarget target)
        {
            var shFile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioP_EndGm_StuntHench.pcc"));

            var originalTriggerStreams = shFile.Exports.Where(x => x.ClassName == @"BioTriggerStream").ToList();
            var triggerStreamProps = originalTriggerStreams
                .Select(x => x.GetProperty<ArrayProperty<StructProperty>>("StreamingStates"))
                .ToList(); // These are the original streaming states in same order as original
            triggerStreamProps.Shuffle();

            for (int i = 0; i < originalTriggerStreams.Count; i++)
            {
                var oTrigStream = originalTriggerStreams[i];
                var newPropSet = triggerStreamProps.PullFirstItem();
                var trigStreams = oTrigStream.GetProperty<ArrayProperty<StructProperty>>("StreamingStates");
                trigStreams[1].Properties
                    .AddOrReplaceProp(newPropSet[1].Properties.GetProp<ArrayProperty<NameProperty>>("LoadChunkNames"));
                trigStreams[2].Properties.AddOrReplaceProp(newPropSet[2].Properties
                    .GetProp<ArrayProperty<NameProperty>>("VisibleChunkNames"));
                oTrigStream.WriteProperty(trigStreams);
            }

            MERFileSystem.SavePackage(shFile);
        }

        #endregion
    }
}