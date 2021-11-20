using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers.Game1._2DA
{
    class RGalaxyMapPlanets2DA
    {
        /// <summary>
        /// Randomizes the planet-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizePlanets(GameTarget target, RandomizationOption option, ExportEntry export)
        {
            option.CurrentOperation = "Randomizing Galaxy Map - Planets";

            Debug.WriteLine("Randomizing Galaxy Map - Planets");
            Bio2DA planet2da = new Bio2DA(export);
            int[] colsToRandomize = { 2, 3 }; //X,Y
            for (int row = 0; row < planet2da.RowNames.Count(); row++)
            {
                for (int i = 0; i < planet2da.ColumnNames.Count(); i++)
                {
                    if (planet2da[row, i] != null && planet2da[row, i].Type == Bio2DACell.Bio2DADataType.TYPE_FLOAT)
                    {
                        Debug.WriteLine("[" + row + "][" + i + "]  (" + planet2da.ColumnNames[i] + ") value is " + planet2da[row, i].FloatValue);
                        float randvalue = ThreadSafeRandom.NextFloat(0, 1);
                        if (i == 11)
                        {
                            randvalue = ThreadSafeRandom.NextFloat(2.5, 8.0);
                        }

                        Debug.WriteLine("Planets Randomizer [" + row + "][" + i + "] (" + planet2da.ColumnNames[i] + ") value is now " + randvalue);
                        planet2da[row, i].FloatValue = randvalue;
                    }
                }
            }

            planet2da.Write2DAToExport();
        }
    }
}
