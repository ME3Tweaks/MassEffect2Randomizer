using System;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Kismet;
using ME2Randomizer.Classes.Randomizers.ME2.Enemy;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class CollectorBase
    {
        public static bool PerformRandomization(RandomizationOption option)
        {
            RandomizeFlyerSpawnPawns();
            MakeTubesSectionHarder();
            RandomizeTheLongWalk(option);
            AutomatePlatforming400(option);
            InstallBorger();
            //RandomizeTheFinalBattle(option);
            return true;
        }

        private static void MakeTubesSectionHarder()
        {
            var preReaperF = MERFileSystem.GetPackageFile("BioD_EndGm2_420CombatZone.pcc");
            if (preReaperF != null && File.Exists(preReaperF))
            {
                var preReaperP = MEPackageHandler.OpenMEPackage(preReaperF);

                // Open tubes on kills to start the attack (post platforms)----------------------
                var seq = preReaperP.GetUExport(15190);

                var attackSw = SeqTools.InstallRandomSwitchIntoSequence(seq, 2); //50% chance

                // killed squad member -> squad still exists to 50/50 sw
                KismetHelper.CreateOutputLink(preReaperP.GetUExport(15298), "SquadStillExists", attackSw);

                // 50/50 to just try to do reaper attack
                KismetHelper.CreateOutputLink(attackSw, "Link 1", preReaperP.GetUExport(14262));

                // Automate the platforms one after another
                KismetHelper.RemoveAllLinks(preReaperP.GetUExport(15010)); //B Plat01 Death
                KismetHelper.RemoveAllLinks(preReaperP.GetUExport(15011)); //B Plat02 Death

                // Sub automate - Remove attack completion gate inputs ----
                KismetHelper.RemoveAllLinks(preReaperP.GetUExport(15025)); //Plat03 Attack complete
                KismetHelper.RemoveAllLinks(preReaperP.GetUExport(15029)); //Plat02 Attack complete

                //// Sub automate - Remove activate input into gate
                var cmb2activated = preReaperP.GetUExport(15082);
                var cmb3activated = preReaperP.GetUExport(15087);

                KismetHelper.RemoveAllLinks(cmb2activated);
                KismetHelper.CreateOutputLink(cmb2activated, "Out", preReaperP.GetUExport(2657));

                // Delay the start of platform 3 by 4 seconds to give player a bit more time to handle first two platforms
                // Player will likely have decent weapons by now so they will be better than my testing for sure
                KismetHelper.RemoveAllLinks(cmb3activated);
                var newDelay = EntryCloner.CloneEntry(preReaperP.GetUExport(14307));
                newDelay.WriteProperty(new FloatProperty(4, "Duration"));
                KismetHelper.AddObjectToSequence(newDelay, preReaperP.GetUExport(15183), true);

                KismetHelper.CreateOutputLink(cmb3activated, "Out", newDelay);
                KismetHelper.CreateOutputLink(newDelay, "Finished", preReaperP.GetUExport(2659));
                //                preReaperP.GetUExport(14451).RemoveProperty("bOpen"); // Plat03 gate - forces gate open so when reaper attack fires it passes through
                //              preReaperP.GetUExport(14450).RemoveProperty("bOpen"); // Plat02 gate - forces gate open so when reaper attack fires it passes through


                // There is no end to Plat03 behavior until tubes are dead
                KismetHelper.CreateOutputLink(preReaperP.GetUExport(14469), "Completed", preReaperP.GetUExport(14374)); // Interp completed to Complete in Plat01
                KismetHelper.CreateOutputLink(preReaperP.GetUExport(14470), "Completed", preReaperP.GetUExport(14379)); // Interp completed to Complete in Plat02

                // if possession fails continue the possession loop on plat3 to end of pre-reaper combat
                KismetHelper.CreateOutputLink(preReaperP.GetUExport(16414), "Failed", preReaperP.GetUExport(14307));




                MERFileSystem.SavePackage(preReaperP);
            }
        }

        private static void GateTubesAttack()
        {
            // This doesn't actually work like expected, it seems gate doesn't store input value

            // Adds a gate to the tubes attack to ensure it doesn't fire while the previous attack is running still.
            var tubesF = MERFileSystem.GetPackageFile("BioD_EndGm2_425ReaperTubes.pcc");
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

        private static void RandomizeFlyerSpawnPawns()
        {
            string[] files =
            {
                "BioD_EndGm2_420CombatZone.pcc",
                "BioD_EndGm2_430ReaperCombat.pcc"
            };
            ChangeFlyersInFiles(files);

            // Install names
            TLKHandler.ReplaceString(7892160, "Indoctrinated Krogan"); //Garm update
            TLKHandler.ReplaceString(7892161, "Enthralled Batarian"); //Batarian Commando update
            //TLKHandler.ReplaceString(7892162, "Collected Human"); //Batarian Commando update
        }

        private static void ChangeFlyersInFiles(string[] files)
        {
            foreach (var f in files)
            {
                var fPath = MERFileSystem.GetPackageFile(f);
                if (fPath != null && File.Exists(fPath))
                {
                    var package = MEPackageHandler.OpenMEPackage(fPath);
                    GenericRandomizeFlyerSpawns(package, 3);
                    MERFileSystem.SavePackage(package);
                }
            }
        }

        private static void GenericRandomizeFlyerSpawns(IMEPackage package, int maxNumNewEnemies, EPortablePawnClassification minClassification = EPortablePawnClassification.Mook, EPortablePawnClassification maxClassification = EPortablePawnClassification.Boss)
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
                            PawnPorting.PortPawnIntoPackage(randPawn, package);
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
                        var randSw = SeqTools.InstallRandomSwitchIntoSequence(flySeq, numEnemies);
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

        private static void InstallBorger()
        {
            var endGame3F = MERFileSystem.GetPackageFile("BioP_EndGm3.pcc");
            if (endGame3F != null && File.Exists(endGame3F))
            {
                var biopEndGm3 = MEPackageHandler.OpenMEPackage(endGame3F);

                var packageBin = MERUtilities.GetEmbeddedStaticFilesBinaryFile("Delux2go_Edmonton_Burger.pcc");
                var burgerPackage = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(packageBin));

                // 1. Add the burger package
                var burgerMDL = PackageTools.PortExportIntoPackage(biopEndGm3, burgerPackage.FindExport("Edmonton_Burger_Delux2go.Burger_MDL"));

                // 2. Link up the textures
                TFCBuilder.RandomizeExport(biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Diff"), null);
                TFCBuilder.RandomizeExport(biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Norm"), null);
                TFCBuilder.RandomizeExport(biopEndGm3.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Spec"), null);

                // 3. Convert the collector base into lunch or possibly early dinner
                // It's early dinner cause that thing will keep you full all night long
                biopEndGm3.GetUExport(11276).WriteProperty(new ObjectProperty(burgerMDL.UIndex, "SkeletalMesh"));
                biopEndGm3.GetUExport(11282).WriteProperty(new ObjectProperty(burgerMDL.UIndex, "SkeletalMesh"));
                MERFileSystem.SavePackage(biopEndGm3);
            }
        }


        private static void AutomatePlatforming400(RandomizationOption option)
        {
            var platformControllerF = MERFileSystem.GetPackageFile("BioD_EndGm2_420CombatZone.pcc");
            if (platformControllerF != null)
            {
                var platformController = MEPackageHandler.OpenMEPackage(platformControllerF);
                var delayToClone = platformController.GetUExport(14314);

                // Remove completion state from squad kills as we won't be using that mechanism
                KismetHelper.RemoveOutputLinks(platformController.GetUExport(14488)); //A Platform 01
                KismetHelper.RemoveOutputLinks(platformController.GetUExport(14496)); //A Platform 02
                KismetHelper.RemoveOutputLinks(platformController.GetUExport(14504)); //A Platform 03
                KismetHelper.RemoveOutputLinks(platformController.GetUExport(14513)); //A Platform 0405 (together)
                                                                                      // there's final platform with the controls on it. we don't touch it

                // Install delays and hook them up to the complection states
                InstallPlatformAutomation(platformController.GetUExport(15057), delayToClone, platformController.GetUExport(14353), 1); //01 to 02
                InstallPlatformAutomation(platformController.GetUExport(15063), delayToClone, platformController.GetUExport(14358), 2); //02 to 03
                InstallPlatformAutomation(platformController.GetUExport(15067), delayToClone, platformController.GetUExport(14363), 3); //03 to 0405
                InstallPlatformAutomation(platformController.GetUExport(15072), delayToClone, platformController.GetUExport(14368), 4); //0405 to 06

                MERFileSystem.SavePackage(platformController);
            }
        }

        private static void InstallPlatformAutomation(ExportEntry seqActivated, ExportEntry delayToClone, ExportEntry finishSeq, int platIdx)
        {
            var seq = seqActivated.GetProperty<ObjectProperty>("ParentSequence").ResolveToEntry(seqActivated.FileRef) as ExportEntry;

            // Clone a delay object, set timer on it
            var delay = delayToClone.Clone();
            seqActivated.FileRef.AddExport(delay);
            KismetHelper.AddObjectToSequence(delay, seq, true);
            delay.WriteProperty(new FloatProperty(ThreadSafeRandom.NextFloat(3f, 10f - platIdx), "Duration"));

            // Point start to delay
            KismetHelper.CreateOutputLink(seqActivated, "Out", delay);

            // Point delay to finish
            KismetHelper.CreateOutputLink(delay, "Finished", finishSeq);
        }


        private static void RandomizeTheLongWalk(RandomizationOption option)
        {
            var prelongwalkfile = MERFileSystem.GetPackageFile("BioD_EndGm2_200Factory.pcc");
            if (prelongwalkfile != null)
            {
                // Pre-long walk selection
                var package = MEPackageHandler.OpenMEPackage(prelongwalkfile);
                var bioticTeamSeq = package.GetUExport(8609);

                var activated = package.GetUExport(8484);
                KismetHelper.RemoveAllLinks(activated);

                // install new logic
                var randSwitch = SeqTools.InstallRandomSwitchIntoSequence(bioticTeamSeq, 13); // don't include theif or veteran as dlc might not be installed
                KismetHelper.CreateOutputLink(activated, "Out", randSwitch);

                // Outputs of random choice
                KismetHelper.CreateOutputLink(randSwitch, "Link 1", package.GetUExport(1420)); //thane
                KismetHelper.CreateOutputLink(randSwitch, "Link 2", package.GetUExport(1419)); //jack
                KismetHelper.CreateOutputLink(randSwitch, "Link 3", package.GetUExport(1403)); //garrus
                KismetHelper.CreateOutputLink(randSwitch, "Link 4", package.GetUExport(1399)); //legion
                KismetHelper.CreateOutputLink(randSwitch, "Link 5", package.GetUExport(1417)); //grunt
                KismetHelper.CreateOutputLink(randSwitch, "Link 6", package.GetUExport(1395)); //jacob
                KismetHelper.CreateOutputLink(randSwitch, "Link 7", package.GetUExport(1418)); //samara
                KismetHelper.CreateOutputLink(randSwitch, "Link 8", package.GetUExport(1415)); //mordin
                KismetHelper.CreateOutputLink(randSwitch, "Link 9", package.GetUExport(1405)); //tali
                KismetHelper.CreateOutputLink(randSwitch, "Link 10", package.GetUExport(1401)); //morinth
                KismetHelper.CreateOutputLink(randSwitch, "Link 11", package.GetUExport(1402)); //miranda

                // kasumi
                if (MERFileSystem.GetPackageFile("BioH_Thief_00.pcc") != null)
                {
                    KismetHelper.CreateOutputLink(randSwitch, "Link 12", package.GetUExport(1396)); //kasumi
                }

                // zaeed
                if (MERFileSystem.GetPackageFile("BioH_Veteran_00.pcc") != null)
                {
                    KismetHelper.CreateOutputLink(randSwitch, "Link 13", package.GetUExport(1416)); //zaeed
                }

                MERFileSystem.SavePackage(package);
            }

            var biodEndGm2F = MERFileSystem.GetPackageFile("BioD_EndGm2.pcc");
            if (biodEndGm2F != null)
            {
                var package = MEPackageHandler.OpenMEPackage(biodEndGm2F);
                var ts = package.GetUExport(7);
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

            var longwalkfile = MERFileSystem.GetPackageFile("BioD_EndGm2_300LongWalk.pcc");
            if (longwalkfile != null)
            {
                // automate TLW

                var package = MEPackageHandler.OpenMEPackage(longwalkfile);
                var seq = package.GetUExport(1629);
                var stopWalking = package.GetUExport(1569);

                // The auto walk delay on Stop Walking
                var delay = package.GetUExport(806).Clone();
                package.AddExport(delay);
                delay.WriteProperty(new FloatProperty(ThreadSafeRandom.NextFloat(2, 7), "Duration")); // how long to wait until auto walk
                KismetHelper.AddObjectToSequence(delay, seq, true);
                KismetHelper.CreateOutputLink(delay, "Finished", package.GetUExport(156));
                KismetHelper.CreateOutputLink(stopWalking, "Out", delay);

                // Do not allow targeting the escort
                package.GetUExport(1915).WriteProperty(new IntProperty(0, "bValue")); // stopped walking
                package.GetUExport(1909).WriteProperty(new IntProperty(0, "bValue")); // loading from save - we will auto start
                KismetHelper.CreateOutputLink(package.GetUExport(1232), "Out", delay); // post loaded from save init

                // Do not enable autosaves, cause it makes it easy to cheese this area. Bypass the 'savegame' item
                KismetHelper.RemoveOutputLinks(package.GetUExport(156));
                KismetHelper.CreateOutputLink(package.GetUExport(156), "Out", package.GetUExport(1106));

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
            var endwalkexportmap = new Dictionary<string, int>()
            {
                {"BioD_EndGm2_300Walk01", 40},
                {"BioD_EndGm2_300Walk02", 5344},
                {"BioD_EndGm2_300Walk03", 8884},
                {"BioD_EndGm2_300Walk04", 6370},
                {"BioD_EndGm2_300Walk05", 3190}
            };

            foreach (var map in endwalkexportmap)
            {
                var file = MERFileSystem.GetPackageFile(map.Key + ".pcc");
                if (file != null)
                {
                    var package = MEPackageHandler.OpenMEPackage(file);
                    var export = package.GetUExport(map.Value);
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
