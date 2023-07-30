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
            //AutomateTheLongWalk(target, option);
            UpdateSpawnsLongWalk(target, sequenceSupportPackage);


            // Platforming and Final Battles
            //UpdatePreFinalBattle(target, sequenceSupportPackage);
            //UpdateFinalBattle(target, sequenceSupportPackage);

            // Post-CollectorBase
            //UpdatePostCollectorBase(target);

            // RandomizeFlyerSpawnPawns(target);
            //MakeTubesSectionHarder(target);

            //MichaelBayifyFinalFight(target, option);
            // InstallBorger(target); // Change to new texture system
            return true;
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
                allowedPawns, 8,
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
            string[] allowedPawns = new[] { "MERChar_EndGm2.Bomination" }; // Special sequence for spawning will kill these as they fly over and these pawns have timed detonators

            AddRandomSpawns(target, package,
                "TheWorld.PersistentLevel.Main_Sequence.CombatEncounter_MoreDrones.Trooper_SpawnSet01.SeqEvent_SequenceActivated_0",
                allowedPawns, 3,
                "TheWorld.PersistentLevel.Main_Sequence.CombatEncounter_MoreDrones.Trooper_SpawnSet01.SeqVar_Object_2",
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
                "TheWorld.PersistentLevel.Main_Sequence.CombatEncounter_FinalPopUp_Zombies.ZombieRespawn_LastRun03.SeqEvent_SequenceActivated_1",
                allowedPawns, 12,
                "TheWorld.PersistentLevel.Main_Sequence.CombatEncounter_FinalPopUp_Zombies.ZombieRespawn_LastRun03.SeqVar_Object_19",
                1600f, sequenceSupportPackage);

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
                "TheWorld.PersistentLevel.Main_Sequence.Corridor150_Combat_Respawn.SeqAct_Gate_0", allowedPawns, 3,
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

        private static void AutomateTheLongWalk(GameTarget target, RandomizationOption option)
        {
            var prelongwalkfile = MERFileSystem.GetPackageFile(target, "BioD_EndGm2_200Factory.pcc");
            if (prelongwalkfile != null)
            {
                // Pre-long walk selection
                var package = MEPackageHandler.OpenMEPackage(prelongwalkfile);
                var bioticTeamSeq =
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B");

                var activated =
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.SeqEvent_SequenceActivated_0");
                KismetHelper.RemoveAllLinks(activated);

                // install new logic
                var randSwitch =
                    MERSeqTools.InstallRandomSwitchIntoSequence(target, bioticTeamSeq,
                        13); // don't include theif or veteran as dlc might not be installed
                KismetHelper.CreateOutputLink(activated, "Out", randSwitch);

                // Outputs of random choice
                KismetHelper.CreateOutputLink(randSwitch, "Link 1",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_9")); //thane
                KismetHelper.CreateOutputLink(randSwitch, "Link 2",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_8")); //jack
                KismetHelper.CreateOutputLink(randSwitch, "Link 3",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_2")); //garrus
                KismetHelper.CreateOutputLink(randSwitch, "Link 4",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_13")); //legion
                KismetHelper.CreateOutputLink(randSwitch, "Link 5",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_6")); //grunt
                KismetHelper.CreateOutputLink(randSwitch, "Link 6",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_0")); //jacob
                KismetHelper.CreateOutputLink(randSwitch, "Link 7",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_7")); //samara
                KismetHelper.CreateOutputLink(randSwitch, "Link 8",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_4")); //mordin
                KismetHelper.CreateOutputLink(randSwitch, "Link 9",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_3")); //tali
                KismetHelper.CreateOutputLink(randSwitch, "Link 10",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_15")); //morinth
                KismetHelper.CreateOutputLink(randSwitch, "Link 11",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_19")); //miranda

                // kasumi
                KismetHelper.CreateOutputLink(randSwitch, "Link 12",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_10")); //kasumi

                // zaeed
                KismetHelper.CreateOutputLink(randSwitch, "Link 13",
                    package.FindExport(
                        "TheWorld.PersistentLevel.Main_Sequence.Factory_Cutscene_Conversation_Sequence.Select_Team_B.BioSeqAct_PMExecuteTransition_5")); //zaeed

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

        private static void PlatformDestroyer(IMEPackage reaperFightPackage, IMEPackage sequenceSupportPackage)
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

            // Add shepard ragdoll recovery to work around... fun issues with ragdoll
            MERSeqTools.InstallSequenceStandalone(sequenceSupportPackage.FindExport("ShepardRagdollRecovery"),
                reaperFightPackage, null);


            // Do not save, calling method will handle it
        }

        /// <summary>
        /// Makes Reaper destroy platforms and use different weapons
        /// </summary>
        /// <param name="reaperFightP"></param>
        private static void MichaelBayifyFinalFight(IMEPackage reaperFightP, IMEPackage sequenceSupportPackage)
        {
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

            }

            PlatformDestroyer(reaperFightP, sequenceSupportPackage);
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

            MERFileSystem.SavePackage(package);

        }

        private static void UpdateFinalBattle(GameTarget target, IMEPackage sequenceSupportPackage)
        {
            string file = "BioD_EndGm2_430ReaperCombat.pcc";
            using var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, file));

            // Logic here
            MichaelBayifyFinalFight(package, sequenceSupportPackage);
            InstallAtmosphereHandler(package, sequenceSupportPackage);


            MERFileSystem.SavePackage(package);
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

        private static void GenericRandomizeFlyerSpawns(GameTarget target, IMEPackage package, int maxNumNewEnemies,
            EPortablePawnClassification minClassification = EPortablePawnClassification.Mook,
            EPortablePawnClassification maxClassification = EPortablePawnClassification.Boss)
        {
            var aiFactories = package.Exports.Where(x => x.ClassName == "SFXSeqAct_AIFactory").ToList();

            foreach (var aiFactory in aiFactories)
            {
                var seq = SeqTools.GetParentSequence(aiFactory);
                var sequenceObjects = KismetHelper.GetSequenceObjects(seq);
                var objectsInSeq = sequenceObjects.OfType<ExportEntry>().ToList();
                var preGate = objectsInSeq.FirstOrDefault(x => x.ClassName == "SeqAct_Gate"); // should not be null
                var outbound = SeqTools.GetOutboundLinksOfNode(preGate);
                var aifactoryObj =
                    outbound[0][0]
                        .LinkedOp as ExportEntry; //First out on first link. Should point to AIFactory assuming these are all duplicated flyers

                var availablePawns = PawnPorting.PortablePawns.Where(x =>
                    x.Classification >= minClassification && x.Classification <= maxClassification).ToList();
                if (availablePawns.Any())
                {
                    // We can add new pawns to install
                    List<PortablePawn> newPawnsInThisSeq = new List<PortablePawn>();
                    int numEnemies = 1; // 1 is the original amount.
                    List<ExportEntry> allAiFactoriesInSeq = new List<ExportEntry>();
                    allAiFactoriesInSeq.Add(aiFactory); // the original one
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
                            allAiFactoriesInSeq.Add(newAiFactorySeqObj);
                            KismetHelper.AddObjectToSequence(newAiFactorySeqObj, seq, false);

                            // Update the backing factory object
                            var backingFactory =
                                newAiFactorySeqObj.GetProperty<ObjectProperty>("Factory")
                                    .ResolveToEntry(package) as ExportEntry;
                            backingFactory.WriteProperty(new ObjectProperty(package.FindExport(randPawn.BioPawnTypeIFP),
                                "ActorType"));
                            var collection =
                                backingFactory.GetProperty<ArrayProperty<ObjectProperty>>("ActorResourceCollection");
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
                        var randSw = MERSeqTools.InstallRandomSwitchIntoSequence(target, seq, numEnemies);
                        outbound[0][0].LinkedOp = randSw;
                        SeqTools.WriteOutboundLinksToNode(preGate, outbound);

                        // Hook up switch to ai factories
                        for (int i = 0; i < numEnemies; i++)
                        {
                            var linkDesc = $"Link {i + 1}";
                            KismetHelper.CreateOutputLink(randSw, linkDesc,
                                allAiFactoriesInSeq[i]); // switches are indexed at 1
                        }
                    }
                }
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
            foreach (var v in PawnPorting.PortablePawns)
            {
                if (allowedPawns.Contains(v.BioPawnTypeIFP))
                {
                    PawnPorting.PortPawnIntoPackage(target, v, package);
                    var bpt = package.FindExport(v.BioPawnTypeIFP);
                    if (bpt == null)
                        Debugger.Break();
                    bioPawnTypes.Add(bpt);
                }
            }

            if (bioPawnTypes.Count == 0)
            {
                Debugger.Break(); // The list should not be empty
            }

            var pawnTypeList = MERSeqTools.MakeSeqVarList(bioPawnTypes, sequence);
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