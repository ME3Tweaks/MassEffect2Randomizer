using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Misc
{
    internal class RPhotoMode
    {
        public static bool InstallAdditionalFilters(GameTarget target, RandomizationOption option)
        {
            var outPackage = @"";
            var items = new[]
            {
                "BioD_Omg004_900Afterlife.BioVFX_Exp2_Adj.Materials.Biotic_Smoke_FB_Mat_INST",
                "BioD_CitCas_420Javik_LOC_INT.BioVFX_Exp3_CitGlobal.Materials.Camera_Overlay_FB_mat_INST",
                "BioD_Lev004_100Surface.BioVFX_DLC_EXP1_Lev004.Material.EMP_FB_Mat_INST",
                "BioD_Nor_130RomLiara.BioVFX_Z_GLOBAL.Materials.Env_Asari_Sex_mat_INST",
                "BioD_GthLeg_200Server_In.BioVFX_Env_Gthleg.Materials.Env_Post_FB_mat_INST",
                "SFXPawn_GethPyro.BioVFX_Crt_FlameThrower.Materials.FlameThrower_FB_INST",
                "SFXPawn_Banshee.BioVFX_Crt_Rpr_Banshee.Materials.Flux_Post_FB_mat_INST",
                "BioD_GthLeg_650Paths.BioVFX_Plc_ReaperIndoc.Materials.indoc_Mat_INST",
                "BioD_End002_300TimConflict.BioVFX_Plc_ReaperIndoc.Materials.indoc_Mat_2_INST",
                "SFXPower_Marksman.BioVFX_C_Marksmen.Material.Marksmen_FB_Mat_tv_INST",
                "SFXPower_Barrier.BioVFX_B_Biotics_Charge.Materials.mat_BioticsMode_Instance",
                "BioVFX_FB_PlayerDamage.Materials.Inst_GameOverBlood",
                "BioD_End002_530Green.BioVFX_Env_End002.Material.MotionBlur_Green_INST",
                "BioVFX_FB_MotionBlur.Material.Instance_001",
                "BioVFX_FB_MotionBlur.Material.Instance_noBurn_INST",
                "BioD_Gth002_300Chase.BioVFX_Env_Gth002.Material.MotionBlur_Mat_Gth002",
                "BioD_Nor_100CabinConv_LOC_INT.biovfx_cine_movietextures.PostProcess.PP_AllersCam_MAT_INST",
                "BioD_Cit002_000Global.BioVFX_Exp3_Cit002.Materials.Security_Vision_Mat_INST",
                "SFXWeapon_Heavy_Cain.BioVFX_FB_PlayerDamage.Materials.Toxic_Dizzy_FB"
            };

            var pack = @"Y:\ModLibrary\LE3\Expanded Photo Mode\DLC_MOD_ExpandedPhotoMode\CookedPCConsole\BIOG_PhotoModeFiltersExpanded.pcc";
            MEPackageHandler.CreateAndSavePackage(pack, MEGame.LE3);
            var biog = MEPackageHandler.OpenMEPackage(pack);
            var objReferencer = PackageTools.CreateObjectReferencer(biog);
            foreach (var item in items)
            {
                var package = item.Split('.')[0];
                var packageExists = File.Exists(MERFileSystem.GetPackageFile(target, package + ".pcc"));
                if (packageExists)
                {
                    var inPath = string.Join('.', item.Split('.').Skip(1));
                    var ext = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(target, package + ".pcc"));
                    var export = ext.FindExport(inPath);
                    EntryExporter.ExportExportToPackage(export, biog, out var inst);
                    PackageTools.AddObjectReferencerReference(inst, objReferencer);
                }
                else
                {
                    Debug.WriteLine($@"SKIPPING {item}");
                }
            }

            biog.Save();

            return true;
            // Install the filter loading extension so we can load packages on demand.
            ScriptTools.InstallScriptToPackage(target, "SFXGame.pcc", "SFXGameModePhoto.InitializeFilters", "PhotoModeFilterLoader.uc", false, saveOnFinish: true);

            // Add the filters
            var bioInput = CoalescedHandler.GetIniFile("BioInput");
            var gamemodePhoto = bioInput.GetOrAddSection("sfxgame.sfxgamemodephoto");

            string[] additionalFilters =
            {
                "SFXPower_AdrenalineRush.BioVFX_C_Adrenaline.FrameBuffer.Adrenaline_FB_INST",
                "BioD_Omg004_900Afterlife.BioVFX_Exp2_Adj.Materials.Biotic_Smoke_FB_Mat_INST",
                "BioD_CitCas_420Javik_LOC_INT.BioVFX_Exp3_CitGlobal.Materials.Camera_Overlay_FB_mat_INST",
                "BioD_Lev004_100Surface.BioVFX_DLC_EXP1_Lev004.Material.EMP_FB_Mat_INST",
                "BioD_Nor_130RomLiara.BioVFX_Z_GLOBAL.Materials.Env_Asari_Sex_mat_INST",
                "BioD_GthLeg_200Server_In.BioVFX_Env_Gthleg.Materials.Env_Post_FB_mat_INST",
                "BioD_Nor_110Tour.biovfx_cin_fades.Materials.Fade_mat_INST",
                "SFXPawn_GethPyro.BioVFX_Crt_FlameThrower.Materials.FlameThrower_FB_INST",
                "BioD_End001_460RoofInterior.BioVFX_Z_GLOBAL.Materials.FlashBack_FB_Mat_INST",
                "SFXPawn_Banshee.BioVFX_Crt_Rpr_Banshee.Materials.Flux_Post_FB_mat_INST",
                "BioD_GthLeg_650Paths.BioVFX_Plc_ReaperIndoc.Materials.indoc_Mat_INST",
                "BioD_End002_300TimConflict.BioVFX_Plc_ReaperIndoc.Materials.indoc_Mat_2_INST",
                "SFXPower_Marksman.BioVFX_C_Marksmen.Material.Marksmen_FB_Mat_tv_INST",
                "SFXPower_Barrier.BioVFX_B_Biotics_Charge.Materials.mat_BioticsMode_Instance",
                "BioVFX_FB_PlayerDamage.Materials.Inst_GameOverBlood",
                "BioD_End002_530Green.BioVFX_Env_End002.Material.MotionBlur_Green_INST",
                "BioVFX_FB_MotionBlur.Material.Instance_001",
                "BioVFX_FB_MotionBlur.Material.Instance_noBurn_INST",
                "BioD_Gth002_300Chase.BioVFX_Env_Gth002.Material.MotionBlur_Mat_Gth002",
                "BioD_Nor_100CabinConv_LOC_INT.biovfx_cine_movietextures.PostProcess.PP_AllersCam_MAT_INST",
                "BioD_Cit002_000Global.BioVFX_Exp3_Cit002.Materials.Security_Vision_Mat_INST",
                "SFXWeapon_Heavy_Cain.BioVFX_FB_PlayerDamage.Materials.Toxic_Dizzy_FB",
            };


            gamemodePhoto.AddEntry(new CoalesceProperty("filtermaterialpaths", additionalFilters.Select(x => new CoalesceValue(x, CoalesceParseAction.AddUnique)).ToList()));
            return true;
        }
    }
}
