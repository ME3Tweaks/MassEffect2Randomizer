using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Dialogue;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game2.ExportTypes
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
            public bool CouldNotResolve { get; set; }
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

        public static bool RandomizePackageActorsInConversation(GameTarget target, IMEPackage package, RandomizationOption option)
        {
            var conversationStartExports = package.Exports.Where(CanRandomizeSeqActStartConvo).ToList();
            if (!conversationStartExports.Any())
                return false;

            MERPackageCache localCache = new MERPackageCache();

            foreach (var convStart in conversationStartExports)
            {
                //if (convStart.UIndex < 13638)
                //    continue;
                if (!CanRandomizeConversationStart(convStart))
                    continue;
                var bioConvImportProp = convStart.GetProperty<ObjectProperty>("Conv");
                if (bioConvImportProp == null)
                    continue; // Some conversation starts are just filler stubs and are missing data like Nor_340a/bZaeed
                var bioConvImport = bioConvImportProp.ResolveToEntry(package) as ImportEntry;
                List<string> inputTags = new();

                // Shuffle the inputs to the conversation. We will store these and then have to update the Owner and Player tags
                // I think......
                var seqLinks = SeqTools.GetVariableLinksOfNode(convStart);

                // Dictionary of linked nodes, and var links that point to them.
                List<ExportEntry> shufflableNodes = new List<ExportEntry>();
                List<SeqTools.VarLinkInfo> applicableLinks = new();
                ExportEntry ownerEntry = null;
                ExportEntry playerEntry = null;
                var sequenceObjects = SeqTools.GetAllSequenceElements(convStart).OfType<ExportEntry>().ToList();
                foreach (var varilink in seqLinks)
                {
                    if (CanLinkBeRandomized(varilink, out _))
                    {
                        var connectedItem = varilink.LinkedNodes[0] as ExportEntry;
                        if (!shufflableNodes.Contains(connectedItem))
                        {
                            // Check to make sure node has a value
                            if (connectedItem.ClassName == "SeqVar_Object")
                            {
                                var objValue = connectedItem.GetProperty<ObjectProperty>("ObjValue");
                                if (objValue == null && !MERSeqTools.IsAssignedBioPawn(convStart, connectedItem, sequenceObjects))
                                {
                                    continue; // This is not a shufflable node, it is never assigned anything
                                }
                            }

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

                    //Debug.WriteLine($"Shuffle actor on varilink in convo: {varilink.LinkedNodes[0].ObjectName.Instanced} => {package.GetUExport(repointedItem).ObjectName.Instanced}");
                    varilink.LinkedNodes[0] = package.GetUExport(repointedItem);
                }

                //SeqTools.PrintVarLinkInfo(seqLinks);

                // Write the updated links out.
                SeqTools.WriteVariableLinksToNode(convStart, seqLinks);



                // Update the localizations
                foreach (var loc in Localizations)
                {
                    var bioConversation = EntryImporter.ResolveImport(bioConvImport, MERFileSystem.GetGlobalCache(), localCache, loc);
                    var conv = new ConversationExtended(bioConversation);
                    conv.LoadConversation(null, true);

                    // Step 1. Update tags via map
                    var allConvEntries = conv.ReplyList.ToList();
                    allConvEntries.AddRange(conv.EntryList);
                    foreach (var convNode in allConvEntries)
                    {
                        // Update speaker
                        if (convNode.IsReply)
                        {
                            // Player. We can't do anything (or can we?)
                        }
                        else
                        {
                            // Non-player node. We can change the tag
                            var speakerTag = convNode.SpeakerTag;

                            // Even though it is dictionary, since it is NameRef, it is considered case sensitive. We have to use case insensitive check
                            var newName = findActorMap.FirstOrDefault(x => x.Key.Instanced.Equals(speakerTag.SpeakerName, StringComparison.InvariantCultureIgnoreCase)).Value;
                            if (newName != null && newName.FindActor.Name != null)
                            {
                                var newTagIdx = conv.Speakers.FirstOrDefault(x => x.SpeakerName.Equals(newName.FindActor.Instanced, StringComparison.InvariantCultureIgnoreCase));
                                if (newTagIdx != null)
                                {
                                    convNode.SpeakerIndex = newTagIdx.SpeakerID;
                                }
                                else
                                {
                                    var newSpeaker = new SpeakerExtended(conv.Speakers.Count - 3, new NameReference(newName.FindActor.Name.ToLower(), newName.FindActor.Number));
                                    newSpeaker.FaceFX_Male = speakerTag.FaceFX_Male;
                                    newSpeaker.FaceFX_Female = speakerTag.FaceFX_Male;

                                    conv.Speakers.Add(newSpeaker);
                                    convNode.SpeakerIndex = newSpeaker.SpeakerID;
                                    //Debugger.Break();
                                }
                            }
                            else
                            {
                                //Debugger.Break();
                            }
                        }



                        // Update interpolation data
                        if (convNode.Interpdata == null)
                            continue;
                        var interpData = convNode.Interpdata;

                        InterpTools.InterpData id = new InterpTools.InterpData(interpData);
                        var convo = id.InterpGroups.FirstOrDefault(x => x.GroupName == "Conversation");
                        if (convo != null)
                        {
                            foreach (var it in convo.Tracks)
                            {
                                var props = it.Export.GetProperties();
                                var findActor = props.GetProp<NameProperty>("m_nmFindActor");
                                if (findActor != null && findActor.Value.Name != "Owner" && findActorMap.TryGetValue(findActor.Value, out var newInfo) && newInfo.FindActor.Name != null)
                                {

                                    //Debug.WriteLine($"Updating find actor info: {findActor.Value.Instanced} -> {newInfo.FindActor.Instanced}");
                                    findActor.Value = newInfo.FindActor;
                                    if (newInfo.FindMode != null)
                                    {
                                        props.AddOrReplaceProp(new EnumProperty(newInfo.FindMode.ToString(), "EActorTrackFindActorMode", target.Game, "m_eFindActorMode"));
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

                                        if (lookAt.Value.Name != "Owner" && findActorMap.TryGetValue(lookAt.Value, out var newInfoLA) && newInfoLA.FindActor.Name != null)
                                        {
                                            //Debug.WriteLine($"Updating lookat find actor info: {lookAt.Value.Instanced} -> {newInfoLA.FindActor.Instanced}");
                                            if (newInfoLA.FindActor.Name == null)
                                                Debugger.Break();
                                            lookAt.Value = newInfoLA.FindActor;
                                            var lookatFindMode = newInfoLA.FindMode?.ToString();
                                            lookatFindMode ??= "ActorTrack_FindActorByTag"; // if it's null, set it to the default. As this is struct, the property must exist
                                            lookAtS.Properties.AddOrReplaceProp(new EnumProperty(lookatFindMode, "EActorTrackFindActorMode", target.Game, "eFindActorMode"));
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
                    conv.SerializeNodes(); // Write the updated info back
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

        private static bool CanRandomizeConversationStart(ExportEntry convStart)
        {
            var fname = Path.GetFileName(convStart.FileRef.FilePath);
            if (fname == "BioD_CitHub_220AsL.pcc" && convStart.UIndex == 803) // seems to be able to softlock thane's interrogation as some of the inputs appear to have been disabled. If they're randomized around, it could cause softlock on convo start which kills loyalty mission
                return false;
            if (fname == "BioD_RprGtA_420CoreBattle.pcc" && convStart.UIndex == 4626) // can softlock cause shep hits a deathplane
                return false;

            return true;
        }

        private static void BuildActorMap(Dictionary<NameReference, ActorLookup> findActorMap, SeqTools.VarLinkInfo sourceLink, int newSourceLinkEntryUindex)
        {
            var originalInfo = GetLookupInfo(sourceLink.LinkedNodes[0] as ExportEntry, sourceLink);
            ActorLookup lookupInfo = GetLookupInfo(sourceLink.LinkedNodes[0].FileRef.GetUExport(newSourceLinkEntryUindex), sourceLink);
            if (lookupInfo != null && originalInfo != null && !lookupInfo.CouldNotResolve && !originalInfo.CouldNotResolve)
            {
                // Some dynamically set objects we won't be able to do. RIP
                // May make it return something so we can tell if we should randomize this conversation at all
                findActorMap[originalInfo.FindActor] = lookupInfo;
            }
        }

        private static ActorLookup GetLookupInfo(ExportEntry entry, SeqTools.VarLinkInfo varilink)
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
                if (varilink.LinkDesc.StartsWith("Node"))
                {

                    if (entry.ClassName == "BioSeqVar_ObjectFindByTag")
                    {
                        var tag = entry.GetProperty<StrProperty>("m_sObjectTagToFind");
                        if (tag != null)
                        {
                            lookupInfo.FindActor = tag.Value;
                        }
                        else
                        {
                            // ??
                            Debug.WriteLine("Could not find object by tag, tag was missing!");
                        }
                    }
                    else if (entry.ClassName == "SeqVar_Object")
                    {
                        // Pinned object.
                        var resolvedEntry = entry.GetProperty<ObjectProperty>("ObjValue")?.ResolveToEntry(entry.FileRef) as ExportEntry;
                        if (resolvedEntry != null)
                        {
                            // Look for it's tag and use that cause it's what will probably be used in the 
                            // conversation
                            var tag = resolvedEntry.GetProperty<NameProperty>("Tag");
                            if (tag != null)
                            {
                                lookupInfo.FindActor = tag.Value;
                            }
                            else
                            {
                                //Debug.WriteLine("No tag on resolved object! Is it dynamic?");
                                //lookupInfo.FindActor = entry.GetProperty<NameProperty>("m_nmFindActor").Value; // keep the original value, I guess
                                lookupInfo.CouldNotResolve = true;
                            }
                        }
                        else
                        {
                            //Debug.WriteLine("Could not resolve object! Is it dynamic?");
                            //lookupInfo.FindActor = entry.GetProperty<NameProperty>("m_nmFindActor").Value; // keep the original value, I guess
                            lookupInfo.CouldNotResolve = true;
                        }
                    }
                    else if (entry.ClassName == "SeqVar_ScopedNamed")
                    {
                        // We have to find an object in the sequence that has the VarName
                        // What a dumb system
                        var findVarName = entry.GetProperty<NameProperty>("FindVarName");
                        if (findVarName == null)
                        {
                            Debugger.Break();
                        }

                        var seqObjs = SeqTools.GetAllSequenceElements(entry).OfType<ExportEntry>();
                        foreach (var seqObj in seqObjs)
                        {
                            var props = seqObj.GetProperties();
                            var varname = props.GetProp<NameProperty>("VarName");
                            if (varname != null && varname.Value == findVarName.Value)
                            {
                                // Pinned object.
                                var resolvedEntry = props.GetProp<ObjectProperty>("ObjValue")?.ResolveToEntry(entry.FileRef) as ExportEntry;
                                if (resolvedEntry != null)
                                {
                                    // Look for it's tag and use that cause it's what will probably be used in the 
                                    // conversation
                                    var tag = resolvedEntry.GetProperty<NameProperty>("Tag");
                                    if (tag != null)
                                    {
                                        lookupInfo.FindActor = tag.Value;
                                    }
                                    else
                                    {
                                        //Debug.WriteLine("No tag on resolved object! Is it dynamic?");
                                        //lookupInfo.FindActor = entry.GetProperty<NameProperty>("m_nmFindActor").Value; // keep the original value, I guess
                                        lookupInfo.CouldNotResolve = true;
                                    }
                                }
                                else
                                {
                                    //Debug.WriteLine("Could not resolve object! Is it dynamic?");
                                    //lookupInfo.FindActor = entry.GetProperty<NameProperty>("m_nmFindActor").Value; // keep the original value, I guess
                                    lookupInfo.CouldNotResolve = true;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Unknown type on Node convo item: {entry.ClassName}");
                    }
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
                        default:
                            Debugger.Break();
                            break;
                    }
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


        public static bool RandomizeExportReplies(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;

            var conv = new ConversationExtended(export);
            conv.LoadConversation(TLKBuilder.TLKLookup);


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