using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.ExportTypes
{
    /// <summary>
    /// Class for passing around Gesture-related data
    /// </summary>
    class GestureInfo
    {
        public ExportEntry GestureAnimSequence { get; set; }
        public ExportEntry GestureAnimSetData { get; set; }
        public string GestureGroup { get; set; }
        /// <summary>
        /// Name of the gesture sequence (like WI_SittingIdle)
        /// </summary>
        public string GestureName => GestureAnimSequence.ObjectName.Name.Substring(GestureGroup.Length + 1);
    }

    /// <summary>
    /// Helper class for dealing with Gestures
    /// </summary>
    class GestureManager
    {

#if __GAME3__
        public static readonly string[] RandomGesturePackages =
        {
            "HMF_AM_Towny", // Dance?
            "HMM_AM_Towny", // Dance
            "HMM_AM_ThinkingFrustration",
            "HMM_AM_Talk",
            "HMF_AM_Talk",
            "HMM_WI_Exercise", // Situp
            "HMM_AM_EatSushi", // Citadel Act I
            "HMF_AM_Party", // Citadel Party
            "HMM_AM_SurrenderPrisoner", // citadel
            "2P_EscapeToDeath", // citadel
            "2P_AM_Kitchen",
            "2P_AM_PushUps",
            "HMF_2P_GarrusShepardTango",
            "2P_BigKiss",
            "HMM_DP_SitFloorInjured",
            "HMM_AM_Possession",
            "HMM_DG_Deaths",
            "NCA_HAN_DL_AnimSet",
        };
#endif
        /// <summary>
        /// Maps a name of an animation package to the actual unreal package name it sits under in packages
        /// </summary>
        private static Dictionary<string, string> mapAnimSetOwners;
        public static void Init(GameTarget target)
        {
            MERLog.Information("Initializing GestureManager");
            // Load gesture mapping
            var gesturesFile = MERFileSystem.GetPackageFile(target, "GesturesConfigDLC.pcc");
            if (!File.Exists(gesturesFile))
            {
                gesturesFile = MERFileSystem.GetPackageFile(target, "GesturesConfig.pcc");
            }

            var gesturesPackage = MERFileSystem.OpenMEPackage(gesturesFile, preventSave: true);
            // name can change if it's dlc so we just do this
            var gestureRuntimeData = gesturesPackage.Exports.FirstOrDefault(x => x.ClassName == "BioGestureRuntimeData");
            var gestMap = ObjectBinary.From<BioGestureRuntimeData>(gestureRuntimeData);

            // Map it for strings since we don't want NameReferences.
            // Also load gestures cache
            _gesturePackageCache = new MERPackageCache(target, null, true);
            mapAnimSetOwners = new Dictionary<string, string>(gestMap.m_mapAnimSetOwners.Count);
            foreach (var v in gestMap.m_mapAnimSetOwners)
            {
                mapAnimSetOwners[v.Key] = v.Value;
                if (RandomGesturePackages.Contains(v.Key.Name))
                {
                    _gesturePackageCache.GetCachedPackageEmbedded(target.Game, $"Gestures.{v.Value.Name}.pcc"); // We don't capture the result - we just preload
                }
            }
        }

        /// <summary>
        /// Determines if the listed object name matches a key in the gesture mapping values (the result value)
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public static bool IsGestureGroupPackage(NameReference objectName)
        {
            return mapAnimSetOwners.Values.Any(x => x.Equals(objectName.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        private static MERPackageCache _gesturePackageCache;

        public static IMEPackage GetGesturePackage(string gestureGroupName)
        {
            return _gesturePackageCache.GetCachedPackage($"Gestures.{mapAnimSetOwners[gestureGroupName]}.pcc", false);
        }

        public static GestureInfo GetRandomMERGesture()
        {
            var gestureGroup = RandomGesturePackages.RandomElement();
            var randomGesturePackage = GetGesturePackage(gestureGroup);
            var candidates = randomGesturePackage.Exports.Where(x => x.ClassName == "AnimSequence" && x.ParentName == mapAnimSetOwners[gestureGroup] && x.ObjectName.Name.StartsWith(gestureGroup)).ToList();
            var randGesture = candidates.RandomElement();

            return new GestureInfo()
            {
                GestureAnimSequence = randGesture,
                GestureGroup = gestureGroup
            };
        }

        /// <summary>
        /// Generates a new BioDynamicAnimSet export under the specified parent
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parent"></param>
        /// <param name="group"></param>
        /// <param name="seq"></param>
        /// <param name="animSetData"></param>
        /// <returns></returns>
        public static ExportEntry GenerateBioDynamicAnimSet(GameTarget target, ExportEntry parent, string group, ExportEntry seq, ExportEntry animSetData)
        {
            PropertyCollection props = new PropertyCollection();
            props.Add(new NameProperty(group, "m_nmOrigSetName"));
            props.Add(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(seq) }, "Sequences"));
            props.Add(new ObjectProperty(animSetData, "m_pBioAnimSetData"));

            var rop = new RelinkerOptionsPackage() { Cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true) };
            var bioDynObj = new ExportEntry(parent.FileRef, parent, parent.FileRef.GetNextIndexedName("BioDynamicAnimSet"), properties: props)
            {
                Class = EntryImporter.EnsureClassIsInFile(parent.FileRef, "BioDynamicAnimSet", rop)
            };
            parent.FileRef.AddExport(bioDynObj);
            return bioDynObj;
        }
    }


    class ComponentActor
    {
        public const string HEAD_MESH_COMP_NAME = "HeadMesh";
        public const string GEAR_MESH_COMP_NAME = "HeadGearMesh";
        public const string HAIR_MESH_COMP_NAME = "HairMesh";
        public const string SKEL_MESH_COMP_NAME = "SkelMesh";

        /// <summary>
        /// Builds a mapping of mesh components to their exports for an actor.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        public static Dictionary<string, ExportEntry> GetComponents(ExportEntry actor)
        {
            Dictionary<string, ExportEntry> components = new Dictionary<string, ExportEntry>();
            var props = actor.GetProperties();
            AddComp(actor, props, components, "HeadMesh", HEAD_MESH_COMP_NAME);
            AddComp(actor, props, components, "HeadGearMesh", GEAR_MESH_COMP_NAME);
            AddComp(actor, props, components, "HairMesh", HAIR_MESH_COMP_NAME);
            AddComp(actor, props, components, "SkeletalMeshComponent", SKEL_MESH_COMP_NAME); // SFXSkeletalMeshActor
            // STUNT ACTOR USES BODYMESH
            return components;
        }

        /// <summary>
        /// Maps component name to export on the actor
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="props"></param>
        /// <param name="components"></param>
        /// <param name="propName"></param>
        /// <param name="keyName"></param>
        private static void AddComp(ExportEntry actor, PropertyCollection props, Dictionary<string, ExportEntry> components, string propName, string keyName)
        {
            var prop = props.GetProp<ObjectProperty>(propName);
            if (prop != null && actor.FileRef.IsUExport(prop.Value))
            {
                components[keyName] = actor.FileRef.GetUExport(prop.Value);
            }
        }
    }

    class SkelMeshComponent
    {
        public static void RandomizeGestures(ExportEntry skelMeshComp)
        {
            var props = skelMeshComp.GetProperties();
            var animNode = props.GetProp<ObjectProperty>("Animations");
            if (animNode == null)
            {
                Debug.WriteLine("NO ANIMATIONS");
                return;
            }
            var animSetData = props.GetProp<ArrayProperty<ObjectProperty>>("AnimSets");
            if (animSetData == null)
            {
                Debug.WriteLine("NO ANIMSETS");
                return;
            }

            var animNodeExp = animNode.ResolveToEntry(skelMeshComp.FileRef) as ExportEntry;
            if (animNodeExp == null)
                return;

            // Port the data
            var randGesture = GestureManager.GetRandomMERGesture();
            EntryExporter.ExportExportToPackage(randGesture.GestureAnimSequence, skelMeshComp.FileRef, out var portedRandGestureSeq, MERCaches.GlobalCommonLookupCache);
            var portedBioAnimSetDataProp = ((ExportEntry)(portedRandGestureSeq)).GetProperty<ObjectProperty>("m_pBioAnimSetData");
            //EntryExporter.ExportExportToPackage(randGesture.GestureAnimSetData, skelMeshComp.FileRef, out var portedBioAnimSetData, MERCaches.GlobalCommonLookupCache);

            // Clone it so we don't have to deal with imports to it.
            // animNodeExp = EntryCloner.CloneEntry(animNodeExp);

            animNodeExp.WriteProperty(new NameProperty(randGesture.GestureName, "AnimSeqName")); // Update the AnimSeqName
            var animSetDyn = animSetData[0].ResolveToEntry(skelMeshComp.FileRef) as ExportEntry;
            PropertyCollection aprops = new PropertyCollection();
            aprops.Add(new NameProperty(randGesture.GestureGroup, "m_nmOrigSetName"));
            aprops.Add(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(portedRandGestureSeq) }, "Sequences"));
            aprops.Add(portedBioAnimSetDataProp);
            animSetDyn.WriteProperties(aprops);
            var objBin = ObjectBinary.From<BioDynamicAnimSet>(animSetDyn);
            objBin.SequenceNamesToUnkMap[randGesture.GestureName] = 0; // Index 0
            animSetDyn.WriteBinary(objBin);
        }

        public static void ValidateGestures(ExportEntry skelMeshComp)
        {
            var props = skelMeshComp.GetProperties();
            var animNode = props.GetProp<ObjectProperty>("Animations");
            if (animNode == null)
            {
                Debug.WriteLine("NO ANIMATIONS");
                return;
            }

            var animNodeExp = animNode.ResolveToEntry(skelMeshComp.FileRef) as ExportEntry;
            if (animNodeExp == null)
                return;

            var animSeqName = animNodeExp.GetProperty<NameProperty>("AnimSeqName");

            // Anim sets contain:
            // The base name of the animation package (e.g. HMM_WI_StandTyping)
            // A list of sequences which are the actual animation data
            // For an animation to work, the following must be true:
            // base name of animation package 
            var animSets = props.GetProp<ArrayProperty<ObjectProperty>>("Animations");
            foreach (var animset in animSets)
            {
                var bioDynAnimSet = animset.ResolveToEntry(skelMeshComp.FileRef) as ExportEntry;
                var origSetName = bioDynAnimSet.GetProperty<NameProperty>("m_nOrigSetName");
                var sequences = bioDynAnimSet.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                foreach (var seq in sequences)
                {

                }
            }
        }
    }

    class RSFXSkeletalMeshActor
    {
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && !exp.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject) && exp.ClassName == "SFXSkeletalMeshActor";
        //&& !exp.ObjectName.Name.Contains("Dead", StringComparison.InvariantCultureIgnoreCase);
        private static string[] smaKeywords = new[] { "Dancing", "Dance", "Angry", "Cursing", "Fearful", "ROM", "Drunk", "Kiss", "Headbutt", "Hugging", "Consoling", "Come_Here", "Cough", "Count", "Bhand_Slapped" };

        public static bool RandomizeBasicGestures(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var components = ComponentActor.GetComponents(export);
            if (!components.TryGetValue(ComponentActor.SKEL_MESH_COMP_NAME, out var skelMeshComp))
                return false;

            SkelMeshComponent.RandomizeGestures(skelMeshComp);

            return true;
        }

        /*
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
                    var installedGestures = new List<Gesture>();
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

        private static void InstallDynamicAnimSetRefForSkeletalMesh(ExportEntry export, Gesture gesture)
        {
            // We have parent sequence data
            var skmDynamicAnimSets = export.GetProperty<ArrayProperty<ObjectProperty>>("AnimSets") ?? new ArrayProperty<ObjectProperty>("AnimSets");

            // Check to see if there is any item that uses our bioanimset
            var bioAnimSet = gesture.GetBioAnimSet(export.FileRef, Game2Gestures.GestureSetNameToPackageExportName);
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
        }*/
    }
}