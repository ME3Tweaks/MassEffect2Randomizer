using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game1.Misc
{

    class RBioPawnGestures
    {


        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject) && exp.ClassName == "SFXSkeletalMeshActorMAT" && !exp.ObjectName.Name.Contains("Dead", StringComparison.InvariantCultureIgnoreCase);
        private static string[] smaKeywords = new[] { "Dancing", "Dance", "Angry", "Cursing", "Fearful", "ROM", "Drunk", "Kiss", "Headbutt", "Hugging", "Consoling", "Come_Here", "Cough", "Count", "Bhand_Slapped" };


        public static bool RandomizeBasicGestures(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            if (export.GetProperty<ObjectProperty>("SkeletalMeshComponent")?.ResolveToEntry(export.FileRef) is ExportEntry smc)
            {
                //Debug.WriteLine($"Installing new lite animations for {export.InstancedFullPath}");
                var animsets = smc.GetProperty<ArrayProperty<ObjectProperty>>("AnimSets");
                var animTreeTemplate = smc.GetProperty<ObjectProperty>("AnimTreeTemplate")?.ResolveToEntry(export.FileRef) as ExportEntry;
                if (animsets != null && animTreeTemplate != null)
                {
                    int numAnimationsSupported = 0;
                    foreach (var animsetO in animsets)
                    {
                        var animset = animsetO.ResolveToEntry(export.FileRef) as ExportEntry;
                        var sequences = animset.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                        numAnimationsSupported += sequences.Count;
                    }

                    smc.RemoveProperty("AnimSets"); // We want to force new animations. we'll waste a bit of memory doing this but oh well
                    List<RBioEvtSysTrackGesture.Gesture> installedGestures = new List<RBioEvtSysTrackGesture.Gesture>();
                    var animationPackagesCache = new MERPackageCache(target);
                    while (numAnimationsSupported > 0)
                    {
                        // should we make sure they're unique?
                        var randGest = RBioEvtSysTrackGesture.InstallRandomFilteredGestureAsset(target, export.FileRef, 2, filterKeywords: smaKeywords, blacklistedKeywords: null, mainPackagesAllowed: null, includeSpecial: true);
                        InstallDynamicAnimSetRefForSkeletalMesh(smc, randGest);
                        installedGestures.Add(randGest);
                        numAnimationsSupported--;
                    }
                    animationPackagesCache.ReleasePackages();
                    var isSubfile = PackageTools.IsLevelSubfile(Path.GetFileName(export.FileRef.FilePath));
                    if (isSubfile)
                    {
                        var newName = export.ObjectName + "_MER";
                        export.ObjectName = new NameReference(newName, ThreadSafeRandom.Next(25685462));
                    }

                    // Update the anim tree to use the new animations
                    // Too lazy to properly trace to find nodes. Just take children of this node that are AnimNodeSequences

                    // Add blend times to nodes so they 'blend' together a bit more, look a bit less jank
                    SetupChildrenBlend(animTreeTemplate);


                    // If the animtree has 'DebugPostLoad' flag, it means MER already is using this for something else
                    // We need to generate a new tree so the animations work properly
                    if (animTreeTemplate.ObjectFlags.Has(UnrealFlags.EObjectFlags.DebugPostLoad))
                    {
                        animTreeTemplate = EntryCloner.CloneTree(animTreeTemplate, true);
                        animTreeTemplate.ObjectName = export.FileRef.GetNextIndexedName("MER_AnimTree"); // New name
                        smc.WriteProperty(new ObjectProperty(animTreeTemplate, "AnimTreeTemplate")); // Write the template back
                    }
                    else if (isSubfile)
                    {
                        // if it's a subfile it won't be used as an import
                        // Let's rename this object
                        animTreeTemplate.ObjectName = new NameReference("MER_AnimTree", ThreadSafeRandom.Next(200000000)); // New name
                    }


                    var animNodeSequences = export.FileRef.Exports.Where(x => x.idxLink == animTreeTemplate.UIndex && x.IsA("AnimNodeSequence")).ToList();
                    for (int i = 0; i < installedGestures.Count; i++)
                    {
                        var installedG = installedGestures[i];
                        var ans = animNodeSequences[i];
                        ans.WriteProperty(new NameProperty(installedG.GestureAnim, "AnimSeqName"));
                    }
                    animTreeTemplate.ObjectFlags |= UnrealFlags.EObjectFlags.DebugPostLoad; // Set as used
                    return true;
                }
            }
            return false;
        }

        private static void SetupChildrenBlend(ExportEntry export)
        {
            // Drill to children
            var childrenX = export.GetProperty<ArrayProperty<StructProperty>>("Children");
            if (childrenX != null)
            {
                foreach (var c in childrenX)
                {
                    var anim = c.GetProp<ObjectProperty>("Anim").ResolveToEntry(export.FileRef) as ExportEntry;
                    if (anim != null)
                    {
                        SetupChildrenBlend(anim);
                    }
                }
            }

            // Update blends
            var randInfo = export.GetProperty<ArrayProperty<StructProperty>>("RandomInfo");
            if (randInfo != null)
            {
                foreach (var ri in randInfo)
                {
                    ri.Properties.AddOrReplaceProp(new FloatProperty(3, "BlendInTime"));
                }
                export.WriteProperty(randInfo);
            }
        }

        private static void InstallDynamicAnimSetRefForSkeletalMesh(ExportEntry export, RBioEvtSysTrackGesture.Gesture gesture)
        {
            // We have parent sequence data
            var skmDynamicAnimSets = export.GetProperty<ArrayProperty<ObjectProperty>>("AnimSets") ?? new ArrayProperty<ObjectProperty>("AnimSets");

            // Check to see if there is any item that uses our bioanimset
            var bioAnimSet = gesture.GetBioAnimSet(export.FileRef);
            if (bioAnimSet != null)
            {
                ExportEntry skmBioDynamicAnimSet = null;
                foreach (var skmDynAnimSet in skmDynamicAnimSets)
                {
                    var kEntry = skmDynAnimSet.ResolveToEntry(export.FileRef) as ExportEntry; // I don't think these can be imports as they're part of the seq
                    var associatedset = kEntry.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(export.FileRef);
                    if (associatedset == bioAnimSet)
                    {
                        // It's this one
                        skmBioDynamicAnimSet = kEntry;
                        break;
                    }
                }

                if (skmBioDynamicAnimSet == null)
                {
                    // We need to generate a new one
                    PropertyCollection props = new PropertyCollection();
                    props.Add(new NameProperty(gesture.GestureSet, "m_nmOrigSetName"));
                    props.Add(new ArrayProperty<ObjectProperty>("Sequences"));
                    props.Add(new ObjectProperty(bioAnimSet, "m_pBioAnimSetData"));
                    skmBioDynamicAnimSet = ExportCreator.CreateExport(export.FileRef, $"BioDynamicAnimSet", "BioDynamicAnimSet", export);

                    // Write a blank count of 0 - we will update this in subsequent call
                    // This must be here to ensure parser can read it
                    skmBioDynamicAnimSet.WritePropertiesAndBinary(props, new byte[4]);
                    skmDynamicAnimSets.Add(new ObjectProperty(skmBioDynamicAnimSet)); // Add new export to sequence's list of biodynamicanimsets
                    export.WriteProperty(skmDynamicAnimSets);
                }

                var currentObjs = skmBioDynamicAnimSet.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                if (currentObjs.All(x => x.Value != gesture.Entry.UIndex))
                {
                    // We need to add our item to it
                    currentObjs.Add(new ObjectProperty(gesture.Entry));
                    var bin = ObjectBinary.From<BioDynamicAnimSet>(skmBioDynamicAnimSet);
                    bin.SequenceNamesToUnkMap[gesture.GestureAnim] = 1; // Not sure what the value should be, or if game actually reads this
                                                                        // FIX IT IF WE EVER FIGURE IT OUT!
                    skmBioDynamicAnimSet.WriteProperty(currentObjs);
                    skmBioDynamicAnimSet.WriteBinary(bin);
                }
            }
        }
    }

}
