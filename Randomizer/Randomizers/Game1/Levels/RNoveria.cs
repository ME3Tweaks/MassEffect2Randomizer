using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.TLK.ME2ME3;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers.Levels
{
    class RNoveria
    {
        private static void RandomizeNoveria(GameTarget target, RandomizationOption option)
        {
            mainWindow.CurrentOperationText = "Randoming Noveria";

            //Make turrets and ECRS guard hostile
            ME1Package introConfrontation = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\ICE\DSG\BIOA_ICE20_01a_DSG.SFM"));

            //Intro area
            var addToSquads = new[]
            {
                introConfrontation.getUExport(1776), introConfrontation.getUExport(1786), introConfrontation.getUExport(1786)
            };
            foreach (var pawnBehavior in addToSquads)
            {
                pawnBehavior.WriteProperty(new ObjectProperty(1958, "Squad"));
            }
            introConfrontation.save();
            ModifiedFiles[introConfrontation.FileName] = introConfrontation.FileName;
        }

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            RandomizeNoveria(target, option);
            return true;
        }
    }
}
