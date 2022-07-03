using System.Collections.Generic;
using System.ComponentModel;
using PropertyChanged;
using Randomizer.MER;

namespace RandomizerUI.Classes
{
    [AddINotifyPropertyChangedInterface]
    public class ImageCredit
    {
        public string Title { get; internal set; }
        public string Author { get; internal set; }
        public string InternalName { get; internal set; }
        public string Link { get; internal set; }
        public string License { get; internal set; }

        /// <summary>
        /// Loads the image credits from an embedded file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fourLine"></param>
        /// <returns></returns>
        public static List<ImageCredit> LoadImageCredits(string file, bool fourLine)
        {
            var textFile = MEREmbedded.GetEmbeddedTextAsset(file).Split('\n');
            List<ImageCredit> credits = new List<ImageCredit>(330);

            ImageCredit currentCredit = null;
            for (int i = 0; i < textFile.Length; i++)
            {
                var trimmedline = textFile[i].Trim();
                if (trimmedline.StartsWith("#"))
                {
                    continue;
                }

                if (trimmedline == "" && currentCredit == null)
                {
                    currentCredit = new ImageCredit();
                    continue;
                }

                if (trimmedline == "")
                {
                    credits.Add(currentCredit);
                    currentCredit = new ImageCredit();
                    continue;
                }

                if (currentCredit == null) currentCredit = new ImageCredit();

                int offsetIndex = 0;

                currentCredit.Title = textFile[i].Trim();
                currentCredit.Author = textFile[i + (++offsetIndex)].Trim();
                if (!fourLine)
                {
                    currentCredit.InternalName = textFile[i + (++offsetIndex)].Trim();
                }

                currentCredit.Link = textFile[i + (++offsetIndex)].Trim();
                currentCredit.License = textFile[i + (++offsetIndex)].Trim();
                i += offsetIndex;
            }

            if (currentCredit != null)
            {
                credits.Add(currentCredit);
            }

            return credits;
        }

    }

}
