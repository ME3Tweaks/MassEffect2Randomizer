using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.Enemy
{
    public enum EPortablePawnClassification
    {
        Mook,
        Subboss,
        Boss
    }

    public class PortablePawn
    {
        /// <summary>
        /// The filename that contains this pawn
        /// </summary>
        public string PackageFilename { get; set; }
        /// <summary>
        /// The full path to the BioChallengeScaledPawnType object
        /// </summary>
        public string BioPawnTypeIFP { get; set; }
        /// <summary>
        /// The asset to port in. Sometimes you don't wnat the BioChallengeScaledPawnType as it won't include things like models (for example, SFXPawn_Garm)
        /// </summary>
        public string AssetToPortIn { get; set; }
        /// <summary>
        /// Assets for ActorFactory
        /// </summary>
        public string[] AssetPaths { get; set; }
        /// <summary>
        /// If this is a corrected package (stored internal to executable)
        /// </summary>
        public bool IsCorrectedPackage { get; set; }
        /// <summary>
        /// Full path to the class of this pawn
        /// </summary>
        public string PawnClassPath { get; internal set; }
        /// <summary>
        /// How strong this pawn is
        /// </summary>
        public EPortablePawnClassification Classification { get; set; }
        /// <summary>
        /// List of textures that should be installed when this pawn is ported in. Key is the full asset path, value is the texture to install
        /// </summary>
        public RTexture2D[] TextureUpdates { get; set; }
    }
    public class PawnPorting
    {
        public static PortablePawn[] PortablePawns = new[]
        {
            // YMIR Mech
            //new PortablePawn()
            //{
            //    PackageFilename = "BioP_ProCer.pcc",
            //    BioPawnTypeIFP = "BioChar_Mechs.SUB_HeavyWeaponMech",
            //    AssetToPortIn = "BioChar_Mechs.SUB_HeavyWeaponMech",
            //    AssetPaths = new[] {
            //        "BIOG_CBT_MHV_NKD_R.NKDa.CBT_MHV_NKDa_MDL",
            //    },
            //    PawnClassPath = "SFXGamePawns.SFXPawn_HeavyMech",
            //    Classification = EPortablePawnClassification.Subboss,
            //    IsCorrectedPackage = false
            //},

            // Bombinatiton
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Bombination3.pcc",
                BioPawnTypeIFP = "MERChar_EndGm2.Bomination",
                AssetToPortIn = "MERChar_EndGm2.Bomination",
                AssetPaths = new[] {
                    "BIOG_ZMB_ARM_NKD_R.NKDd.ZMB_ARM_NKDd_MDL",
                    "BIOG_ZMB_ARM_NKD_R.NKDd.ZMB_ARM_NKDd_MAT_1a"
                },
                PawnClassPath = "MERGamePawns.SFXPawn_Bombination",
                Classification = EPortablePawnClassification.Mook,
                IsCorrectedPackage = false
            },
            // Bombinatiton - Suicide version (faster cast on the power to fix flying bug)
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Bombination3.pcc",
                BioPawnTypeIFP = "MERChar_EndGm2.SuicideBomination",
                AssetToPortIn = "MERChar_EndGm2.SuicideBomination",
                AssetPaths = new[] {
                    "BIOG_ZMB_ARM_NKD_R.NKDd.ZMB_ARM_NKDd_MDL",
                    "BIOG_ZMB_ARM_NKD_R.NKDd.ZMB_ARM_NKDd_MAT_1a"
                },
                PawnClassPath = "MERGamePawns.SFXPawn_BombinationSuicide",
                Classification = EPortablePawnClassification.Mook,
                IsCorrectedPackage = false
            },

            // Husk
            new PortablePawn()
            {
                PackageFilename = "BioD_ShpCr2_170HubRoom2.pcc",
                BioPawnTypeIFP = "BioChar_Collectors.SWARM_BlueHusk",
                AssetToPortIn = "BioChar_Collectors.SWARM_BlueHusk",
                AssetPaths = new[] {
                    "BIOG_ZMB_ARM_NKD_R.NKDa.ZMBLite_ARM_NKDa_MDL",
                    "BIOG_ZMB_ARM_NKD_R.NKDa.ZMB_ARM_NKDa_MAT_1a"
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_HuskLite",
                Classification = EPortablePawnClassification.Mook,
                IsCorrectedPackage = false
            },

            // Charging husk - charges immediately
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_ChargingHusk.pcc",
                BioPawnTypeIFP = "MERChar_Enemies.ChargingHusk",
                AssetToPortIn = "MERChar_Enemies.ChargingHusk",
                AssetPaths = new[] {
                    "BIOG_ZMB_ARM_NKD_R.NKDa.ZMBLite_ARM_NKDa_MDL",
                    "BIOG_ZMB_ARM_NKD_R.NKDa.ZMB_ARM_NKDa_MAT_1a"
                },
                PawnClassPath = "MERGamePawns.SFXPawn_ChargingHusk",
                Classification = EPortablePawnClassification.Mook,
                IsCorrectedPackage = false
            },

            // Klixen
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Spider.pcc",
                BioPawnTypeIFP = "BioChar_Animals.Combat.ELT_Spider",
                AssetToPortIn = "BioChar_Animals.Combat.ELT_Spider",
                AssetPaths = new[] {
                    "biog_cbt_rac_nkd_r.NKDa.CBT_RAC_NKDa_MDL",
                    "EffectsMaterials.Users.Creatures.CBT_SPD_NKD_MAT_1a_USER",
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_Spider",
            },

            // Scion
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Scion.pcc",
                BioPawnTypeIFP = "BioChar_Collectors.ELT_Scion",
                AssetToPortIn = "BioChar_Collectors.ELT_Scion",
                AssetPaths = new[] {
                    // I don't think these are really necessary, technically...
                    "BIOG_SCI_ARM_NKD_R.NKDa.SCI_ARM_NKDa_MDL",
                    "BIOG_SCI_ARM_NKD_R.NKDa.SCI_ARM_NKDa_MAT_1a",
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_Scion",
            },

            // Varren - they don't work properly when flown in
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Varren.pcc",
                BioPawnTypeIFP = "MERChar_Enemies.Animal.VarrenSpawnable",
                AssetToPortIn = "MERChar_Enemies.Animal.VarrenSpawnable",
                AssetPaths = new[] {
                    "BIOG_CBT_VAR_NKD_R.NKDa.CBT_VAR_NKDa_MAT_2a",
                    "BIOG_CBT_VAR_NKD_R.NKDa.CBT_VAR_NKDa_MAT_2b",
                    "BIOG_CBT_VAR_NKD_R.NKDa.CBT_VAR_NKDa_MDL",
                    "BIOG_CBT_VAR_NKD_R.NKDa.CBT_VAR_NKDa_MAT_3a",
                    "BIOG_CBT_VAR_NKD_R.NKDa.CBT_VAR_NKDa_MAT_3b"
                },
                PawnClassPath = "MERGamePawns.SFXPawn_VarrenFull",
            },

            new PortablePawn()
            {
                PackageFilename = "SFXPawn_GethDestroyer.pcc",
                BioPawnTypeIFP = "MERChar_Enemies.GethDestroyerSpawnable",
                AssetToPortIn = "MERChar_Enemies.GethDestroyerSpawnable",
                AssetPaths = new string[] {
                    // Assets are already referenced by custom pawn
                },
                PawnClassPath = "MERGamePawns.SFXPawn_GethDestroyerFull",
            },

            //Geth Prime. AI doesn't let him climb over shit so he's pretty stationary
            // He's too strong given the other changes made in LE2R
            //new PortablePawn()
            //{
            //    PackageFilename = "BioPawn_GethPrime.pcc",
            //    BioPawnTypeIFP = "BioChar_Geth.Geth.SUB_GethPrime",
            //    AssetToPortIn = "BioChar_Geth.Geth.SUB_GethPrime",
            //    AssetPaths = new[] {
            //        "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MDL", //Body
            //        "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MAT_2a", //Material
            //    },
            //    //PawnClassPath = "SFXGamePawns.", // not used for this class
            //},


            // Flamethrower Vorcha
            //new PortablePawn()
            //{
            //    PackageFilename = "SFXPawn_VorchaFlamethrower.pcc",
            //    BioPawnTypeIFP = "MERChar_EndGm2.Soldiers.FlamethrowerVorcha",
            //    AssetToPortIn = "MERChar_EndGm2.Soldiers.FlamethrowerVorcha",
            //    AssetPaths = new[] {
            //        "BIOG_HMM_ARM_HVY_R.HVYa.HMM_ARM_HVYa_MAT_18a",
            //        "BIOG_HMM_HGR_HVY_R.HVYa.HMM_HGR_HVYa_MAT_18a",
            //        "BIOG_HMM_ARM_HVY_R.HVYa.HMM_ARM_HVYa_MDL",
            //        "BIOG_HMM_HGR_HVY_R.HVYa.HMM_HGR_HVYa_MDL",
            //        "BIOG_HMM_HGR_HVY_R.HVYa.HMM_VSR_HVYa_MDL",
            //        "BIOG_HMM_HGR_HVY_R.HVYa.HMM_VSR_HVYa_MAT_1a",
            //        "BIOG_HMM_HGR_HVY_R.BRT.HMM_BTR_HVY_MDL",
            //        "BIOG_HMM_HGR_HVY_R.BRT.HMM_BTR_HVY_MAT_1a",
            //        "BIOG_BAT_HED_PROMorph_R.PROBase.BAT_HED_PROBase_MDL",
            //        "BIOG_BAT_HED_PROMorph_R.PROBase.BAT_HED_PROMorph_MAT_1a"
            //    },
            //    //PawnClassPath = "SFXGamePawns.", // not used for this class
            //},

            // Krogan
            //new PortablePawn()
            //{
            //    PackageFilename = "SFXPawn_CollectorKrogan",
            //    BioPawnTypeIFP = "BioChar_Geth.Geth.SUB_GethPrime",
            //    AssetToPortIn = "BioChar_Geth.Geth.SUB_GethPrime",
            //    AssetPaths = new[] {
            //        "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MDL", //Body
            //        "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MAT_2a", //Material
            //    },
            //}
        };

        public static void ResetClass()
        {
            foreach (var pp in PortablePawns)
            {
                if (pp.TextureUpdates != null)
                {
                    foreach (var tu in pp.TextureUpdates)
                    {
                        tu.Reset();
                    }
                }
            }
        }

        internal static void PortHelper(GameTarget target)
        {
            var pName = "BioPawn_CollectorAsari_S1.pcc";
            var afUindex = 2914;

            var package = MEPackageHandler.OpenMEPackageFromStream(MEREmbedded.GetEmbeddedPackage(target.Game, "correctedpawns." + pName));
            var af = package.GetUExport(afUindex).GetProperty<ArrayProperty<ObjectProperty>>("ActorResourceCollection");
            foreach (var v in af)
            {
                var resolved = v.ResolveToEntry(package);
                Debug.WriteLine($"\"{resolved.InstancedFullPath}\",");
            }
        }

        public static bool IsPawnAssetInPackageAlready(PortablePawn pawn, IMEPackage targetPackage)
        {
            return targetPackage.FindExport(pawn.AssetToPortIn) != null;
        }

        public static bool PortPawnIntoPackage(GameTarget target, PortablePawn pawn, IMEPackage targetPackage)
        {
            if (IsPawnAssetInPackageAlready(pawn, targetPackage))
            {
                return true; // Pawn asset to port in already ported in
            }

            IMEPackage pawnPackage = null;
            if (pawn.IsCorrectedPackage)
            {
                // DEBUG
                //if (pawn.PackageFilename == "BioPawn_Collector_Batarian.pcc")
                //{
                //    pawnPackage = MEPackageHandler.OpenMEPackage(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\binary\correctedpawns\" + pawn.PackageFilename);
                //}
                //else
                //{
                var correctedPawnData = MEREmbedded.GetEmbeddedPackage(target.Game, $"correctedpawns.{pawn.PackageFilename}");
                pawnPackage = MEPackageHandler.OpenMEPackageFromStream(correctedPawnData);
                //}
            }
            else
            {
                var pF = MERFileSystem.GetPackageFile(target, pawn.PackageFilename);
                if (pF != null)
                {
                    pawnPackage = MERFileSystem.OpenMEPackage(pF);
                }
                else
                {
                    Debug.WriteLine($"Pawn package not found: {pawn.PackageFilename}");
                }
            }

            if (pawnPackage != null)
            {
                var iAsset = pawnPackage.FindExport(pawn.AssetToPortIn);
                if (iAsset == null)
                    Debugger.Break();
                PackageTools.PortExportIntoPackage(target, targetPackage, iAsset, useMemorySafeImport: !pawn.IsCorrectedPackage);

                // Ensure the assets are too as they may not be directly referenced except in the level instance
                foreach (var asset in pawn.AssetPaths)
                {
                    if (targetPackage.FindExport(asset) == null)
                    {
                        PackageTools.PortExportIntoPackage(target, targetPackage, pawnPackage.FindExport(asset), useMemorySafeImport: !pawn.IsCorrectedPackage);
                    }
                }

                if (pawn.TextureUpdates != null)
                {
                    foreach (var tu in pawn.TextureUpdates)
                    {
                        var targetTextureExp = targetPackage.FindExport(tu.TextureInstancedFullPath);
                        TextureHandler.InstallTexture(target, tu, targetTextureExp);
                    }
                }

                return true;
            }
            return false;
        }
    }
}
