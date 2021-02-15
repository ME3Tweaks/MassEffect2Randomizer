using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Dialogue;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using static ME2Randomizer.Classes.Randomizers.Utility.InterpTools;
using static ME2Randomizer.Classes.Randomizers.Utility.SeqTools;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioConversation
    {
        private enum EActorTrackFindActorMode
        {
            Invalid, // Default
            ActorTrack_FindActorByTag, // level tag
            ActorTrack_UseObjectPinForActor,
            ActorTrack_FindActorByNode, //attached? Maybe by who is standing in the node?
        }

        class ActorLookup
        {
            public NameReference FindActor { get; set; }
            public EActorTrackFindActorMode? FindMode { get; set; }
        }

        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioConversation";
        private static bool CanRandomizeSeqActStartConvo(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"SFXSeqAct_StartConversation";

        private static string[] Localizations = new[] { "INT" }; // Add more later, maybe.

        private enum EActorType
        {
            Invalid,
            Actor,
            NodePosition
        }

        /// <summary>
        /// Can the variables in this VarLink not be swapped with another?
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static bool CanLinkBeRandomized(SeqTools.VarLinkInfo info, out EActorType actorType)
        {
            actorType = EActorType.Invalid;
            if (info.LinkedNodes.Count != 1)
                return false;
            if (info.LinkDesc.Contains("Owner", StringComparison.InvariantCultureIgnoreCase) || info.LinkDesc.Contains("Puppet", StringComparison.InvariantCultureIgnoreCase))
            {
                actorType = EActorType.Actor;
                return true;
            }
            else if (info.LinkDesc.Contains("Node", StringComparison.InvariantCultureIgnoreCase))
            {
                actorType = EActorType.NodePosition;
                return true; // Node is position data. We need to apply special logic to ensure this is properly changed
            }

            return false;
        }

        public static bool RandomizeExportActorsInPackage(IMEPackage package, RandomizationOption option)
        {
            var conversationStartExports = package.Exports.Where(CanRandomizeSeqActStartConvo).ToList();
            if (!conversationStartExports.Any())
                return false;

            MERPackageCache cache = new MERPackageCache();

            foreach (var convStart in conversationStartExports)
            {
                if (convStart.UIndex != 4341)
                    continue;
                var bioConvImport = convStart.GetProperty<ObjectProperty>("Conv").ResolveToEntry(package) as ImportEntry;
                List<string> inputTags = new();

                // Shuffle the inputs to the conversation. We will store these and then have to update the Owner and Player tags
                // I think......
                var seqLinks = SeqTools.GetVariableLinksOfNode(convStart);

                // Dictionary of linked nodes, and var links that point to them.
                List<ExportEntry> shufflableNodes = new List<ExportEntry>();
                List<SeqTools.VarLinkInfo> applicableLinks = new();
                ExportEntry ownerEntry = null;
                ExportEntry playerEntry = null;
                foreach (var varilink in seqLinks)
                {
                    if (CanLinkBeRandomized(varilink, out _))
                    {
                        var connectedItem = varilink.LinkedNodes[0] as ExportEntry;
                        if (!shufflableNodes.Contains(connectedItem))
                        {
                            shufflableNodes.Add(connectedItem);
                        }

                        if (varilink.LinkedNodes[0].ClassName == "SeqVar_Player")
                        {
                            playerEntry = varilink.LinkedNodes[0] as ExportEntry;
                        }
                        else if (varilink.LinkDesc == "Owner")
                        {
                            ownerEntry = varilink.LinkedNodes[0] as ExportEntry;
                        }

                        applicableLinks.Add(varilink);
                    }
                }

                if (shufflableNodes.Count <= 1)
                    continue; // We cannot edit this one
                if (shufflableNodes.Count == 2 && playerEntry != null && ownerEntry != null)
                    continue; // We can't swap owner and player due to hardcoded owner and player tags

                // Build shuffle map
                bool retry = true;
                Dictionary<int, int> shuffleMap = null;
                while (retry)
                {
                    shuffleMap = new Dictionary<int, int>();
                    var reAssigned = shufflableNodes.ToList();
                    reAssigned.Shuffle();

                    for (int i = 0; i < shufflableNodes.Count; i++)
                    {
                        var sourceEntry = shufflableNodes[i];
                        var remapEntry = reAssigned.PullFirstItem();

                        int retryCount = reAssigned.Count;
                        while (retryCount > 0 && (sourceEntry == remapEntry || (ownerEntry == sourceEntry && remapEntry == ownerEntry)))
                        {
                            reAssigned.Add(remapEntry);
                            sourceEntry = reAssigned.PullFirstItem();
                            retryCount--;
                        }

                        if (sourceEntry == ownerEntry && remapEntry == playerEntry)
                        {
                            // Cannot set player to owner
                            break; // We must force a retry
                        }

                        shuffleMap[sourceEntry.UIndex] = remapEntry.UIndex;
                    }

                    if (shuffleMap.Count != shufflableNodes.Count)
                        continue; // Try again

                    // We did it
                    retry = false;
                }

                // Apply the shuffle map
                Dictionary<NameReference, ActorLookup> findActorMap = new();

                foreach (var varilink in applicableLinks)
                {
                    var repointedItem = shuffleMap[varilink.LinkedNodes[0].UIndex];
                    //if (varilink.LinkedNodes[0] == playerEntry || repointedItem == playerEntry.UIndex)
                    //{
                    // Build FindActor mapping for player
                    // it should be:
                    // Player -> [Some actor, like Pup1_1]
                    // Pup1_1 -> Player
                    // When parsing the interpdatas, change the findactor's
                    // by this methodology
                    BuildActorMap(findActorMap, varilink, repointedItem);
                    //}

                    // Actor map for updating the bioconversation

                    Debug.WriteLine($"Shuffle actor on varilink in convo: {varilink.LinkedNodes[0].ObjectName.Instanced} => {package.GetUExport(repointedItem).ObjectName.Instanced}");
                    varilink.LinkedNodes[0] = package.GetUExport(repointedItem);
                }

                SeqTools.PrintVarLinkInfo(seqLinks);

                // Write the updated links out.
                SeqTools.WriteVariableLinksToNode(convStart, seqLinks);



                // Update the localizations
                foreach (var loc in Localizations)
                {
                    var bioConversation = EntryImporter.ResolveImport(bioConvImport, cache, loc);
                    var conv = new ConversationExtended(bioConversation);
                    conv.LoadConversation(null, true);

                    List<ActorLookup> actorsToFind = new List<ActorLookup>();
                    List<InterpTrack> tracksToUpdate = new();

                    // Step 1. Catalog all actor tags that can be searched for in this conversation.
                    var entries = conv.ReplyList.ToList();
                    entries.AddRange(conv.EntryList);
                    foreach (var v in entries)
                    {
                        if (v.Interpdata == null)
                            continue;
                        var interpData = v.Interpdata;
                        InterpData id = new InterpData(interpData);
                        var convo = id.InterpGroups.FirstOrDefault(x => x.GroupName == "Conversation");
                        if (convo != null)
                        {
                            foreach (var it in convo.Tracks)
                            {
                                var props = it.Export.GetProperties();
                                var findActor = props.GetProp<NameProperty>("m_nmFindActor");
                                if (findActor != null && findActor.Value.Name != "Owner" && findActorMap.TryGetValue(findActor.Value, out var newInfo))
                                {

                                    Debug.WriteLine($"Updating find actor info: {findActor.Value.Instanced} -> {newInfo.FindActor.Instanced}");
                                    findActor.Value = newInfo.FindActor;
                                    if (newInfo.FindMode != null)
                                    {
                                        props.AddOrReplaceProp(new EnumProperty(newInfo.FindMode.ToString(), "EActorTrackFindActorMode", MERFileSystem.Game, "m_eFindActorMode"));
                                    }
                                    else
                                    {
                                        props.RemoveNamedProperty("m_eFindActorMode");
                                    }
                                }

                                var lookAtKeys = props.GetProp<ArrayProperty<StructProperty>>("m_aLookAtKeys");
                                if (lookAtKeys != null)
                                {
                                    foreach (var lookAtS in lookAtKeys)
                                    {
                                        var lookAt = lookAtS.GetProp<NameProperty>("nmFindActor");

                                        if (lookAt.Value.Name != "Owner" && findActorMap.TryGetValue(lookAt.Value, out var newInfoLA))
                                        {
                                            Debug.WriteLine($"Updating lookat find actor info: {lookAt.Value.Instanced} -> {newInfoLA.FindActor.Instanced}");
                                            lookAt.Value = newInfoLA.FindActor;
                                            var lookatFindMode = newInfoLA.FindMode?.ToString();
                                            lookatFindMode ??= "ActorTrack_FindActorByTag"; // if it's null, set it to the default. As this is struct, the property must exist
                                            lookAtS.Properties.AddOrReplaceProp(new EnumProperty(lookatFindMode, "EActorTrackFindActorMode", MERFileSystem.Game, "m_eFindActorMode"));

                                        }
                                    }
                                }

                                it.Export.WriteProperties(props);

                                //if (IsAllowedFindActor(findActor))
                                //{
                                //    if (actorsToFind.All(x => x.FindActor.Instanced != findActor.Value.Instanced))
                                //    {

                                //        ActorLookup al = new ActorLookup() { FindActor = findActor.Value };
                                //        var lookupType = it.Export.GetProperty<EnumProperty>("m_eFindActorMode");
                                //        if (lookupType != null && Enum.TryParse<EActorTrackFindActorMode>(lookupType.Value.Name, out var result))
                                //        {
                                //            al.FindMode = result;
                                //        }

                                //        actorsToFind.Add(al);
                                //    }

                                //    // add track
                                //    tracksToUpdate.Add(it);
                                //}
                            }
                        }
                    }
                    MERFileSystem.SavePackage(bioConversation.FileRef);
                }
            }

            //// Step 2. Build the remapping
            //if (actorsToFind.Count <= 1)
            //    return false; // Nothing to randomize

            //// Instanced name to NameReference
            //Dictionary<string, ActorLookup> actorRemap = new Dictionary<string, ActorLookup>();
            //var shuffledActors = actorsToFind.ToList();
            //var unshuffledActors = actorsToFind.Select(x => x.FindActor.Instanced).ToList();
            //shuffledActors.Shuffle();

            //Debug.WriteLine("");
            //while (shuffledActors.Count > 0)
            //{
            //    var sourceItem = unshuffledActors.PullFirstItem();
            //    var remappedItem = shuffledActors.PullFirstItem();
            //    actorRemap[sourceItem] = remappedItem;
            //    Debug.WriteLine($"Remapping actor: {sourceItem} => {remappedItem.FindActor.Instanced}");
            //}

            //// Step 3. Update all tracks with new remap
            //foreach (var track in tracksToUpdate)
            //{
            //    var props = track.Export.GetProperties();
            //    var findActor = props.GetProp<NameProperty>("m_nmFindActor");
            //    if (IsAllowedFindActor(findActor))
            //    {
            //        if (actorRemap[findActor.Value.Instanced].FindMode != null)
            //        {
            //            props.AddOrReplaceProp(new EnumProperty(actorRemap[findActor.Value.Instanced].FindMode.ToString(), "EActorTrackFindActorMode", MERFileSystem.Game, "m_eFindActorMode"));
            //        }
            //        else
            //        {
            //            props.RemoveNamedProperty("m_eFindActorNode");
            //        }
            //        findActor.Value = actorRemap[findActor.Value.Instanced].FindActor;
            //        track.Export.WriteProperties(props);
            //    }
            //}


            return true;
        }

        private static void BuildActorMap(Dictionary<NameReference, ActorLookup> findActorMap, VarLinkInfo sourceLink, int newSourceLinkEntryUindex)
        {
            var originalInfo = GetLookupInfo(sourceLink.LinkedNodes[0] as ExportEntry, sourceLink);
            ActorLookup lookupInfo = GetLookupInfo(sourceLink.LinkedNodes[0].FileRef.GetUExport(newSourceLinkEntryUindex), sourceLink);
            findActorMap[originalInfo.FindActor] = lookupInfo;
        }

        private static ActorLookup GetLookupInfo(ExportEntry entry, VarLinkInfo varilink)
        {
            ActorLookup lookupInfo = new ActorLookup();

            if (entry.ClassName == "SeqVar_Player")
            {
                // We now look for 'Player'
                lookupInfo.FindActor = "Player";
                entry.WriteProperty(new BoolProperty(true, "bReturnPawns")); // This is required for moved links to work. So just always add it
            }
            else
            {
                switch (varilink.LinkDesc)
                {

                    // We don't need to do owner as everything for Owner seems to be always pointing
                    // to the input of the SfxSeqAct_StartConversation
                    // So if we change it there, it... in theory should change
                    case "Owner":
                        lookupInfo.FindActor = "Owner"; // Special case. We should not change owner to something else. We should only update what owner now points to 
                                                        // or something... this shit is so confusing
                        break;
                    case "Puppet1_1":
                        lookupInfo.FindActor = new NameReference("Pup1", 2);
                        lookupInfo.FindMode = EActorTrackFindActorMode.ActorTrack_FindActorByNode;
                        break;
                    case "Puppet1_2":
                        lookupInfo.FindActor = new NameReference("Pup1", 3);
                        lookupInfo.FindMode = EActorTrackFindActorMode.ActorTrack_FindActorByNode;
                        break;
                    case "Puppet2_1":
                        lookupInfo.FindActor = new NameReference("Pup2", 2);
                        lookupInfo.FindMode = EActorTrackFindActorMode.ActorTrack_FindActorByNode;
                        break;
                    case "Puppet2_2":
                        lookupInfo.FindActor = new NameReference("Pup2", 3);
                        lookupInfo.FindMode = EActorTrackFindActorMode.ActorTrack_FindActorByNode;
                        break;
                    case "Node1":
                    case "Node2":
                    case "Node3":
                        if (entry.ClassName == "BioSeqVar_ObjectFindByTag")
                        {
                            var tag = entry.GetProperty<StrProperty>("m_sObjectTagToFind");
                            if (tag != null)
                            {
                                lookupInfo.FindActor = tag.Value;
                            }
                        }
                        else if (entry.ClassName == "SeqVar_Object")
                        {
                            // Pinned object.
                            var resolvedEntry = entry.GetProperty<ObjectProperty>("ObjValue").ResolveToEntry(entry.FileRef) as ExportEntry;
                            if (resolvedEntry != null)
                            {
                                // Look for it's tag and use that cause it's what will probably be used in the 
                                // conversation
                                var tag = resolvedEntry.GetProperty<NameProperty>("Tag");
                                if (tag != null)
                                {
                                    lookupInfo.FindActor = tag.Value;
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Could not resolve object!");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Unknown type on Node convo item");
                        }
                        break;
                    default:
                        Debugger.Break();
                        break;
                }
            }
            return lookupInfo;
        }

        private static string[] DisallowedConvoSwaps = new[]
        {
            //"Pup1", // Camera thing on aljilani
            //"Pup1", // Camera thing on aljilani
            //"Pup1", // Camera thing on aljilani
            
            // These may be optional, like when you take specific squadmate with you
            "hench_geth",
            "hench_vixen",
            "hench_convict",
            "hench_professor",
            "hench_leading"
        };

        private static bool IsAllowedFindActor(NameProperty findActor)
        {
            var name = findActor.Value.Instanced;
            return !DisallowedConvoSwaps.Contains(name, StringComparer.CurrentCultureIgnoreCase);
        }


        public static bool RandomizeExportReplies(ExportEntry export, RandomizationOption option)
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