using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game1._2DA;
using Randomizer.Randomizers.Game1.GalaxyMap;

namespace Randomizer.Randomizers.Levels
{
    class RBringDownTheSky
    {
        private static void RandomizeBDTS(GameTarget target, RandomizationOption option)
        {
            //Randomize planet in the sky
            var bdtsPlanetFile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"DLC\DLC_UNC\CookedPC\Maps\UNC52\LAY\BIOA_UNC52_00_LAY.SFM"));
            ExportEntry planetMaterial = bdtsPlanetFile.GetUExport(1546); //BIOA_DLC_UNC52_T.GXM_EarthDup
            PlanetMIC.RandomizePlanetMaterialInstanceConstant(target, planetMaterial, realistic: true);
            MERFileSystem.SavePackage(bdtsPlanetFile);

            //Randomize the Bio2DA talent table for the turrets
            var bdtsTalents = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"DLC\DLC_UNC\CookedPC\Packages\2DAs\BIOG_2DA_UNC_Talents_X.upk"));
            Bio2DA talentEffectLevels = new Bio2DA(bdtsTalents.GetUExport(2));

            for (int i = 0; i < talentEffectLevels.RowCount; i++)
            {
                string rowEffect = talentEffectLevels[i, "GameEffect_Label"].DisplayableValue;
                if (rowEffect.EndsWith("Cooldown") || rowEffect.EndsWith("CastingTime"))
                {
                    float newValue = ThreadSafeRandom.NextFloat(0, 1);
                    if (ThreadSafeRandom.Next(2) == 0) newValue = 0.01f;
                    for (int j = 1; j < 12; j++)
                    {
                        talentEffectLevels[i, "Level" + j].DisplayableValue = newValue.ToString();
                    }
                }
                else if (rowEffect.EndsWith("TravelSpeed"))
                {
                    int newValue = ThreadSafeRandom.Next(2000) + 2000;
                    for (int j = 1; j < 12; j++)
                    {
                        talentEffectLevels[i, "Level" + j].DisplayableValue = newValue.ToString();
                    }
                }
            }

            talentEffectLevels.Write2DAToExport();
            MERFileSystem.SavePackage(bdtsTalents);

        }

        public static bool PerformRandomization(GameTarget arg1, RandomizationOption arg2)
        {
            RandomizeBDTS(arg1, arg2);
            return true;
        }
    }
}
