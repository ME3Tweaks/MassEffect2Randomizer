using System.Collections.Generic;
using System.ComponentModel;

namespace RandomizerUI.Classes
{
    public class ImageCredit : INotifyPropertyChanged
    {
        public string Title { get; internal set; }
        public string Author { get; internal set; }
        public string InternalName { get; internal set; }
        public string Link { get; internal set; }
        public string License { get; internal set; }
#pragma warning disable
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore

        /// <summary>
        /// Loads the image credits from an embedded file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fourLine"></param>
        /// <returns></returns>
        public static List<ImageCredit> LoadImageCredits(string file, bool fourLine)
        {
            var textFile = MERUtilities.GetEmbeddedStaticFilesTextFile(file).Split('\n');
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
