using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Microsoft.Win32;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Misc
{
    class RCharacterNames
    {
        /// <summary>
        /// Static list that is not modified beyond loading
        /// </summary>
        private static List<string> PawnNames { get; } = new List<string>();
        /// <summary>
        /// List actually used to pull from
        /// </summary>
        private static List<string> PawnNameListInstanced { get; } = new List<string>();

        /// <summary>
        /// Allows loading a list of names for pawns
        /// </summary>
        public static void SetupRandomizer(RandomizationOption option)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Title = "Select text file with list of names, one per line",
                Filter = "Text files|*.txt",
            };
            var result = ofd.ShowDialog();
            if (result.HasValue && result.Value)
            {
                try
                {
                    PawnNames.ReplaceAll(File.ReadAllLines(ofd.FileName));
                    option.Description = $"{PawnNames.Count} name(s) loaded for randomization";
                }
                catch (Exception e)
                {
                    MERLog.Exception(e, "Error reading names for CharacterNames randomizer");
                }
            }
        }

        public static bool InstallNameSet(GameTarget target, RandomizationOption option)
        {
            // Setup
            PawnNameListInstanced.ReplaceAll(PawnNames);
            if (option.SliderValue > 1)
            {
                for (int i = 1; i < option.SliderValue; i++)
                {
                    // Layer it up
                    PawnNameListInstanced.AddRange(PawnNames);
                }
            }

            PawnNameListInstanced.Shuffle();

            var pawnStringIds = MEREmbedded.GetEmbeddedTextAsset("namerefs.txt").Split("\n");
            pawnStringIds.Shuffle();
            
            foreach (var strId in pawnStringIds)
            {
                if (!PawnNameListInstanced.Any())
                    break;
                // The string ids file has a : to make it easier to go back and fix stuff
                var id = int.Parse(strId.Substring(0, strId.IndexOf(':')));
                TLKBuilder.ReplaceString(id, PawnNameListInstanced.PullFirstItem());
            }
            return true;
        }

        private static void FixFirstSurvivorNameBchLmL(GameTarget target)
        {

            // Make it so the beating scene shows names
            var beachPathF = MERFileSystem.GetPackageFile(target, "BioD_BchLmL_102BeachFight.pcc");
            if (beachPathF != null)
            {
                var beachPathP = MEPackageHandler.OpenMEPackage(beachPathF);

                // Make memory unique
                var tlkId = InstallName();
                if (tlkId != 0)
                {
                    beachPathP.GetUExport(700).WriteProperty(new StringRefProperty(tlkId, "ActorGameNameStrRef"));
                    beachPathP.GetUExport(700).ObjectName = "survivor_female_MER";
                    MERFileSystem.SavePackage(beachPathP);
                }
            }
        }

        private static int count = 0;

        private static int InstallName(int stringId = 0)
        {
            count++;
            if (PawnNameListInstanced.Any())
            {
                var newPawnName = PawnNameListInstanced.PullFirstItem();
                if (stringId == 0)
                    stringId = TLKBuilder.GetNewTLKID();
                TLKBuilder.ReplaceString(stringId, newPawnName);
                return stringId;
            }

            return 0; // Not changed
        }

    }
}
