using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RSFXSeqAct_StartConversation
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"SFXSeqAct_StartConversation";

        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var seqLinks = SeqTools.GetVariableLinksOfNode(export);

            // Make a new list of sequence vars based on the originals. We will null out items in this list to prevent randomization of them
            // before we merge back in.
            var randomizationMask = new List<SeqTools.VarLinkInfo>(seqLinks);
            for (int i = 0; i < randomizationMask.Count; i++)
            {
                var info = randomizationMask[i];
                if (!info.LinkedNodes.Any() || !CanLinkBeRandomized(info))
                {
                    // blacklisted or no nodes
                    randomizationMask[i] = null; // Null this object
                    continue;
                }
            }

            // Get total amount of nodes that can be randomized.
            var allNodes = randomizationMask.Where(x => x != null).SelectMany(x => x.LinkedNodes).ToList();
            if (allNodes.Count() < 2)
            {
                return false; // Can't do anything as there is not enough nodes to randomize.
            }

            // Shuffle the list of nodes that will be installed
            allNodes.Shuffle();

            // Iterate over the randomization mask and use it to determine which varlinks to update
            for (int i = 0; i < randomizationMask.Count; i++)
            {
                var info = randomizationMask[i];
                if (info != null)
                {
                    // Can be changed. As they are just references we can update this list directly
                    info.LinkedNodes.Clear();
                    info.LinkedNodes.Add(allNodes.PullFirstItem());
                }
            }

            // Write the updated links out.
            SeqTools.WriteVariableLinksToNode(export, seqLinks);

            return true;
        }

        /// <summary>
        /// Can the variables in this VarLink not be swapped with another?
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static bool CanLinkBeRandomized(SeqTools.VarLinkInfo info)
        {
            if (info.LinkedNodes.Count != 1) return false;
            if (info.LinkDesc.Contains("owner", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (info.LinkDesc.Contains("Node", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (info.LinkDesc.Contains("Puppet", StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }
    }
}
