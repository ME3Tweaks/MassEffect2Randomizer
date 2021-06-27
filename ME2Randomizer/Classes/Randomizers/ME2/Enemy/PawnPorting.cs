using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Packages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Enemy
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
        public string ChallengeTypeFullPath { get; set; }
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
            new PortablePawn()
            {
                PackageFilename = "BioP_ProCer.pcc",
                ChallengeTypeFullPath = "BioChar_Mechs.SUB_HeavyWeaponMech",
                AssetToPortIn = "BioChar_Mechs.SUB_HeavyWeaponMech",
                AssetPaths = new[] {
                    "BIOG_CBT_MHV_NKD_R.NKDa.CBT_MHV_NKDa_MDL",
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_HeavyMech",
                Classification = EPortablePawnClassification.Subboss,
                IsCorrectedPackage = false
            },

            // Klixen
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Spider.pcc",
                ChallengeTypeFullPath = "BioChar_Animals.Combat.ELT_Spider",
                AssetToPortIn = "BioChar_Animals.Combat.ELT_Spider",
                AssetPaths = new[] {
                    "biog_cbt_rac_nkd_r.NKDa.CBT_RAC_NKDa_MDL",
                    "EffectsMaterials.Users.Creatures.CBT_SPD_NKD_MAT_1a_USER",
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_Spider",
                IsCorrectedPackage = true
            },

            //Collector Krogan
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Collector_Krogan.pcc",
                ChallengeTypeFullPath = "BioChar_MER.Combat.SUB_Collector_Krogan",
                AssetToPortIn = "BioChar_MER.Combat.SUB_Collector_Krogan",
                AssetPaths = new[] {
                    "BIOG_KRO_ARM_HVY_R.HVYe.KRO_ARM_HVYe_MDL", //Body
                    "BIOG_KRO_HED_PROMorph.KRO_HED_PROBase_MDL", //Head
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_Collector_Krogan", // not garm!
                IsCorrectedPackage = true
            },

            // Does not seem to work, unfortunately ;(
            //new PortablePawn()
            //{
            //    PackageFilename = "SFXPawn_GethColossus.pcc",
            //    ChallengeTypeFullPath = "BioChar_Geth.Armature.BOS_GethColossus",
            //    AssetToPortIn = "BioChar_Geth.Armature.BOS_GethColossus",
            //    AssetPaths = new string[] {
            //        //"BIOG_KRO_HED_PROMorph.KRO_HED_PROBase_MDL", //Head
            //        "BIOG_RBT_TNK_NKD_R.NKDa.RBT_TNK_NKDa_MDL", //Body
            //    },
            //    PawnClassPath = "SFXGamePawns.SFXPawn_Colossus",
            //    IsCorrectedPackage = true
            //},

            // Praetorian - NEEDS NERFED
            //new PortablePawn()
            //{
            //    PackageFilename = "SFXPawn_Praetorian.pcc",
            //    ChallengeTypeFullPath = "BioChar_Collectors.BOS_Praetorian",
            //    AssetToPortIn = "BioChar_Collectors.BOS_Praetorian",
            //    AssetPaths = new string[] {
            //        "BIOG_CBT_PRA_NKD_R.NKDa.CBT_PRA_NKDa_MDL", //Body
            //    },
            //    PawnClassPath = "SFXGamePawns.SFXPawn_Praetorian",
            //    IsCorrectedPackage = true
            //},

            //Geth Prime. AI doesn't let him climb over shit so he's pretty stationary
            new PortablePawn()
            {
                PackageFilename = "BioPawn_GethPrime.pcc",
                ChallengeTypeFullPath = "BioChar_Geth.Geth.SUB_GethPrime",
                AssetToPortIn = "BioChar_Geth.Geth.SUB_GethPrime",
                AssetPaths = new[] {
                    "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MDL", //Body
                    "BIOG_GTH_STP_NKD_R.NKDa.GTH_STP_NKDa_MAT_2a", //Material
                },
                //PawnClassPath = "SFXGamePawns.", // not used for this class
                IsCorrectedPackage = true
            },

            //Collector Batarian
            new PortablePawn()
            {
                PackageFilename = "BioPawn_Collector_Batarian.pcc",
                ChallengeTypeFullPath = "BioChar_MER.Combat.ELT_Collector_Batarian",
                AssetToPortIn = "BioChar_MER.Combat.ELT_Collector_Batarian",
                AssetPaths = new[] {
                    "BIOG_HMM_ARM_HVY_R.HVYa.HMM_ARM_HVYa_MAT_8a",
                    "BIOG_HMM_ARM_HVY_R.HVYa.HMM_ARM_HVYa_MDL",
                    "BIOG_HMM_ARM_HVY_R.HVYa.HMM_ARM_HVYa_MAT_18a",
                    "BIOG_BAT_HED_PROMorph_R.PROBase.BAT_HED_PROBase_MDL",
                    "BIOG_BAT_HED_PROMorph_R.PROBase.BAT_HED_PROMorph_MAT_1a",
                },
                //PawnClassPath = "SFXGamePawns.", // not used for this class
                IsCorrectedPackage = true,
                TextureUpdates = new []
                {
                    new RTexture2D
                    {
                        // Darker head with veins (that you honestly can't really see...)
                        TextureInstancedFullPath = "BIOG_BAT_HED_PROMorph_R.PROBase.BAT_HED_PROMorph_Diff",
                        AllowedTextureAssetNames = new List<string>
                        {
                            "Pawn.Collector_Batarian_HeadMorph.bin",
                        }
                    },
                }
            },


            // Collector Asari
            new PortablePawn()
            {
                PackageFilename = "BioPawn_CollectorAsari_S1.pcc",
                ChallengeTypeFullPath = "BioChar_MER.Vanguards.ELT_Collector_Asari",
                AssetToPortIn = "BioChar_MER.Vanguards.ELT_Collector_Asari",
                AssetPaths = new[] {
                    "BIOG_HMF_ARM_MED_R.MEDa.HMF_ARM_MEDa_MAT_17a",
                    "BIOG_HMF_ARM_MED_R.MEDa.HMF_ARM_MEDa_MDL",
                    "BIOG_ASA_HED_PROMorph_R.PROBase.ASA_HED_PROBASE_MDL",
                    "BIOG_ASA_HED_PROMorph_R.PROBase.ASA_HED_PROMorph_Mat_1a",
                    "BIOG_ASA_HED_PROMorph_R.ASA_HED_EYE_MAT_1a",
                    "BIOG_ASA_HED_PROMorph_R.PROBase.ASA_HED_PRO_Lash_Mat_1a",
                },
                IsCorrectedPackage = true,
            }
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

        internal static void PortHelper()
        {
            var pName = "BioPawn_CollectorAsari_S1.pcc";
            var afUindex = 2914;

            var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(MERUtilities.GetEmbeddedStaticFilesBinaryFile("correctedpawns." + pName)));
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

        public static bool PortPawnIntoPackage(PortablePawn pawn, IMEPackage targetPackage)
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
                var correctedPawnData = MERUtilities.GetEmbeddedStaticFilesBinaryFile($"correctedpawns.{pawn.PackageFilename}");
                pawnPackage = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(correctedPawnData));
                //}
            }
            else
            {
                var pF = MERFileSystem.GetPackageFile(pawn.PackageFilename);
                if (pF != null)
                {
                    pawnPackage = MERFileSystem.OpenMEPackage(pF);
                }
                else
                {
                    Debug.WriteLine("Pawn package not found: {pawn.PackageFilename}");
                }
            }

            if (pawnPackage != null)
            {
                PackageTools.PortExportIntoPackage(targetPackage, pawnPackage.FindExport(pawn.AssetToPortIn), useMemorySafeImport: !pawn.IsCorrectedPackage);

                // Ensure the assets are too as they may not be directly referenced except in the level instance
                foreach (var asset in pawn.AssetPaths)
                {
                    if (targetPackage.FindExport(asset) == null)
                    {
                        PackageTools.PortExportIntoPackage(targetPackage, pawnPackage.FindExport(asset), useMemorySafeImport: !pawn.IsCorrectedPackage);
                    }
                }

                if (pawn.TextureUpdates != null)
                {
                    foreach (var tu in pawn.TextureUpdates)
                    {
                        var targetTextureExp = targetPackage.FindExport(tu.TextureInstancedFullPath);
                        TFCBuilder.InstallTexture(tu, targetTextureExp);
                    }
                }

                return true;
            }
            return false;
        }
    }
}
