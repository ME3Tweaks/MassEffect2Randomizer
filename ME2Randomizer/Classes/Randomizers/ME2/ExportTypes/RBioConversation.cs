using System.Collections.Generic;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.Dialogue;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioConversation
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioConversation";

        public static bool RandomizeExport(ExportEntry export,RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;

            var conv = new ConversationExtended(export);
            conv.LoadConversation(TLKHandler.TLKLookup);


            // SHUFFLE THE NODES THE REPLIES CONNECT TO
            foreach (var node in conv.EntryList)
            {
                // Shuffles camera intimacy
                var cameraIntimacy = node.NodeProp.GetProp<IntProperty>("nCameraIntimacy");
                if (cameraIntimacy != null)
                {
                    cameraIntimacy.Value = ThreadSafeRandom.Next(5); //Not sure what the range of values can be
                }

                var replyNodeDetails = node.NodeProp.GetProp<ArrayProperty<StructProperty>>("ReplyListNew");
                if (replyNodeDetails != null)
                {
                    List<IntProperty> replyNodeIndices = new List<IntProperty>();
                    foreach (var bdrld in replyNodeDetails)
                    {
                        replyNodeIndices.Add(bdrld.GetProp<IntProperty>("nIndex"));
                    }

                    replyNodeIndices.Shuffle();

                    foreach (var bdrld in replyNodeDetails)
                    {
                        bdrld.Properties.AddOrReplaceProp(replyNodeIndices[0]);
                        replyNodeIndices.RemoveAt(0);
                    }
                }
            }

            conv.SerializeNodes(true);
            return true;
        }
    }
}