using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game2.Levels
{
    class Romance
    {
        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            RandomizeRomance(target);
            return true;
        }


        /// <summary>
        /// Technically this is not part of Nor (It's EndGm). But it takes place on normandy so users
        /// will think it is part of the normandy.
        /// </summary>
        /// <param name="random"></param>
        private static void RandomizeRomance(GameTarget target)
        {

            // Romance is 2 pass: 

            // Pass 1: The initial chances that are not ME1 or Miranda
            {
                var romChooserPackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_EndGm1_110Romance.pcc"));
                var romSeq = romChooserPackage.GetUExport(88);
                var outToRepoint = romChooserPackage.GetUExport(65); //repoint to our switch

                // Install random switch and point it at the romance log culminations for each
                // Miranda gets 2 as she has a 50/50 of miranda or lonely shep.
                var randomSwitch = MERSeqTools.InstallRandomSwitchIntoSequence(target, romSeq, 7);
                var outLinks = SeqTools.GetOutboundLinksOfNode(randomSwitch);

                outLinks[0].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(28) }); // JACOB
                outLinks[1].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(20) }); // GARRUS
                outLinks[2].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(30) }); // TALI
                outLinks[3].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(29) }); // THANE
                outLinks[4].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(27) }); // JACK
                outLinks[5].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(52) }); // MIRANDA--| -> Delay into teleport
                outLinks[6].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(52) }); // ME1------| -> Delay into teleport

                SeqTools.WriteOutboundLinksToNode(randomSwitch, outLinks);

                // Repoint to our randomswitch
                var penultimateOutbound = SeqTools.GetOutboundLinksOfNode(outToRepoint);
                penultimateOutbound[0].Clear();
                penultimateOutbound[0].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = randomSwitch });
 
                // DEBUG ONLY: FORCE LINK
                //penultimateOutbound[0].Add(new SeqTools.OutboundLink() { InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(27) });
                SeqTools.WriteOutboundLinksToNode(outToRepoint, penultimateOutbound);

                MERFileSystem.SavePackage(romChooserPackage);
            }

            // Pass 2: ME1 or Miranda if Pass 1 fell through at runtime
            {
                var romChooserPackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_EndGm1_110ROMMirranda.pcc"));
                var romSeq = romChooserPackage.GetUExport(8468);
                var outToRepoint = romChooserPackage.GetUExport(2354); //repoint to our switch

                // Install random switch and point it at the romance log culminations for each
                // Miranda gets 2 as she has a 50/50 of miranda or lonely shep.
                var randomSwitch = MERSeqTools.InstallRandomSwitchIntoSequence(target, romSeq, 2);
                var outLinks = SeqTools.GetOutboundLinksOfNode(randomSwitch);

                outLinks[0].Add(new SeqTools.OutboundLink() {InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(8432)}); // MIRANDA
                outLinks[1].Add(new SeqTools.OutboundLink() {InputLinkIdx = 0, LinkedOp = romChooserPackage.GetUExport(8413)}); // ME1

                SeqTools.WriteOutboundLinksToNode(randomSwitch, outLinks);

                // Repoint to our randomswitch
                var penultimateOutbound = SeqTools.GetOutboundLinksOfNode(outToRepoint);
                penultimateOutbound[0].Clear();
                penultimateOutbound[0].Add(new SeqTools.OutboundLink() {InputLinkIdx = 0, LinkedOp = randomSwitch});
                SeqTools.WriteOutboundLinksToNode(outToRepoint, penultimateOutbound);

                MERFileSystem.SavePackage(romChooserPackage);
            }
        }
    }
}
