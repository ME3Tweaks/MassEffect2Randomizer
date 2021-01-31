using System;
using System.IO;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public static class OmegaHub
    {
        private static void RandomizeShepDance()
        {
            // Relay at the end of the DLC
            var vipLoungeF = MERFileSystem.GetPackageFile(@"BioD_OmgHub_500DenVIP_LOC_INT.pcc");
            if (vipLoungeF != null && File.Exists(vipLoungeF))
            {
                var vipLounge = MEPackageHandler.OpenMEPackage(vipLoungeF);

                var playerDanceInterpData = vipLounge.GetUExport(547);
                var randomNewGesture1 = RBioEvtSysTrackGesture.InstallRandomGestureAsset(vipLounge);
                var randomNewGesture2 = RBioEvtSysTrackGesture.InstallRandomGestureAsset(vipLounge);
                var danceGestureData = RBioEvtSysTrackGesture.GetGestures(playerDanceInterpData);
                danceGestureData[0] = randomNewGesture1;
                danceGestureData[1] = randomNewGesture2;
                RBioEvtSysTrackGesture.WriteGestures(playerDanceInterpData, danceGestureData);

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
