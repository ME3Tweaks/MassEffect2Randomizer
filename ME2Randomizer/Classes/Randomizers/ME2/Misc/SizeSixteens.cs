using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class SizeSixteens
    {
        /// <summary>
        /// People who survived ME1's onslaught of SizeSixteens' stream
        /// </summary>
        private static string[] SizeSixteenChatMembers = new[]
        {
            // DIED IN ME1
            //"Red Falcon",
            //"Rocket Boy",
            //"John Doe",
            //"Red Line"

            // Survivors from ME1
            "Nalie Walie",
            "Jed Ted",
            "Mok",
            "Shamrock Snipes",
            "Steeler Wayne",
            "Castle Arrrgh",
            "Bev",
            "Lurxx",
            "Chirra Kitteh",
            "Daynan",

            // New people in game
            "Admiral Kirk",
            "Darth Krytie",
            "Peeress Sabine",
            "DNC 510",
            "Horny Heracross",
            "Thorg",
            "Invisible Guardian",
            "Nox"
        };

        private static List<string> AvailableMembers;

        public static void ResetClass()
        {
            AvailableMembers = SizeSixteenChatMembers.ToList();
            AvailableMembers.Shuffle();
        }

        private static string GetMember()
        {
            return AvailableMembers.PullFirstItem();
        }

        public static bool InstallSSChanges(RandomizationOption option)
        {
            // Freedoms progress
            SetVeetorFootage();

            return true;
        }


        private static void SetVeetorFootage()
        {
            var moviedata = RTextureMovie.GetTextureMovieAssetBinary("Veetor.size_mer.bik");
            var veetorFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioD_ProFre_501Veetor")).ToList();
            foreach (var v in veetorFiles)
            {
                MERLog.Information($@"Setting veetor footage in {v}");
                var mpackage = MERFileSystem.GetPackageFile(v);
                var package = MEPackageHandler.OpenMEPackage(mpackage);
                var veetorExport = package.FindExport("BioVFX_Env.Hologram.ProFre_501_VeetorFootage");
                if (veetorExport != null)
                {
                    RTextureMovie.RandomizeExportDirect(veetorExport, null, moviedata);
                }
                MERFileSystem.SavePackage(package);
            }
        }
    }
}
