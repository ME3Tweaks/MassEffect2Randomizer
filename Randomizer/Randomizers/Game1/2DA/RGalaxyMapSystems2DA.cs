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
    class RGalaxyMapSystems2DA
    {
        /// <summary>
        /// Randomizes the mid-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeSystems(GameTarget target, RandomizationOption option, ExportEntry export)
        {
            option.CurrentOperation = "Randomizing Galaxy Map - Systems";

            Debug.WriteLine("Randomizing Galaxy Map - Systems");
            Bio2DA system2da = new Bio2DA(export);
            int[] colsToRandomize = { 2, 3 }; //X,Y
            for (int row = 0; row < system2da.RowNames.Count(); row++)
            {
                for (int i = 0; i < colsToRandomize.Count(); i++)
                {
                    //Debug.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(system2da[row, colsToRandomize[i]].Data, 0));
                    float randvalue = ThreadSafeRandom.NextFloat(0, 1);
                    Debug.WriteLine("System Randomizer [" + row + "][" + colsToRandomize[i] + "] value is now " + randvalue);
                    system2da[row, colsToRandomize[i]].FloatValue = randvalue;
                }

                //string value = system2da[row, 9].GetDisplayableValue();
                //Debug.WriteLine("Scale: [" + row + "][9] value is " + value);
                float scalerandvalue = ThreadSafeRandom.NextFloat(0.25, 2);
                Debug.WriteLine("System Randomizer [" + row + "][9] value is now " + scalerandvalue);
                system2da[row, 9].FloatValue = scalerandvalue;
            }

            system2da.Write2DAToExport();
        }
    }
}
