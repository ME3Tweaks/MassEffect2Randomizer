using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Coalesced.Xml;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using Randomizer.MER;
using Randomizer.Randomizers.Shared.Classes;

namespace Randomizer.Randomizers.Handlers
{
    class CoalescedHandler
    {
        #region Static Access
        private static CoalescedHandler CurrentHandler { get; set; }
        private SortedDictionary<string, CoalesceAsset> IniFiles { get; set; }
        /// <summary>
        /// Starts up the Coalesced.ini subsystem. These methods should not be across multiple threads as they are not thread safe!
        /// </summary>
        /// <param name="usingDLCSystem"></param>
        public static void StartHandler()
        {
            CurrentHandler = new CoalescedHandler();
            CurrentHandler.Start();
        }

        public static CoalesceAsset GetIniFile(string filename)
        {
            return CurrentHandler.GetFile(Path.GetFileNameWithoutExtension(filename));
        }

        public static void EndHandler()
        {
            // Commit
            CurrentHandler?.Commit();
            CurrentHandler = null;
        }

        #endregion

        #region Private members
        private void Start()
        {
            IniFiles = new SortedDictionary<string, CoalesceAsset>();

#if __GAME2__
            // Load BioEngine.ini as it already exists.
            IniFiles["BIOEngine.ini"] = ConfigFileProxy.LoadIni(Path.Combine(MERFileSystem.DLCModCookedPath, @"BIOEngine.ini"));
#elif __GAME3__
            // Load the Coalesced file
            using var cf = File.OpenRead(Path.Combine(MERFileSystem.DLCModCookedPath, @"Default_DLC_MOD_LE3Randomizer.bin"));
            var decompiled = CoalescedConverter.DecompileGame3ToMemory(cf);
            foreach (var f in decompiled)
            {
                IniFiles[f.Key] = XmlCoalesceAsset.LoadFromMemory(f.Value);
            }
#endif
        }

        private CoalesceAsset GetFile(string filename)
        {
#if __GAME3__
            var ext = ".xml";
#else
            var ext = ".ini";
#endif
            if (!IniFiles.TryGetValue(filename + ext, out var dupIni))
            {
                dupIni = new CoalesceAsset(filename);
                IniFiles[filename] = dupIni;
            }

            return dupIni;
        }


        private void Commit()
        {
#if __GAME2__
            foreach (var ini in IniFiles)
            {
                // Write it out to disk. Might need to check BOM
                File.WriteAllText(Path.Combine(MERFileSystem.DLCModCookedPath, ini.Key), ini.Value.ToString());
            }
#elif __GAME3__
            foreach (var f in IniFiles)
            {
                var assetTexts = new Dictionary<string, string>();
                foreach (var asset in IniFiles)
                {
                    assetTexts[asset.Key] = asset.Value.ToXmlString();
                }

                var outBin = CoalescedConverter.CompileFromMemory(assetTexts);
                outBin.WriteToFile(Path.Combine(MERFileSystem.DLCModCookedPath, @"Default_DLC_MOD_LE3Randomizer.bin"));
            }
#endif
        }
        #endregion

        public static void AddDynamicLoadMappingEntries(IEnumerable<CoalesceValue> mappings)
        {
            var engine = CoalescedHandler.GetIniFile("BioEngine");
            var sfxengine = engine.GetOrAddSection("sfxgame.sfxengine");
            sfxengine.AddEntry(new CoalesceProperty("dynamicloadmapping", mappings.ToList()));
        }

        public static void AddDynamicLoadMappingEntry(SeekFreeInfo mapping)
        {
            var engine = CoalescedHandler.GetIniFile("BioEngine");
            var sfxengine = engine.GetOrAddSection("sfxgame.sfxengine");
            sfxengine.AddEntry(new CoalesceProperty("dynamicloadmapping", new CoalesceValue(mapping.GetSeekFreeStructText())));
        }
    }
}
