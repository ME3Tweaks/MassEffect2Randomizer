using System;
using System.Diagnostics;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal.BinaryConverters;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    class RMorphTarget
    {
        public static void ResetClass()
        {
            ShiftDirectionGroups.Clear();
        }
        private static bool CanRandomize(ExportEntry export, out int shiftDirection, out float min, out float max)
        {
            min = -7;
            max = 7;
            shiftDirection = -1;
            if (export.IsDefaultObject || export.ClassName != "MorphTarget") return false;

            // Check and setup shift directions.
            var name = export.ObjectName.Name;
            if (name.Contains("eye"))
            {
                shiftDirection = SetupShiftDir("eye");
                min = -1;
                max = 5;
                return true;
            }
            if (name.Contains("jaw"))
            {
                shiftDirection = SetupShiftDir("jaw");
                if (shiftDirection == 0)
                {
                    // in/out
                    min = -5f;
                    max = 20;
                }
                else
                {
                    // up down left right
                    min = -1f;
                    max = 20f;
                }
                return true;
            }
            if (name.Contains("mouth"))
            {
                shiftDirection = SetupShiftDir("mouth");
                if (shiftDirection == 0)
                {
                    // in/out
                    min = -5f;
                    max = 20;
                }
                else
                {
                    // up down left right
                    min = -5f;
                    max = 5f;
                }
                return true;
            }
            if (name.Contains("nose"))
            {
                shiftDirection = SetupShiftDir("nose");
                if (shiftDirection == 0)
                {
                    // in/out
                    min = -15f;
                    max = 15;
                }
                else
                {
                    // up down
                    min = -2f;
                    max = 10f;
                }

                return true;
            }

            // Leave disabled. Doesn't seem to do anything useful
            //if (name.Contains("teeth"))
            //{
            //    shiftDirection = SetupShiftDir("teeth");
            //    min = -5;
            //    max = 5;
            //    return true;
            //}
            Debug.WriteLine($"Ignored thing {name}");
            return false;
        }

        private static int SetupShiftDir(string groupName)
        {
            if (!ShiftDirectionGroups.TryGetValue(groupName, out var sd))
            {
                if (groupName == "nose")
                {
                    // X = 0 = in/out
                    // Y = 1 = left/right
                    // Z = 2 = up/down
                    sd = ThreadSafeRandom.Next(2) == 0 ? 0 : 2;
                }
                else
                {
                    sd = ThreadSafeRandom.Next(3);
                }
                ShiftDirectionGroups[groupName] = sd;
            }

            return sd;
        }

        private static CaseInsensitiveDictionary<int> ShiftDirectionGroups = new CaseInsensitiveDictionary<int>();

        public static bool RandomizeExport(ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp, out var shiftDirection, out var min, out var max)) return false;
            var shiftAmt = ThreadSafeRandom.NextFloat(min, max);
            var morphTarget = ObjectBinary.From<MorphTarget>(exp);

            foreach (var lm in morphTarget.MorphLODModels)
            {
                foreach (var v in lm.Vertices)
                {
                    var posDelta = v.PositionDelta;
                    if (shiftDirection == 0) // IN OUT
                                             //posDelta.X += shiftAmt;
                        posDelta.X *= shiftAmt;
                    if (shiftDirection == 1) // LEFT RIGHT
                                             //posDelta.Y += shiftAmt;
                        posDelta.Y *= shiftAmt;
                    if (shiftDirection == 2) // UP DOWN
                                             //posDelta.Z += shiftAmt;
                        posDelta.Z *= shiftAmt;
                    v.PositionDelta = posDelta; // Require reassignment
                }
            }


            exp.WriteBinary(morphTarget);
            return true;
        }

        /// <summary>
        /// Same as RandomizeExport but will not run on BioP_Char file or the player file
        /// </summary>
        /// <param name="export"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool RandomizeGlobalExport(ExportEntry export, RandomizationOption option)
        {
            // Extra Jacked Up Randomizer
            // Doesn't seem to reliably work...
            if (export.FileRef.FilePath.EndsWith("BioP_Char.pcc", StringComparison.InvariantCultureIgnoreCase)) return false;
            if (export.FileRef.FilePath.Contains("Player", StringComparison.InvariantCultureIgnoreCase)) return false;
            return RandomizeExport(export, option);
        }
    }
}