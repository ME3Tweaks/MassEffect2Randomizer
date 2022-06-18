using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Shared;
using WinCopies.Util;
using System.Linq;
using LegendaryExplorerCore.ME1.Unreal.UnhoodBytecode;

namespace Randomizer.Randomizers.Game3.Levels
{
    class Romance
    {
        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            return RandomizeRomance(target);
        }


        /// <summary>
        /// Technically this is not part of Nor (It's EndGm). But it takes place on normandy so users
        /// will think it is part of the normandy.
        /// </summary>
        /// <param name="random"></param>
        private static bool RandomizeRomance(GameTarget target)
        {
            var romChooserPackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_Nor_203CIC.pcc"));
            var romSeq = romChooserPackage.Exports.FirstOrDefault(x => x.ObjectName.Name == "Cat004_Start");
            if (romSeq == null)
                return false; // Could not find Romance sequence!

            var kismetObjects = KismetHelper.GetSequenceObjects(romSeq).OfType<ExportEntry>().ToList();
            var outToRepoint = MERSeqTools.FindSequenceObjectByClassAndPosition(kismetObjects, "BioSeqAct_PlayLoadingMovie");

            // Install random switch and point it at the romance log culminations for each
            // Miranda gets 2 as she has a 50/50 of miranda or lonely shep.
            var randomSwitch = MERSeqTools.InstallRandomSwitchIntoSequence(target, romSeq, 8);
            var outLinks = SeqTools.GetOutboundLinksOfNode(randomSwitch);

            var outOptions = kismetObjects.Where(x => x.ClassName == "BioSeqAct_SetStreamingState").ToList();
            for (int i = 0; i < outLinks.Count; i++)
            {
                outLinks[i].Add(new SeqTools.OutboundLink()
                {
                    InputLinkIdx = 0,
                    LinkedOp = outOptions[i]
                });
            }

            SeqTools.WriteOutboundLinksToNode(randomSwitch, outLinks);

            // Repoint to our randomswitch
            var playMovieOutbound = SeqTools.GetOutboundLinksOfNode(outToRepoint);
            playMovieOutbound[0].Clear();
            playMovieOutbound[0].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = randomSwitch });

            // DEBUG ONLY: FORCE LINK
            //penultimateOutbound[0].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(27) });
            SeqTools.WriteOutboundLinksToNode(outToRepoint, playMovieOutbound);

            MERFileSystem.SavePackage(romChooserPackage);
            return true;
        }
    }
}
