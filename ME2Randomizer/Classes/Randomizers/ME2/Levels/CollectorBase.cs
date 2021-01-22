using System;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class CollectorBase
    {
        public static bool PerformRandomization(RandomizationOption option)
        {
            RandomizeTheLongWalk(option);
            InstallBorger();
            RandomizeTheFinalBattle(option);
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
            new FinalBattlePawn() {PackageName = "BioP_ProCer.pcc", BioPawnChallengeScaledTypeUIndex = 3417, PawnClassFullName = "SFXGamePawns.SFXPawn_HeavyMech", MDLAssetFullName = "BIOG_CBT_MHV_NKD_R.NKDa.CBT_MHV_NKDa_MDL"}, // Confirmed working
            new FinalBattlePawn() {PackageName = "BioD_SunTlA_205Colossus.pcc", BioPawnChallengeScaledTypeUIndex = 2420, PawnClassFullName = "SFXGamePawns.SFXPawn_Colossus", MDLAssetFullName = "BIOG_RBT_TNK_NKD_R.NKDa.RBT_TNK_NKDa_MDL"}, // Might require imports. Might need to run code on import check
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

        private static void RandomizeTheLongWalk(RandomizationOption option)
        {
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
