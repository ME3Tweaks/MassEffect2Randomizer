using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class SeqTools
    {
        /// <summary>
        /// Changes a single output link to a new target and commits the properties.
        /// </summary>
        /// <param name="export">Export to operate on</param>
        /// <param name="outputLinkIndex">The index of the item in 'OutputLinks'</param>
        /// <param name="linksIndex">The index of the item in the Links array</param>
        /// <param name="newTarget">The UIndex of the new target</param>
        public static void ChangeOutlink(ExportEntry export, int outputLinkIndex, int linksIndex, int newTarget)
        {
            var props = export.GetProperties();
            ChangeOutlink(props, outputLinkIndex, linksIndex, newTarget);
            export.WriteProperties(props);
        }

        /// <summary>
        /// Changes a single output link to a new target.
        /// </summary>
        /// <param name="export">The export properties list to operate on</param>
        /// <param name="outputLinkIndex">The index of the item in 'OutputLinks'</param>
        /// <param name="linksIndex">The index of the item in the Links array</param>
        /// <param name="newTarget">The UIndex of the new target</param>
        public static void ChangeOutlink(PropertyCollection props, int outputLinkIndex, int linksIndex, int newTarget)
        {
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[outputLinkIndex].GetProp<ArrayProperty<StructProperty>>("Links")[linksIndex].GetProp<ObjectProperty>("LinkedOp").Value = newTarget;
        }

        /// <summary>
        /// Removes a sequence element from the graph, by repointing incoming references to the ones referenced by outgoing items on this export. This is a very basic utility, only use it for items with one input and potentially multiple outputs.
        /// </summary>
        /// <param name="seqElement"></param>
        public static void SkipSequenceElement(ExportEntry seqElement, int outboundLinkIdx)
        {
            // List of outbound link elements on the specified item we want to skip. These will be placed into the inbound item
            Debug.WriteLine($@"Attempting to skip {seqElement.UIndex} in {seqElement.FileRef.FilePath}");
            var outboundList = seqElement.GetProperty<ArrayProperty<StructProperty>>("OutputLinks")[outboundLinkIdx].GetProp<ArrayProperty<StructProperty>>("Links");
            var replacementItems = outboundList.Select(x => OutboundLink.FromStruct(x, seqElement.FileRef)).ToList(); // List of items we should replace them with.


            var inboundNodes = FindConnectionsToNode(seqElement, GetAllSequenceElements(seqElement).OfType<ExportEntry>().ToList());

            foreach (var preNode in inboundNodes)
            {
                // For every node that links to the one we want to skip...
                var preNodeLinks = GetOutboundLinksOfNode(preNode);

                foreach (var ol in preNodeLinks)
                {
                    var numRemoved = ol.RemoveAll(x => x.LinkedOp == seqElement);

                    if (numRemoved > 0)
                    {
                        // At least one was removed. Repoint it
                        ol.AddRange(replacementItems);
                    }
                }

                WriteOutboundLinksToNode(preNode, preNodeLinks);
            }
        }

        /// <summary>
        /// Builds a list of OutputLinkIdx => [List of nodes pointed to]
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<List<OutboundLink>> GetOutboundLinksOfNode(ExportEntry node)
        {
            var outputLinksMapping = new List<List<OutboundLink>>();
            var outlinksProp = node.GetProperty<ArrayProperty<StructProperty>>("OutputLinks");
            if (outlinksProp != null)
            {
                int i = 0;
                foreach (var ol in outlinksProp)
                {
                    List<OutboundLink> oLinks = new List<OutboundLink>();
                    outputLinksMapping.Add(oLinks);

                    var links = ol.GetProp<ArrayProperty<StructProperty>>("Links");
                    if (links != null)
                    {
                        foreach (var l in links)
                        {
                            oLinks.Add(OutboundLink.FromStruct(l, node.FileRef));
                        }
                    }

                    i++;
                }
            }

            return outputLinksMapping;
        }

        /// <summary>
        /// Writes a list of outbound links to a sequence node. Note that this cannot add output link points (like an additional output param), but only existing connections.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="linkSet"></param>
        public static void WriteOutboundLinksToNode(ExportEntry node, List<List<OutboundLink>> linkSet)
        {
            var outlinksProp = node.GetProperty<ArrayProperty<StructProperty>>("OutputLinks");

            if (linkSet.Count != outlinksProp.Count)
            {
                Debug.WriteLine("Sets are out of sync for WriteLinksToNode()!");
                return; // Sets are not compatible with this code
            }

            for (int i = 0; i < linkSet.Count; i++)
            {
                var oldL = outlinksProp[i].GetProp<ArrayProperty<StructProperty>>("Links");
                var newL = linkSet[i];
                oldL.ReplaceAll(newL.Select(x => x.GenerateStruct()));
            }

            node.WriteProperty(outlinksProp);
        }

        internal class OutboundLink
        {
            public IEntry? LinkedOp { get; set; }
            public int InputLinkIdx { get; set; }

            public static OutboundLink FromStruct(StructProperty sp, IMEPackage package)
            {
                return new OutboundLink()
                {
                    LinkedOp = sp.GetProp<ObjectProperty>("LinkedOp")?.ResolveToEntry(package),
                    InputLinkIdx = sp.GetProp<IntProperty>("InputLinkIdx")
                };
            }

            public StructProperty GenerateStruct()
            {
                return new StructProperty("SeqOpOutputInputLink", false,
                    new ObjectProperty(LinkedOp.UIndex, "LinkedOp"),
                    new IntProperty(InputLinkIdx, "InputLInkIdx"),
                    new NoneProperty());
            }
        }

        public static List<ExportEntry> FindConnectionsToNode(ExportEntry node, List<ExportEntry> sequenceElements)
        {
            List<ExportEntry> referencingNodes = new List<ExportEntry>();

            foreach (var seqObj in sequenceElements)
            {
                if (seqObj == node) continue; // Skip node pointing to itself
                var linkSet = GetOutboundLinksOfNode(seqObj);
                if (linkSet.Any(x => x.Any(y => y.LinkedOp != null && y.LinkedOp.UIndex == node.UIndex)))
                {
                    referencingNodes.Add(seqObj);
                }
            }

            return referencingNodes.Distinct().ToList();
        }

        /// <summary>
        /// Gets a list of all entries that are referenced by this sequence. If the passed in object is not a Sequence, the parent sequence is used. Returns null if there is no parent sequence.
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        public static List<IEntry> GetAllSequenceElements(ExportEntry export)
        {
            if (export.ClassName == "Sequence")
            {
                var seqObjs = export.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects");
                if (seqObjs != null)
                {
                    return seqObjs.Where(x => export.FileRef.IsEntry(x.Value)).Select(x => x.ResolveToEntry(export.FileRef)).ToList();
                }
            }
            else
            {
                // Pull from parent sequence.
                var pSeqObj = export.GetProperty<ObjectProperty>("ParentSequence");
                if (pSeqObj != null && pSeqObj.ResolveToEntry(export.FileRef) is ExportEntry pSeq && pSeq.ClassName == "Sequence")
                {
                    return GetAllSequenceElements(pSeq);
                }
            }

            return null;
        }

        /// <summary>
        /// Basic description of a VarLink (bottom of kismet action - this includes all links)
        /// </summary>
        internal class VarLinkInfo
        {
            public string LinkDesc { get; set; }
            public string PropertyName { get; set; }
            public IEntry ExpectedType { get; set; }
            public string ExpectedTypeName => ExpectedType.ObjectName;
            public List<IEntry> LinkedNodes { get; set; }

            public static VarLinkInfo FromStruct(StructProperty sp, IMEPackage package)
            {
                return new VarLinkInfo()
                {
                    LinkDesc = sp.GetProp<StrProperty>("LinkDesc"),
                    PropertyName = sp.GetProp<NameProperty>("PropertyName")?.Value,
                    ExpectedType = sp.GetProp<ObjectProperty>("ExpectedType").ResolveToEntry(package),
                    LinkedNodes = sp.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables")?.Select(x => x.ResolveToEntry(package)).ToList() ?? new List<IEntry>()
                };
            }
        }

        public static List<VarLinkInfo> GetVariableLinksOfNode(ExportEntry export)
        {
            var varLinks = new List<VarLinkInfo>();
            var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
            if (variableLinks != null)
            {
                foreach (var vl in variableLinks)
                {
                    varLinks.Add(VarLinkInfo.FromStruct(vl, export.FileRef));
                }
            }

            return varLinks;
        }

        /// <summary>
        /// Writes the list of variable links to the node. Only the linked objects are written. The list MUST be in order and be the same length as the current list.
        /// </summary>
        /// <param name="export"></param>
        /// <param name="varLinks"></param>
        public static void WriteVariableLinksToNode(ExportEntry export, List<VarLinkInfo> varLinks)
        {
            var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
            if (variableLinks != null && varLinks.Count == variableLinks.Count)
            {
                for (int i = 0; i < variableLinks.Count; i++)
                {
                    var linkedVarList = variableLinks[i].GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                    linkedVarList?.ReplaceAll(varLinks[i].LinkedNodes.Select(x => new ObjectProperty(x)));
                }
            }

            export.WriteProperty(variableLinks);
        }
    }
}
