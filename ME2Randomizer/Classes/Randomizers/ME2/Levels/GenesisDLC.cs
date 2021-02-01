using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.ME1.Unreal.UnhoodBytecode;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class GenesisDLC
    {
        public static bool PerformRandomization(RandomizationOption option)
        {
            return CompletelyRandomizeAudio();
        }

        private static bool CompletelyRandomizeAudio()
        {
            var g2LocIntF = MERFileSystem.GetSpecificFile(@"BioGame\DLC\DLC_DHME1\CookedPC\BioD_ProNor_LOC_int.pcc");
            if (g2LocIntF != null && File.Exists(g2LocIntF))
            {
                var g2LocIntP = MEPackageHandler.OpenMEPackage(g2LocIntF);

                RandomizeAudio(g2LocIntP, 3317, true);
                RandomizeAudio(g2LocIntP, 3318, false);

                MERFileSystem.SavePackage(g2LocIntP);
            }
            return false;
        }

        private static void RandomizeAudio(IMEPackage package, int topLevelUIndex, bool female)
        {
            var audioToChange = package.Exports.Where(x => x.idxLink == topLevelUIndex && x.ClassName == "WwiseStream").ToList();
            var audioSources = MERFileSystem.LoadedFiles.Keys.Where(x => x.Contains("_LOC_INT", StringComparison.InvariantCultureIgnoreCase) && x.Contains("Bio")).ToList();
            foreach (var aExp in audioToChange)
            {
                bool installed = false;
                while (!installed)
                {
                    var rAudioSourceF = audioSources.RandomElement();
                    var rAudioSourceP = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(rAudioSourceF));
                    var audioOptions = rAudioSourceP.Exports.Where(x => x.ClassName == "WwiseStream").ToList();
                    if (!audioOptions.Any())
                        continue;

                    var audioChoice = audioOptions.RandomElement();

                    // Repoint the TLK to match what's going to be said
                    var nTlk = WwiseTools.ExtractTLKIdFromExportName(audioChoice);
                    var oTlk = WwiseTools.ExtractTLKIdFromExportName(aExp);
                    if (nTlk != -1 && oTlk != -1 && !string.IsNullOrWhiteSpace(TLKHandler.TLKLookupByLang(nTlk, "INT")))
                    {
                        TLKHandler.ReplaceString(oTlk, TLKHandler.TLKLookupByLang(nTlk, "INT"));

                        WwiseTools.RepointWwiseStream(audioChoice, aExp);
                        installed = true;
                    }
                }



            }

        }
    }
}
