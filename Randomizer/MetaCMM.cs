using System;
using System.Collections.Generic;
using System.IO;

namespace Randomizer
{
    /// <summary>
    /// Class that represents data in _metacmm.txt files - files that describe the installed mod
    /// </summary>
    public class MetaCMM
    {
        #region Info Prefixes
        public static readonly string PrefixOptionsSelectedOnInstall = @"[INSTALLOPTIONS]";
        public static readonly string PrefixIncompatibleDLC = @"[INCOMPATIBLEDLC]";
        #endregion

        public string ModName { get; set; }
        public string Version { get; set; }
        public string InstalledBy { get; set; }
        public string InstallerInstanceGUID { get; set; }
        public ObservableCollectionExtended<string> IncompatibleDLC { get; } = new ObservableCollectionExtended<string>();
        public ObservableCollectionExtended<string> OptionsSelectedAtInstallTime { get; } = new ObservableCollectionExtended<string>();

        public MetaCMM()
        {

        }

        public MetaCMM(string metaFile)
        {
            var lines = File.ReadAllLines(metaFile);
            int i = 0;
            foreach (var line in lines)
            {
                switch (i)
                {
                    case 0:
                        ModName = line;
                        break;
                    case 1:
                        Version = line;
                        break;
                    case 2:
                        InstalledBy = line;
                        break;
                    case 3:
                        InstallerInstanceGUID = line;
                        break;
                    default:
                        // MetaCMM Extended
                        if (line.StartsWith(PrefixOptionsSelectedOnInstall))
                        {
                            var parsedline = line.Substring(PrefixOptionsSelectedOnInstall.Length);
                            OptionsSelectedAtInstallTime.ReplaceAll(StringStructParser.GetSemicolonSplitList(parsedline));
                        }
                        else if (line.StartsWith(PrefixIncompatibleDLC))
                        {
                            var parsedline = line.Substring(PrefixIncompatibleDLC.Length);
                            IncompatibleDLC.ReplaceAll(StringStructParser.GetSemicolonSplitList(parsedline));
                        }
                        break;
                }
                i++;
            }
        }

        /// <summary>
        /// Converts this object to the text on disk
        /// </summary>
        /// <returns></returns>
        public string ToCMMText()
        {
            var metaOutLines = new List<string>();

            // Write out MetaCMM Classic
            metaOutLines.Add(ModName);
            metaOutLines.Add(Version);
            metaOutLines.Add(InstalledBy);
            metaOutLines.Add(Guid.NewGuid().ToString()); // This is not used in Mod Manager 6

            // Write MetaCMM Extended
            if (IncompatibleDLC.Any())
            {
                metaOutLines.Add($@"{MetaCMM.PrefixIncompatibleDLC}{string.Join(';', IncompatibleDLC)}");
            }

            if (OptionsSelectedAtInstallTime.Any())
            {
                // I hope this covers all cases. Mods targeting moddesc 6 or lower don't need friendlyname or description, but virtually all of them did
                // as MM4/5 autonaming was ugly
                metaOutLines.Add($@"{MetaCMM.PrefixOptionsSelectedOnInstall}{string.Join(';', OptionsSelectedAtInstallTime)}");
            }

            return string.Join('\n', metaOutLines);
        }
    }
}
