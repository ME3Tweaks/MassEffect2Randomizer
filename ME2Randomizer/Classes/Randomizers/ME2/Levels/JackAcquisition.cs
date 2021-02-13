using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class JackAcquisition
    {

        public static bool PerformRandomization(RandomizationOption option)
        {
            ChangePrisonerNames();
            return true;
        }

        private static int BeatPrisonerTLKID = 7892170;
        private static int BeatPrisonerGuardTLKID = 7892171;

        private static void ChangePrisonerNames()
        {
            TLKHandler.ReplaceString(342079, SizeSixteensChatHandler.GetMember()); // Prisoner 780
            TLKHandler.ReplaceString(342078, SizeSixteensChatHandler.GetMember()); // Prisoner 403
            TLKHandler.ReplaceString(BeatPrisonerTLKID, SizeSixteensChatHandler.GetMember()); // Beat Prisoner
            TLKHandler.ReplaceString(BeatPrisonerGuardTLKID, SizeSixteensChatHandler.GetMember()); // Beating Guard

            // Make it so the beating scene shows names
            var cellBLock3F = MERFileSystem.GetPackageFile("BioD_PrsCvA_103CellBlock03.pcc");
            if (cellBLock3F != null)
            {
                var cellBlock3P = MEPackageHandler.OpenMEPackage(cellBLock3F);

                // Clone the turianguard pawn type so we can change the name, maybe something else if we want
                var newGuardBPCST = EntryCloner.CloneTree(cellBlock3P.GetUExport(701), true);
                newGuardBPCST.ObjectName = "MER_NamedBeatGuard";
                newGuardBPCST.WriteProperty(new StringRefProperty(BeatPrisonerGuardTLKID, "ActorGameNameStrRef"));
                cellBlock3P.GetUExport(668).WriteProperty(new ObjectProperty(newGuardBPCST, "ActorType"));

                // Change shown name for the prisoner
                cellBlock3P.GetUExport(699).WriteProperty(new StringRefProperty(BeatPrisonerTLKID, "ActorGameNameStrRef"));

                // Make the two people 'selectable' so they show up with names
                cellBlock3P.GetUExport(682).RemoveProperty("m_bTargetableOverride"); // guard
                cellBlock3P.GetUExport(677).RemoveProperty("m_bTargetableOverride"); // prisoner

                MERFileSystem.SavePackage(cellBlock3P);
            }
        }
    }
}
