using System;
using System.Collections.Generic;
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
        public static Gesture InstallRandomGestureAsset(GameTarget target, IMEPackage package, float minSequenceLength = 0, MERPackageCache cache = null)
        {
            var gestureFiles = MERUtilities.ListStaticAssets("binary.gestures");
            var randGestureFile = gestureFiles.RandomElement();
            cache ??= new MERPackageCache(target);
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

            var portedInExp = PackageTools.PortExportIntoPackage(target, package, randomGestureExport);

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
            var bioAnimSet = gesture.GetBioAnimSet(export.FileRef, Game2Gestures.GestureSetNameToPackageExportName);
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

        public static string GetPackageExportNameForGestureSet(string gestureset)
        {
            Game2Gestures.GestureSetNameToPackageExportName.TryGetValue(gestureset, out var res);
            return res;
        }

        public static bool IsGesturePackage(string packagename)
        {
            return Game2Gestures.GestureSetNameToPackageExportName.Values.Any(x => x == packagename);
        }


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

        public static Gesture InstallRandomFilteredGestureAsset(GameTarget target, IMEPackage targetPackage, float minLength = 0, string[] filterKeywords = null, string[] blacklistedKeywords = null, string[] mainPackagesAllowed = null, bool includeSpecial = false, MERPackageCache cache = null)
        {
            var gestureFiles = MERUtilities.ListStaticPackageAssets(target, "Gestures", false);

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
            cache ??= new MERPackageCache(target);
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

                var portedInExp = PackageTools.PortExportIntoPackage(target, targetPackage, randomGestureExport);
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
