using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK.ME2ME3;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Handlers
{
    /// <summary>
    /// Class for handling and installing TLK strings
    /// </summary>
    class TLKBuilder
    {
        #region Static Access
        /// <summary>
        /// Loaded BioWare global TLKs
        /// </summary>
        private static List<ITalkFile> LoadedOfficialTalkFiles { get; set; }

        /// <summary>
        /// Loaded MER global TLKs
        /// </summary>
        private static List<ITalkFile> MERTalkFiles { get; set; }

        /// <summary>
        /// The package that contains the global TLK
        /// </summary>
        private static List<IMEPackage> Game1GlobalTlkPackages { get; set; }

        /// <summary>
        /// Gets the talk file
        /// </summary>
        /// <returns></returns>
        public static ITalkFile GetBuildingTLK()
        {
            return CurrentHandler.InternalGetBuildingTLK();
        }

        private ITalkFile InternalGetBuildingTLK()
        {
            return MERTalkFile;
        }

        private static TLKBuilder CurrentHandler { get; set; }
        public const int FirstDynamicID = 7421320;


        /// <summary>
        /// Starts up the TLK subsystem. These methods should not be across multiple threads as they are not thread safe!
        /// </summary>
        /// <param name="usingDLCSystem"></param>
        public static void StartHandler(GameTarget target)
        {
            CurrentHandler = new TLKBuilder();
            CurrentHandler.Start(target);
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
        public static void ReplaceString(int stringid, string newText, MELocalization? localization = null)
        {
            CurrentHandler.InternalReplaceString(stringid, newText, localization);
        }

        /// <summary>
        /// Looks up a string, but only in a certain language
        /// </summary>
        /// <param name="stringId"></param>
        /// <param name="langCode">Upper case lang code</param>
        /// <returns></returns>
        public static string TLKLookupByLang(int stringId, MELocalization localization)
        {
            if (stringId <= 0) return null; // No data
            if (LoadedOfficialTalkFiles != null)
            {
                foreach (var tf in LoadedOfficialTalkFiles.Where(x => x.Localization == localization))
                {
                    var data = tf.FindDataById(stringId, returnNullIfNotFound: true, noQuotes: true);
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
                foreach (var tf in LoadedOfficialTalkFiles)
                {
                    var data = tf.FindDataById(stringId, returnNullIfNotFound: true);
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



        #endregion

        #region Private members

        private SortedSet<MELocalization> loadedLanguages = new();
        private int NextDynamicID = FirstDynamicID;
        private ITalkFile MERTalkFile;

        private void Start(GameTarget target)
        {
            LoadedOfficialTalkFiles = new();
            MERTalkFiles = new();

#if __GAME1__
            // Load the basegame TLKs
            _updatedTlkStrings = new List<int>();
            var loadedFiles = MELoadedFiles.GetFilesLoadedInGame(target.Game, true, gameRootOverride: target.TargetPath);

            Game1GlobalTlkPackages = new List<IMEPackage>(3);
            // ME1: GlobalTLKs, LE1: Startup_INT
            if (target.Game.IsOTGame())
            {
                foreach (var f in loadedFiles.Where(x => x.Key.Contains("GlobalTlk")))
                {
                    // Todo: Filter out languages
                    Game1GlobalTlkPackages.Add(MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, f.Key)));
                }
            }
            else
            {
                // Main TLK is Startup
                Game1GlobalTlkPackages.Add(MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "Startup_INT")));

                // Mod Tlks
                foreach (var f in loadedFiles.Where(x => x.Key.Contains("GlobalTlk")))
                {
                    // Todo: Filter out languages
                    Game1GlobalTlkPackages.Add(MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, f.Key)));
                }
            }

            foreach (var package in Game1GlobalTlkPackages)
            {
                foreach (var tlkExp in package.Exports.Where(x => x.ClassName == "BioTlk"))
                {
                    //if (tlkFile.Contains("DLC_440")) // Change if our module number changes
                    //{
                    //    var tf = new TalkFile();
                    //    tf.LoadTlkData(tlkFile);
                    //    MERTalkFiles.Add(tf);
                    //    if (tlkFile.Contains("_INT"))
                    //        MERTalkFile = tf;
                    //    var fname = Path.GetFileNameWithoutExtension(tlkFile);
                    //    loadedLanguages.Add(fname.Substring(fname.LastIndexOf("_") + 1));
                    //}
                    //else
                    //{
                    var tf = new ME1TalkFile(tlkExp);
                    LoadedOfficialTalkFiles.Add(tf);
                    loadedLanguages.Add(MELocalization.INT); // we only support INT.
                    //}
                }
            }

            var tlkPackage = MEPackageHandler.OpenMEPackageFromStream(MEREmbedded.GetEmbeddedAsset("Package", $"BlankTlkPackage.{(target.Game.IsOTGame() ? "upk" : "pcc")}"));

            MERTalkFile = new ME1TalkFile(tlkPackage.Exports.FirstOrDefault(x => x.ClassName == "BioTlk"));
#elif __GAME2__
            // Load the basegame TLKs
            var bgPath = M3Directories.GetBioGamePath(target);
            // ME2 specific - ignore ME2Randomizer TLKs, we do not want to modify those
            var tlkFiles = Directory.GetFiles(bgPath, "*.tlk", SearchOption.AllDirectories);
            foreach (var tlkFile in tlkFiles)
            {
                if (tlkFile.Contains("DLC_440")) // Change if our module number changes
                {
                    var tf = new ME2ME3TalkFile();
                    tf.LoadTlkData(tlkFile);
                    MERTalkFiles.Add(tf);
                    if (tlkFile.Contains("_INT"))
                        MERTalkFile = tf;
                    var fname = Path.GetFileNameWithoutExtension(tlkFile);
                    loadedLanguages.Add(fname.GetUnrealLocalization());
                }
                else
                {
                    var tf = new ME2ME3TalkFile();
                    tf.LoadTlkData(tlkFile);
                    LoadedOfficialTalkFiles.Add(tf);
                    var fname = Path.GetFileNameWithoutExtension(tlkFile);
                    loadedLanguages.Add(fname.GetUnrealLocalization());
                }
            }
#elif __GAME3__
            // Load the basegame TLKs
            var bgPath = M3Directories.GetBioGamePath(target);
            // ME3 specific - ignore ME3Randomizer TLKs, we do not want to modify those
            var tlkFiles = Directory.GetFiles(bgPath, "*.tlk", SearchOption.AllDirectories);
            foreach (var tlkFile in tlkFiles)
            {
                if (tlkFile.Contains("LE3Randomizer")) 
                {
                    var tf = new ME2ME3TalkFile();
                    tf.LoadTlkData(tlkFile);
                    MERTalkFiles.Add(tf);
                    if (tlkFile.Contains("_INT"))
                        MERTalkFile = tf;
                    var fname = Path.GetFileNameWithoutExtension(tlkFile);
                    loadedLanguages.Add(fname.GetUnrealLocalization());
                }
                else
                {
                    var tf = new ME2ME3TalkFile();
                    tf.LoadTlkData(tlkFile);
                    LoadedOfficialTalkFiles.Add(tf);
                    var fname = Path.GetFileNameWithoutExtension(tlkFile);
                    loadedLanguages.Add(fname.GetUnrealLocalization());
                }
            }
#endif
        }

        private void Commit()
        {
#if __GAME1__
            // Game 1 uses embedded TLKs. This method will commit the
            // the GlobalTlk for the DLC component.

#elif __GAME2__ || __GAME3__

            // Write out the TLKs
            Parallel.ForEach(MERTalkFiles, tf =>
            {
                if (tf.IsModified)
                {
                    var gsTLK = tf as ME2ME3TalkFile;
                    var hc = new LegendaryExplorerCore.TLK.ME2ME3.HuffmanCompression();
                    hc.LoadInputData(tf.StringRefs);
                    hc.SaveToFile(gsTLK.FilePath);
                }
            });
#endif

            // Free memory
            MERTalkFile = null;
            MERTalkFiles = null;
            LoadedOfficialTalkFiles = null;
            _updatedTlkStrings = null;
#if __GAME1__
            Game1GlobalTlkPackages = null;
#endif
        }

        private int GetNextID()
        {
            return NextDynamicID++;
        }

        private void InternalReplaceString(int stringid, string newText, MELocalization? localization = null)
        {
            foreach (var tf in MERTalkFiles)
            {
                // Check if this string should be replaced in this language
                if (localization != null && tf.Localization != localization) continue;
                //Debug.WriteLine($"TLK installing {stringid}: {newText}");
                lock (syncObj)
                {
                    //Debug.WriteLine($"ReplaceStr {stringid} to {newText}");
                    tf.ReplaceString(stringid, newText, true);
                }
            }
        }

        private object syncObj = new object();



        public static IEnumerable<ITalkFile> GetOfficialTLKs()
        {
            return CurrentHandler.InternalGetOfficialTLKs();
        }

        private IEnumerable<ITalkFile> InternalGetOfficialTLKs()
        {
            return LoadedOfficialTalkFiles;
        }

        public static List<ITalkFile> GetMERTLKs()
        {
            return CurrentHandler.InternalGetMERTLKs();
        }

        private List<ITalkFile> InternalGetMERTLKs()
        {
            return MERTalkFiles;
        }

        public static List<ITalkFile> GetAllTLKs()
        {
            return CurrentHandler.InternalGetAllTLKs();
        }

        private List<ITalkFile> InternalGetAllTLKs()
        {
            var items = LoadedOfficialTalkFiles.ToList();
            items.AddRange(MERTalkFiles);
            return items;
        }

        private void InternalReplaceStringByRepoint(int oldTlkId, int newTlkId)
        {
            foreach (var lang in loadedLanguages)
            {
                ReplaceString(oldTlkId, TLKLookupByLang(newTlkId, lang), lang);
            }
        }
        #endregion



        public static bool IsAssignedMERString(int descriptionPropValue)
        {
            return descriptionPropValue >= FirstDynamicID && descriptionPropValue <= CurrentHandler.NextDynamicID;
        }

        private List<int> _updatedTlkStrings;
        public static IReadOnlyList<int> UpdatedTlkStrings => CurrentHandler._updatedTlkStrings;

        public static void AddUpdatedTlk(int descriptionReference)
        {
            CurrentHandler._updatedTlkStrings.Add(descriptionReference);
        }

        public static int FindIdbyValue(string value, IMEPackage me1Package)
        {
            if (me1Package != null)
            {
                foreach (var p in me1Package.LocalTalkFiles)
                {
                    var id = p.FindIdByData(value);
                    if (id >= 0)
                    {
                        return id;
                    }
                }
            }

            foreach (var officialTlk in LoadedOfficialTalkFiles)
            {
                var id = officialTlk.FindIdByData(value);
                if (id >= 0)
                {
                    return id;
                }
            }

            return -1;
        }
    }
}
