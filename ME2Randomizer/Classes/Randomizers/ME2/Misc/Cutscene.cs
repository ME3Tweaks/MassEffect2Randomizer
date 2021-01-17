using MassEffectRandomizer.Classes;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class Cutscene
    {
        private static List<string> acceptableTagsForPawnShuffling;

        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == "SeqAct_Interp";
        public static bool ShuffleCutscenePawns(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
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
                    Log.Information("Randomizing pawns in interp: " + export.FullPath);
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
            acceptableTagsForPawnShuffling = Utilities.GetEmbeddedStaticFilesTextFile("allowedcutscenerandomizationtags.txt").Split('\n').ToList();
        }
    }
}
