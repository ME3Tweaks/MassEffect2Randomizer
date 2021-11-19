using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME2ME3;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers.Game1._2DA
{
    class RGalaxyMapClusters2DA
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"Bio2DANumberedRows" && export.ObjectName.Name.StartsWith("GalaxyMap_Cluster");

        /// <summary>
        /// Randomizes the highest-level galaxy map view. Values are between 0 and 1 for columns 1 and 2 (X,Y).
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        public static bool RandomizeClustersXY(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;

            Bio2DA cluster2da = new Bio2DA(export);
            int xColIndex = cluster2da.GetColumnIndexByName("X");
            int yColIndex = cluster2da.GetColumnIndexByName("Y");

            for (int row = 0; row < cluster2da.RowNames.Count(); row++)
            {
                //Randomize X,Y
                cluster2da[row, xColIndex].FloatValue = ThreadSafeRandom.NextFloat(0, 1);
                cluster2da[row, yColIndex].FloatValue = ThreadSafeRandom.NextFloat(0, 1);
            }

            cluster2da.Write2DAToExport();
            return true;
        }
    }
}
