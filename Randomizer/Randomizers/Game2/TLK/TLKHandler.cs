using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME2ME3;
using HuffmanCompression = LegendaryExplorerCore.TLK.ME2ME3.HuffmanCompression;

namespace Randomizer.Randomizers.Game2.TLK
{
    class TLKHandler
    {
        #region Static Access
        private static List<TalkFile> LoadedOfficialTalkFiles { get; set; }
        private static List<TalkFile> MERTalkFiles { get; set; }
        private static TLKHandler CurrentHandler { get; set; }
        public const int FirstDynamicID = 7421320;


        /// <summary>
        /// Starts up the TLK subsystem. These methods should not be across multiple threads as they are not thread safe!
        /// </summary>
        /// <param name="usingDLCSystem"></param>
        public static void StartHandler()
        {
            CurrentHandler = new TLKHandler();
            CurrentHandler.Start();
        }

        public static void EndHandler()
        {
            // Commit
            if (CurrentHandler != null)
            {
                CurrentHandler.Commit();
                CurrentHandler = null;
            }
        }

        public static int GetNewTLKID()
        {
            return CurrentHandler.GetNextID();
        }

        /// <summary>
        /// Replaces a string with another. Specify an uppercase language code if you want to replace a string only for a specific language.
        /// </summary>
        /// <param name="stringid"></param>
        /// <param name="newText"></param>
        public static void ReplaceString(int stringid, string newText, string langCode = null)
        {
            CurrentHandler.InternalReplaceString(stringid, newText, langCode);
        }

        /// <summary>
        /// Looks up a string, but only in a certain language
        /// </summary>
        /// <param name="stringId"></param>
        /// <param name="langCode">Upper case lang code</param>
        /// <returns></returns>
        public static string TLKLookupByLang(int stringId, string langCode)
        {
            if (stringId <= 0) return null; // No data
            if (LoadedOfficialTalkFiles != null)
            {
                foreach (TalkFile tf in LoadedOfficialTalkFiles.Where(x => Path.GetFileNameWithoutExtension(x.path).EndsWith($"_{langCode}")))
                {
                    var data = tf.findDataById(stringId, returnNullIfNotFound: true, noQuotes: true);
                    if (data != null)
                        return data;
                }
            }
            return null;
        }

        /// <summary>
        /// Used to look up a string. Doesn't care about the language. Used only for some debugging code
        /// </summary>
        /// <param name="stringId"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public static string TLKLookup(int stringId, IMEPackage package) // package attribute is not used but is required by signature
        {
            if (stringId <= 0) return null; // No data
            if (LoadedOfficialTalkFiles != null)
            {
                foreach (TalkFile tf in LoadedOfficialTalkFiles)
                {
                    var data = tf.findDataById(stringId, returnNullIfNotFound: true);
                    if (data != null)
                        return data;
                }
            }
            return null;
        }

        /// <summary>
        /// Performs language-aware string replacements, by making the data resolved by oldTlkId return data that would be resolved if it was fetching from newTlkId
        /// </summary>
        /// <param name="oldTlkId"></param>
        /// <param name="newTlkId"></param>
        public static void ReplaceStringByRepoint(int oldTlkId, int newTlkId)
        {
            // ME2 doesn't appear to use $ repoints
            CurrentHandler.InternalReplaceStringByRepoint(oldTlkId, newTlkId);
        }

        /// <summary>
        /// Gets the talk file
        /// </summary>
        /// <returns></returns>
        public static TalkFile GetBuildingTLK()
        {
            return CurrentHandler.InternalGetBuildingTLK();
        }

        private TalkFile InternalGetBuildingTLK()
        {
            return MERTalkFile;
        }

        #endregion

        #region Private members

        private SortedSet<string> loadedLanguages = new SortedSet<string>();
        private int NextDynamicID = FirstDynamicID;
        private TalkFile MERTalkFile;
        private void Start()
        {
            LoadedOfficialTalkFiles = new List<TalkFile>();
            MERTalkFiles = new List<TalkFile>();
            // Load the basegame TLKs
            var bgPath = MEDirectories.GetBioGamePath(MERFileSystem.Game);
            // ME2 specific - ignore ME2Randomizer TLKs, we do not want to modify those
            var tlkFiles = Directory.GetFiles(bgPath, "*.tlk", SearchOption.AllDirectories);
            foreach (var tlkFile in tlkFiles)
            {
                if (tlkFile.Contains("DLC_440")) // Change if our module number changes
                {
                    TalkFile tf = new TalkFile();
                    tf.LoadTlkData(tlkFile);
                    MERTalkFiles.Add(tf);
                    if (tlkFile.Contains("_INT"))
                        MERTalkFile = tf;
                    var fname = Path.GetFileNameWithoutExtension(tlkFile);
                    loadedLanguages.Add(fname.Substring(fname.LastIndexOf("_") + 1));
                }
                else
                {
                    TalkFile tf = new TalkFile();
                    tf.LoadTlkData(tlkFile);
                    LoadedOfficialTalkFiles.Add(tf);
                    var fname = Path.GetFileNameWithoutExtension(tlkFile);
                    loadedLanguages.Add(fname.Substring(fname.LastIndexOf("_") + 1));
                }
            }
        }

        private void Commit()
        {
            // Write out the TLKs
            Parallel.ForEach(MERTalkFiles, tf =>
            {
                if (tf.IsModified)
                {
                    HuffmanCompression hc = new HuffmanCompression();
                    hc.LoadInputData(tf.StringRefs);
                    hc.SaveToFile(tf.path);
                }
            });

            // Free memory
            MERTalkFile = null;
            MERTalkFiles = null;
            LoadedOfficialTalkFiles = null;
        }

        private int GetNextID()
        {
            return NextDynamicID++;
        }

        private void InternalReplaceString(int stringid, string newText, string langCode = null)
        {
            foreach (var tf in MERTalkFiles)
            {
                // Check if this string should be replaced in this language
                if (langCode != null && !Path.GetFileNameWithoutExtension(tf.path).EndsWith($@"_{langCode}")) continue;
                //Debug.WriteLine($"TLK installing {stringid}: {newText}");
                lock (syncObj)
                {
                    //Debug.WriteLine($"ReplaceStr {stringid} to {newText}");
                    tf.ReplaceString(stringid, newText, true);
                }
            }
        }

        private object syncObj = new object();

        public static IEnumerable<TalkFile> GetOfficialTLKs()
        {
            return CurrentHandler.InternalGetOfficialTLKs();
        }

        private IEnumerable<TalkFile> InternalGetOfficialTLKs()
        {
            return LoadedOfficialTalkFiles;
        }

        private void InternalReplaceStringByRepoint(int oldTlkId, int newTlkId)
        {
            foreach (var lang in loadedLanguages)
            {
                ReplaceString(oldTlkId, TLKLookupByLang(newTlkId, lang), lang);
            }
        }
        #endregion

        public static List<TalkFile> GetMERTLKs()
        {
            return CurrentHandler.InternalGetMERTLKs();
        }

        private List<TalkFile> InternalGetMERTLKs()
        {
            return MERTalkFiles;
        }

        public static List<TalkFile> GetAllTLKs()
        {
            return CurrentHandler.InternalGetAllTLKs();
        }

        private List<TalkFile> InternalGetAllTLKs()
        {
            var items = LoadedOfficialTalkFiles.ToList();
            items.AddRange(MERTalkFiles);
            return items;
        }

        public static bool IsAssignedMERString(int descriptionPropValue)
        {
            return descriptionPropValue >= FirstDynamicID && descriptionPropValue <= CurrentHandler.NextDynamicID;
        }
    }
}
