using System;
using System.Collections.Generic;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.Dialogue;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using static ME2Randomizer.Classes.Randomizers.Utility.InterpTools;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioConversation
    {
        private enum EActorTrackFindActorMode
        {
            ActorTrack_FindActorByTag, // level tag
            ActorTrack_UseObjectPinForActor,
            ActorTrack_FindActorByNode, //attached?
        }

        class ActorLookup
        {
            public NameReference FindActor { get; set; }
            public EActorTrackFindActorMode? FindMode { get; set; }
        }

        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioConversation";

        public static bool RandomizeExportActors(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;

            var conv = new ConversationExtended(export);
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
                        var findActor = it.Export.GetProperty<NameProperty>("m_nmFindActor");
                        if (findActor != null)
                        {
                            if (!actorsToFind.Any(x => x.FindActor.Instanced == findActor.Value.Instanced))
                            {

                                ActorLookup al = new ActorLookup() { FindActor = findActor.Value };
                                var lookupType = it.Export.GetProperty<EnumProperty>("m_eFindActorMode");
                                if (lookupType != null && Enum.TryParse<EActorTrackFindActorMode>(lookupType.Value.Name, out var result))
                                {
                                    al.FindMode = result;
                                }

                                actorsToFind.Add(al);
                            }

                            // add track
                            tracksToUpdate.Add(it);
                        }
                    }
                }
            }

            // Step 2. Build the remapping
            if (actorsToFind.Count <= 0)
                return false; // Nothing to randomize

            // Instanced name to NameReference
            Dictionary<string, ActorLookup> actorRemap = new Dictionary<string, ActorLookup>();
            var shuffledActors = actorsToFind.ToList();
            var unshuffledActors = actorsToFind.Select(x => x.FindActor.Instanced).ToList();
            shuffledActors.Shuffle();

            while (shuffledActors.Count > 0)
            {
                var sourceItem = unshuffledActors.PullFirstItem();
                var remappedItem = shuffledActors.PullFirstItem();
                actorRemap[sourceItem] = remappedItem;
            }

            // Step 3. Update all tracks with new remap
            foreach (var track in tracksToUpdate)
            {
                var props = track.Export.GetProperties();
                var findActor = props.GetProp<NameProperty>("m_nmFindActor");
                findActor.Value = actorRemap[findActor.Value.Instanced].FindActor;

                if (actorRemap[findActor.Value.Instanced].FindMode != null)
                {
                    props.AddOrReplaceProp(new EnumProperty(actorRemap[findActor.Value.Instanced].FindMode.ToString(), "EActorTrackFindActorMode", MERFileSystem.Game, "m_eFindActorMode"));
                } else
                {
                    props.RemoveNamedProperty("m_eFindActorNode");
                }
                track.Export.WriteProperties(props);
            }


            return true;
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