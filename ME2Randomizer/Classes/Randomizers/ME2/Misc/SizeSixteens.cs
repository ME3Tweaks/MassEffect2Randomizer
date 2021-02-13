using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class SizeSixteens
    {
        /// <summary>
        /// People who survived ME1's onslaught of SizeSixteens' stream
        /// </summary>
        private static string[] SizeSixteenChatMembers = new[]
        {
            // DIED IN ME1
            //"Red Falcon",
            //"Rocket Boy",
            //"John Doe",
            //"Red Line"

            // Survivors from ME1
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

            // New people in game
            "Admiral Kirk",
            "Darth Krytie",
            "Peeress Sabine",  
            "DNC 510",
            "Horny Heracross",
            "Thorg",
            "Invisible Guardian",
            "Nox"
        };

        private static List<string> AvailableMembers;

        public static void ResetClass()
        {
            AvailableMembers = SizeSixteenChatMembers.ToList();
            AvailableMembers.Shuffle();
        }

        private static string GetMember()
        {
            return AvailableMembers.PullFirstItem();
        }

        public static bool InstallSSChanges(RandomizationOption option)
        {
            // Archangel mission
            TLKHandler.ReplaceString(236473, SizeSixteens.GetMember()); // Guy
            TLKHandler.ReplaceString(233780, SizeSixteens.GetMember()); // Freelancer (captain)

            // Jack mission
            TLKHandler.ReplaceString(343493, SizeSixteens.GetMember()); // Technician
            ChangePrisonerNames();

            // Freedoms progress
            SetVeetorFootage();

            // Omega Hub
            TLKHandler.ReplaceString(282031, SizeSixteens.GetMember()); // Annoyed Human

            // Omega VIP (MwL)
            TLKHandler.ReplaceString(338532, SizeSixteens.GetMember()); // Vij (tickets guy)
            TLKHandler.ReplaceString(236437, SizeSixteens.GetMember()); // Meln (drunk turian)

            // Gernasback (Jacob Mission)
            TLKHandler.ReplaceString(266999, SizeSixteens.GetMember()); // Survivor
            TLKHandler.ReplaceString(267000, SizeSixteens.GetMember()); // Survivor
            TLKHandler.ReplaceString(267001, SizeSixteens.GetMember()); // Survivor
            TLKHandler.ReplaceString(287918, SizeSixteens.GetMember()); // Survivor
            TLKHandler.ReplaceString(287919, SizeSixteens.GetMember()); // Survivor
            TLKHandler.ReplaceString(287920, SizeSixteens.GetMember()); // Survivor
            FixFirstSurvivorNameBchLmL();


            return true;
        }

        private static void FixFirstSurvivorNameBchLmL()
        {

            // Make it so the beating scene shows names
            var beachPathF = MERFileSystem.GetPackageFile("BioD_BchLmL_102BeachFight.pcc");
            if (beachPathF != null)
            {
                var beachPathP = MEPackageHandler.OpenMEPackage(beachPathF);

                // Make memory unique
                var tlkId = TLKHandler.GetNewTLKID();
                TLKHandler.ReplaceString(tlkId, GetMember());
                beachPathP.GetUExport(700).WriteProperty(new StringRefProperty(tlkId, "ActorGameNameStrRef"));
                beachPathP.GetUExport(700).ObjectName = "survivor_female_MER";

                MERFileSystem.SavePackage(beachPathP);
            }
        }


        private static int BeatPrisonerTLKID = 7892170;
        private static int BeatPrisonerGuardTLKID = 7892171;

        private static void ChangePrisonerNames()
        {
            TLKHandler.ReplaceString(342079, SizeSixteens.GetMember()); // Prisoner 780
            TLKHandler.ReplaceString(342078, SizeSixteens.GetMember()); // Prisoner 403
            TLKHandler.ReplaceString(BeatPrisonerTLKID, SizeSixteens.GetMember()); // Beat Prisoner
            TLKHandler.ReplaceString(BeatPrisonerGuardTLKID, SizeSixteens.GetMember()); // Beating Guard

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

        private static void SetVeetorFootage()
        {
            var moviedata = RTextureMovie.GetTextureMovieAssetBinary("Veetor.size_mer.bik");
            var veetorFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioD_ProFre_501Veetor")).ToList();
            foreach (var v in veetorFiles)
            {
                Log.Information($@"Setting veetor footage in {v}");
                var mpackage = MERFileSystem.GetPackageFile(v);
                var package = MEPackageHandler.OpenMEPackage(mpackage);
                var veetorExport = package.FindExport("BioVFX_Env.Hologram.ProFre_501_VeetorFootage");
                if (veetorExport != null)
                {
                    RTextureMovie.RandomizeExportDirect(veetorExport, null, moviedata);
                }
                MERFileSystem.SavePackage(package);
            }
        }
    }
}
