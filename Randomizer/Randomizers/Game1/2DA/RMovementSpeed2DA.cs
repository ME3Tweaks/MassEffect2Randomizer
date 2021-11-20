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
    class RMovementSpeed2DA
    {
        public static bool RandomizeExport(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            Bio2DA movementSpeed2DA = new Bio2DA(export);
            int[] colsToRandomize = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15, 16, 17, 18, 19 };
            for (int row = 0; row < movementSpeed2DA.RowNames.Count(); row++)
            {
                for (int i = 0; i < colsToRandomize.Count(); i++)
                {
                    //Debug.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(cluster2da[row, colsToRandomize[i]].Data, 0));
                    int randvalue = ThreadSafeRandom.Next(10, 1200);
                    Debug.WriteLine("Movement Speed Randomizer [" + row + "][" + colsToRandomize[i] + "] value is now " + randvalue);
                    movementSpeed2DA[row, colsToRandomize[i]].IntValue = randvalue;
                }
            }

            movementSpeed2DA.Write2DAToExport();
            return true;
        }

        private static bool CanRandomize(ExportEntry export)
        {
            if (export.ClassName != "Bio2DA") return false;
            if (!export.ObjectName.Name.StartsWith("MovementTables_CreatureSpeeds")) return false;
            return true;
        }
    }
}
