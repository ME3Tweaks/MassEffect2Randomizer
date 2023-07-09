using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.Misc;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    /// <summary>
    /// Gesture Randomizer for Gestures in InterpDatas
    /// </summary>
    class RBioEvtSysTrackGesture
    {
        /// <summary>
        /// Gets the gestures used by the BioEvtSysTrack export
        /// </summary>
        /// <param name="sysTrack"></param>
        /// <returns></returns>
        public static List<Gesture> GetSysTrackGestures(ExportEntry sysTrack)
        {
            var gestures = sysTrack.GetProperty<ArrayProperty<StructProperty>>("m_aGestures");
            return gestures?.Select(x => new Gesture(x)).ToList();
        }

        /// <summary>
        /// Writes the list of gestures to the sysTrack, then looks up the parent path to find the sequence's biodynamicanim sets and ensures the values are in them. Does not support adding additional items
        /// </summary>
        /// <param name="export"></param>
        /// <param name="gestures"></param>
        public static void WriteSysTrackGestures(ExportEntry export, List<Gesture> gesturesToWrite)
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
                InstallDynamicAnimSetRefForKismet(ref owningSequence, export, ngesture);
            }

            export.WriteProperty(gestures);
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

        public static void WriteDefaultPose(ExportEntry export, Gesture newPose)
        {
            var props = export.GetProperties();
            props.AddOrReplaceProp(new NameProperty(newPose.GestureSet, "nmStartingPoseSet"));
            props.AddOrReplaceProp(new NameProperty(newPose.GestureAnim, "nmStartingPoseAnim"));
            export.WriteProperties(props);
            ExportEntry owningSeq = null;
            InstallDynamicAnimSetRefForKismet(ref owningSeq, export, newPose);
        }

        /*
        private static void VerifyGesturesWork(ExportEntry trackExport)
        {
            var gestures = RBioEvtSysTrackGesture.GetSysTrackGestures(trackExport);
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
        private static void InstallDynamicAnimSetRefForKismet(ref ExportEntry owningSequence, ExportEntry export, Gesture gesture)
        {
            // Find owning sequence
            if (owningSequence == null)
                owningSequence = export;
            while (owningSequence.ClassName != "Sequence")
            {
                //owningSequence = SeqTools.GetParentSequence(owningSequence);

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
            var bioAnimSet = GestureManager.GetBioAnimSet(gesture, export.FileRef);
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
    }
}