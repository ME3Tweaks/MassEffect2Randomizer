using System.Collections.Generic;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game1._2DA
{
    /// <summary>
    /// Randomizes Music 2DA tables
    /// </summary>
    class RMusic2DA
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"Bio2DA" && export.ObjectName.Name.StartsWith("Music_Music");

        /// <summary>
        /// Randomizes the sound cues in the music table.
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        public static bool RandomizeExport(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;

            Bio2DA music2da = new Bio2DA(export);

            // ENHANCEMENT: ADD MORE SOUND CUES (like the ones we used in ahern's map)
            List<NameReference> availableMusic = new List<NameReference>();

            // Get list of available music to shuffle
            string[] colsToRandomize = { "SoundCue", "1", "2", "3", "4", "5", "6", "7", "8" };
            for (int row = 0; row < music2da.RowNames.Count; row++)
            {
                foreach (var col in colsToRandomize)
                {
                    var colIndex = music2da.GetColumnIndexByName(col);
                    if (music2da[row, colIndex] != null)
                    {
                        var currentValue = music2da[row, colIndex].NameValue.Name;
                        if (currentValue.StartsWith("music")) // CASE SENSITIVE
                        {
                            availableMusic.Add(currentValue);
                        }
                    }
                }
            }

            // Shuffle the music
            availableMusic.Shuffle();
            for (int row = 0; row < music2da.RowNames.Count; row++)
            {
                foreach (var col in colsToRandomize)
                {
                    var colIndex = music2da.GetColumnIndexByName(col);
                    if (music2da[row, colIndex] != null)
                    {
                        if (music2da[row, colIndex].NameValue.Name.StartsWith("music")) // CASE SENSITIVE
                        {
                            music2da[row, colIndex].NameValue = availableMusic.PullFirstItem();
                        }
                    }
                }
            }

            music2da.Write2DAToExport();
            return true;
        }
    }
}
