using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // Garm Krogan (Indoctrinated)
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Garm.pcc",
                ChallengeTypeFullPath = "BioChar_OmgGrA.Combat.SUB_BloodPack_Leader",
                AssetToPortIn = "BioChar_OmgGrA.Combat.SUB_BloodPack_Leader",
                AssetPaths = new[] {
                    "BIOG_KRO_HED_PROMorph.KRO_HED_PROBase_MDL", //Head
                    "BIOG_KRO_ARM_HVY_R.HVYe.KRO_ARM_HVYe_MDL", //Body
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_Garm",
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
            new PortablePawn()
            {
                PackageFilename = "SFXPawn_Praetorian.pcc",
                ChallengeTypeFullPath = "BioChar_Collectors.BOS_Praetorian",
                AssetToPortIn = "BioChar_Collectors.BOS_Praetorian",
                AssetPaths = new string[] {
                    "BIOG_CBT_PRA_NKD_R.NKDa.CBT_PRA_NKDa_MDL", //Body
                },
                PawnClassPath = "SFXGamePawns.SFXPawn_Praetorian",
                IsCorrectedPackage = true
            },

            // Todo: Scion, Vorcha
            // Varren are pointless for this purpose
        };

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
                var correctedPawnData = Utilities.GetEmbeddedStaticFilesBinaryFile($"correctedpawns.{pawn.PackageFilename}");
                pawnPackage = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(correctedPawnData));
            }
            else
            {
                var pF = MERFileSystem.GetPackageFile(pawn.PackageFilename);
                if (pF != null)
                {
                    pawnPackage = MEPackageHandler.OpenMEPackage(pF);
                }
                else
                {
                    Debug.WriteLine("Pawn package not found: {pawn.PackageFilename}");
                }
            }

            if (pawnPackage != null)
            {
                PackageTools.PortExportIntoPackage(targetPackage, pawnPackage.FindExport(pawn.AssetToPortIn));

                // Ensure the assets are too as they may not be directly referenced except in the level instance
                foreach (var asset in pawn.AssetPaths)
                {
                    if (targetPackage.FindExport(asset) == null)
                    {
                        PackageTools.PortExportIntoPackage(targetPackage, pawnPackage.FindExport(asset));
                    }
                }

                return true;
            }
            return false;
        }
    }
}
