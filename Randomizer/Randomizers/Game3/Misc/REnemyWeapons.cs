using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Misc
{
    public class REnemyWeapons
    {
        private static bool InstallRandomWeaponScript(GameTarget target, RandomizationOption option, List<ObjectDecookInfo> decookedWeapons)
        {
            var sfxgame = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "SFXGame.pcc"));
            var targetScriptExport = sfxgame.FindExport("BioPawn.CreateWeapon");

            string scriptText = MERUtilities.GetEmbeddedTextAsset($"Scripts.RandomEnemyWeapon.uc", false);

            int startingSwitchIndex = 71;

            StringBuilder sb = new StringBuilder();
            foreach (var weapon in decookedWeapons)
            {
                sb.AppendLine($"case {startingSwitchIndex++}:");
                sb.AppendLine($"weaponAsset =  \"{weapon.SeekFreeInfo.SeekFreePackage}\"; keepDamageAsIs = true; break;");
            }

            scriptText = scriptText.Replace("%ADDITIONALCASESTATEMENTS%", sb.ToString());
            scriptText = scriptText.Replace("%FULLWEAPONPOOLSIZE%", startingSwitchIndex.ToString()); // no +1 cause the ++ on the loop will have incremented it one further.
            Application.Current.Dispatcher.Invoke(() =>
            {
                Clipboard.SetText(scriptText);
            });
            ScriptTools.InstallScriptTextToExport(targetScriptExport, scriptText, "RandomEnemyWeapon-DYNAMIC");
            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public static bool RandomizeEnemyWeapons(GameTarget target, RandomizationOption option)
        {
            var weaponsToDecook = new List<ObjectDecookInfo>
            {
                new ObjectDecookInfo()
                {
                    SourceFileName = "SFXPawn_Cannibal.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AI_CannibalRifle",
                        EntryPath = "SFXGameContent.SFXWeapon_AI_CannibalRifle"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_Cat002_500Cathedral.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AI_GunShipMiniGun",
                        EntryPath = "SFXGameContent.SFXWeapon_AI_GunShipMiniGun"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "SFXPawn_Harvester.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AI_Harvester",
                        EntryPath = "SFXGameContent.SFXWeapon_AI_Harvester"
                    }
                },new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AI_ScionCannon_Shared",
                        EntryPath = "SFXGameContentDLC_Shared.SFXWeapon_AI_ScionCannon_Shared"
                    }
                },
                // VISIBLE WEAPON
                new ObjectDecookInfo()
                {
                    SourceFileName = "SFXPower_GethPrimeTurret.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AI_GethPrimeRifle",
                        EntryPath = "SFXGameContent.SFXWeapon_AI_GethPrimeRifle"
                    }
                },new ObjectDecookInfo()
                {
                    SourceFileName = "SFXPawn_GethRocketTrooper.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AI_GethRocketLauncher",
                        EntryPath = "SFXGameContent.SFXWeapon_AI_GethRocketLauncher"
                    }
                },new ObjectDecookInfo()
                {
                    SourceFileName = "SFXPawn_Ravager.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AI_Ravager",
                        EntryPath = "SFXGameContent.SFXWeapon_AI_Ravager"
                    }
                },new ObjectDecookInfo()
                {
                    SourceFileName = "SFXPower_SentryTurretShock.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_AssaultRifle_SentryTurret",
                        EntryPath = "SFXGameContent.SFXWeapon_AssaultRifle_SentryTurret"
                    }
                }
                ,new ObjectDecookInfo()
                {
                    SourceFileName = "SFXPawn_Atlas.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        SeekFreePackage = "SFXWeapon_Heavy_Atlas",
                        EntryPath = "SFXGameContent.SFXWeapon_Heavy_Atlas"
                    }
                }
            };

            // Decook extra weapons to packages
            MERDecooker.DecookObjectsToPackages(target, option, weaponsToDecook, "Decooking weapons", true);

            // Install script to grant random weapons.
            InstallRandomWeaponScript(target, option, weaponsToDecook);
            return true;
        }
    }
}
