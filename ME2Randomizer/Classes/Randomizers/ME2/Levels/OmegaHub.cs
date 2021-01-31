using System;
using System.IO;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME3ExplorerCore.Dialogue;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public static class OmegaHub
    {
        private static void RandomizeShepDance()
        {
            var vipLoungeLF = MERFileSystem.GetPackageFile(@"BioD_OmgHub_500DenVIP_LOC_INT.pcc");
            if (vipLoungeLF != null && File.Exists(vipLoungeLF))
            {
                var vipLounge = MEPackageHandler.OpenMEPackage(vipLoungeLF);

                var playerDanceInterpData = vipLounge.GetUExport(547);
                var randomNewGesture1 = RBioEvtSysTrackGesture.InstallRandomGestureAsset(vipLounge);
                var randomNewGesture2 = RBioEvtSysTrackGesture.InstallRandomGestureAsset(vipLounge);
                var danceGestureData = RBioEvtSysTrackGesture.GetGestures(playerDanceInterpData);
                danceGestureData[0] = randomNewGesture1;
                danceGestureData[1] = randomNewGesture2;
                RBioEvtSysTrackGesture.WriteGestures(playerDanceInterpData, danceGestureData);

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

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            RandomizeShepDance();
            return true;
        }
    }
}
