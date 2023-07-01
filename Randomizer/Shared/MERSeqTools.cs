using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Shared
{
    class MERSeqTools
    {
        /// <summary>
        /// Installs a random switch into the sequence with the specified number of outlinks.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sequence"></param>
        /// <param name="numLinks"></param>
        /// <returns></returns>
        public static ExportEntry InstallRandomSwitchIntoSequence(GameTarget target, ExportEntry sequence, int numLinks)
        {
            var packageBin = MEREmbedded.GetEmbeddedPackage(target.Game, "PremadeSeqObjs.pcc");
            var premadeObjsP = MEPackageHandler.OpenMEPackageFromStream(packageBin);

            // 1. Add the switch object and link it to the sequence
            var nSwitch = PackageTools.PortExportIntoPackage(target, sequence.FileRef, premadeObjsP.FindExport("SeqAct_RandomSwitch_0"), sequence.UIndex, false, true);
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
        /// Adds a new switch output link. Returns the link number (NOT THE INDEX!!) that you can use 'Link X' as the description for
        /// </summary>
        /// <param name="existingSwitch"></param>
        /// <returns></returns>
        public static int AddRandomSwitchOutput(ExportEntry existingSwitch)
        {
            var outputLinks = existingSwitch.GetProperty<ArrayProperty<StructProperty>>("OutputLinks");
            int numExistingOutputLinks = 0;

            if (outputLinks == null)
            {
                outputLinks = new ArrayProperty<StructProperty>(new List<StructProperty>(), "OutputLinks");
                existingSwitch.WriteProperty(outputLinks); // Add outputlinks
            }
            else
            {
                numExistingOutputLinks = outputLinks.Count;
            }

            KismetHelper.CreateNewOutputLink(existingSwitch, $"Link {numExistingOutputLinks + 1}", null);
            existingSwitch.WriteProperty(new IntProperty(numExistingOutputLinks + 1, "LinkCount")); // Update the link count
            return numExistingOutputLinks + 1;
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
        /// Is the specified node assigned to a subclass of the specified type?
        /// </summary>
        /// <param name="sequenceObj">The object that we are checking that should be ignored for references (like conversation start)</param>
        /// <param name="vlNode">The node to check</param>
        /// <param name="sequenceElements">List of nodes in the sequence</param>
        /// <returns></returns>
        public static bool IsAssignedClassType(ExportEntry sequenceObj, ExportEntry vlNode, List<ExportEntry> sequenceElements, string rootClass)
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
                                        if (linkedNodeData.IsA(rootClass))
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

        /// <summary>
        /// Gets the next connected node from the specified kismet node. Does not do any error handling!
        /// </summary>
        /// <param name="node"></param>
        /// <param name="outputLinkIdx"></param>
        /// <returns></returns>
        public static ExportEntry GetNextNode(ExportEntry node, int outputLinkIdx)
        {
            return SeqTools.GetOutboundLinksOfNode(node)[outputLinkIdx][0].LinkedOp as ExportEntry;
        }

        public static ExportEntry FindSequenceObjectByClassAndPosition(ExportEntry sequence, string className, int posX = int.MinValue, int posY = int.MinValue)
        {
            var seqObjs = sequence.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects")
                .Select(x => x.ResolveToEntry(sequence.FileRef)).OfType<ExportEntry>().ToList();
            return FindSequenceObjectByClassAndPosition(seqObjs, className, posX, posY);
        }

        /// <summary>
        /// Removes all links to other sequence objects from the given object.
        /// Use the optional parameters to specify which types of links can be removed.
        /// </summary>
        /// <param name="export">Sequence object to remove all links from</param>
        /// <param name="outlinks">If true, output links will be removed. Default: True</param>
        /// <param name="variablelinks">If true, variable links will be removed. Default: True</param>
        /// <param name="eventlinks">If true, event links will be removed. Default: True</param>
        public static void RemoveAllNamedOutputLinks(ExportEntry export, string linkName)
        {
            var props = export.GetProperties();
            var outLinksProp = props.GetProp<ArrayProperty<StructProperty>>("OutputLinks");
            if (outLinksProp != null)
            {
                foreach (var prop in outLinksProp)
                {
                    if (prop.GetProp<StrProperty>("LinkDesc")?.Value == linkName)
                    {
                        prop.GetProp<ArrayProperty<StructProperty>>("Links").Clear();
                    }
                }
            }
            export.WriteProperties(props);
        }

        public static ExportEntry FindSequenceObjectByClassAndPosition(List<ExportEntry> seqObjs, string className, int posX = int.MinValue, int posY = int.MinValue)
        {
            seqObjs = seqObjs.Where(x => x.ClassName == className).ToList();
            foreach (var obj in seqObjs)
            {
                if (posX != int.MinValue && posY != int.MinValue)
                {
                    var props = obj.GetProperties();
                    var foundPosX = props.GetProp<IntProperty>("ObjPosX")?.Value;
                    var foundPosY = props.GetProp<IntProperty>("ObjPosY")?.Value;
                    if (foundPosX != null && foundPosY != null &&
                        foundPosX == posX && foundPosY == posY)
                    {
                        return obj;
                    }
                }
                else if (seqObjs.Count == 1)
                {
                    return obj; // First object
                }
                else
                {
                    throw new Exception($"COULD NOT FIND OBJECT OF TYPE {className}");
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new sequence object in the given sequence
        /// </summary>
        /// <param name="SelectedSequence"></param>
        /// <param name="info"></param>
        public static void CreateNewObject(ExportEntry SelectedSequence, ClassInfo info)
        {
            if (SelectedSequence == null)
            {
                return;
            }

            IEntry classEntry;
            if (SelectedSequence.FileRef.Exports.Any(exp => exp.ObjectName == info.ClassName) || SelectedSequence.FileRef.Imports.Any(imp => imp.ObjectName == info.ClassName) ||
                GlobalUnrealObjectInfo.GetClassOrStructInfo(SelectedSequence.FileRef.Game, info.ClassName) is { } classInfo && EntryImporter.IsSafeToImportFrom(classInfo.pccPath, SelectedSequence.FileRef.Game, SelectedSequence.FileRef.FilePath))
            {
                var rop = new RelinkerOptionsPackage();
                classEntry = EntryImporter.EnsureClassIsInFile(SelectedSequence.FileRef, info.ClassName, rop);
            }
            else
            {
                classEntry = EntryImporter.EnsureClassIsInFile(SelectedSequence.FileRef, info.ClassName, new RelinkerOptionsPackage());
            }
            if (classEntry is null)
            {
                return;
            }
            var packageCache = new PackageCache { AlwaysOpenFromDisk = false };
            packageCache.InsertIntoCache(SelectedSequence.FileRef);
            var newSeqObj = new ExportEntry(SelectedSequence.FileRef, SelectedSequence, SelectedSequence.FileRef.GetNextIndexedName(info.ClassName), properties: SequenceObjectCreator.GetSequenceObjectDefaults(SelectedSequence.FileRef, info, packageCache))
            {
                Class = classEntry,
            };
            newSeqObj.ObjectFlags |= UnrealFlags.EObjectFlags.Transactional;
            SelectedSequence.FileRef.AddExport(newSeqObj);
            KismetHelper.AddObjectToSequence(newSeqObj, SelectedSequence, true);
        }

        /// <summary>
        /// Installs a sequence with no inputs. The sequence should have its own events to trigger itself
        /// </summary>
        public static ExportEntry InstallSequenceStandalone(ExportEntry sourceSequence, IMEPackage targetPackage, ExportEntry parentSequence = null)
        {
            parentSequence ??= targetPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceSequence, targetPackage, parentSequence, true, new RelinkerOptionsPackage(), out var newUiSeq);
            KismetHelper.AddObjectToSequence(newUiSeq as ExportEntry, parentSequence);
            return newUiSeq as ExportEntry;
        }

        /// <summary>
        /// Adds a new delay object to a sequence
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static ExportEntry AddDelay(ExportEntry sequence, float delay)
        {
            var newDelay = SequenceObjectCreator.CreateSequenceObject(sequence.FileRef, "SeqAct_Delay");
            KismetHelper.AddObjectToSequence(newDelay, sequence);
            newDelay.WriteProperty(new FloatProperty(delay, "Duration"));
            return newDelay;
        }
    }
}
