using System.Collections.Generic;
using System.IO;
using ME2Randomizer.Classes.gameini;
using DuplicatingIni = ME3ExplorerCore.Misc.DuplicatingIni;

namespace ME2Randomizer.Classes.Randomizers.ME2.Coalesced
{
    class CoalescedHandler
    {
        #region Static Access
        private static CoalescedHandler CurrentHandler { get; set; }

        private SortedDictionary<string, DuplicatingIni> IniFiles { get; set; }
        /// <summary>
        /// Starts up the Coalesced.ini subsystem. These methods should not be across multiple threads as they are not thread safe!
        /// </summary>
        /// <param name="usingDLCSystem"></param>
        public static void StartHandler(bool usingDLCSystem)
        {
            CurrentHandler = new CoalescedHandler()
            {
                UsingDLCSystem = usingDLCSystem
            };

            CurrentHandler.Start();
        }

        public static DuplicatingIni GetIniFile(string filename)
        {
            return CurrentHandler.GetFile(filename);
        }

        public static void EndHandler()
        {
            // Commit
            CurrentHandler.Commit();
            CurrentHandler = null;
        }

        #endregion

        #region Private members
        /// <summary>
        /// If the subsystem uses the DLC system or the base Coalesced.ini file
        /// </summary>
        private bool UsingDLCSystem { get; set; }

        private void Start()
        {
            if (!UsingDLCSystem)
            {
                // load Coalesced.int
                ME2Coalesced c = new ME2Coalesced(MERFileSystem.GetSpecificFile(@"BioGame\Config\PC\Cooked\Coalesced.ini"));
                IniFiles = c.Inis;
            }
            else
            {
                IniFiles = new SortedDictionary<string, DuplicatingIni>();

                // Load BioEngine.ini as it already exists.
                IniFiles["BIOEngine.ini"] = DuplicatingIni.LoadIni(Path.Combine(MERFileSystem.DLCModCookedPath, @"BIOEngine.ini"));
            }
        }

        private DuplicatingIni GetFile(string filename)
        {
            if (!IniFiles.TryGetValue(filename, out var dupIni))
            {
                dupIni = new DuplicatingIni();
                IniFiles[filename] = dupIni;
            }

            return dupIni;
        }


        private void Commit()
        {
            if (UsingDLCSystem)
            {
                foreach (var ini in IniFiles)
                {
                    // Write it out to disk. Might need to check BOM
                    File.WriteAllText(Path.Combine(MERFileSystem.DLCModCookedPath, ini.Key), ini.Value.ToString());
                }
            }
            else
            {
                ME2Coalesced c = new ME2Coalesced()
                {
                    Inis = IniFiles
                };
                c.Serialize(MERFileSystem.GetSpecificFile(@"BioGame\Config\PC\Cooked\Coalesced.ini"));
            }
        }
        #endregion
    }
}
