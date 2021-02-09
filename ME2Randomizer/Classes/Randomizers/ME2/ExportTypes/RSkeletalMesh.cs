using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RSkeletalMesh
    {

        public static void FuzzSkeleton(ExportEntry export, RandomizationOption option)
        {
            // Goofs up the RefSkeleton values
            var objBin = ObjectBinary.From<SkeletalMesh>(export);
            foreach (var bone in objBin.RefSkeleton)
            {
                if (!bone.Name.Name.Contains("eye", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("sneer", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("nose", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("brow", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("jaw", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                    var v3 = bone.Position;
                v3.X *= ThreadSafeRandom.NextFloat(0.1, 1.9);
                v3.Y *= ThreadSafeRandom.NextFloat(0.1, 1.9);
                v3.Z *= ThreadSafeRandom.NextFloat(0.1, 1.9);
                bone.Position = v3;
            }


            export.WriteBinary(objBin);
        }
    }
}
