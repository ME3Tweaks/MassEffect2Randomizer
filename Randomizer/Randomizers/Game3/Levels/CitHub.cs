using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Levels
{
    /// <summary>
    /// Randomize for the CitHub series of levels
    /// </summary>
    internal class CitHub
    {
        private static LEXOpenable[] VILineOptions = new[]
        {
            #region LOUD SCREAMING
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "Wwise_Weapons_P_Spitfire.Exertion_Falling_Example_01_wav",
                FilePath = "BioD_Cit001_300CarLot.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "Wwise_Weapons_P_Spitfire.Exertion_Falling_Example_02_wav",
                FilePath = "BioD_Cit001_300CarLot.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "Wwise_Weapons_P_Spitfire.Exertion_Falling_Example_03_wav",
                FilePath = "BioD_Cit001_300CarLot.pcc"
            },
            #endregion
            #region STATIC
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "proear_crashedchopper_v_d.Audio.Int.en-us,global_anderson,proear_crashedchopper_v,00599866_m_wav",
                FilePath = "BioD_ProEar_420Radio_LOC_INT.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "proear_crashedchopper_v_d.Audio.Int.en-us,hench_kaidan,proear_crashedchopper_v,00589316_m_wav",
                FilePath = "BioD_ProEar_420Radio_LOC_INT.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "proear_crashedchopper_v_d.Audio.Int.en-us,hench_ashley,proear_crashedchopper_v,00589318_m_wav",
                FilePath = "BioD_ProEar_420Radio_LOC_INT.pcc"
            },
            #endregion
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "promar_tram_chat_v_D.Audio.Int.en-us,promar_cerberus_second,promar_tram_chat_v,00696923_m_wav",
                FilePath = "BioD_ProMar_530Gondolas_LOC_INT.pcc"
            },
        };

        public static bool RandomizeLevel(GameTarget target, RandomizationOption option)
        {
            RandomizeVIAudioLines(target, option);
            return true;
        }

        private static void RandomizeVIAudioLines(GameTarget target, RandomizationOption option)
        {
            var underBellyF = MERFileSystem.GetPackageFile(target, @"BioD_CitHub_Underbelly_LOC_INT.pcc");
            var underBellyP = MEPackageHandler.OpenMEPackage(underBellyF);

            MERPackageCache sourceCache = new MERPackageCache(target, null, false);
            foreach (var v in underBellyP.Exports.Where(x => x.ClassName == "WwiseStream" && x.ObjectName.Name.Contains("citwrd_shepard_vi")))
            {
                // Todo: Filter to only VI
                Debug.WriteLine($"Randoming audio: {v.InstancedFullPath}");
                var randomLine = VILineOptions.RandomElement();
                var sourcePackage = sourceCache.GetCachedPackage(MERFileSystem.GetPackageFile(target, randomLine.FilePath));
                WwiseTools.RepointWwiseStream(sourcePackage.FindExport(randomLine.EntryPath), v);
            }
            MERFileSystem.SavePackage(underBellyP);
        }
    }
}
