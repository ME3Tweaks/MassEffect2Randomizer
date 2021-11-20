using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;
using Serilog;

namespace Randomizer.Randomizers.Game1.Misc
{
    // MUSIC                     case "Music_Music":
    // UIMUSIC                     case "UISounds_GuiMusic":
    // UI SOUNDS                     case "UISounds_GuiSounds":

    class RSounds
    {
        /// <summary>
        /// Randomizes the sounds and music in GUIs. This is shared between two tables as it contains the same indexing and table format
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeGUISounds(GameTarget target, RandomizationOption option, ExportEntry export, string requiredprefix = null)
        {
            option.CurrentOperation = "Randomizing UI - Sounds";
            Bio2DA guisounds2da = new Bio2DA(export);
            var names = new List<NameReference>();


            // TODO: REWRITE THIS WITH NEW INFO ABOUT HOW SOUNDS WORK
            //if (requiredprefix != "music")
            //{

            //    for (int row = 0; row < guisounds2da.RowNames.Count(); row++)
            //    {
            //        if (guisounds2da[row, 0] != null && guisounds2da[row, 0].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
            //        {
            //            if (requiredprefix != null && !guisounds2da[row, 0].DisplayableValue.StartsWith(requiredprefix))
            //            {
            //                continue;
            //            }

            //            names.Add(guisounds2da[row, 0].NameValue);
            //        }
            //    }
            //}
            //else
            //{
            //    for (int n = 0; n < export.FileRef.Names.Count; n++)
            //    {
            //        string name = export.FileRef.Names[n];
            //        if (name.StartsWith("music.mus"))
            //        {
            //            Int64 nameval = n;
            //            names.Add(nameval));
            //        }
            //    }
            //}

            for (int row = 0; row < guisounds2da.RowNames.Count(); row++)
            {

                // TODO UPDATE
                //if (guisounds2da[row, 0] != null && guisounds2da[row, 0].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
                //{
                //    if (requiredprefix != null && !guisounds2da[row, 0].DisplayableValue.StartsWith(requiredprefix))
                //    {
                //        continue;
                //    }

                //    Thread.Sleep(20);
                //    Debug.WriteLine("[" + row + "][" + 0 + "]  (" + guisounds2da.ColumnNames[0] + ") value originally is " + guisounds2da[row, 0].GetDisplayableValue());
                //    int r = ThreadSafeRandom.Next(names.Count);
                //    byte[] pnr = names[r];
                //    names.RemoveAt(r);
                //    guisounds2da[row, 0].NameValue = pnr;
                //    Debug.WriteLine("Sounds - GUI Sounds Randomizer [" + row + "][" + 0 + "] (" + guisounds2da.ColumnNames[0] + ") value is now " + guisounds2da[row, 0].GetDisplayableValue());

                //}
            }

            guisounds2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the the music table
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeMusic(GameTarget target, RandomizationOption option, ExportEntry export)
        {
            option.CurrentOperation = "Randomizing Music";
            Bio2DA music2da = new Bio2DA(export);
            List<byte[]> names = new List<byte[]>();
            int[] colsToRandomize = { 0, 5, 6, 7, 8, 9, 10, 11, 12 };
            //for (int row = 0; row < music2da.RowNames.Count(); row++)
            //{
            //    foreach (int col in colsToRandomize)
            //    {
            //        if (music2da[row, col] != null && music2da[row, col].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
            //        {
            //            if (!music2da[row, col].DisplayableValue.StartsWith("music"))
            //            {
            //                continue;
            //            }

            //            names.Add(music2da[row, col].Data.TypedClone());
            //        }
            //    }
            //}

            //for (int row = 0; row < music2da.RowNames.Count(); row++)
            //{
            //    foreach (int col in colsToRandomize)
            //    {
            //        if (music2da[row, col] != null && music2da[row, col].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
            //        {
            //            if (!music2da[row, col].GetDisplayableValue().StartsWith("music"))
            //            {
            //                continue;
            //            }

            //            MERLog.Information("[" + row + "][" + col + "]  (" + music2da.ColumnNames[col] + ") value originally is " + music2da[row, col].GetDisplayableValue());
            //            int r = ThreadSafeRandom.Next(names.Count);
            //            byte[] pnr = names[r];
            //            names.RemoveAt(r);
            //            music2da[row, col].Data = pnr;
            //            MERLog.Information("Music Randomizer [" + row + "][" + col + "] (" + music2da.ColumnNames[col] + ") value is now " + music2da[row, col].GetDisplayableValue());

            //        }
            //    }
            //}

            // TODO: CHANGE WITH NEW INFO WE KNOW ABOUT HOW MUSIC WORKS

            music2da.Write2DAToExport();
        }
    }
}
