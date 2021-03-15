using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Dialogue;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Kismet;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public static class OmegaHub
    {
        private static string[] danceKeywords = new[] { "Dancing", "Dismiss", "Across_The_Throat", "Begging", "Sexy", "ROM", };
        public readonly static string[] notDanceKeywords = new[] { "Idle", "Base", "Standing", "Twitch", };

        private static void RandomizeVIPShepDance()
        {
            var vipLoungeLF = MERFileSystem.GetPackageFile(@"BioD_OmgHub_500DenVIP_LOC_INT.pcc");
            if (vipLoungeLF != null && File.Exists(vipLoungeLF))
            {
                var vipLounge = MEPackageHandler.OpenMEPackage(vipLoungeLF);

                var playerDanceInterpData = vipLounge.GetUExport(547);
                var c = new MERPackageCache();

                InstallShepardDanceGesture(playerDanceInterpData, c); // Paragon
                InstallShepardDanceGesture(vipLounge.GetUExport(559), c); // Stupid shep lol


                // Make able to dance again and again in convo
                var danceTalk = vipLounge.GetUExport(217);
                var bc = new ConversationExtended(danceTalk);
                bc.LoadConversation(null);
                bc.StartingList.Clear();
                bc.StartingList.Add(0, 2);
                bc.SerializeNodes();

                MERFileSystem.SavePackage(vipLounge);
            }

            // make able to always talk to dancer
            var vipLoungeF = MERFileSystem.GetPackageFile(@"BioD_OmgHub_500DenVIP.pcc");
            if (vipLoungeF != null && File.Exists(vipLoungeF))
            {
                var vipLounge = MEPackageHandler.OpenMEPackage(vipLoungeF);
                var selectableBool = vipLounge.GetUExport(8845);
                selectableBool.WriteProperty(new IntProperty(1, "bValue"));
                MERFileSystem.SavePackage(vipLounge);
            }
        }


        private static void RandomizeAfterlifeShepDance()
        {
            var denDanceF = MERFileSystem.GetPackageFile(@"BioD_OmgHub_230DenDance.pcc");
            if (denDanceF != null)
            {
                var loungeP = MEPackageHandler.OpenMEPackage(denDanceF);
                var sequence = loungeP.GetUExport(3924);

                MERPackageCache cache = new MERPackageCache();
                List<InterpTools.InterpData> interpDatas = new List<InterpTools.InterpData>();
                var interp1 = loungeP.GetUExport(3813);

                // Make 2 additional dance options by cloning the interp and the data tree
                var interp2 = SeqTools.CloneBasicSequenceObject(interp1);
                var interp3 = SeqTools.CloneBasicSequenceObject(interp1);


                // Clone the interp data for attaching to 2 and 3
                var interpData1 = loungeP.GetUExport(1174);
                var interpData2 = EntryCloner.CloneTree(interpData1);
                var interpData3 = EntryCloner.CloneTree(interpData2);
                KismetHelper.AddObjectToSequence(interpData2, sequence);
                KismetHelper.AddObjectToSequence(interpData3, sequence);

                // Load ID for randomization
                interpDatas.Add(new InterpTools.InterpData(interpData1));
                interpDatas.Add(new InterpTools.InterpData(interpData2));
                interpDatas.Add(new InterpTools.InterpData(interpData3));


                // Chance the data for interp2/3
                var id2 = SeqTools.GetVariableLinksOfNode(interp2);
                id2[0].LinkedNodes[0] = interpData2;
                SeqTools.WriteVariableLinksToNode(interp2, id2);

                var id3 = SeqTools.GetVariableLinksOfNode(interp3);
                id3[0].LinkedNodes[0] = interpData3;
                SeqTools.WriteVariableLinksToNode(interp3, id3);

                // Add additional finished states for fadetoblack when done
                KismetHelper.CreateOutputLink(loungeP.GetUExport(958), "Finished", interp2, 2);
                KismetHelper.CreateOutputLink(loungeP.GetUExport(958), "Finished", interp3, 2);


                // Link up the random choice it makes
                var randSw = SeqTools.InstallRandomSwitchIntoSequence(sequence, 3);
                KismetHelper.CreateOutputLink(randSw, "Link 1", interp1);
                KismetHelper.CreateOutputLink(randSw, "Link 2", interp2);
                KismetHelper.CreateOutputLink(randSw, "Link 3", interp3);

                // Break the original output to start the interp, repoint it's output to the switch instead
                var sgm = loungeP.GetUExport(1003); //set gesture mode
                KismetHelper.RemoveOutputLinks(sgm);
                KismetHelper.CreateOutputLink(sgm, "Done", loungeP.GetUExport(960));
                KismetHelper.CreateOutputLink(sgm, "Done", randSw);

                // Now install the dances
                foreach (var id in interpDatas)
                {
                    var danceTrack = id.InterpGroups[0].Tracks[0];
                    OmegaHub.InstallShepardDanceGesture(danceTrack.Export, cache);
                }

                MERFileSystem.SavePackage(loungeP);
            }
        }

        public static bool InstallShepardDanceGesture(ExportEntry danceTrackExp, MERPackageCache cache)
        {
            cache ??= new MERPackageCache();

            var danceGestureData = RBioEvtSysTrackGesture.GetGestures(danceTrackExp);
            var newGestures = new List<RBioEvtSysTrackGesture.Gesture>();

            int i = danceGestureData.Count + 1; // The default pose is the +1
            while (i > 0)
            {
                newGestures.Add(RBioEvtSysTrackGesture.InstallRandomFilteredGestureAsset(danceTrackExp.FileRef, 6, danceKeywords, notDanceKeywords, null, true, cache));
                i--;
            }

            for (int k = 0; k < danceGestureData.Count; k++)
            {
                danceGestureData[k] = newGestures.PullFirstItem();
            }
            RBioEvtSysTrackGesture.WriteGestures(danceTrackExp, danceGestureData);

            // Update the default pose
            RBioEvtSysTrackGesture.WriteDefaultPose(danceTrackExp, newGestures.PullFirstItem());

            return true;
        }

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            RandomizeVIPShepDance();
            RandomizeAfterlifeShepDance();
            RandomizeALDancers();
            return true;
        }

        private static void RandomizeALDancers()
        {
            {
                var denBar = MERFileSystem.GetPackageFile(@"BioD_OmgHub_220DenBar.pcc");
                if (denBar != null)
                {
                    var denBarP = MEPackageHandler.OpenMEPackage(denBar);
                    RandomizeDancer(denBarP.GetUExport(1287));
                    RandomizeDancer(denBarP.GetUExport(1288));
                    RandomizeDancer(denBarP.GetUExport(1289));
                    RandomizeDancer(denBarP.GetUExport(1292));
                    RandomizeDancer(denBarP.GetUExport(1293));
                    MERFileSystem.SavePackage(denBarP);
                }
            }

            var denDance = MERFileSystem.GetPackageFile(@"BioD_OmgHub_230DenDance.pcc");
            if (denDance != null)
            {
                var denDanceP = MEPackageHandler.OpenMEPackage(denDance);
                RandomizeDancer(denDanceP.GetUExport(1257)); //sit
                RandomizeDancer(denDanceP.GetUExport(1250));
                RandomizeDancer(denDanceP.GetUExport(1251));

                // shep sits at dancer. it uses different pawn.
                var entertainerBPSKM = denDanceP.GetUExport(4322);
                var newInfo = IlliumHub.DancerOptions.RandomElement();
                while (newInfo.Location != null || newInfo.Rotation != null || newInfo.KeepHead == false)
                {
                    // I don't want anything that requires specific positioning data, and I want to keep the head.
                    newInfo = IlliumHub.DancerOptions.RandomElement();
                }

                var newDancerMDL = PackageTools.PortExportIntoPackage(denDanceP, newInfo.BodyAsset.GetAsset());
                entertainerBPSKM.WriteProperty(new ObjectProperty(newDancerMDL,"SkeletalMesh"));
                MERFileSystem.SavePackage(denDanceP);
            }
        }

        private static void RandomizeDancer(ExportEntry skeletalMeshActorMatArchetype)
        {
            // Install new head and body assets
            var newInfo = IlliumHub.DancerOptions.RandomElement();
            while (newInfo.Location != null || newInfo.Rotation != null || (newInfo.BodyAsset != null && !newInfo.BodyAsset.IsAssetFileAvailable()) || (newInfo.HeadAsset != null && !newInfo.HeadAsset.IsAssetFileAvailable()))
            {
                // Make sure assets are available, if not, repick
                // I don't want anything that requires specific positioning data
                newInfo = IlliumHub.DancerOptions.RandomElement();
            }

            var newBody = PackageTools.PortExportIntoPackage(skeletalMeshActorMatArchetype.FileRef, newInfo.BodyAsset.GetAsset());

            var bodySM = skeletalMeshActorMatArchetype.GetProperty<ObjectProperty>("SkeletalMeshComponent").ResolveToEntry(skeletalMeshActorMatArchetype.FileRef) as ExportEntry;
            var headSM = skeletalMeshActorMatArchetype.GetProperty<ObjectProperty>("HeadMesh").ResolveToEntry(skeletalMeshActorMatArchetype.FileRef) as ExportEntry;

            bodySM.WriteProperty(new ObjectProperty(newBody.UIndex, "SkeletalMesh"));

            if (newInfo.HeadAsset != null)
            {
                var newHead = PackageTools.PortExportIntoPackage(skeletalMeshActorMatArchetype.FileRef, newInfo.HeadAsset.GetAsset());
                headSM.WriteProperty(new ObjectProperty(newHead.UIndex, "SkeletalMesh"));
            }
            else if (!newInfo.KeepHead)
            {
                headSM.RemoveProperty("SkeletalMesh");
            }


            if (newInfo.DrawScale != 1)
            {
                // Install DS3D on the archetype. It works. Not gonna question it
                var ds = new CFVector3()
                {
                    X = newInfo.DrawScale,
                    Y = newInfo.DrawScale,
                    Z = newInfo.DrawScale,
                };
                skeletalMeshActorMatArchetype.WriteProperty(ds.ToLocationStructProperty("DrawScale3D")); //hack
            }

            if (newInfo.MorphFace != null)
            {
                var newHead = PackageTools.PortExportIntoPackage(skeletalMeshActorMatArchetype.FileRef, newInfo.MorphFace.GetAsset());
                headSM.WriteProperty(new ObjectProperty(newHead.UIndex, "MorphHead"));
            }
        }
    }
}
