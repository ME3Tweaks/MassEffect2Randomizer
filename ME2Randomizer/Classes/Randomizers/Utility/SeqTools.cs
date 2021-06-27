using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class MERSeqTools
    {
        public static ExportEntry InstallRandomSwitchIntoSequence(ExportEntry sequence, int numLinks)
        {
            var packageBin = MERUtilities.GetEmbeddedStaticFilesBinaryFile("PremadeSeqObjs.pcc");
            var premadeObjsP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(packageBin));

            // 1. Add the switch object and link it to the sequence
            var nSwitch = PackageTools.PortExportIntoPackage(sequence.FileRef, premadeObjsP.FindExport("SeqAct_RandomSwitch_0"), sequence.UIndex, false, true);
            KismetHelper.AddObjectToSequence(nSwitch, sequence);

            // 2. Generate the output links array. We will refresh the properties
            // with new structs so we don't have to make a copy constructor
            var olinks = nSwitch.GetProperty<ArrayProperty<StructProperty>>("OutputLinks");
            while (olinks.Count < numLinks)
            {
                olinks.Add(olinks[0]); // Just add a bunch of the first link
            }
            nSwitch.WriteProperty(olinks);

            // Reload the olinks with unique structs now
            olinks = nSwitch.GetProperty<ArrayProperty<StructProperty>>("OutputLinks");
            for (int i = 0; i < numLinks; i++)
            {
                olinks[i].GetProp<StrProperty>("LinkDesc").Value = $"Link {i + 1}";
            }

            nSwitch.WriteProperty(olinks);
            nSwitch.WriteProperty(new IntProperty(numLinks, "LinkCount"));
            return nSwitch;

        }

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
        /// <param name="elementToSkip">Th sequence object to skip</param>
        /// <param name="outboundLinkIdx">The 0-indexed outbound link that should be attached the preceding entry element, as if this one had fired that link.</param>
        public static void SkipSequenceElement(ExportEntry elementToSkip, string outboundLinkName = null, int outboundLinkIdx = -1)
        {
            if (outboundLinkIdx == -1 && outboundLinkName == null)
                throw new Exception(@"SkipSequenceElement() must have an outboundLinkName or an outboundLinkIdx!");

            if (outboundLinkIdx == -1)
            {
                var outboundLinkNames = KismetHelper.GetOutboundLinkNames(elementToSkip);
                outboundLinkIdx = outboundLinkNames.IndexOf(outboundLinkName);
            }


            // List of outbound link elements on the specified item we want to skip. These will be placed into the inbound item
            Debug.WriteLine($@"Attempting to skip {elementToSkip.UIndex} in {elementToSkip.FileRef.FilePath}");
            var outboundLinkLists = SeqTools.GetOutboundLinksOfNode(elementToSkip);
            var inboundToSkippedNode = SeqTools.FindOutboundConnectionsToNode(elementToSkip, SeqTools.GetAllSequenceElements(elementToSkip).OfType<ExportEntry>());

            var newTargetNodes = outboundLinkLists[outboundLinkIdx];

            foreach (var preNode in inboundToSkippedNode)
            {
                // For every node that links to the one we want to skip...
                var preNodeLinks = GetOutboundLinksOfNode(preNode);

                foreach (var ol in preNodeLinks)
                {
                    var numRemoved = ol.RemoveAll(x => x.LinkedOp == elementToSkip);

                    if (numRemoved > 0)
                    {
                        // At least one was removed. Repoint it
                        ol.AddRange(newTargetNodes);
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
        public static List<List<SeqTools.OutboundLink>> GetOutboundLinksOfNode(ExportEntry node)
        {
            var outputLinksMapping = new List<List<SeqTools.OutboundLink>>();
            var outlinksProp = node.GetProperty<ArrayProperty<StructProperty>>("OutputLinks");
            if (outlinksProp != null)
            {
                int i = 0;
                foreach (var ol in outlinksProp)
                {
                    var oLinks = new List<SeqTools.OutboundLink>();
                    outputLinksMapping.Add(oLinks);

                    var links = ol.GetProp<ArrayProperty<StructProperty>>("Links");
                    if (links != null)
                    {
                        foreach (var l in links)
                        {
                            oLinks.Add(SeqTools.OutboundLink.FromStruct(l, node.FileRef));
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
        public static void WriteOutboundLinksToNode(ExportEntry node, List<List<SeqTools.OutboundLink>> linkSet)
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

        /// <summary>
        /// Finds outbound connections that come to this node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sequenceElements"></param>
        /// <returns></returns>
        public static List<ExportEntry> FindOutboundConnectionsToNode(ExportEntry node, IEnumerable<ExportEntry> sequenceElements)
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
        /// Finds variable connections that come to this node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sequenceElements"></param>
        /// <returns></returns>
        public static List<ExportEntry> FindVariableConnectionsToNode(ExportEntry node, List<ExportEntry> sequenceElements)
        {
            List<ExportEntry> referencingNodes = new List<ExportEntry>();

            foreach (var seqObj in sequenceElements)
            {
                if (seqObj == node) continue; // Skip node pointing to itself
                var linkSet = SeqTools.GetVariableLinksOfNode(seqObj);
                if (linkSet.Any(x => x.LinkedNodes.Any(y => y == node)))
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
        /// Clones basic data type objects. Do not use with complicated types.
        /// </summary>
        /// <param name="itemToClone"></param>
        /// <returns></returns>
        public static ExportEntry CloneBasicSequenceObject(ExportEntry itemToClone)
        {
            ExportEntry exp = itemToClone.Clone();
            //needs to have the same index to work properly
            if (exp.ClassName == "SeqVar_External")
            {
                // Probably shouldn't clone these...
                exp.indexValue = itemToClone.indexValue;
            }

            itemToClone.FileRef.AddExport(exp);

            var sequence = itemToClone.GetProperty<ObjectProperty>("ParentSequence").ResolveToEntry(itemToClone.FileRef) as ExportEntry;
            KismetHelper.AddObjectToSequence(exp, sequence);
            return exp;
        }

        /// <summary>
        /// Gets the containing sequence of the specified export. Performed by looking for ParentSequence object property. Pass true to continue up the chain.
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        public static ExportEntry GetParentSequence(ExportEntry export, bool lookup = false)
        {
            var result = export?.GetProperty<ObjectProperty>("ParentSequence")?.ResolveToEntry(export.FileRef) as ExportEntry;
            while (lookup && result != null && result.ClassName != "Sequence")
            {
                result = result.GetProperty<ObjectProperty>("ParentSequence")?.ResolveToEntry(export.FileRef) as ExportEntry;
            }

            return result;
        }

        public static void WriteOriginator(ExportEntry export, IEntry originator)
        {
            export.WriteProperty(new ObjectProperty(originator.UIndex, "Originator"));
        }

        public static void WriteObjValue(ExportEntry export, IEntry objValue)
        {
            export.WriteProperty(new ObjectProperty(objValue.UIndex, "ObjValue"));
        }

        public static void PrintVarLinkInfo(List<SeqTools.VarLinkInfo> seqLinks)
        {
            foreach (var link in seqLinks)
            {
                Debug.WriteLine($"VarLink {link.LinkDesc}, expected type: {link.ExpectedTypeName}");
                foreach (var linkedNode in link.LinkedNodes.OfType<ExportEntry>())
                {
                    var findTag = linkedNode.GetProperty<StrProperty>("m_sObjectTagToFind");
                    var objValue = linkedNode.GetProperty<ObjectProperty>("ObjValue");
                    Debug.WriteLine($"   {linkedNode.UIndex} {linkedNode.ObjectName.Instanced} {findTag?.Value} {objValue?.ResolveToEntry(linkedNode.FileRef).ObjectName.Instanced}");
                }
            }
        }

        /// <summary>
        /// Is the specified node assigned to a BioPawn or subclass of? 
        /// </summary>
        /// <param name="sequenceObj">The object that we are checking that should be ignored for references (like conversation start)</param>
        /// <param name="vlNode">The node to check</param>
        /// <param name="sequenceElements">List of nodes in the sequence</param>
        /// <returns></returns>
        public static bool IsAssignedBioPawn(ExportEntry sequenceObj, ExportEntry vlNode, List<ExportEntry> sequenceElements)
        {
            var inboundConnections = SeqTools.FindVariableConnectionsToNode(vlNode, sequenceElements);
            foreach (var sequenceObject in inboundConnections)
            {
                if (sequenceObject == sequenceObj)
                    continue; // Obviously we reference this node

                // Is this a 'SetObject' that is assigning the value?
                if (sequenceObject.InheritsFrom("SequenceAction") && sequenceObject.ClassName == "SeqAct_SetObject" && sequenceObject != sequenceObj)
                {
                    //check if target is my node
                    var referencingVarLinks = SeqTools.GetVariableLinksOfNode(sequenceObject);
                    var targetLink = referencingVarLinks.FirstOrDefault(x => x.LinkDesc == "Target"); // What is the target node?
                    if (targetLink != null)
                    {
                        //see if target is node we are investigating for setting.
                        foreach (var potentialTarget in targetLink.LinkedNodes.OfType<ExportEntry>())
                        {
                            if (potentialTarget == vlNode)
                            {
                                // There's a 'SetObject' with Target of the attached variable to our interp cutscene
                                // That means something is 'setting' the value of this
                                // We need to inspect what it is to see if we can shuffle it

                                //Debug.WriteLine("Found a setobject to variable linked item on a sequence");

                                //See what value this is set to. If it inherits from BioPawn we can use it in the shuffling.
                                var pointedAtValueLink = referencingVarLinks.FirstOrDefault(x => x.LinkDesc == "Value");
                                if (pointedAtValueLink != null && pointedAtValueLink.LinkedNodes.Count == 1) // Only 1 item being set. More is too complicated
                                {
                                    var linkedNode = pointedAtValueLink.LinkedNodes[0] as ExportEntry;
                                    var linkedNodeType = linkedNode.GetProperty<ObjectProperty>("ObjValue");
                                    if (linkedNodeType != null)
                                    {
                                        var linkedNodeData = sequenceObj.FileRef.GetUExport(linkedNodeType.Value);
                                        if (linkedNodeData.IsA("BioPawn"))
                                        {
                                            //We can shuffle this item.

                                            // We write the property to the node so if it's not assigned at runtime (like on gender check) it still can show something.
                                            // Cutscene will still be goofed up but will instead show something instead of nothing

                                            linkedNode.WriteProperty(linkedNodeType);

                                            //Debug.WriteLine("Adding shuffle item: " + objRef.Value);
                                            // Original value below was 'linkedNode' which i'm pretty sure is the value that would be assigned, not the actual object that holds that value oncea assigned
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
