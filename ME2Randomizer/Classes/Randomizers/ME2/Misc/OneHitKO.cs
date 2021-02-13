using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class OneHitKO
    {
        public static bool InstallOHKO(RandomizationOption arg)
        {
            Log.Information("Installing One-Hit KO");
            {
                var sfxgame = MERFileSystem.GetPackageFile("SFXGame.pcc");
                if (sfxgame != null && File.Exists(sfxgame))
                {
                    var sfxgameP = MEPackageHandler.OpenMEPackage(sfxgame);

                    // Blood on screen VFX
                    sfxgameP.GetUExport(29336).RemoveProperty("BleedOutEventPair");
                    sfxgameP.GetUExport(29336).RemoveProperty("BleedOutVFXTemplate");

                    // Prevent weird landing glitch
                    var fallingStateLanded = sfxgameP.GetUExport(11293);
                    var landedData = fallingStateLanded.Data;
                    NopRange(landedData, 0x2C, 0x14);
                    fallingStateLanded.Data = landedData;

                    MERFileSystem.SavePackage(sfxgameP);
                }
            }

            // Vanguard biotic charge stuff
            {
                var ccVanguard = MERFileSystem.GetPackageFile("SFXCharacterClass_Vanguard.pcc");
                if (ccVanguard != null && File.Exists(ccVanguard))
                {
                    var ccVanguardP = MEPackageHandler.OpenMEPackage(ccVanguard);

                    var vanPassive2 = ccVanguardP.GetUExport(404);
                    var hbVP2 = vanPassive2.GetProperty<ArrayProperty<FloatProperty>>("HealthBonus");
                    hbVP2[0].Value = 0;
                    hbVP2[1].Value = 0;
                    hbVP2[2].Value = 0;
                    hbVP2[3].Value = 0;
                    vanPassive2.WriteProperty(hbVP2);

                    var vanPassive = ccVanguardP.GetUExport(400);
                    vanPassive.WriteProperty(hbVP2);

                    // Remove immunity on charge
                    var shieldEffectOnApplied = ccVanguardP.GetUExport(530);
                    var seData = shieldEffectOnApplied.Data;
                    NopRange(seData, 0x2BF, 0x13);
                    shieldEffectOnApplied.Data = seData;

                    MERFileSystem.SavePackage(ccVanguardP);
                }
            }

            // Player classes - Remove shields, set maxhealth to 1
            string[] classes = new[] { "Adept", "Engineer", "Infiltrator", "Sentinel", "Soldier", "Vanguard" };
            foreach (var c in classes)
            {
                var charClass = MERFileSystem.GetPackageFile($"SFXCharacterClass_{c}.pcc");
                if (charClass != null && File.Exists(charClass))
                {
                    var charClassP = MEPackageHandler.OpenMEPackage(charClass);
                    var ccLoadout = charClassP.FindExport($"BioChar_Loadouts.Player.PLY_{c}");

                    var ccProps = ccLoadout.GetProperties();
                    ccProps.GetProp<ArrayProperty<StructProperty>>("ShieldLoadouts").Clear(); // Remove shields

                    // Set health to 1
                    var health = ccProps.GetProp<StructProperty>("MaxHealth");
                    health.GetProp<FloatProperty>("X").Value = 1;
                    health.GetProp<FloatProperty>("Y").Value = 1;

                    ccLoadout.WriteProperties(ccProps);
                    MERFileSystem.SavePackage(charClassP);
                }
            }

            // Remove health bonus from Reave
            {
                var reaveF = MERFileSystem.GetPackageFile("SFXPower_Reave_Player.pcc");
                if (reaveF != null && File.Exists(reaveF))
                {
                    var reaveP = MEPackageHandler.OpenMEPackage(reaveF);

                    var defaultReave = reaveP.GetUExport(27);
                    var regen = defaultReave.GetProperty<ArrayProperty<FloatProperty>>("HealthRegenMult");
                    regen[0].Value = 0;
                    regen[1].Value = 0;
                    regen[2].Value = 0;
                    regen[3].Value = 0;
                    defaultReave.WriteProperty(regen);

                    var bonusDuration = defaultReave.GetProperty<ArrayProperty<FloatProperty>>("HealthBonusDuration");
                    bonusDuration[0].Value = 0;
                    bonusDuration[1].Value = 0;
                    bonusDuration[2].Value = 0;
                    bonusDuration[3].Value = 0;
                    defaultReave.WriteProperty(bonusDuration);

                    MERFileSystem.SavePackage(reaveP);
                }
            }

            return true;
        }

        /// <summary>
        /// Installs NOP bytecodes into function data
        /// </summary>
        /// <param name="funcData"></param>
        /// <param name="startOffset"></param>
        /// <param name="nopLen"></param>
        private static void NopRange(byte[] funcData, int startOffset, int nopLen)
        {
            for (int i = 0; i < nopLen; i++)
            {
                funcData[startOffset + i] = 0x0B; // EX_Nothing
            }
        }
    }
}
