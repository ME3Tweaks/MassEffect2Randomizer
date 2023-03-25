using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game3.Framework
{
    internal class RNPC
    {
        public static bool RandomizeNPCs(GameTarget target, RandomizationOption option)
        {
            var npcFolder = Path.Combine(M3Directories.GetDLCPath(target), "DLC_MOD_Framework", "CookedPCConsole", "NPC");
            var vanillaPackageToTagNameMap = new Dictionary<string, NameReference>();
            var vanillaTagNameToPackage = new Dictionary<NameReference, string>();
            if (Directory.Exists(npcFolder))
            {
                // Step 1: Inventory all packages to build tag map
                var npcFiles = Directory.GetFiles(npcFolder, "*.pcc", SearchOption.TopDirectoryOnly);
                option.ProgressIndeterminate = false;
                option.ProgressValue = 0;
                option.ProgressMax = npcFiles.Length;
                option.CurrentOperation = "Randomizing NPCs (Inventory)";
                foreach (var npcFile in npcFiles.Where(x=>x.GetUnrealLocalization() == MELocalization.None))
                {
                    var packageName = Path.GetFileName(npcFile);
                    var npcPackagePath = MERFileSystem.GetPackageFile(target, packageName, false);
                    using var package = MEPackageHandler.UnsafePartialLoad(npcPackagePath, x => x.ClassName == "SFXStuntActor" || x.ClassName == "Level" 
                                                                                                                               || x.IsA("SFXPawn") || x.InheritsFrom("SFXPawn")); // Pawns will reference instances and classes

                    List<ExportEntry> randomizableActors = new List<ExportEntry>();
                    var actors = ObjectBinary.From<Level>(package.FindExport("TheWorld.PersistentLevel")).Actors;
                    foreach (var actor in actors.Where(x => x > 0).Select(x => package.GetUExport(x)))
                    {
                        if (actor.ClassName == "BioWorldInfo") continue;
                        if (actor.ClassName == "BlockingVolume") continue;
                        randomizableActors.Add(actor);
                    }

                    // Currently we only support 1 swap
                    if (randomizableActors.Count == 1)
                    {
                        if (randomizableActors[0].ClassName != "SFXStuntActor" && !randomizableActors[0].IsA("SFXPawn"))
                            Debugger.Break();
                        // var props = randomizableActors[0].GetProperties();
                        var tag = randomizableActors[0].GetProperty<NameProperty>("Tag");

                        var archetype = randomizableActors[0].Archetype as ExportEntry;
                        while (tag == null)
                        {
                            // It's probably on the archetype...
                            tag = archetype.GetProperty<NameProperty>("Tag");
                            archetype = archetype.Archetype as ExportEntry;
                            if (tag == null)
                            {
                                // Might be an SFXPawn?
                                tag = (archetype.Class as ExportEntry).GetDefaults().GetProperty<NameProperty>("Tag");
                            }
                        }

                        vanillaPackageToTagNameMap[packageName] = tag.Value;
                        vanillaTagNameToPackage[tag.Value] = packageName;
                    }
                    option.ProgressValue++;
                }


                // Step 2: Randomize the package mapping
                option.ProgressValue = 0;
                option.ProgressMax = vanillaPackageToTagNameMap.Count;
                option.CurrentOperation = "Randomizing NPCs";

                Dictionary<string, NameReference> newMapping = new Dictionary<string, NameReference>();
                var packages = vanillaPackageToTagNameMap.Keys.ToList();
                var tags = vanillaPackageToTagNameMap.Values.ToList();

                tags.Shuffle();
                var packageCount = packages.Count;
                for (int i = 0; i < packageCount; i++)
                {
                    newMapping[packages.PullFirstItem()] = tags.PullFirstItem();
                }

                // Step 3: Rename packages to temporary names
                //foreach (var v in npcFiles)
                //{
                //    var newName = Path.GetFileNameWithoutExtension(v) + "_TMP.pcc";
                //    File.Move(v, Path.Combine(npcFolder, newName));
                //}

                // Step 4: Rename packages to final names
                foreach (var originalFilename in vanillaPackageToTagNameMap.Keys)
                {
                    var newTag = newMapping[originalFilename];
                    var destName = vanillaTagNameToPackage[newTag];

                    //var tempName = Path.GetFileNameWithoutExtension(originalFilename) + "_TMP.pcc"; // The original filename + _TMP.pcc. This is the file being renamed
                    //File.Move(Path.Combine(npcFolder, tempName), Path.Combine(npcFolder, destName));

                    // Step 5: Update the tag
                    var newPackageF = MERFileSystem.GetPackageFile(target, originalFilename, false);
                    var newPackage = MEPackageHandler.OpenMEPackage(newPackageF);
                    var stuntActor = newPackage.Exports.FirstOrDefault(x => x.ClassName == "SFXStuntActor" && x.Parent.InstancedFullPath == "TheWorld.PersistentLevel");
                    if (stuntActor == null)
                        stuntActor  = newPackage.Exports.FirstOrDefault(x => x.IsA("SFXPawn") && x.Parent.InstancedFullPath == "TheWorld.PersistentLevel");
                    stuntActor.WriteProperty(new NameProperty(newTag, "Tag"));

                    // Update the sequencing
                    var mainSeq = newPackage.FindExport("TheWorld.PersistentLevel.Main_Sequence");
                    var seqObjs = KismetHelper.GetSequenceObjects(mainSeq).OfType<ExportEntry>().ToList();
                    foreach (var seqObj in seqObjs)
                    {
                        var eventName = Path.GetFileNameWithoutExtension(destName).Substring(3); //Get the new event name - ignoring 'Bio'
                        if (seqObj.ClassName == "SeqEvent_RemoteEvent")
                        {
                            seqObj.WriteProperty(new NameProperty("Poll_" + eventName, "EventName"));
                        }
                        else if (seqObj.ClassName == "SeqAct_ActivateRemoteEvent")
                        {
                            seqObj.WriteProperty(new NameProperty("Live_" + eventName, "EventName"));
                        }
                    }

                    MERLog.Information($@"Updated tag: {destName} => {newTag}");
                    destName = Path.GetFileNameWithoutExtension(destName) + "_TMP.pcc"; // This prevents MERFS interference
                    MERFileSystem.SavePackage(newPackage, forcedFileName: destName);
                    option.ProgressValue++;
                }

                // Strip _TMP
                var filesToFix = Directory.GetFiles(MERFileSystem.DLCModCookedPath, "*_TMP.pcc", SearchOption.AllDirectories);
                foreach (var f in filesToFix)
                {
                    var newDest = Path.Combine(MERFileSystem.DLCModCookedPath, f.Substring(0, f.Length - 8) + ".pcc");
                    File.Move(f, newDest);
                }


                return true;
            }
            else
            {
                MERLog.Warning(@"LE3 Framework not found, skipping NPC randomizer");
                return false;
            }
        }
    }
}
