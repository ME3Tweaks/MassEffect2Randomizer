using System.IO;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using Randomizer.MER;

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
            var sfxplayercontrollerDefaults = sfxgame.GetUExport(30777);
            var cac = sfxplayercontrollerDefaults.GetProperty<ArrayProperty<ObjectProperty>>("CustomActionClasses");
            cac[5].Value = 25988; //SFXCustomActionRagdoll
            sfxplayercontrollerDefaults.WriteProperty(cac);

            // Update power script design and patch out player physics level
            var sd = sfxgame.GetUExport(14353).Data;
            OneHitKO.NopRange(sd, 0x62, 0x27);
            sfxgame.GetUExport(14353).Data = sd;

            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public static bool TurnOnFriendlyFire(GameTarget target, RandomizationOption option)
        {
            var sfxgame = GetSFXGame(target);
            var md = sfxgame.GetUExport(21353);
            var patched = md.Data;
            for (int i = 0x0285; i < 0x0317; i++)
            {
                patched[i] = 0x0B; // nop
            }
            md.Data = patched;

            if (option.HasSubOptionSelected(SUBOPTIONKEY_CARELESSFF))
            {
                // Copy a 'return false' over the top of IsFriendlyBlockingSightline
                var fExport = sfxgame.GetUExport(5723);
                var fObjBin = ObjectBinary.From<UFunction>(fExport);

                var friendlyBlockingFunc = sfxgame.GetUExport(1203);
                var fbObjBin = ObjectBinary.From<UFunction>(friendlyBlockingFunc);

                fbObjBin.ScriptBytecodeSize = fObjBin.ScriptBytecodeSize;
                fbObjBin.ScriptStorageSize = fObjBin.ScriptStorageSize;
                fbObjBin.ScriptBytes = fObjBin.ScriptBytes;
                friendlyBlockingFunc.WriteBinary(fbObjBin);
            }
            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public const string SUBOPTIONKEY_CARELESSFF = "CarelessMode";
    }
}
