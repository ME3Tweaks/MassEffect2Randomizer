using System;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.TLK;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.Levels
{
    class GenesisDLC
    {
        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            return CompletelyRandomizeAudio(target);
        }

        private static bool CompletelyRandomizeAudio(GameTarget target)
        {
            var g2LocIntF = MERFileSystem.GetSpecificFile(target, @"BioGame\DLC\DLC_DHME1\CookedPC\BioD_ProNor_LOC_int.pcc");
            if (g2LocIntF != null && File.Exists(g2LocIntF))
            {
                var g2LocIntP = MEPackageHandler.OpenMEPackage(g2LocIntF);

                RandomizeAudio(target, g2LocIntP, 3317, true);
                RandomizeAudio(target, g2LocIntP, 3318, false);

                MERFileSystem.SavePackage(g2LocIntP);
            }
            return false;
        }

        private static void RandomizeAudio(GameTarget target, IMEPackage package, int topLevelUIndex, bool female)
        {
            var audioToChange = package.Exports.Where(x => x.idxLink == topLevelUIndex && x.ClassName == "WwiseStream").ToList();
            var audioSources = MERFileSystem.LoadedFiles.Keys.Where(x => x.Contains("_LOC_INT", StringComparison.InvariantCultureIgnoreCase) && x.Contains("Bio")).ToList();
            foreach (var aExp in audioToChange)
            {
                bool installed = false;
                while (!installed)
                {
                    var rAudioSourceF = audioSources.RandomElement();
                    var rAudioSourceP = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(target, rAudioSourceF));
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
