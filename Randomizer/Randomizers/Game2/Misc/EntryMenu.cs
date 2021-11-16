using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace Randomizer.Randomizers.Game2.Misc
{
    public class EntryMenu
    {

        public static bool SetupFastStartup(RandomizationOption option)
        {
            var entrymenuF = MERFileSystem.GetPackageFile("EntryMenu.pcc");
            var entrymenuP = MEPackageHandler.OpenMEPackage(entrymenuF);
            var skipElem = entrymenuP.GetUExport(86); // should show splash
            if (!skipElem.ObjectFlags.HasFlag(UnrealFlags.EObjectFlags.DebugPostLoad))
            {
                skipElem.ObjectFlags |= UnrealFlags.EObjectFlags.DebugPostLoad; // mark as modified so subsequent passes don't operate on this
                SeqTools.SkipSequenceElement(skipElem, outboundLinkIdx: 1);
                MERFileSystem.SavePackage(entrymenuP);
            }

            
            return true;
        }
    }
}
