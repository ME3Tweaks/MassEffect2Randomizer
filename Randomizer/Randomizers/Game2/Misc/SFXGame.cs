using System.IO;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.Misc
{
    public class SFXGame
    {
        public static IMEPackage GetSFXGame(GameTarget target)
        {
            var sfxgame = Path.Combine(target.TargetPath, "BioGame", "CookedPCConsole", "SFXGame.pcc");
            if (File.Exists(sfxgame))
            {
                return MEPackageHandler.OpenMEPackage(sfxgame);
            }

            return null;
        }


        public static bool MakeShepardRagdollable(GameTarget target, RandomizationOption option)
        {
            var sfxgame = GetSFXGame(target);

            // Add ragdoll power to shep
            var sfxplayercontrollerDefaults = sfxgame.FindExport(@"Default__SFXPlayerController");
            var cac = sfxplayercontrollerDefaults.GetProperty<ArrayProperty<ObjectProperty>>("CustomActionClasses");
            cac[5].Value = sfxgame.FindExport(@"SFXCustomAction_Ragdoll").UIndex; //SFXCustomAction_Ragdoll in this slot
            sfxplayercontrollerDefaults.WriteProperty(cac);

            // Update power script design and patch out player physics level
            var sd = sfxgame.FindExport(@"BioPowerScriptDesign.GetPhysicsLevel");
            ScriptTools.InstallScriptToExport(sd, "GetPhysicsLevel.uc", false, null);

            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public static bool TurnOnFriendlyFire(GameTarget target, RandomizationOption option)
        {
            // Remove the friendly pawn check
            var sfxgame = ScriptTools.InstallScriptToPackage(target, "SFXGame.pcc", "SFXGame.ModifyDamage", "SFXGame.ModifyDamage.uc", false);
            if (option.HasSubOptionSelected(SUBOPTIONKEY_CARELESSFF))
            {
                // Remove the friendly fire check
                ScriptTools.InstallScriptToPackage(sfxgame, "BioAiController.IsFriendlyBlockingFireLine", "IsFriendlyBlockingFireLine.uc", false);
            }
            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public const string SUBOPTIONKEY_CARELESSFF = "CarelessMode";
    }
}
