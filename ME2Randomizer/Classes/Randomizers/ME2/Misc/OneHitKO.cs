using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class OneHitKO
    {
        class OHKOAsset : IlliumHub.AssetSource
        {
            public string[] PropertiesToZeroOut { get; set; }
        }

        private static OHKOAsset[] ZeroOutStatAssets = new[]
        {
            // Miranda
            new OHKOAsset(){ PackageFile = "BioH_Vixen_00.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_MirandaPassive", PropertiesToZeroOut = new [] {"SquadHealthBonus"}},
            new OHKOAsset(){ PackageFile = "BioH_Vixen_01.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_MirandaPassive", PropertiesToZeroOut = new [] {"SquadHealthBonus"}},
            new OHKOAsset(){ PackageFile = "BioH_Vixen_02.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_MirandaPassive", PropertiesToZeroOut = new [] {"SquadHealthBonus"}},

            new OHKOAsset(){ PackageFile = "BioH_Vixen_00.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_MirandaPassive_Evolved2", PropertiesToZeroOut = new [] {"SquadHealthBonus"}},
            new OHKOAsset(){ PackageFile = "BioH_Vixen_01.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_MirandaPassive_Evolved2", PropertiesToZeroOut = new [] {"SquadHealthBonus"}},
            new OHKOAsset(){ PackageFile = "BioH_Vixen_02.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_MirandaPassive_Evolved2", PropertiesToZeroOut = new [] {"SquadHealthBonus"}},

            // Barrier
            new OHKOAsset(){ PackageFile = "SFXPower_Barrier_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Barrier_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},
            new OHKOAsset(){ PackageFile = "SFXPower_Barrier_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Barrier_Heavy_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},

            // Reave
            new OHKOAsset(){ PackageFile = "SFXPower_Reave_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Reave_Player", PropertiesToZeroOut = new [] {"HealthRegenMult", "HealthBonusDuration"}},
            
            // Player Reave evolutions don't inherit from reave_player for some reason. So we must also change them too
            new OHKOAsset(){ PackageFile = "SFXPower_Reave_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Reave_Evolved1_Player", PropertiesToZeroOut = new [] {"HealthRegenMult", "HealthBonusDuration"}},
            new OHKOAsset(){ PackageFile = "SFXPower_Reave_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Reave_Evolved2_Player", PropertiesToZeroOut = new [] {"HealthRegenMult", "HealthBonusDuration"}},

            // Dominate
            new OHKOAsset(){ PackageFile = "SFXPower_Dominate_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Dominate_Player", PropertiesToZeroOut = new [] {"ShieldStrength"}},
            new OHKOAsset(){ PackageFile = "SFXPower_Dominate_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Dominate_Evolved1_Player", PropertiesToZeroOut = new [] {"ShieldStrength"}},

            // Geth Shield Boost
            new OHKOAsset(){ PackageFile = "SFXPower_GethShieldBoost_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_GethShieldBoost_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},
            new OHKOAsset(){ PackageFile = "SFXPower_GethShieldBoost_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_GethShieldBoost_Evolved1_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},
            // Shield Boost (in same file)
            new OHKOAsset(){ PackageFile = "SFXPower_GethShieldBoost_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_ShieldBoost_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},
            new OHKOAsset(){ PackageFile = "SFXPower_GethShieldBoost_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_ShieldBoost_Evolved1_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},

            // Fortification
            new OHKOAsset(){ PackageFile = "SFXPower_Fortification_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Fortification_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},
            new OHKOAsset(){ PackageFile = "SFXPower_Fortification_Player.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_Fortification_Evolved2_Player", PropertiesToZeroOut = new [] {"ShieldValue"}},

            // Power Armor
            new OHKOAsset(){ PackageFile = "SFXCharacterClass_Sentinel.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_PowerArmor", PropertiesToZeroOut = new [] {"ShieldStrength"}},
            new OHKOAsset(){ PackageFile = "SFXCharacterClass_Sentinel.pcc" , AssetPath = "SFXGameContent_Powers.SFXPower_PowerArmor_Evolved2", PropertiesToZeroOut = new [] {"ShieldStrength"}},
        };


        public static bool InstallOHKO(RandomizationOption arg)
        {
            MERLog.Information("Installing One-Hit KO");

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
            else
            {

            }


            // ProCer tutorials setting min1Health
            SetupProCer();

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

                    // Remove any passive powers
                    ZeroOutStatList(charClassP.FindExport($"SFXGameContent_Powers.SFXPower_{c}Passive"), "HealthBonus", false);
                    ZeroOutStatList(charClassP.FindExport($"SFXGameContent_Powers.SFXPower_{c}Passive_Evolved1"), "HealthBonus", false);
                    ZeroOutStatList(charClassP.FindExport($"SFXGameContent_Powers.SFXPower_{c}Passive_Evolved2"), "HealthBonus", false);

                    if (c == "Vanguard")
                    {
                        // Patch the immunity effect
                        var shieldEffectOnApplied = charClassP.GetUExport(530);
                        var seData = shieldEffectOnApplied.Data;
                        NopRange(seData, 0x2BF, 0x13);
                        shieldEffectOnApplied.Data = seData;
                    }

                    MERFileSystem.SavePackage(charClassP);
                }
            }

            // Zero out stats in tables
            MERPackageCache cache = new MERPackageCache();
            foreach (var asset in ZeroOutStatAssets)
            {
                var statClass = asset.GetAsset(cache);
                if (statClass != null)
                {
                    foreach (var zos in asset.PropertiesToZeroOut)
                    {
                        ZeroOutStatList(statClass, zos, true);
                    }
                }
            }

            foreach (var package in cache.GetPackages())
            {
                MERFileSystem.SavePackage(package);
            }

            {
                //var reaveF = MERFileSystem.GetPackageFile("SFXPower_Reave_Player.pcc");
                //if (reaveF != null && File.Exists(reaveF))
                //{
                //    var reaveP = MEPackageHandler.OpenMEPackage(reaveF);

                //    var defaultReave = reaveP.GetUExport(27);
                //    var regen = defaultReave.GetProperty<ArrayProperty<FloatProperty>>("HealthRegenMult");
                //    regen[0].Value = 0;
                //    regen[1].Value = 0;
                //    regen[2].Value = 0;
                //    regen[3].Value = 0;
                //    defaultReave.WriteProperty(regen);

                //    var bonusDuration = defaultReave.GetProperty<ArrayProperty<FloatProperty>>("HealthBonusDuration");
                //    bonusDuration[0].Value = 0;
                //    bonusDuration[1].Value = 0;
                //    bonusDuration[2].Value = 0;
                //    bonusDuration[3].Value = 0;
                //    defaultReave.WriteProperty(bonusDuration);

                //    MERFileSystem.SavePackage(reaveP);
                //}
            }

            return true;
        }

        private static void SetupProCer()
        {
            /*
             * PLAYER
                Min1Health in BioD_ProCer.pcc, export 1171, sequence TheWorld.PersistentLevel.Main_Sequence, target SeqVar_Player
                Min1Health in BioD_ProCer_100RezRoom.pcc, export 3956, sequence TheWorld.PersistentLevel.Main_Sequence.LS0_LevelLoad, target SeqVar_Player
             */
            var DPProCerF = MERFileSystem.GetPackageFile("BioD_ProCer.pcc");
            if (DPProCerF != null && File.Exists(DPProCerF))
            {
                var bpProCerP = MEPackageHandler.OpenMEPackage(DPProCerF);
                bpProCerP.GetUExport(1171).WriteProperty(new IntProperty(0, "bValue"));
                MERFileSystem.SavePackage(bpProCerP);
            }
            var rezRoom = MERFileSystem.GetPackageFile("BioD_ProCer_100RezRoom.pcc");
            if (rezRoom != null && File.Exists(rezRoom))
            {
                var rezRoomP = MEPackageHandler.OpenMEPackage(rezRoom);
                rezRoomP.GetUExport(3956).WriteProperty(new IntProperty(0, "bValue"));
                MERFileSystem.SavePackage(rezRoomP);
            }
        }

        private static void ZeroOutStatList(ExportEntry statClassExp, string propertyName, bool createIfNonExistent)
        {
            var statExp = statClassExp.GetDefaults();
            var stat = statExp.GetProperty<ArrayProperty<FloatProperty>>(propertyName);
            if (stat == null && createIfNonExistent)
            {
                stat = new ArrayProperty<FloatProperty>(propertyName);
            }

            if (stat == null)
                return; // Nothing to do

            stat.Clear();
            int i = 0;
            while (i < 4)
            {
                stat.Add(new FloatProperty(0));
                i++;
            }
            statExp.WriteProperty(stat);
        }

        /// <summary>
        /// Installs NOP bytecodes into function data
        /// </summary>
        /// <param name="funcData"></param>
        /// <param name="startOffset"></param>
        /// <param name="nopLen"></param>
        public static void NopRange(byte[] funcData, int startOffset, int nopLen)
        {
            for (int i = 0; i < nopLen; i++)
            {
                funcData[startOffset + i] = 0x0B; // EX_Nothing
            }
        }
    }
}
