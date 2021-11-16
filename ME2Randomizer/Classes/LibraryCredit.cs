using System.Collections.Generic;
using System.ComponentModel;

namespace RandomizerUI.Classes
{
    public class LibraryCredit : INotifyPropertyChanged
    {
        public string LibraryName { get; internal set; }
        public string LibraryPurpose { get; internal set; }
        public string Link { get; internal set; }
#pragma warning disable
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore

        /// <summary>
        /// Loads the image credits from an embedded file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fourLine"></param>
        /// <returns></returns>
        public static List<LibraryCredit> LoadLibraryCredits(string file)
        {
            var textFile = MERUtilities.GetEmbeddedStaticFilesTextFile(file).Split('\n');
            List<LibraryCredit> credits = new List<LibraryCredit>(100);

            LibraryCredit currentCredit = null;
            for (int i = 0; i < textFile.Length; i++)
            {
                var trimmedline = textFile[i].Trim();

                if (trimmedline == "" && currentCredit == null)
                {
                    // First line blank
                    currentCredit = new LibraryCredit();
                    continue;
                }

                if (trimmedline == "")
                {
                    // Going to new credit
                    credits.Add(currentCredit);
                    currentCredit = new LibraryCredit();
                    continue;
                }

                // First line not blank, initialize credit object
                if (currentCredit == null) currentCredit = new LibraryCredit();
                int creditStartLine = 0;

                // Read data
                currentCredit.LibraryName = textFile[i].Trim();
                currentCredit.LibraryPurpose = textFile[i + (++creditStartLine)].Trim();
                currentCredit.Link = textFile[i + (++creditStartLine)].Trim();
                
                // Move pointer
                i += creditStartLine;
            }

            if (currentCredit != null)
            {
                credits.Add(currentCredit);
            }

            return credits;
        }

    }

}
