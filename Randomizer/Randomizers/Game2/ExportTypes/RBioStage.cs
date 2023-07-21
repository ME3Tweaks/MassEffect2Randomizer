using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    internal class RBioStage
    {
        public static bool CanRandomize(ExportEntry exp)
        {
            return !exp.IsDefaultObject && exp.ClassName == "BioStage";
        }


        public static bool RandomizeBioStage(GameTarget gameTarget, ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;

            var mesh = exp.GetProperty<ObjectProperty>("Mesh")?.ResolveToExport(exp.FileRef);
            if (mesh == null) return false;
            var skelMesh = mesh.GetProperty<ObjectProperty>("SkeletalMesh")?.ResolveToExport(exp.FileRef);
            if (skelMesh == null) return false;

            var stageMesh = ObjectBinary.From<SkeletalMesh>(skelMesh);
            List<int> allowedIndicesToSwap = new List<int>();
            List<NameReference> swapNames = new List<NameReference>();

            foreach (var v in stageMesh.NameIndexMap)
            {
                if (v.Key.Instanced.Contains("cam", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                allowedIndicesToSwap.Add(v.Value);
                swapNames.Add(v.Key);
            }

            allowedIndicesToSwap.Shuffle();
            swapNames.Shuffle();
            while (swapNames.Any())
            {
                var swapName = swapNames.PullFirstItem();
                stageMesh.NameIndexMap[swapName] = allowedIndicesToSwap.PullFirstItem();
            }

            skelMesh.WriteBinary(stageMesh);
            return true;
        }

    }
}
