using System;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Kismet;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class CollectorBase
    {
        public static bool PerformRandomization(RandomizationOption option)
        {
            RandomizeTheLongWalk(option);
            AutomatePlatforming400(option);
            InstallBorger();
            //RandomizeTheFinalBattle(option);
            return true;
        }

        private static void InstallBorger()
        {
            var endGame3F = MERFileSystem.GetPackageFile("BioP_EndGm3.pcc");
            if (endGame3F != null && File.Exists(endGame3F))
            {
                var biopEndGm3 = MEPackageHandler.OpenMEPackage(endGame3F);

                var packageBin = Utilities.GetEmbeddedStaticFilesBinaryFile("Delux2go_Edmonton_Burger.pcc");
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

        class FinalBattlePawn
        {
            /// <summary>
            /// Source package
            /// </summary>
            public string PackageName { get; set; }
            /// <summary>
            /// The loadout UIndex for the pawn
            /// </summary>
            public int BioPawnChallengeScaledTypeUIndex { get; set; }
            /// <summary>
            /// The full path to the class of the pawn. For actor factory
            /// </summary>
            public string PawnClassFullName { get; set; }
            /// <summary>
            /// The MDL asset fullname. For actor factory
            /// </summary>
            public string MDLAssetFullName { get; set; }
            /// <summary>
            /// Function that is invoked when porting has completed
            /// </summary>
            public Action<IMEPackage> AdjustmentFunc { get; set; }
        }

        private static FinalBattlePawn[] FinalBattlePawnTypes = new[]
        {

            // Spawns one time
            new FinalBattlePawn() {PackageName = "BioP_ProCer.pcc", BioPawnChallengeScaledTypeUIndex = 3417, PawnClassFullName = "SFXGamePawns.SFXPawn_HeavyMech", MDLAssetFullName = "BIOG_CBT_MHV_NKD_R.NKDa.CBT_MHV_NKDa_MDL"}, // Confirmed working
            
            // Have never seen spawn
            new FinalBattlePawn() {PackageName = "BioP_RprGtA.pcc", BioPawnChallengeScaledTypeUIndex = 3073, PawnClassFullName = "SFXGamePawns.SFXPawn_Scion", MDLAssetFullName = "BIOG_SCI_ARM_NKD_R.NKDa.SCI_ARM_NKDa_MDL"}, // Might require imports. Might need to run code on import check
            
            // Can't be used for various reasons :l
            //new FinalBattlePawn() {PackageName = "BioP_OmgPrA.pcc", BioPawnChallengeScaledTypeUIndex = 2060, PawnClassFullName = "SFXGamePawns.SFXPawn_Vorcha", MDLAssetFullName = "BIOG_SCI_ARM_NKD_R.NKDa.SCI_ARM_NKDa_MDL"}, // Might require imports. Might need to run code on import check
            //new FinalBattlePawn() {PackageName = "BioH_Wilson.pcc", BioPawnChallengeScaledTypeUIndex = 5249, PawnClassFullName = "SFXGamePawns.SFXPawn_Wilson", MDLAssetFullName = "BIOG_HMM_ARM_CTH_R.CTHb.HMM_ARM_CTHb_MDL", AdjustmentFunc=AdjustWilsonAI}, // AI might need adjusted?
        };


        private static void RandomizeTheFinalBattle(RandomizationOption option)
        {
            // Port in new enemies.
            // Change the ActorFactories to use them
            // This will greatly change the final battle

            var bossFightP = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile("BioD_EndGm2_430ReaperCombat.pcc"));

            List<FinalBattlePawn> fbp = new List<FinalBattlePawn>(FinalBattlePawnTypes);
            fbp.Shuffle();

            var actorFactories = bossFightP.Exports.Where(x => x.ClassName == "BioActorFactory");
            foreach (var af in actorFactories.ToList())
            {
                var props = af.GetProperties();
                //if (ThreadSafeRandom.Next(3) != 0)
                {
                    // Change it
                    var pawninfo = fbp.RandomElement();
                    // See if already ported in
                    var existingClass = bossFightP.FindExport(pawninfo.PawnClassFullName);
                    var existingMDL = bossFightP.FindExport(pawninfo.MDLAssetFullName);

                    if (existingMDL == null || existingClass == null)
                    {
                        // Port in
                        var sourceP = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(pawninfo.PackageName));
                        var portedInItem = PackageTools.PortExportIntoPackage(bossFightP, sourceP.GetUExport(pawninfo.BioPawnChallengeScaledTypeUIndex));

                        pawninfo.AdjustmentFunc?.Invoke(bossFightP);

                        existingClass = bossFightP.FindExport(pawninfo.PawnClassFullName);
                        existingMDL = bossFightP.FindExport(pawninfo.MDLAssetFullName);
                        if (existingMDL == null || existingClass == null)
                        {
                            Debugger.Break();
                        }
                    }

                    props.GetProp<ObjectProperty>("ActorType").Value = existingClass.UIndex;
                    var resources = props.GetProp<ArrayProperty<ObjectProperty>>("ActorResourceCollection");
                    resources.Clear();
                    resources.Add(new ObjectProperty(existingMDL.UIndex));
                    af.WriteProperties(props);
                }
            }
            MERFileSystem.SavePackage(bossFightP);

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
