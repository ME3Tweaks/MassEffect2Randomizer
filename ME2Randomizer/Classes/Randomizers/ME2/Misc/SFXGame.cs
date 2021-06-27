using System;
using System.IO;
using ALOTInstallerCore;
using ALOTInstallerCore.Helpers;
using MassEffectRandomizer.Classes;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class SFXGame
    {
        /// <summary>
        /// Determines if this is an ME2Controller-mod based install, which can change the behavior of the application to fit the different UI
        /// </summary>
        /// <returns></returns>
        public static bool IsControllerBasedInstall()
        {
#if __ME2__
            var target = Locations.GetTarget(MERFileSystem.Game);
            if (target == null) return false;

            var sfxgame = Path.Combine(target.TargetPath, "BioGame", "CookedPC", "SFXGame.pcc");
            if (File.Exists(sfxgame))
            {
                var sfxgameP = MEPackageHandler.OpenMEPackage(sfxgame);
                var upa = sfxgameP.GetUExport(29126); //UpdatePlayerAccuracy
                var md5 = Utilities.CalculateMD5(new MemoryStream(upa.Data));
                return md5 == "315324313211026536f3cab95a1101d4"; // ME2Controller 1.7.2
            }

            return false;
#endif
        }

        public static bool MakeShepardRagdollable(RandomizationOption option)
        {
            var sfxgame = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile("SFXGame.pcc"));

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

        public static bool TurnOnFriendlyFire(RandomizationOption option)
        {
            var sfxgame = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile("SFXGame.pcc"));
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

        public static bool RemoveStormCameraShake(RandomizationOption arg)
        {
            var sfxgame = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile("SFXGame.pcc"));

            // SFXCameraMode_CombatStorm
            var md = sfxgame.GetUExport(25096);
            md.WriteProperty(new BoolProperty(false, "bIsCameraShakeEnabled"));

            //SFXCameraMode_ExporeStorm
            md = sfxgame.GetUExport(25116);
            md.WriteProperty(new BoolProperty(false, "bIsCameraShakeEnabled"));

            MERFileSystem.SavePackage(sfxgame);
            return true;
        }
    }
}
