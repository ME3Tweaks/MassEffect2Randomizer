using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xaml.Behaviors.Media;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioAnimSetData
    {
        /// <summary>
        /// Group names of bones that can be swapped around. For example any element that contains 'Finger' can be swapped to any other element that contains 'Finger'.
        /// </summary>
        private static string[] basicBoneGroups = new[]
        {
            "ankle",
            "wrist",
            "finger",
            "toe",
            "sneer",
            "Tongue",
        };

        private static string[] advancedBoneGroups = new[]
        {
            "ankle",
            "wrist",
            "finger",
            "toe",
            "sneer",
            "jaw",
            "brow",
            "eye_l",
            "spine",
            "cheek",
            "mouth",
            "chin",
            "throat",
            "chest",
            "pelvis",
            //"hip",
            //"neck",
            //"head"
        };

        private static string[] bonegroupsToNeverRandomize = new[]
        {
            "door",
            "camera",
            "wall",
            "latch",
            "shutter",
            "roof",
            "base",
        };

        public static string UIConverter(double setting)
        {
            if (setting == BASIC_RANDOM) return "Basic bones only";
            if (setting == MODERATE_RANDOM) return "Advanced bones only";
            if (setting == HIGH_RANDOM) return "Advanced bones + 3";
            if (setting == VERY_RANDOM) return "Up to 10 random bones";
            if (setting == ALL_RANDOM) return "All bones, unplayable";
            return "Unknown setting!";
        }

        /// <summary>
        /// Basic bone groups only. Only 
        /// </summary>
        private const double BASIC_RANDOM = 1;
        /// <summary>
        /// Advanced random bones.
        /// </summary>
        private const double MODERATE_RANDOM = 2;
        /// <summary>
        /// Advanced random bones, plus 2 more.
        /// </summary>
        private const double HIGH_RANDOM = 3;
        /// <summary>
        /// Up to 15 random bones
        /// </summary>
        private const double VERY_RANDOM = 4;
        /// <summary>
        /// Shuffle the entire list of bones.
        /// </summary>
        private const double ALL_RANDOM = 5;

        // Do not randomize camera animsets as this will make game impossible to play
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioAnimSetData" && !export.ObjectName.Name.Contains("CAM", StringComparison.InvariantCultureIgnoreCase);


        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            //build groups
            //var props = export.GetProperties();
            var trackBoneNames = export.GetProperty<ArrayProperty<NameProperty>>("TrackBoneNames");
            if (trackBoneNames != null)
            {
                // Test first if we should randomize based on the option.
                //int max = option.SliderValue switch
                //{
                //    MODERATE_RANDOM => 8,
                //    HIGH_RANDOM => 4,
                //    VERY_RANDOM => 3,
                //    ALL_RANDOM => 1,
                //    _ => 10
                //};

                //if (ThreadSafeRandom.Next(max) != 0) return false; // Skip this pass!

                // We should randomize this pass
                if (option.SliderValue == ALL_RANDOM)
                {
                    // Randomize everything. This will probably entirely ruin it, but why not.
                    trackBoneNames.Shuffle();
                }
                else if (option.SliderValue == VERY_RANDOM)
                {
                    // Index => New Value at that index
                    SwapRandomBones(10, trackBoneNames);
                }
                else
                {
                    var randomizationGroups = new Dictionary<string, List<string>>();

                    List<string> shuffledItems = new List<string>(); // Items that cannot be added to the > Basic Randomizer pass.
                    var groups = basicBoneGroups;
                    if (option.SliderValue >= MODERATE_RANDOM)
                        groups = advancedBoneGroups;
                    // GROUP RANDOMIZER -------------------
                    foreach (var key in groups)
                    {
                        var keysP1 = trackBoneNames.Where(x => x.Value.Name.Contains(key, StringComparison.InvariantCultureIgnoreCase)).ToList();
                        randomizationGroups[key] = keysP1.Select(x => x.Value.Name).ToList();
                        shuffledItems.AddRange(randomizationGroups[key]);
                        randomizationGroups[key].Shuffle();
                    }

                    if (shuffledItems.Any())
                    {
                        // For every bone in the track bones...
                        foreach (var prop in trackBoneNames)
                        {
                            // Get the name of the bone
                            var propname = prop.Value.Name;

                            // Get the key of the bone to the list of keys it can map to that contain the key... (?)
                            var randoKey = randomizationGroups.Keys.FirstOrDefault(x => propname.Contains(x, StringComparison.InvariantCultureIgnoreCase));
                            //Debug.WriteLine(propname);
                            if (randoKey != null)
                            {
                                var randoKeyList = randomizationGroups[randoKey];
                                prop.Value = randoKeyList[0];
                                randoKeyList.RemoveAt(0);
                            }
                        } // ----------------------------------
                    }

                    // > BASIC RANDOMIZER
                    int numAdditionalToSwap = option.SliderValue switch
                    {
                        HIGH_RANDOM => 3,
                        _ => 0
                        // The other cases are handled above in a diff if statement block
                    };
                    SwapRandomBones(numAdditionalToSwap, trackBoneNames);
                }

                export.WriteProperty(trackBoneNames);
                return true;
            }

            return false;
        }

        private static void SwapRandomBones(int numToSwap, ArrayProperty<NameProperty> trackBoneNames)
        {
            if (trackBoneNames.Count < 2) return; // There is nothing to swap

            int i = 0;
            while (i < numToSwap)
            {
                var index1 = trackBoneNames.RandomIndex();
                var bn = trackBoneNames[index1];

                int numTries = 10;
                while (numTries > 0 && bonegroupsToNeverRandomize.Any(x => x.Contains(bn.Value, StringComparison.InvariantCultureIgnoreCase)))
                {
                    index1 = trackBoneNames.RandomIndex();
                    bn = trackBoneNames[index1];
                    numTries--;
                }

                if (numTries < 0)
                    return; // Don't try, we could not find a bone to swap to

                int index2 = index1;
                while (numTries > 0 && (index2 == index1 || bonegroupsToNeverRandomize.Any(x => x.Contains(bn.Value, StringComparison.InvariantCultureIgnoreCase))))
                {
                    index2 = trackBoneNames.RandomIndex();
                    bn = trackBoneNames[index2];
                    numTries--;
                }

                if (numTries < 0)
                    return; // Don't try, we could not find a bone to swap to

                // Swap em'
                var item1 = trackBoneNames[index1];
                trackBoneNames[index1] = trackBoneNames[index2];
                trackBoneNames[index2] = item1;
                i++;
            }
        }
    }
}
