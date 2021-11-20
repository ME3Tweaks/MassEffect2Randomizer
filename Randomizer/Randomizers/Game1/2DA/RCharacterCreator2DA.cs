using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game1._2DA
{
    class RCharacterCreator2DA
    {
        // Run on specific character creator 2DA types
        public const string RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE = "RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE";

        /// <summary>
        /// Randomizes the character creator
        /// </summary>
        /// <param name="random">Random number generator</param>
        private void RandomizeCharacterCreator2DA(GameTarget target, RandomizationOption option, ExportEntry export)
        {
            option.CurrentOperation = "Randomizing Character Creator";
            //if (headrandomizerclasses.Contains(export.ObjectName))
            //{
            //    RandomizePregeneratedHead(export, random);
            //    continue;
            //}
            Bio2DA export2da = new Bio2DA(export);
            bool hasChanges = false;
            for (int row = 0; row < export2da.RowNames.Count(); row++)
            {
                float numberedscalar = 0;
                for (int col = 0; col < export2da.ColumnNames.Count(); col++)
                {
                    Bio2DACell cell = export2da[row, col];

                    //Extent
                    if (export2da.ColumnNames[col] == "Extent" || export2da.ColumnNames[col] == "Rand_Extent")
                    {
                        float multiplier = ThreadSafeRandom.NextFloat(0.5, 6);
                        Debug.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value originally is " + export2da[row, col].DisplayableValue);

                        if (cell.Type == Bio2DACell.Bio2DADataType.TYPE_FLOAT)
                        {
                            cell.FloatValue = cell.FloatValue * multiplier;
                            hasChanges = true;
                        }
                        else
                        {
                            cell.FloatValue = cell.IntValue * multiplier; // int to float
                            hasChanges = true;
                        }

                        Debug.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value now is " + cell.DisplayableValue);
                        continue;
                    }

                    //Hair Scalars
                    if (export.ObjectName.Name.Contains("MorphHair") && row > 0 && col >= 4 && col <= 8)
                    {

                        float scalarval = ThreadSafeRandom.NextFloat(0, 1);
                        if (col == 5)
                        {
                            numberedscalar = scalarval;
                        }
                        else if (col > 5)
                        {
                            scalarval = numberedscalar;
                        }

                        // Bio2DACell cellX = cell;
                        Debug.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value originally is " + cell.DisplayableValue);
                        cell.FloatValue = scalarval;
                        Debug.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value now is " + cell.DisplayableValue);
                        hasChanges = true;
                        continue;
                    }

                    //Skin Tone
                    if (cell != null && cell.Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
                    {
                        if (export.ObjectName.Name.Contains("Skin_Tone") && option.HasSubOptionSelected(RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE))
                        {
                            continue; //skip
                        }

                        string value = cell.DisplayableValue;
                        if (value.StartsWith("RGB("))
                        {
                            //Make new item
                            string rgbNewName = ColorTools.GetRandomColorRBGStr();
                            cell.NameValue = rgbNewName;
                            hasChanges = true;
                        }
                    }

                    string columnName = export2da.ColumnNames[col];
                    if (columnName.Contains("Scalar") && cell != null && cell.Type != Bio2DACell.Bio2DADataType.TYPE_NAME)
                    {
                        float currentValue = float.Parse(cell.DisplayableValue);
                        cell.FloatValue = currentValue * ThreadSafeRandom.NextFloat(0.5, 2);
                        hasChanges = true;
                    }

                    //if (export.ObjectName.Contains("Skin_Tone") && mainWindow.RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE && row > 0 && col >= 1 && col <= 5)
                    //{
                    //    if (export.ObjectName.Contains("Female"))
                    //    {
                    //        if (col < 5)
                    //        {
                    //            //Females have one less column
                    //            string rgbNewName = GetRandomColorRBGStr(random);
                    //            int newValue = export.FileRef.FindNameOrAdd(rgbNewName);
                    //            export2da[row, col].Data = BitConverter.GetBytes((ulong)newValue); //name is 8 bytes
                    //            hasChanges = true;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        string rgbNewName = GetRandomColorRBGStr(random);
                    //        int newValue = export.FileRef.FindNameOrAdd(rgbNewName);
                    //        export2da[row, col].Data = BitConverter.GetBytes((ulong)newValue); //name is 8 bytes
                    //        hasChanges = true;
                    //    }
                    //}
                }
            }

            if (hasChanges)
            {
                export2da.Write2DAToExport();
            }


        }

    }
}
