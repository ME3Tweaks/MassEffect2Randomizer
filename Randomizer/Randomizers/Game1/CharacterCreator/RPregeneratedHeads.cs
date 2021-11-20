using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.Classes;

namespace Randomizer.Randomizers.Game1.CharacterCreator
{
    class RPregeneratedHeads
    {
        private void RandomizePregeneratedHead(ExportEntry export, Random random)
        {
            int[] floatSliderIndexesToRandomize = { 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 19, 20, 21, 22, 24, 25, 26, 27, 29, 30 };
            Dictionary<int, int> columnMaxDictionary = new Dictionary<int, int>();
            columnMaxDictionary[1] = 7; //basehead
            columnMaxDictionary[2] = 6; //skintone
            columnMaxDictionary[3] = 3; //archetype
            columnMaxDictionary[4] = 14; //scar
            columnMaxDictionary[12] = 8; //eyeshape
            columnMaxDictionary[18] = 13; //iriscolor +1
            columnMaxDictionary[23] = 10; //mouthshape
            columnMaxDictionary[28] = 13; //noseshape
            columnMaxDictionary[31] = 14; //beard
            columnMaxDictionary[32] = 7; //brows +1
            columnMaxDictionary[33] = 9; //hair
            columnMaxDictionary[34] = 8; //haircolor
            columnMaxDictionary[35] = 8; //facialhaircolor

            if (export.ObjectName.Name.Contains("Female"))
            {
                floatSliderIndexesToRandomize = new int[] { 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 19, 20, 21, 22, 24, 25, 26, 27, 29, 30 };
                columnMaxDictionary.Clear();
                //there are female specific values that must be used
                columnMaxDictionary[1] = 10; //basehead
                columnMaxDictionary[2] = 6; //skintone
                columnMaxDictionary[3] = 3; //archetype
                columnMaxDictionary[4] = 11; //scar
                columnMaxDictionary[12] = 10; //eyeshape
                columnMaxDictionary[18] = 13; //iriscolor +1
                columnMaxDictionary[23] = 10; //mouthshape
                columnMaxDictionary[28] = 12; //noseshape
                columnMaxDictionary[31] = 8; //haircolor
                columnMaxDictionary[32] = 10; //hair
                columnMaxDictionary[33] = 17; //brows
                columnMaxDictionary[34] = 7; //browcolor
                columnMaxDictionary[35] = 7; //blush
                columnMaxDictionary[36] = 8; //lipcolor
                columnMaxDictionary[37] = 8; //eyemakeupcolor

            }

            Bio2DA export2da = new Bio2DA(export);
            for (int row = 0; row < export2da.RowNames.Count(); row++)
            {
                foreach (int col in floatSliderIndexesToRandomize)
                {
                    export2da[row, col].FloatValue = ThreadSafeRandom.NextFloat(0, 2);
                }
            }

            for (int row = 0; row < export2da.RowNames.Count(); row++)
            {
                foreach (KeyValuePair<int, int> entry in columnMaxDictionary)
                {
                    int col = entry.Key;
                    Debug.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value originally is " + export2da[row, col].DisplayableValue);

                    export2da[row, col].IntValue = ThreadSafeRandom.Next(0, entry.Value) + 1;
                    Debug.WriteLine("Character Creator Randomizer [" + row + "][" + col + "] (" + export2da.ColumnNames[col] + ") value is now " + export2da[row, col].DisplayableValue);

                }
            }

            Debug.WriteLine("Writing export " + export.ObjectName);
            export2da.Write2DAToExport();
        }

    }
}
