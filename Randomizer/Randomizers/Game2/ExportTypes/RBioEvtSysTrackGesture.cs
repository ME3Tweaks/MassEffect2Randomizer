using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using Randomizer.MER;
using RandomizerUI.Classes.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    /// <summary>
    /// Gesture Randomizer for Gestures in InterpDatas
    /// </summary>
    class RBioEvtSysTrackGesture
    {
        public static Gesture InstallRandomGestureAsset(IMEPackage package, float minSequenceLength = 0, MERPackageCache cache = null)
        {
            var gestureFiles = MERUtilities.ListStaticAssets("binary.gestures");
            var randGestureFile = gestureFiles.RandomElement();
            cache ??= new MERPackageCache();
            var gPackage = cache.GetCachedPackageEmbedded(randGestureFile, isFullPath: true);
            var options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence").ToList();

            // Pick a random element, make sure it's long enough
            var randomGestureExport = options.RandomElement();
            var seqLength = randomGestureExport.GetProperty<FloatProperty>("SequenceLength");

            int numRetries = 5;
            while (seqLength < minSequenceLength)
            {
                randomGestureExport = options.RandomElement();
                seqLength = randomGestureExport.GetProperty<FloatProperty>("SequenceLength");
                numRetries--;
            }

            var portedInExp = PackageTools.PortExportIntoPackage(package, randomGestureExport);

            return new Gesture(portedInExp);
        }


        public static void DebugS()
        {
            // Gesture data output
            //var p = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile("SFXGame.pcc"));
            //var rtd = ObjectBinary.From<BioGestureRuntimeData>(p.GetUExport(35297));
            //foreach (var d in rtd.m_mapAnimSetOwners)
            //{
            //    Debug.WriteLine($"{{\"{d.Key}\", \"{d.Value}\"}},");
            //}

            // Build 
            //im
        }

        public static List<Gesture> GetGestures(ExportEntry export)
        {
            var gestures = export.GetProperty<ArrayProperty<StructProperty>>("m_aGestures");
            return gestures?.Select(x => new Gesture(x)).ToList();
        }

        /// <summary>
        /// Writes the list of gestures to the export, then looks up the parent path to find the sequence's biodynamicanim sets and ensures the values are in them. Does not support adding additional items
        /// </summary>
        /// <param name="export"></param>
        /// <param name="gestures"></param>
        public static void WriteGestures(ExportEntry export, List<Gesture> gesturesToWrite)
        {
            var gestures = export.GetProperty<ArrayProperty<StructProperty>>("m_aGestures");
            if (gestures.Count != gesturesToWrite.Count)
            {
                throw new Exception("Cannot write different amount of gestures out!!");
            }

            ExportEntry owningSequence = null;
            for (int i = 0; i < gestures.Count; i++)
            {
                var gesture = gestures[i];
                var ngesture = gesturesToWrite[i];
                gesture.Properties.AddOrReplaceProp(new NameProperty(ngesture.GestureSet, "nmGestureSet"));
                gesture.Properties.AddOrReplaceProp(new NameProperty(ngesture.GestureAnim, "nmGestureAnim"));
                InstallDynamicAnimSetRefForSeq(ref owningSequence, export, ngesture);
            }

            export.WriteProperty(gestures);
        }
        /*
        private static void VerifyGesturesWork(ExportEntry trackExport)
        {
            var gestures = RBioEvtSysTrackGesture.GetGestures(trackExport);
            var defaultPose = RBioEvtSysTrackGesture.GetDefaultPose(trackExport);

            var gesturesToCheck = gestures.Append(defaultPose).ToList();

            // Get the containing sequence
            var owningSequence = SeqTools.GetParentSequence(trackExport);
            while (owningSequence.ClassName != "Sequence")
            {
                owningSequence = owningSequence.Parent as ExportEntry;
                var parSeq = SeqTools.GetParentSequence(owningSequence);
                if (parSeq != null)
                {
                    owningSequence = parSeq;
                }
            }

            var kismetBioDynamicAnimSets = owningSequence.GetProperty<ArrayProperty<ObjectProperty>>("m_aBioDynAnimSets");
            if (kismetBioDynamicAnimSets == null)
            {
                // We don't have any animsets!
                throw new Exception("Track's sequence is missing animsets property!");
            }

            // Get a list of all supported animations
            List<Gesture> supportedGestures = new List<Gesture>();
            foreach (var kbdas in kismetBioDynamicAnimSets)
            {
                var sequenceBioDynamicAnimSet = kbdas.ResolveToEntry(trackExport.FileRef) as ExportEntry; // I don't think these can be imports as they're part of the seq
                var associatedset = sequenceBioDynamicAnimSet.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(trackExport.FileRef);

            }

            // Check all gestures
            foreach (var gesture in gesturesToCheck)
            {
                var bioAnimSet = gesture.GetBioAnimSet(trackExport.FileRef);

            }



        }

        internal class TestingBioDynamicAnimSet
        {
            public NameReference OrigSetName { get; }
            public List<string> SupportedGesturesFullPaths { get; }
            public IEntry BioAnimSetData { get; }

            internal TestingBioDynamicAnimSet(ExportEntry export)
            {
                var props = export.GetProperties();
                OrigSetName = props.GetProp<NameProperty>("m_nmOrigSetName").Value;
                BioAnimSetData = props.GetProp<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(export.FileRef);
                SupportedGesturesFullPaths = props.GetProp<ArrayProperty<ObjectProperty>>("Sequences").Select(x => x.ResolveToEntry(export.FileRef).InstancedFullPath).ToList();
            }
        }
        */
        private static void InstallDynamicAnimSetRefForSeq(ref ExportEntry owningSequence, ExportEntry export, Gesture gesture)
        {
            // Find owning sequence
            if (owningSequence == null)
                owningSequence = export;
            while (owningSequence.ClassName != "Sequence")
            {
                owningSequence = owningSequence.Parent as ExportEntry;
                var parSeq = SeqTools.GetParentSequence(owningSequence);
                if (parSeq != null)
                {
                    owningSequence = parSeq;
                }
            }

            // We have parent sequence data
            var kismetBioDynamicAnimSets = owningSequence.GetProperty<ArrayProperty<ObjectProperty>>("m_aBioDynAnimSets")
                                     ?? new ArrayProperty<ObjectProperty>("m_aBioDynamicAnimSets");

            // Check to see if there is any item that uses our bioanimset
            var bioAnimSet = gesture.GetBioAnimSet(export.FileRef);
            if (bioAnimSet != null)
            {
                ExportEntry kismetBDAS = null;
                foreach (var kbdas in kismetBioDynamicAnimSets)
                {
                    var kEntry = kbdas.ResolveToEntry(export.FileRef) as ExportEntry; // I don't think these can be imports as they're part of the seq
                    var associatedset = kEntry.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(export.FileRef);
                    if (associatedset == bioAnimSet)
                    {
                        // It's this one
                        kismetBDAS = kEntry;
                        break;
                    }
                }

                if (kismetBDAS == null)
                {
                    // We need to generate a new one
                    PropertyCollection props = new PropertyCollection();
                    props.Add(new NameProperty(gesture.GestureSet, "m_nmOrigSetName"));
                    props.Add(new ArrayProperty<ObjectProperty>("Sequences"));
                    props.Add(new ObjectProperty(bioAnimSet, "m_pBioAnimSetData"));
                    kismetBDAS = ExportCreator.CreateExport(export.FileRef, $"KIS_DYN_{gesture.GestureSet}", "BioDynamicAnimSet", owningSequence);
                    kismetBDAS.indexValue = 0;

                    // Write a blank count of 0 - we will update this in subsequent call
                    // This must be here to ensure parser can read it
                    kismetBDAS.WritePropertiesAndBinary(props, new byte[4]);
                    kismetBioDynamicAnimSets.Add(new ObjectProperty(kismetBDAS)); // Add new export to sequence's list of biodynamicanimsets
                    owningSequence.WriteProperty(kismetBioDynamicAnimSets);
                }

                var currentObjs = kismetBDAS.GetProperty<ArrayProperty<ObjectProperty>>("Sequences");
                if (currentObjs.All(x => x.Value != gesture.Entry.UIndex))
                {
                    // We need to add our item to it
                    currentObjs.Add(new ObjectProperty(gesture.Entry));
                    var bin = ObjectBinary.From<BioDynamicAnimSet>(kismetBDAS);
                    bin.SequenceNamesToUnkMap[gesture.GestureAnim] = 1; // Not sure what the value should be, or if game actually reads this
                    // FIX IT IF WE EVER FIGURE IT OUT!
                    kismetBDAS.WriteProperty(currentObjs);
                    kismetBDAS.WriteBinary(bin);
                }
            }
        }

        public class Gesture
        {
            public string GestureSet { get; set; }
            public NameReference GestureAnim { get; set; }
            /// <summary>
            /// Entry that was used to generate the info for this Gesture if it was loaded from an export
            /// </summary>
            public IEntry Entry { get; set; }
            public Gesture(StructProperty structP)
            {
                GestureSet = structP.GetProp<NameProperty>("nmGestureSet").Value;
                GestureAnim = structP.GetProp<NameProperty>("nmGestureAnim").Value;
            }

            public Gesture(ExportEntry export)
            {
                GestureAnim = export.GetProperty<NameProperty>("SequenceName").Value;
                GestureSet = export.ObjectName.Name.Substring(0, export.ObjectName.Instanced.Length - GestureAnim.Instanced.Length - 1); // -1 for _
                Entry = export;

            }

            public Gesture() { }

            /// <summary>
            /// Fetches the IEntry that this gesture uses by looking up the animsequence and it's listed bioanimset. Can return null if it can't be found!
            /// </summary>
            /// <param name="exportFileRef"></param>
            /// <returns></returns>
            public IEntry GetBioAnimSet(IMEPackage exportFileRef)
            {
                if (PopulateEntry(exportFileRef) && Entry is ExportEntry exp)
                {
                    return exp.GetProperty<ObjectProperty>("m_pBioAnimSetData")?.ResolveToEntry(exportFileRef);
                }

                return null;
            }

            /// <summary>
            /// Populates the Entry variable, locating the animsequence in the specified package
            /// </summary>
            /// <param name="exportFileRef"></param>
            /// <returns></returns>
            private bool PopulateEntry(IMEPackage exportFileRef)
            {
                if (Entry != null) return true;
                if (GestureSetNameToPackageExportName.TryGetValue(GestureSet, out var pName))
                {
                    Entry = exportFileRef.FindEntry($"{pName}.{GestureSet}_{GestureAnim}");
                    return Entry != null;
                }

                return false;
            }
        }

        public static string GetPackageExportNameForGestureSet(string gestureset)
        {
            GestureSetNameToPackageExportName.TryGetValue(gestureset, out var res);
            return res;
        }

        public static bool IsGesturePackage(string packagename)
        {
            return GestureSetNameToPackageExportName.Values.Any(x => x == packagename);
        }


        private static Dictionary<string, string> GestureSetNameToPackageExportName = new Dictionary<string, string>()
        {
            // nmAnimSet -> Package export name that contains that export. The export is prefixed with the value with the sequencename concatenated to it followed by an underscore.
            {"HMM_DL_StandingDefault", "BIOG_HMM_DL_A"},
            {"HMM_DP_ArmsCross", "BIOG_HMM_DP_A"},
            {"HMM_DP_ArmsCrossedBack", "BIOG_HMM_DP_A"},
            {"HMM_DP_BackAgainstWall", "BIOG_HMM_DP_A"},
            {"HMM_DP_BowKnee", "BIOG_HMM_DP_A"},
            {"HMM_DP_ChinTouch", "BIOG_HMM_DP_A"},
            {"HMM_DP_ClenchFist", "BIOG_HMM_DP_A"},
            {"HMM_DP_Crouch", "BIOG_HMM_DP_A"},
            {"HMM_DP_Execute", "BIOG_HMM_DP_A"},
            {"HMM_DP_FaceRight", "BIOG_HMM_DP_A"},
            {"HMM_DP_HandOnHip", "BIOG_HMM_DP_A"},
            {"HMM_DP_HandsBehindBack", "BIOG_HMM_DP_A"},
            {"HMM_DP_HandsFace", "BIOG_HMM_DP_A"},
            {"HMM_DP_LeanBar", "BIOG_HMM_DP_A"},
            {"HMM_DP_ObjManipulate", "BIOG_HMM_DP_A"},
            {"HMM_DP_PistolHover", "BIOG_HMM_DP_A"},
            {"HMM_DP_PistolThreaten", "BIOG_HMM_DP_A"},
            {"HMM_DP_PistolThreatenLow", "BIOG_HMM_DP_A"},
            {"HMM_DP_PoundFist", "BIOG_HMM_DP_A"},
            {"HMM_DP_Rifle", "BIOG_HMM_DP_A"},
            {"HMM_DP_Salute", "BIOG_HMM_DP_A"},
            {"HMM_DP_Startle", "BIOG_HMM_DP_A"},
            {"HMM_DP_StepAside", "BIOG_HMM_DP_A"},
            {"HMM_DP_StepBack", "BIOG_HMM_DP_A"},
            {"HMM_DP_StepForward", "BIOG_HMM_DP_A"},
            {"HMM_DP_ToughGuy", "BIOG_HMM_DP_A"},
            {"HMM_DP_TwistBehind", "BIOG_HMM_DP_A"},
            {"HMM_DP_TwoHandChop", "BIOG_HMM_DP_A"},
            {"HMM_DP_WalktoNode", "BIOG_HMM_DP_A"},
            {"HMM_DP_WanderEast", "BIOG_HMM_DP_A"},
            {"HMM_DP_WanderNorth", "BIOG_HMM_DP_A"},
            {"HMM_DP_WanderSouth", "BIOG_HMM_DP_A"},
            {"HMM_DP_WanderWest", "BIOG_HMM_DP_A"},
            {"HMM_DP_Whisper", "BIOG_HMM_DP_A"},
            {"HMM_2P_Choking", "BIOG_HMM_2P_A"},
            {"HMM_2P_CloseProximity", "BIOG_HMM_2P_A"},
            {"HMM_2P_Consoling", "BIOG_HMM_2P_A"},
            {"HMM_2P_Conspire", "BIOG_HMM_2P_A"},
            {"HMM_2P_Grab", "BIOG_HMM_2P_A"},
            {"HMM_2P_KickLow", "BIOG_HMM_2P_A"},
            {"HMM_2P_KneetoGroin", "BIOG_HMM_2P_A"},
            {"HMM_2P_PinAgainstWall", "BIOG_HMM_2P_A"},
            {"HMM_2P_PistolPoint", "BIOG_HMM_2P_A"},
            {"HMM_2P_ShootKnee", "BIOG_HMM_2P_A"},
            {"HMM_2P_SnapNeck", "BIOG_HMM_2P_A"},
            {"HMM_2P_WallClose", "BIOG_HMM_2P_A"},
            {"HMF_2P_Main", "BIOG_HMF_2P_A"},
            {"HMM_2P_Main", "BIOG_HMM_2P_A"},
            {"HMM_WI_Sitting", "BIOG_HMM_WI_A"},
            {"HMM_WI_Asleep", "BIOG_HMM_WI_A"},
            {"HMM_WI_BackAgainstWall", "BIOG_HMM_WI_A"},
            {"HMM_WI_Bartender", "BIOG_HMM_WI_A"},
            {"HMM_WI_CockPit", "BIOG_HMM_WI_A"},
            {"HMM_WI_Desk", "BIOG_HMM_WI_A"},
            {"HMM_WI_DeskElbows1", "BIOG_HMM_WI_A"},
            {"HMM_WI_DeskElbows2", "BIOG_HMM_WI_A"},
            {"HMM_WI_DeskElbows2Action1", "BIOG_HMM_WI_A"},
            {"HMM_WI_DeskLean", "BIOG_HMM_WI_A"},
            {"HMM_WI_ExamineLow", "BIOG_HMM_WI_A"},
            {"HMM_WI_ExamineMid", "BIOG_HMM_WI_A"},
            {"HMM_WI_FixLow", "BIOG_HMM_WI_A"},
            {"HMM_WI_Heal", "BIOG_HMM_WI_A"},
            {"HMM_WI_KeyPad", "BIOG_HMM_WI_A"},
            {"HMM_WI_Sitting2", "BIOG_HMM_WI_A"},
            {"HMM_WI_SittingForward1", "BIOG_HMM_WI_A"},
            {"HMM_WI_SittingForward2", "BIOG_HMM_WI_A"},
            {"HMM_WI_SittingForward3", "BIOG_HMM_WI_A"},
            {"HMM_WI_StandTyping", "BIOG_HMM_WI_A"},
            {"HMM_WI_StateTransitions", "BIOG_HMM_WI_A"},
            {"HMM_WI_Reclining", "BIOG_HMM_WI_A"},
            {"HMM_WI_LifePod", "BIOG_HMM_WI_A"},
            {"HMM_WI_Lying", "BIOG_HMM_WI_A"},
            {"HMM_WI_PDA", "BIOG_HMM_WI_A"},
            {"HMM_WI_PistolCover", "BIOG_HMM_WI_A"},
            {"HMM_WI_Railing", "BIOG_HMM_WI_A"},
            {"HMM_WI_WallLeanLeft", "BIOG_HMM_WI_A"},
            {"HMM_WI_WallLeanRight", "BIOG_HMM_WI_A"},
            {"HMM_WI_WeldingLow", "BIOG_HMM_WI_A"},
            {"HMM_WI_WeldingMid", "BIOG_HMM_WI_A"},
            {"HMF_WI_ComSit", "BIOG_HMF_WI_A"},
            {"HMF_WI_Lying", "BIOG_HMF_WI_A"},
            {"HMF_WI_Railing", "BIOG_HMF_WI_A"},
            {"HMF_WI_RailingAction1", "BIOG_HMF_WI_A"},
            {"HMF_WI_Reclining", "BIOG_HMF_WI_A"},
            {"HMF_WI_Sitting", "BIOG_HMF_WI_A"},
            {"HMF_WI_RecliningAction1", "BIOG_HMF_WI_A"},
            {"HMF_WI_WallLeanLeft", "BIOG_HMF_WI_A"},
            {"HMF_WI_WallLeanRight", "BIOG_HMF_WI_A"},
            {"HMM_AM_Console", "BIOG_HMM_AM_A"},
            {"HMM_AM_Environmental", "BIOG_HMM_AM_A"},
            {"HMM_AM_Gamble", "BIOG_HMM_AM_A"},
            {"HMM_AM_Guard", "BIOG_HMM_AM_A"},
            {"HMM_AM_HostilePistol", "BIOG_HMM_AM_A"},
            {"HMM_AM_HostileRifle", "BIOG_HMM_AM_A"},
            {"HMM_AM_ObjManipulate", "BIOG_HMM_AM_A"},
            {"HMM_AM_PistolWeightShift", "BIOG_HMM_AM_A"},
            {"HMM_AM_RifleTwitch", "BIOG_HMM_AM_A"},
            {"HMM_AM_Talk", "BIOG_HMM_AM_A"},
            {"HMM_AM_Towny", "BIOG_HMM_AM_A"},
            {"HMM_AM_Wander", "BIOG_HMM_AM_A"},
            {"HMF_AM_Talk", "BIOG_HMF_AM_A"},
            {"HMF_AM_Towny", "BIOG_HMF_AM_A"},
            {"HMF_DL_StandingDefault", "BIOG_HMF_DL_A"},
            {"HMF_DL_Turnaround", "BIOG_HMF_DL_A"},
            {"DL_Body", "BIOG_HMM_DL_A"},
            {"HMM_DL_Accept", "BIOG_HMM_DL_A"},
            {"HMM_DL_Decline", "BIOG_HMM_DL_A"},
            {"HMM_DL_HandChop", "BIOG_HMM_DL_A"},
            {"HMM_DL_HandDismiss", "BIOG_HMM_DL_A"},
            {"HMM_DL_HeadGesture", "BIOG_HMM_DL_A"},
            {"HMM_DL_Point", "BIOG_HMM_DL_A"},
            {"HMM_DL_PoseBreaker", "BIOG_HMM_DL_A"},
            {"HMM_DL_Shrug", "BIOG_HMM_DL_A"},
            {"HMM_DL_Smoking", "BIOG_HMM_DL_A"},
            {"HMM_DL_TouchHead", "BIOG_HMM_DL_A"},
            {"HMM_DL_Wave", "BIOG_HMM_DL_A"},
            {"HMM_HandBeckon", "BIOG_HMM_DL_A"},
            {"HMM_FC_Afraid", "BIOG_HMM_FC_A"},
            {"HMM_FC_Angry", "BIOG_HMM_FC_A"},
            {"HMM_FC_Custom", "BIOG_HMM_FC_A"},
            {"HMM_FC_DesignerCutscenes", "BIOG_HMM_FC_A"},
            {"HMM_FC_Drunk", "BIOG_HMM_FC_A"},
            {"HMM_FC_Grovel", "BIOG_HMM_FC_A"},
            {"HMM_FC_Injuried", "BIOG_HMM_FC_A"},
            {"HMM_FC_Main", "BIOG_HMM_FC_A"},
            {"HMM_FC_Pistol", "BIOG_HMM_FC_A"},
            {"HMM_FC_Rifle", "BIOG_HMM_FC_A"},
            {"HMM_FC_Sad", "BIOG_HMM_FC_A"},
            {"HMM_FC_Urgent", "BIOG_HMM_FC_A"},
            {"NCA_VOL_DL_AnimSet", "BIOG_NCA_A"},
            {"NCA_VOL_DP_AnimSet", "BIOG_NCA_A"},
            {"NCA_HAN_EX_AnimSet", "BIOG_NCA_A"},
            {"NCA_HAN_DL_AnimSet", "BIOG_NCA_A"},
            {"NCA_ELC_DL_AnimSet", "BIOG_NCA_A"},
            {"NCA_ELC_EX_AnimSet", "BIOG_NCA_A"},
            {"HMM_DG_Deaths", "BIOG_HMM_DG_A"},
            {"HMM_DG_Exploration", "BIOG_HMM_DG_A"},
            {"HMM_DG_GetUp", "BIOG_HMM_DG_A"},
            {"HMM_DG_PistolBiotics", "BIOG_HMM_DG_A"},
            {"HMM_DG_Rifle", "BIOG_HMM_DG_A"},
            {"HMM_DG_RifleBiotics", "BIOG_HMM_DG_A"},
            {"PTY_DL_Krogan", "BIOG_PTY_A"},
            {"PTY_EX_Krogan", "BIOG_PTY_A"},
            {"PTY_EX_Asari", "BIOG_PTY_A"},
            {"PTY_EX_HumanFemale", "BIOG_PTY_A"},
            {"PTY_EX_HumanMale", "BIOG_PTY_A"},
            {"PTY_EX_Quarian", "BIOG_PTY_A"},
            {"PTY_EX_Turian", "BIOG_PTY_A"},
            {"PTY_Pose_Asari", "BIOG_PTY_A"},
            {"HMF_WI_StateTransitions", "BIOG_HMF_WI_A"},
            {"HMM_CB_Rifle", "BIOG_HMM_CB_A"},
            {"HMF_FC_Custom", "BIOG_HMM_FC_A"},
            {"HMM_DP_StandingDefault", "BIOG_HMM_DP_A"},
            {"PTY_EX_Geth", "BIOG_PTY_A"},
            {"HMM_DP_ShotgunThreaten", "BIOG_HMM_DP_A"},
            {"NCA_VOL_EX_AnimSet", "BIOG_NCA_A"},
            {"PTY_EX_Assasin", "BIOG_PTY_A"},
            {"PTY_EX_Professor", "BIOG_PTY_A"},
            {"HMM_CB_Pistol", "BIOG_HMM_CB_A"},
            {"HMM_BC_Main", "BIOG_HMM_BC_A"},
            {"HMM_CB_PistolCover", "BIOG_HMM_CB_A"},
            {"HMM_CB_RifleCover", "BIOG_HMM_CB_A"},
            {"HMM_DL_HenchActions", "BIOG_HMM_DL_A"},
            {"HMM_2P_KissMale", "BIOG_HMM_2P_A"},
            {"HMM_2P_HoldingHands", "BIOG_HMM_2P_A"},
            {"HMM_2P_SittingExchange", "BIOG_HMM_2P_A"},
            {"HMM_2P_TurnBodyOver", "BIOG_HMM_2P_A"},
            {"HMM_2P_ForcedExit", "BIOG_HMM_2P_A"},
            {"HMM_2P_BackhandSlap", "BIOG_HMM_2P_A"},
            {"KRO_2P_Tussle", "BIOG_HMM_2P_A"},
            {"KRO_2P_VarrenRestrain", "BIOG_HMM_2P_A"},
            {"VAR_2P_VarrenRestain", "BIOG_HMM_2P_A"},
            {"CBT_VAR_EX_AnimSet", "BIOG_CBT_A.Var"},
            {"HMM_2P_HeadButt", "BIOG_HMM_2P_A"},
            {"HMM_2P_PatDown", "BIOG_HMM_2P_A"},
            {"KRO_WI_Sitting", "BIOG_KRO_A"},
            {"HMF_DP_ArmsCrossed", "BIOG_HMF_DP_A"},
            {"HMM_DP_SitFloor", "BIOG_HMM_DP_A"},
            {"HMM_FC_A", "BIOG_HMM_FC_A"},
            {"HMM_DP_Kneel", "BIOG_HMM_DP_A"},
            {"HMM_DP_BarActions", "BIOG_HMM_DP_A"},
            {"HMM_CB_RifleAuto", "BIOG_HMM_CB_A"},
            {"HMM_CB_SniperAuto", "BIOG_HMM_CB_A"},
            {"HMM_CB_PistolAuto", "BIOG_HMM_CB_A"},
            {"HMM_CB_ShotgunAuto", "BIOG_HMM_CB_A"},
            {"HMM_DP_HandsFold", "BIOG_HMM_DP_A"},
            {"HMM_DL_Intervene", "BIOG_HMM_DL_A"},
            {"LMCH_BC_LightingBall", "BIOG_LMCH_BC_A"},
            {"LMCH_CB_Pistol", "BIOG_LMCH_CB_A"},
            {"LMCH_CB_Rifle", "BIOG_LMCH_CB_A"},
            {"LMCH_DG_A", "BIOG_LMCH_DG_A"},
            {"LMCH_EX_Pistol", "BIOG_LMCH_EX_A"},
            {"HMM_DP_SitFloorInjured", "BIOG_HMM_DP_A"},
            {"HMM_WI_StandAtDesk", "BIOG_HMM_WI_A"},
            {"HMM_BC_RifleTelekinesisFloat", "BIOG_HMM_BC_A"},
            {"HMM_CB_RifleDraw", "BIOG_HMM_CB_A"},
            {"HMM_2P_PushThruGlass", "BIOG_HMM_2P_A"},
            {"HMM_DP_PistolThreaten_Alt", "BIOG_HMM_DP_A"},
            {"HMM_DP_PistolThreatenLowAlt", "BIOG_HMM_DP_A"},
            {"HMM_DL_ElusiveMan", "BIOG_HMM_DL_A"},
            {"CBT_COL_Collector_Animset", "BIOG_CBT_A.Col"},
            {"VAR_2P_VarrenRelease", "BIOG_HMM_2P_A"},
            {"HMM_DL_EmoStates", "BIOG_HMM_DL_A"},
            {"HMM_BC_Wargear_2handedweapons", "BIOG_HMM_BC_A"},
            {"HMM_WI_OmniTool", "BIOG_HMM_WI_A"},
            {"HMM_GUI_Adept", "BIOG_HMM_GUI_A.Adept"},
            {"HMM_GUI_Engineer", "BIOG_HMM_GUI_A.Engineer"},
            {"HMM_GUI_Infiltrator", "BIOG_HMM_GUI_A.Infiltrator"},
            {"HMM_GUI_Sentinel", "BIOG_HMM_GUI_A.Sentinel"},
            {"HMM_GUI_Soldier", "BIOG_HMM_GUI_A.Soldier"},
            {"HMM_GUI_Vanguard", "BIOG_HMM_GUI_A.Vanguard"},
            {"HMM_2P_Hostage", "BIOG_HMM_2P_A"},
            {"HMM_AM_VehicleInteraction", "BIOG_HMM_AM_A"},
            {"HMM_EX_Player_Pistol", "BIOG_HMM_EX_A"},
            {"HMM_EX_PistolScared", "BIOG_HMM_EX_A"},
            {"HMM_EX_Pistol", "BIOG_HMM_EX_A"},
            {"CBT_VAR_CB_AnimSet", "BIOG_CBT_A.Var"},
            {"CBT_VAR_Rake_AnimSet", "BIOG_CBT_A.Var"},
            {"HMM_DL_Turns", "BIOG_HMM_DL_A"},
            {"HMM_DL_Gestures", "BIOG_HMM_DL_A"},
            {"HMM_2P_Interrogation", "BIOG_HMM_2P_A"},
            {"HMM_WI_SitDrink", "BIOG_HMM_WI_A"},
            {"HMM_DL_Melee", "BIOG_HMM_DL_A"},
            {"HMM_DP_PistolThreatenHigh", "BIOG_HMM_DP_A"},
            {"HMM_2P_GunInterrupt", "BIOG_HMM_2P_A"},
            {"HMM_2P_PunchInterrupt", "BIOG_HMM_2P_A"},
            {"HMM_2P_Comraderie", "BIOG_HMM_2P_A"},
            {"RBT_TNK_CB_AnimSet", "BIOG_RBT_A.TNK"},
            {"HMM_CB_Expression", "BIOG_HMM_CB_A"},
            {"RBT_TNK_DL_AnimSet", "BIOG_RBT_A.TNK"},
            {"RBT_TNK_Suppression_AnimSet", "BIOG_RBT_A.TNK"},
            {"RBT_ROB_AnimSet", "BIOG_RBT_A.ROB"},
            {"HMM_DL_PistolMove", "BIOG_HMM_DL_A"},
            {"HMM_DL_RifleMove", "BIOG_HMM_DL_A"},
            {"HMF_DL_IdleOffset", "BIOG_HMF_DL_A"},
            {"HMM_2P_KissCheek", "BIOG_HMM_2P_A"},
            {"KRO_CB_Pistol", "BIOG_KRO_A"},
            {"KRO_CB_Rifle", "BIOG_KRO_A"},
            {"JKR_EX_PistolWalk", "BIOG_HMM_EX_A"},
            {"KRO_CB_Designer", "BIOG_KRO_CB_A"},
            {"KRO_SM_Charge", "BIOG_KRO_CB_A"},
            {"KRO_EX_Pistol", "BIOG_KRO_A"},
            {"HMM_BC_TeleportDash", "BIOG_HMM_BC_A"},
            {"HMM_BC_RifleWeaken", "BIOG_HMM_BC_A"},
            {"HMM_BC_RifleKineticBarrier", "BIOG_HMM_BC_A"},
            {"HMM_WG_Grenade_Draw", "BIOG_HMM_WG_A"},
            {"HMM_WG_Grenade_Holster", "BIOG_HMM_WG_A"},
            {"SAL_EX_Pistol", "BIOG_SAL_A"},
            {"HMF_DP_ArmsCrossed_Alt", "BIOG_HMF_DP_A"},
            {"HMM_AM_QuickDraw", "BIOG_HMM_AM_A"},
            {"RBT_MDG_CB", "BIOG_RBT_A"},
            {"HMF_DP_BackAgainstWall", "BIOG_HMF_DP_A"},
            {"HMF_DP_ChinTouch", "BIOG_HMF_DP_A"},
            {"HMF_DP_HandsFace", "BIOG_HMF_DP_A"},
            {"HMF_DP_Kneel", "BIOG_HMF_DP_A"},
            {"HMF_WI_DeskElbows", "BIOG_HMF_WI_A"},
            {"HMF_WI_DeskElbows2Action1", "BIOG_HMF_WI_A"},
            {"KRO_FC_Custom", "BIOG_HMM_FC_A"},
            {"RBT_MDG_EX", "BIOG_RBT_A"},
            {"HMF_DP_PoundFist", "BIOG_HMF_DP_A"},
            {"HMM_2P_GruntPinShep", "BIOG_HMM_2P_A"},
            {"KRO_2P_GruntPinShep", "BIOG_HMM_2P_A"},
            {"HMM_DP_PistolThreatenDraw", "BIOG_HMM_DP_A"},
            {"CBT_RPR_ReaperBaby_Animset", "BIOG_CBT_A.RPR"},
            {"HMF_DP_BarActions", "BIOG_HMF_DP_A"},
            {"HMF_WI_SittingForward", "BIOG_HMF_WI_A"},
            {"HMM_DL_ElusiveMan_Alt", "BIOG_HMM_DL_A"},
            {"HMM_DP_WalktoNode_Alt", "BIOG_HMM_DP_A"},
            {"HMM_2P_ZaeedShepard", "BIOG_HMM_2P_Zaeed_A"},
            {"HMM_2P_ZaeedShepard_Pillar", "BIOG_HMM_2P_Zaeed_A"},
            {"HMM_AM_DLC_Guard", "BIOG_HMM_AM_DLC_A"},
        };

        public static void WriteDefaultPose(ExportEntry export, Gesture newPose)
        {
            var props = export.GetProperties();
            props.AddOrReplaceProp(new NameProperty(newPose.GestureSet, "nmStartingPoseSet"));
            props.AddOrReplaceProp(new NameProperty(newPose.GestureAnim, "nmStartingPoseAnim"));
            export.WriteProperties(props);
            ExportEntry owningSeq = null;
            InstallDynamicAnimSetRefForSeq(ref owningSeq, export, newPose);
        }

        public static Gesture GetDefaultPose(ExportEntry export)
        {
            var props = export.GetProperties();
            return new Gesture()
            {
                GestureAnim = props.GetProp<NameProperty>("nmStartingPoseSet").Value,
                GestureSet = props.GetProp<NameProperty>("nmStartingPoseSet").Value,
            };
        }

        public static Gesture InstallRandomFilteredGestureAsset(IMEPackage targetPackage, float minLength = 0, string[] filterKeywords = null, string[] blacklistedKeywords = null, string[] mainPackagesAllowed = null, bool includeSpecial = false, MERPackageCache cache = null)
        {
            var gestureFiles = MERUtilities.ListStaticAssets("binary.gestures", includeSpecial);

            // Special and package file filtering
            if (mainPackagesAllowed != null)
            {
                var newList = new List<string>();
                foreach (var gf in gestureFiles)
                {
                    if (includeSpecial && gf.Contains("gestures.special."))
                    {
                        newList.Add(gf);
                        continue;
                    }

                    var packageName = Path.GetFileNameWithoutExtension(MERUtilities.GetFilenameFromAssetName(gf));
                    if (mainPackagesAllowed.Contains(packageName))
                    {
                        newList.Add(gf);
                        continue;
                    }
                }

                gestureFiles = newList;
            }

            // Pick a random package
            var randGestureFile = gestureFiles.RandomElement();
            var hasCache = cache != null;
            cache ??= new MERPackageCache();
            var gPackage = cache.GetCachedPackageEmbedded(randGestureFile, isFullPath: true);
            List<ExportEntry> options;
            if (filterKeywords != null && blacklistedKeywords != null)
            {
                options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence"
                                                      && x.ObjectName.Name.ContainsAny(StringComparison.InvariantCultureIgnoreCase, filterKeywords)
                                                      && !x.ObjectName.Name.ContainsAny(blacklistedKeywords)).ToList();
            }
            else if (filterKeywords != null)
            {
                options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence"
                                                      && x.ObjectName.Name.ContainsAny(StringComparison.InvariantCultureIgnoreCase, filterKeywords)).ToList();
            }
            else if (blacklistedKeywords != null)
            {
                options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence"
                                                      && !x.ObjectName.Name.ContainsAny(blacklistedKeywords)).ToList();
            }
            else
            {
                options = gPackage.Exports.Where(x => x.ClassName == "AnimSequence").ToList();
            }

            if (options.Any())
            {
                // Pick a random element
                var randomGestureExport = options.RandomElement();

                // Filter it out if we cannot use it
                var seqLength = randomGestureExport.GetProperty<FloatProperty>("SequenceLength");

                int numRetries = 7;
                while (seqLength < minLength && numRetries >= 0)
                {
                    randomGestureExport = options.RandomElement();
                    seqLength = randomGestureExport.GetProperty<FloatProperty>("SequenceLength");
                    numRetries--;
                }

                var portedInExp = PackageTools.PortExportIntoPackage(targetPackage, randomGestureExport);
                if (!hasCache)
                {
                    cache.ReleasePackages();
                }

                return new Gesture(portedInExp);
            }

            return null;
        }
    }
}
