using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;
using WinCopies.Util;

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
            // KID ANIMATION
            "HMC_AM_Scared",
            "HMC_AM_MoveStartStop",
            "HMC_DP_Crawl",
            "HMC_AM_StandingDefault",

            // CHOKE FIGHT
            "HMF_2P_Choke",
            "HMF_2P_GarrusRomance",
            "HMF_2P_Main", // has kiss

            // "HMF_2P_PHT_SynchMelee", // NOT IN GESTURES

            "HMF_DP_BackAgainstWall",
            "HMF_DP_ArmsCrossed",
            "HMF_DP_BarActions",
            "HMF_DP_HandsFace",

            "HMF_FC_Communicator",
            "HMF_FC_Custom",

            // "HMM_2P_BAN_Synch",
            // "HMM_2P_ATA_Synch",
            "HMM_2P_ChokeLift",
            "HMM_2P_Choking",
            "HMM_2P_Comraderie",
            "HMM_2P_Consoling",
            "HMM_2P_Conspire",
            "HMM_2P_End002",
            "HMM_2P_ForcedExit",
            "HMM_2P_Grab",

            "HMM_2P_GunInterrupt",
            "HMM_2P_Handshake",
            "HMM_2P_HeadButt",
            "HMM_2P_HoldingHands",
            "HMM_2P_Hostage",
            "HMM_2P_InjuredAgainstWall",
            "HMM_2P_KaiLengDeath",
            "HMM_2P_KissCheek",
            "HMM_2P_KissMale",
            "HMM_2P_LiftPillar",
            "HMM_2P_Main",
            // "HMM_2P_PHT_SyncMelee",
            "HMM_2P_PinAgainstWall",
            "HMM_2P_PunchInterrupt",
            "HMM_2P_ThaneKaiLengFight",
            //"HMM_AM_BeckonPistol",
            //"HMM_AM_BeckonRifle",
            "HMM_AM_Biotic",
            "HMM_AM_Environmental",
            "HMM_AM_Gamble",
            "HMM_AM_HandsClap",
            "HMM_DG_Deaths",
            "HMM_DG_Exploration",
            "HMM_DL_Decline",
            "HMM_DL_ElusiveMan",
            "HMM_DL_EmoStates",
            "HMM_DL_Gestures",
            "HMM_DL_HandChop",
            "HMM_DL_HandDismiss",
            "HMM_DL_HenchActions",
            "HMM_DL_Melee",
            "HMM_DL_PoseBreaker",
            "HMM_DL_Smoking",
            "HMM_DL_Sparring",
            "HMM_DL_StandingDefault",
            "HMM_DP_ArmsCross",
            "HMM_DP_ArmsCrossedBack",
            "HMM_DP_BarActions",
            "HMM_DP_CatchDogTags",
            "HMM_DP_ChinTouch",
            "HMM_DP_ClenchFist",
            "HMM_DP_HandOnHip",
            "HMM_DP_HandsBehindBack",
            "HMM_DP_HandsFace",
            "HMM_DP_Salute",
            "HMM_DP_Shuttle",
            "HMM_DP_ShuttleTurbulence",
            "HMM_DP_ToughGuy",
            "HMM_DP_Whisper",
            "HMM_FC_Angry",
            "HMM_FC_Communicator",
            "HMM_FC_DesignerCutscenes",
            "HMM_FC_Main",
            "HMM_FC_Sad",
            "HMM_FC_Startled",
            "NCA_ELC_EX_AnimSet",
            "NCA_VOL_DL_AnimSet",
            "PTY_EX_Geth",
            "PTY_EX_Asari",
            "PTY_EX_Krogan",
            //"RPR_BAN_CB_Banshe",
            // "RPR_HSK_AM_Husk",
            // "RPR_HSK_CB_2PMelee", // Is in special .RPR subpackage, will need special handling
            // "RPR_HSK_CB_Husk",
            "YAH_SBR_CB_AnimSet",
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
        };
#endif
        /// <summary>
        /// Maps a name of an animation package to the actual unreal package name it sits under in packages
        /// </summary>
        private static Dictionary<string, string> mapAnimSetOwners;
        public static void Init(GameTarget target, bool loadGestures = true)
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
                if (loadGestures && RandomGesturePackages.Contains(v.Key.Name))
                {
                    _gesturePackageCache.GetCachedPackageEmbedded(target.Game, $"Gestures.{v.Value.Name}.pcc"); // We don't capture the result - we just preload
                }
            }
        }

        /// <summary>
        /// Determines if the listed object path matches a key in the gesture mapping values (the result value)
        /// </summary>
        /// <param name="instancedFullPath">The path of the object to check against</param>
        /// <returns></returns>
        public static bool IsGestureGroupPackage(string instancedFullPath)
        {
            return mapAnimSetOwners.Values.Any(x => x.Equals(instancedFullPath, StringComparison.InvariantCultureIgnoreCase));
        }

        private static MERPackageCache _gesturePackageCache;

        public static IMEPackage GetGesturePackage(string gestureGroupName)
        {
            if (mapAnimSetOwners.TryGetValue(gestureGroupName, out var packageName))
            {
                return _gesturePackageCache.GetCachedPackage($"Gestures.{packageName}.pcc", false);
            }
            Debug.WriteLine($"PACKAGE NOT FOUND IN GESTURE MAP {gestureGroupName}");
            return null;
        }

        /// <summary>
        /// Gets a random looping gesture. Can return null
        /// </summary>
        /// <returns></returns>
        public static GestureInfo GetRandomMERGesture()
        {
            int retryCount = 10;
            while (retryCount > 0)
            {
                retryCount--;

                IMEPackage randomGesturePackage = null;
                string gestureGroup = null;
                while (randomGesturePackage == null)
                {
                    gestureGroup = RandomGesturePackages.RandomElement();
                    randomGesturePackage = GetGesturePackage(gestureGroup);
                }
                var candidates = randomGesturePackage.Exports.Where(x => x.ClassName == "AnimSequence" && x.ParentName == mapAnimSetOwners[gestureGroup]
                                                                                                       && x.ObjectName.Name.StartsWith(gestureGroup+"_")
                                                                                                       && !x.ObjectName.Name.StartsWith(gestureGroup+"_Alt") // This is edge case for animation names
                                                                                                       ).ToList();
                var randGesture = candidates.RandomElement();

                // Get animations that loop.
                if (randGesture.ObjectName.Name.Contains("Exit", StringComparison.InvariantCultureIgnoreCase) ||
                    randGesture.ObjectName.Name.Contains("Enter", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // Make sure it has the right animgroup - some are subsets of another - e.g. ArmsCrossed and ArmsCrossed_Alt


                return new GestureInfo()
                {
                    GestureAnimSequence = randGesture,
                    GestureGroup = gestureGroup
                };
            }

            return null;
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
        public static ExportEntry GenerateBioDynamicAnimSet(GameTarget target, ExportEntry parent, GestureInfo gestInfo, bool isKismet = false)
        {
            // The incoming gestinfo might be pointing to the cached embedded version.
            // We look up the value in the given package to ensure we use the right values.

            var animSeq = parent.FileRef.FindExport(gestInfo.GestureAnimSequence.InstancedFullPath);
            var animSet = animSeq.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(parent.FileRef);

            PropertyCollection props = new PropertyCollection();
            props.Add(new NameProperty(gestInfo.GestureGroup, "m_nmOrigSetName"));
            props.Add(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(animSeq) }, "Sequences"));
            props.Add(new ObjectProperty(animSet, "m_pBioAnimSetData"));

            BioDynamicAnimSet bin = new BioDynamicAnimSet()
            {
                SequenceNamesToUnkMap = new OrderedMultiValueDictionary<NameReference, int>(1)
                {
                    {gestInfo.GestureName, 1} // If we ever add support for multiple we do it here.
                }
            };

            var rop = new RelinkerOptionsPackage() { Cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true) };
            var bioDynObj = new ExportEntry(parent.FileRef, parent, parent.FileRef.GetNextIndexedName(isKismet ? $"KIS_DYN_{gestInfo.GestureGroup}" : "BioDynamicAnimSet"), properties: props, binary: bin)
            {
                Class = EntryImporter.EnsureClassIsInFile(parent.FileRef, "BioDynamicAnimSet", rop)
            };

            // These will always be unique
            if (isKismet)
            {
                bioDynObj.indexValue = 0;
            }
            parent.FileRef.AddExport(bioDynObj);
            return bioDynObj;
        }
    }


    class ComponentActor
    {
        // Components
        public const string HEAD_MESH_COMP_NAME = "HeadMesh";
        public const string GEAR_MESH_COMP_NAME = "HeadGearMesh";
        public const string HAIR_MESH_COMP_NAME = "HairMesh";
        public const string SKEL_MESH_COMP_NAME = "SkelMesh";

        /// <summary>
        /// Builds a mapping of mesh components and modules to their exports for an actor.
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

            if (actor.ClassName == "SFXSkeletalMeshActor")
            {
                AddComp(actor, props, components, "SkeletalMeshComponent", SKEL_MESH_COMP_NAME);
            }
            else if (actor.ClassName == "SFXStuntActor")
            {
                AddComp(actor, props, components, "BodyMesh", SKEL_MESH_COMP_NAME);
                AddModules(actor, props, components);
            }

            return components;
        }

        /// <summary>
        /// Maps the modules list to exports on the actor
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="props"></param>
        /// <param name="components"></param>
        /// <param name="propName"></param>
        /// <param name="keyName"></param>
        private static void AddModules(ExportEntry actor, PropertyCollection props, Dictionary<string, ExportEntry> components)
        {
            var prop = props.GetProp<ArrayProperty<ObjectProperty>>("Modules");
            if (prop != null)
            {
                foreach (var item in prop)
                {
                    var exp = item.ResolveToEntry(actor.FileRef) as ExportEntry;
                    if (exp != null && exp.ClassName.StartsWith("SFXModule_"))
                    {
                        components[exp.ClassName.Substring(10)+"Module"] = exp;
                    }
                    else
                    {
                        Debug.WriteLine($"SKIP MODULE {exp.ClassName}");
                    }
                    // SKIP
                }
            }
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

    class ConversationGestures
    {
        public static bool RandomizeConversationGestures(GameTarget target, ExportEntry bioConv, RandomizationOption option)
        {
            if (bioConv.ClassName != "BioConversation")
                return false;

            var matSeq = bioConv.GetProperty<ObjectProperty>("MatineeSequence").ResolveToEntry(bioConv.FileRef) as ExportEntry;
            var dynamicAnimSets = matSeq.GetProperty<ArrayProperty<ObjectProperty>>("m_aSFXSharedAnimSets");
            if (dynamicAnimSets == null)
                return false; // We don't bother with this.

            var dynAnimSetMap = new CaseInsensitiveDictionary<ExportEntry>(); // Maps a GestureGroup to it's animset in this kismet sequence.
            foreach (var dynamicAnim in dynamicAnimSets)
            {
                var entry = dynamicAnim.ResolveToEntry(bioConv.FileRef) as ExportEntry;
                dynAnimSetMap[entry.ObjectName.Name.Substring(8)] = entry; // Remove KIS_DYN_
            }


            var interps = SeqTools.GetAllSequenceElements(matSeq).OfType<ExportEntry>().Where(x => x.ClassName == "InterpData");

            foreach (var interp in interps)
            {
                InterpTools.InterpData data = new InterpTools.InterpData(interp);
                foreach (var group in data.InterpGroups)
                {
                    if (group.GroupName != "Conversation")
                        continue; // Don't care

                    foreach (var track in group.Tracks)
                    {
                        if (track.TrackTitle == null || !track.TrackTitle.StartsWith("Gesture"))
                            continue; // Don't care

                        var isPlayerTrack = track.TrackTitle.Contains("Player");

                        var randGesture = GestureManager.GetRandomMERGesture();
                        EntryExporter.ExportExportToPackage(randGesture.GestureAnimSequence, bioConv.FileRef, out var portedRandGestureSeq, MERCaches.GlobalCommonLookupCache);
                        if (!dynAnimSetMap.TryGetValue(randGesture.GestureGroup, out var dynAnimSetToUpdate))
                        {
                            // Making a ne animset since we don't have a dynamic animset for this.
                            var newBDS = GestureManager.GenerateBioDynamicAnimSet(target, matSeq, randGesture, true);
                            dynAnimSetMap[randGesture.GestureGroup] = newBDS;
                        }
                        else
                        {
                            // We have an existing animset.
                            var existing = dynAnimSetToUpdate.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                            if (existing.All(x => x.Value != portedRandGestureSeq.UIndex))
                            {
                                // It's a new animation - add it to the list of sequences
                                existing.Add(new ObjectProperty(portedRandGestureSeq.UIndex));
                                dynAnimSetToUpdate.WriteProperty(existing);

                                // VERIFY
                                string foundSt = null;
                                foreach (var v in existing)
                                {
                                    var seqExp = v.ResolveToEntry(portedRandGestureSeq.FileRef) as ExportEntry;
                                    if (seqExp != null)
                                    {
                                        if (foundSt == null)
                                        {
                                            foundSt = seqExp.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(portedRandGestureSeq.FileRef).InstancedFullPath;
                                        }
                                        else
                                        {
                                            var temp = seqExp.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(portedRandGestureSeq.FileRef).InstancedFullPath;
                                            if (temp != foundSt)
                                                Debugger.Break(); // WE HAVE DIFFERENT BIOANIMSETDATA
                                        }
                                    }
                                }
                            }
                        }

                        // Install the gesture strings
                        PropertyCollection props = track.Export.GetProperties();
                        InstallRandomGestureToGestureTrack(randGesture, props);
                        track.Export.WriteProperties(props);
                    }
                }
            }

            // Update the binary for the dynamic anim sets
            foreach (var dynSet in dynAnimSetMap)
            {
                var sequences = dynSet.Value.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                var bin = ObjectBinary.From<BioDynamicAnimSet>(dynSet.Value);

                // Rebuild the binary data with the sequence names
                bin.SequenceNamesToUnkMap.Clear();
                foreach (var seq in sequences)
                {
                    var seqExp = seq.ResolveToEntry(dynSet.Value.FileRef);
                    if (seqExp.ObjectName.Name == "AnimSequence")
                    {
                        if (seqExp is ImportEntry imp)
                            throw new Exception("Cannot look up name of imported object");
                        var actualExp = seqExp as ExportEntry;
                        bin.SequenceNamesToUnkMap[actualExp.GetProperty<NameProperty>("SequenceName").Value] = 1;
                    }
                    else
                    {
                        var seqName = seqExp.ObjectName.Name.Substring(dynSet.Key.Length + 1);
                        bin.SequenceNamesToUnkMap[seqName] = 1;
                    }
                }

                dynSet.Value.WriteBinary(bin);
            }

            // Write the referenes to the animsets
            dynamicAnimSets.Clear();
            dynamicAnimSets.AddRange(dynAnimSetMap.Select(x => new ObjectProperty(x.Value)));
            matSeq.WriteProperty(dynamicAnimSets);

            return true;
        }

        private static void InstallRandomGestureToGestureTrack(GestureInfo randGesture, PropertyCollection props)
        {
            //BioEvtSysTrackGesture
            var gestures = props.GetProp<ArrayProperty<StructProperty>>("m_aGestures");
            if (gestures != null)
            {
                foreach (var g in gestures)
                {
                    var gestSet = g.GetProp<NameProperty>("nmTransitionSet");
                    var gestAnim = g.GetProp<NameProperty>("nmTransitionAnim");

                    if (gestSet.Value == "None" && gestAnim.Value == "None")
                    {
                        gestSet.Value = randGesture.GestureGroup;
                        gestAnim.Value = randGesture.GestureName;
                        continue;
                    }


                }
            }
            else
            {
                // It is just a standard anim (e.g. other speaker). So set the root
                props.AddOrReplaceProp(new NameProperty(randGesture.GestureGroup, "nmStartingPoseSet"));
                props.AddOrReplaceProp(new NameProperty(randGesture.GestureName, "nmStartingPoseAnim"));
            }
        }
    }

    class SFXModuleGestures
    {
        public static void RandomizeGestures(GameTarget target, ExportEntry gesturesComp)
        {
            //SFXModule_Gestures
            var props = gesturesComp.GetProperties();

            // Port the gesture animation
            var randGesture = GestureManager.GetRandomMERGesture();
            if (randGesture == null)
                return;
            EntryExporter.ExportExportToPackage(randGesture.GestureAnimSequence, gesturesComp.FileRef, out var portedRandGestureSeq, MERCaches.GlobalCommonLookupCache);
            if (portedRandGestureSeq is ImportEntry imp)
            {
                return; // We aren't going to randomize this - it's too much work to work with these.
            }

            var portedBioAnimSetDataProp = (portedRandGestureSeq as ExportEntry).GetProperty<ObjectProperty>("m_pBioAnimSetData");

            // Set pose name
            props.AddOrReplaceProp(new NameProperty(randGesture.GestureName, "m_nmDefaultPoseAnim"));
            // Set the bioanimset data
            ExportEntry poseSet = props.GetProp<ObjectProperty>("m_pDefaultPoseSet").ResolveToEntry(gesturesComp.FileRef) as ExportEntry;
            PropertyCollection aprops = new PropertyCollection();
            aprops.Add(new NameProperty(randGesture.GestureGroup, "m_nmOrigSetName"));
            aprops.Add(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(portedRandGestureSeq) }, "Sequences"));
            aprops.Add(portedBioAnimSetDataProp);
            poseSet.WriteProperties(aprops);
            var objBin = ObjectBinary.From<BioDynamicAnimSet>(poseSet);
            objBin.SequenceNamesToUnkMap[randGesture.GestureName] = 0; // Index 0
            poseSet.WriteBinary(objBin);
            gesturesComp.WriteProperties(props);
        }
    }

    class SkelMeshComponent
    {
        public static void RandomizeGestures(GameTarget target, ExportEntry skelMeshComp)
        {
            var props = skelMeshComp.GetProperties();

            var animSetData = props.GetProp<ArrayProperty<ObjectProperty>>("AnimSets");
            if (animSetData == null)
            {
                return;
            }

            // SFXSkeletalMeshActor component seems to use this, which specifies just one animation it uses.
            var animNode = props.GetProp<ObjectProperty>("Animations");
            ExportEntry animNodeExp = null;
            if (animNode != null)
            {
                animNodeExp = animNode.ResolveToEntry(skelMeshComp.FileRef) as ExportEntry;
            }



            // Port the data
            var randGesture = GestureManager.GetRandomMERGesture();
            if (randGesture == null)
                return;
            EntryExporter.ExportExportToPackage(randGesture.GestureAnimSequence, skelMeshComp.FileRef, out var portedRandGestureSeq, MERCaches.GlobalCommonLookupCache);
            var portedBioAnimSetDataProp = ((ExportEntry)(portedRandGestureSeq)).GetProperty<ObjectProperty>("m_pBioAnimSetData");
            //EntryExporter.ExportExportToPackage(randGesture.GestureAnimSetData, skelMeshComp.FileRef, out var portedBioAnimSetData, MERCaches.GlobalCommonLookupCache);

            // Clone it so we don't have to deal with imports to it.
            // animNodeExp = EntryCloner.CloneEntry(animNodeExp);

            if (animNodeExp != null)
            {
                // SFXSkeletalMeshActor will have this probably
                animNodeExp.WriteProperty(new NameProperty(randGesture.GestureName, "AnimSeqName")); // Update the AnimSeqName
            }

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
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && !exp.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject)
                                                                                  && (exp.ClassName == "SFXSkeletalMeshActor" || exp.ClassName == "SFXStuntActor");
        //&& !exp.ObjectName.Name.Contains("Dead", StringComparison.InvariantCultureIgnoreCase);
        private static string[] smaKeywords = new[] { "Dancing", "Dance", "Angry", "Cursing", "Fearful", "ROM", "Drunk", "Kiss", "Headbutt", "Hugging", "Consoling", "Come_Here", "Cough", "Count", "Bhand_Slapped" };

        public static bool RandomizeBasicGestures(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var components = ComponentActor.GetComponents(export);
            if (!components.TryGetValue(ComponentActor.SKEL_MESH_COMP_NAME, out var skelMeshComp))
                return false;

            if (export.ClassName == "SFXSkeletalMeshActor")
            {
                SkelMeshComponent.RandomizeGestures(target, skelMeshComp);
            }
            else if (export.ClassName == "SFXStuntActor")
            {
                SFXModuleGestures.RandomizeGestures(target, components["GesturesModule"]);
            }

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