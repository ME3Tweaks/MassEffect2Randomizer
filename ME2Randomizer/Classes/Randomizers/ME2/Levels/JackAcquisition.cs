using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class JackAcquisition
    {
        /// <summary>
        /// People who survived ME1's onslaught of SizeSixteens' stream
        /// </summary>
        private static string[] prisonerNames = new[]
        {
            "Nalie Walie",
            "Jed Ted",
            "Mok",
            "Shamrock Snipes",
            "Steeler Wayne",
            "Castle Arrrgh",
            "Bev",
            "Lurxx",
            "Chirra Kitteh",
            "Daynan",
            "dnc510", // new
            "Audemus" // new
        };

        public static bool PerformRandomization(RandomizationOption option)
        {
            ChangePrisonerNames();
            return true;
        }

        private static int BeatPrisonerTLKID = 7892170;
        private static int BeatPrisonerGuardTLKID = 7892171;

        private static void ChangePrisonerNames()
        {
            var prisonerNameChoices = prisonerNames.ToList();
            prisonerNameChoices.Shuffle();
            TLKHandler.ReplaceString(342079, prisonerNameChoices.PullFirstItem()); // Prisoner 780
            TLKHandler.ReplaceString(342078, prisonerNameChoices.PullFirstItem()); // Prisoner 403
            TLKHandler.ReplaceString(BeatPrisonerTLKID, prisonerNameChoices.PullFirstItem()); // Beat Prisoner
            TLKHandler.ReplaceString(BeatPrisonerGuardTLKID, prisonerNameChoices.PullFirstItem()); // Beating Guard

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
