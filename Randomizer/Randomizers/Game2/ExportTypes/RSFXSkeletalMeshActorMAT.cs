using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.Misc;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    class RSFXSkeletalMeshActorMAT
    {
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject &&
                                                             exp.ClassName == "SFXSkeletalMeshActorMAT" &&
                                                             !exp.ObjectName.Name.Contains("Dead",
                                                                 StringComparison.InvariantCultureIgnoreCase);

        public static bool RandomizeBasicGestures(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            if (export.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject))
            {
                return RandomizeArchetype(target, export);
            }
            else
            {
                return RandomizeInstance(export);
            }

            return false;
        }

        private static bool RandomizeInstance(ExportEntry export)
        {
            if (export.Archetype is ExportEntry archetype &&
                archetype.ObjectFlags.HasFlag(UnrealFlags.EObjectFlags.DebugPostLoad))
            {
                var skmc = export.GetProperty<ObjectProperty>(@"SkeletalMeshComponent").ResolveToEntry(export.FileRef) as ExportEntry;
                skmc?.RemoveProperty("AnimSets"); // Ensure AnimSets is not populated
                skmc?.RemoveProperty("AnimTreeTemplate"); // Ensure AnimTreeTemplate is not populated
            }

            return true;
        }

        private static bool RandomizeArchetype(GameTarget target, ExportEntry export)
        {
            if (export.GetProperty<ObjectProperty>("SkeletalMeshComponent")?.ResolveToEntry(export.FileRef) is
                ExportEntry smc)
            {
                //Debug.WriteLine($"Installing new lite animations for {export.InstancedFullPath}");
                var animsets = smc.GetProperty<ArrayProperty<ObjectProperty>>("AnimSets");
                var animTreeTemplate =
                    smc.GetProperty<ObjectProperty>("AnimTreeTemplate")?.ResolveToEntry(export.FileRef) as ExportEntry;
                if (animsets != null && animTreeTemplate != null)
                {
                    int numAnimationsSupported = 0;
                    // Count the total number of animations this litepawn supported
                    // before we modify it - look at the animsets and count the number of 
                    // sequences in each animset and total them up
                    foreach (var animsetO in animsets)
                    {
                        var animset = animsetO.ResolveToEntry(export.FileRef) as ExportEntry;
                        var sequences = animset.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                        numAnimationsSupported += sequences.Count;
                    }

                    smc.RemoveProperty(
                        "AnimSets"); // We want to force new animations. we'll waste a bit of memory doing this but oh well

                    var installedGestures = new List<Gesture>();
                    var animationPackagesCache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true);
                    while (numAnimationsSupported > 0)
                    {
                        // should we make sure they're unique?

                        // Install gesture
                        var randGest = GestureManager.InstallRandomFilteredGestureAsset(target, export.FileRef, 2,
                            filterKeywords: null, blacklistedKeywords: null, mainPackagesAllowed: null,
                            includeSpecial: true);
                        GestureManager.InstallDynamicAnimSetRefForSkeletalMesh(smc, randGest);
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
                        smc.WriteProperty(new ObjectProperty(animTreeTemplate,
                            "AnimTreeTemplate")); // Write the template back
                    }
                    else if (isSubfile)
                    {
                        // if it's a subfile it won't be used as an import
                        // Let's rename this object
                        animTreeTemplate.ObjectName =
                            new NameReference("MER_AnimTree", ThreadSafeRandom.Next(200000000)); // New name
                    }


                    var animNodeSequences = export.FileRef.Exports
                        .Where(x => x.idxLink == animTreeTemplate.UIndex && x.IsA("AnimNodeSequence")).ToList();
                    for (int i = 0; i < installedGestures.Count; i++)
                    {
                        var installedG = installedGestures[i];
                        var ans = animNodeSequences[i];
                        ans.WriteProperty(new NameProperty(installedG.GestureAnim, "AnimSeqName"));
                    }

                    animTreeTemplate.ObjectFlags |= UnrealFlags.EObjectFlags.DebugPostLoad; // Set as used
                    export.ObjectFlags |= UnrealFlags.EObjectFlags.DebugPostLoad; // Set as modified - used for instances
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
    }
}