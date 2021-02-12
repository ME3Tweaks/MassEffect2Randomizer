using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RMorphTarget
    {
        private static bool CanRandomize(ExportEntry export, out int shiftDirection)
        {
            shiftDirection = -1;
            if (export.IsDefaultObject || export.ClassName != "MorphTarget") return false;

            // Check and setup shift directions.
            var name = export.ObjectName.Name;
            if (name.Contains("eye"))
            {
                shiftDirection = SetupShiftDir("eye");
                return true;
            }
            if (name.Contains("jaw"))
            {
                shiftDirection = SetupShiftDir("jaw");
                return true;
            }
            if (name.Contains("mouth"))
            {
                shiftDirection = SetupShiftDir("mouth");
                return true;
            }
            if (name.Contains("nose"))
            {
                shiftDirection = SetupShiftDir("nose");
                return true;
            }
            if (name.Contains("teeth"))
            {
                shiftDirection = SetupShiftDir("teeth");
                return true;
            }

            return false;
        }

        private static int SetupShiftDir(string groupName)
        {
            if (!ShiftDirectionGroups.TryGetValue(groupName, out var sd))
            {
                sd = ThreadSafeRandom.Next(3);
                ShiftDirectionGroups[groupName] = sd;
            }

            return sd;
        }

        private static CaseInsensitiveDictionary<int> ShiftDirectionGroups = new CaseInsensitiveDictionary<int>();

        public static bool RandomizeExport(ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp, out var shiftDirection)) return false;
            var shiftAmt = ThreadSafeRandom.NextFloat(-10, 10);
            var morphTarget = ObjectBinary.From<MorphTarget>(exp);

            foreach (var lm in morphTarget.MorphLODModels)
            {
                foreach (var v in lm.Vertices)
                {
                    var posDelta = v.PositionDelta;
                    if (shiftDirection == 0)
                        posDelta.X *= shiftAmt;
                    if (shiftDirection == 1)
                        posDelta.Y *= shiftAmt;
                    if (shiftDirection == 2)
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