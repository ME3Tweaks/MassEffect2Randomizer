using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME2ME3;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Levels
{
    class RNoveria
    {
        private static void RandomizeNoveria(GameTarget target, RandomizationOption option)
        {
            option.CurrentOperation = "Randoming Noveria";
            
            //Make turrets and ECRS guard hostile
            var introConfrontation = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\ICE\DSG\BIOA_ICE20_01a_DSG.SFM"));

            //Intro area
            var addToSquads = new[]
            {
                introConfrontation.GetUExport(1776), introConfrontation.GetUExport(1786), introConfrontation.GetUExport(1786)
            };
            foreach (var pawnBehavior in addToSquads)
            {
                pawnBehavior.WriteProperty(new ObjectProperty(1958, "Squad"));
            }

            MERFileSystem.SavePackage(introConfrontation);
        }

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            RandomizeNoveria(target, option);
            return true;
        }
    }
}
