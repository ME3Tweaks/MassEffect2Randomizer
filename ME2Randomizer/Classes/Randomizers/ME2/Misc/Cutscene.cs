using MassEffectRandomizer.Classes;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME2Randomizer.Classes.Randomizers.Utility;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class Cutscene
    {

        private static List<string> acceptableTagsForPawnShuffling;

        private static bool CanRandomize(ExportEntry export, out string cutsceneName)
        {
            cutsceneName = null;
            if (!export.IsDefaultObject && export.ClassName == "SeqAct_Interp" && export.GetProperty<StrProperty>("ObjName") is { } strp && strp.Value.StartsWith("ANIMCUTSCENE_"))
            {
                cutsceneName = strp;
                return true;
            }
            return false;
        }

        private static string[] HenchObjComments = new[]
        {
            "assassin",
            "leading",
            "convict",
            "mystic",
            "geth",
            "grunt",
            "garrus",
            "professor",
            "tali",
            "vixen"
        };


        public static bool ShuffleCutscenePawns2(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export, out var cutsceneName)) return false;
            if (acceptableTagsForPawnShuffling == null) LoadAsset();
            var variableLinks = SeqTools.GetVariableLinksOfNode(export);

            // Entries that can be shuffled.
            // This list must not to have items removed!
            List<ExportEntry> pawnsToShuffleDirectAttached = new List<ExportEntry>();
            List<ExportEntry> pawnsToShuffleDynamicSet = new List<ExportEntry>();

            var sequenceElements = SeqTools.GetAllSequenceElements(export).OfType<ExportEntry>().ToList(); ;

            foreach (var vl in variableLinks)
            {
                if (!BlackListedLinkDesc(vl) && (vl.ExpectedTypeName == "SeqVar_Object" || vl.ExpectedTypeName == "SeqVar_Player" || vl.ExpectedTypeName == "BioSeqVar_ObjectFindByTag"))
                {
                    // It's a canidate for randomization

                    // Some ObjectFindByTag have an attached ObjValue for some reason.
                    // It's findobjectbytag but in same file
                    // some leftover development thing
                    foreach (var variableLinkNode in vl.LinkedNodes.OfType<ExportEntry>())
                    {
                        bool addedToShuffler = false;
                        if (variableLinkNode.GetProperty<ObjectProperty>("ObjValue")?.ResolveToEntry(export.FileRef) is ExportEntry linkedObjectEntry)
                        {
                            // It's a SeqVar_Object with a linked param
                            if (linkedObjectEntry.IsA("BioPawn"))
                            {
                                // This might be leftover from ME1
                                var flyingpawn = linkedObjectEntry.GetProperty<BoolProperty>("bCanFly")?.Value;
                                if (flyingpawn == null || flyingpawn == false)
                                {
                                    pawnsToShuffleDirectAttached.Add(variableLinkNode); //can be shuffled. This is a var link so it must be added
                                    addedToShuffler = true;
                                }
                            }
                        }
                        // It's not a directly set object
                        // WORK ON CODE BELOW
                        else if (vl.ExpectedTypeName == "SeqVar_Object" && !addedToShuffler)
                        {
                            // Find what assigns
                            if (MERSeqTools.IsAssignedBioPawn(export, variableLinkNode, sequenceElements))
                            {
                                pawnsToShuffleDirectAttached.Add(variableLinkNode);
                                addedToShuffler = true;
                            }
                        }



                        if (!addedToShuffler)
                        {
                            // Check if is SeqVar_Player. We can always shuffle this one
                            string className = variableLinkNode.ClassName;
                            if (className == "SeqVar_Player")
                            {
                                variableLinkNode.WriteProperty(new BoolProperty(true, "bReturnsPawns")); // This is in ME1R, not sure it's needed, but let's just make sure it's true
                                pawnsToShuffleDirectAttached.Add(variableLinkNode); //pointer to this node
                            }
                            else if (className == "BioSeqVar_ObjectFindByTag")
                            {
                                var tagToFind = variableLinkNode.GetProperty<StrProperty>("m_sObjectTagToFind")?.Value;
                                //if (tagToFind == "hench_pilot")
                                //    Debugger.Break();
                                if (tagToFind != null && acceptableTagsForPawnShuffling.Contains(tagToFind))
                                {
                                    pawnsToShuffleDynamicSet.Add(variableLinkNode); //pointer to this node
                                }
                                else
                                {
                                    //Debug.WriteLine($"Cannot shuffle tag: {tagToFind}");
                                }
                            }
                        }

                    }
                }
            }

            var pawnsToShuffle = pawnsToShuffleDirectAttached;
            // If it's a dynamic object we only want to add it if it's not already attached somewhere else
            // This can occur if the value to set is also the value used directly
            foreach (var dp in pawnsToShuffleDynamicSet)
            {
                //if (!pawnsToShuffle.Contains(dp))
                //{
                pawnsToShuffle.Add(dp);
                //}
            }

            if (pawnsToShuffle.Count > 1)
            {
                // Now we have a list of all exports that can be shuffled
                var shufflerList = pawnsToShuffle.ToList();
                shufflerList.Shuffle();

                // Now we go through the list of var links and look if the linked node is in the list of pawnsToShuffle.
                // If it is we pull one off the shufflerList and replace the value with that one instead

                foreach (var vl in variableLinks)
                {
                    for (int i = 0; i < vl.LinkedNodes.Count; i++)
                    {
                        var existingItem = vl.LinkedNodes[i];
                        if (pawnsToShuffle.Contains(existingItem))
                        {
                            var newItem = shufflerList.PullFirstItem();

                            if (newItem == existingItem)
                            {
                                var prepickCount = shufflerList.Count;
                                newItem = AttemptRepick(shufflerList, newItem, 4, pawnsToShuffle);
                                var postpickCount = shufflerList.Count;
                                if (prepickCount != postpickCount)
                                {
                                    // Should not be any count change as we already drew an item
                                    Debugger.Break();
                                }
                            }

                            vl.LinkedNodes[i] = newItem;
                        }
                    }
                }

                // The linked nodes are now randomized
                // Write out the values

                if (shufflerList.Count != 0)
                {
                    // This can occur in sequences that have weird duplicates
                    //Debugger.Break();
                }

                SeqTools.WriteVariableLinksToNode(export, variableLinks);
                Debug.WriteLine($"Randomized {pawnsToShuffle.Count} links in animcutscene in {cutsceneName}, file {Path.GetFileName(export.FileRef.FilePath)}");
                return true;
            }

            return false;
        }

        private static ExportEntry AttemptRepick(List<ExportEntry> shufflerList, ExportEntry triedItem, int numAttempts, List<ExportEntry> failoverList)
        {
            if (shufflerList.Count == 0)
                return triedItem; // We cannot re-attempt as we will just draw the same item over and over again

            shufflerList.Add(triedItem); // Put back the tried item
            while (numAttempts > 0)
            {
                var newItem = shufflerList.PullFirstItem();
                if (newItem != triedItem)
                    return newItem;
                // Pulled item was the same as the original item!
                // Put item back into the list of options
                numAttempts--;
                shufflerList.Add(newItem); //Put the item we pulled back into the list
            }

            return shufflerList.PullFirstItem(); // Could not find something else. Just take the first item
        }

        private static bool BlackListedLinkDesc(SeqTools.VarLinkInfo vl)
        {
            if (vl == null) return true;
            if (vl.LinkDesc.StartsWith("line1")) return true; // Used in EndGm2 410 Hold The Line Huddle3 Intro to assign voice lines
            return false;
        }

        /// <summary>
        /// Shuffler from ME1 Randomizer
        /// </summary>
        /// <param name="export"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool ShuffleCutscenePawns(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export, out var cutsceneName)) return false;
            if (acceptableTagsForPawnShuffling == null) LoadAsset();
            var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");

            List<ObjectProperty> pawnsToShuffle = new List<ObjectProperty>();
            var playerRefs = new List<ExportEntry>();
            foreach (var variableLink in variableLinks)
            {
                var expectedType = variableLink.GetProp<ObjectProperty>("ExpectedType");
                var expectedTypeStr = export.FileRef.GetEntry(expectedType.Value).ObjectName;
                var DEBUG = variableLink.GetProp<StrProperty>("LinkDesc");
                if (expectedTypeStr == "SeqVar_Object" || expectedTypeStr == "SeqVar_Player" || expectedTypeStr == "BioSeqVar_ObjectFindByTag")
                {
                    //Investigate the links
                    var linkedVariables = variableLink.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                    foreach (var objRef in linkedVariables)
                    {
                        var linkedObj = export.FileRef.GetUExport(objRef.Value).GetProperty<ObjectProperty>("ObjValue");
                        if (linkedObj != null)
                        {
                            //This is the data the node is referencing
                            var linkedObjectEntry = export.FileRef.GetEntry(linkedObj.Value);
                            var linkedObjName = linkedObjectEntry.ObjectName;
                            if (linkedObjName == "BioPawn" && linkedObjectEntry is ExportEntry bioPawnExport)
                            {
                                var flyingpawn = bioPawnExport.GetProperty<BoolProperty>("bCanFly")?.Value;
                                if (flyingpawn == null || flyingpawn == false)
                                {
                                    pawnsToShuffle.Add(objRef); //pointer to this node
                                }
                            }
                        }
                        else if (expectedTypeStr == "SeqVar_Object")
                        {
                            //We might be assigned to. We need to look at the parent sequence
                            //and find what assigns me
                            var node = export.FileRef.GetUExport(objRef.Value);
                            var parentRef = node.GetProperty<ObjectProperty>("ParentSequence");
                            if (parentRef != null)
                            {
                                var parent = export.FileRef.GetUExport(parentRef.Value);
                                var sequenceObjects = parent.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects");
                                if (sequenceObjects != null)
                                {
                                    foreach (var obj in sequenceObjects)
                                    {
                                        if (obj.Value <= 0) continue;
                                        var sequenceObject = export.FileRef.GetUExport(obj.Value);
                                        if (sequenceObject.InheritsFrom("SequenceAction") && sequenceObject.ClassName == "SeqAct_SetObject" && sequenceObject != export)
                                        {
                                            //check if target is my node
                                            var varlinqs = sequenceObject.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
                                            if (varlinqs != null)
                                            {
                                                var targetLink = varlinqs.FirstOrDefault(x =>
                                                {
                                                    var linkdesc = x.GetProp<StrProperty>("LinkDesc");
                                                    return linkdesc != null && linkdesc == "Target";
                                                });
                                                var targetLinkedVariables = targetLink?.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                                                if (targetLinkedVariables != null)
                                                {
                                                    //see if target is node we are investigating for setting.
                                                    foreach (var targetLinkedVariable in targetLinkedVariables)
                                                    {
                                                        var potentialTarget = export.FileRef.GetUExport(targetLinkedVariable.Value);
                                                        if (potentialTarget == node)
                                                        {
                                                            Debug.WriteLine("FOUND TARGET!");
                                                            //See what value this is set to. If it inherits from BioPawn we can use it in the shuffling.
                                                            var valueLink = varlinqs.FirstOrDefault(x =>
                                                            {
                                                                var linkdesc = x.GetProp<StrProperty>("LinkDesc");
                                                                return linkdesc != null && linkdesc == "Value";
                                                            });
                                                            var valueLinkedVariables = valueLink?.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                                                            if (valueLinkedVariables != null && valueLinkedVariables.Count == 1)
                                                            {
                                                                var linkedNode = export.FileRef.GetUExport(valueLinkedVariables[0].Value);
                                                                var linkedNodeType = linkedNode.GetProperty<ObjectProperty>("ObjValue");
                                                                if (linkedNodeType != null)
                                                                {
                                                                    var linkedNodeData = export.FileRef.GetUExport(linkedNodeType.Value);
                                                                    if (linkedNodeData.InheritsFrom("BioPawn"))
                                                                    {
                                                                        //We can shuffle this item.
                                                                        Debug.WriteLine("Adding shuffle item: " + objRef.Value);
                                                                        pawnsToShuffle.Add(objRef); //pointer to this node
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        string className = export.FileRef.GetUExport(objRef.Value).ClassName;
                        if (className == "SeqVar_Player")
                        {
                            playerRefs.Add(export.FileRef.GetUExport(objRef.Value));
                            pawnsToShuffle.Add(objRef); //pointer to this node
                        }
                        else if (className == "BioSeqVar_ObjectFindByTag")
                        {
                            var tagToFind = export.FileRef.GetUExport(objRef.Value).GetProperty<StrProperty>("m_sObjectTagToFind")?.Value;
                            if (tagToFind != null && acceptableTagsForPawnShuffling.Contains(tagToFind))
                            {
                                pawnsToShuffle.Add(objRef); //pointer to this node
                            }
                        }
                    }
                }
            }

            if (pawnsToShuffle.Count > 1)
            {
                int reshuffleAttemptsRemaining = 3;
                while (reshuffleAttemptsRemaining > 0)
                {
                    reshuffleAttemptsRemaining--;
                    MERLog.Information("Randomizing pawns in interp: " + export.FullPath);
                    foreach (var refx in playerRefs)
                    {
                        refx.WriteProperty(new BoolProperty(true, "bReturnsPawns")); //Ensure the object returns pawns. It should, but maybe it doesn't.
                    }

                    var newAssignedValues = pawnsToShuffle.Select(x => x.Value).ToList();
                    newAssignedValues.Shuffle();
                    for (int i = 0; i < pawnsToShuffle.Count; i++)
                    {
                        pawnsToShuffle[i].Value = newAssignedValues[i];
                    }

                    export.WriteProperty(variableLinks);
                    if (export.EntryHasPendingChanges)
                    {
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        private static void LoadAsset()
        {
            acceptableTagsForPawnShuffling = MERUtilities.GetEmbeddedStaticFilesTextFile("allowedcutscenerandomizationtags.txt").Split(new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
    }
}
