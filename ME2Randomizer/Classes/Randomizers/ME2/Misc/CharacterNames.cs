using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ALOTInstallerCore.Helpers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using Microsoft.Win32;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class CharacterNames
    {
        /// <summary>
        /// Static list that is not modified beyond loading
        /// </summary>
        private static List<string> PawnNames { get; } = new List<string>();
        /// <summary>
        /// List actually used to pull from
        /// </summary>
        private static List<string> PawnNameListInstanced { get; } = new List<string>();

        /// <summary>
        /// Allows loading a list of names for pawns
        /// </summary>
        public static void SetupRandomizer(RandomizationOption option)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Title = "Select text file with list of names, one per line",
                Filter = "Text files|*.txt",
            };
            var result = ofd.ShowDialog();
            if (result.HasValue && result.Value)
            {
                try
                {
                    PawnNames.ReplaceAll(File.ReadAllLines(ofd.FileName));
                    option.Description = $"{PawnNames.Count} name(s) loaded for randomization";
                }
                catch (Exception e)
                {
                    MERLog.Exception(e, "Error reading names for CharacterNames randomizer");
                }
            }
        }


        private static int BeatPrisonerTLKID = 7892170;
        private static int BeatPrisonerGuardTLKID = 7892171;

        private static void ChangePrisonerNames()
        {
            InstallName(342079); // Prisoner 780
            InstallName(342078); // Prisoner 403
            var didPrisoner = InstallName(BeatPrisonerTLKID) != 0; // Beat Prisoner
            var didGuard = InstallName(BeatPrisonerGuardTLKID) != 0; // Beating Guard

            if (didGuard && didPrisoner)
            {
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

        public static bool InstallNameSet(RandomizationOption option)
        {
            // Setup
            PawnNameListInstanced.ReplaceAll(PawnNames);
            PawnNameListInstanced.Shuffle();

            // Archangel mission
            InstallName(236473); // Guy
            InstallName(233780); // Freelancer (captain)

            // Jack mission
            InstallName(343493); // Technician
            ChangePrisonerNames();

            // Omega Hub
            InstallName(282031); // Annoyed Human
            InstallName(351554); // Zaeed DLC - prisoner
            InstallName(282029); // Elcor bouncer
            InstallName(212201); // Dancer
            InstallName(193689); // Bartender

            // Omega - Professor acquisition
            InstallName(214818); // Sick Batarian
            InstallName(183669); // Guard
            InstallName(263271); // Guard

            InstallName(183671); // Human Looter
            InstallName(342063); // Human Looter
            InstallName(184147); // Refugee
            InstallName(184148); // Refugee

            // Citadel - Thane mission?
            InstallName(334752); // Ambassador


            // Omega VIP (MwL)
            InstallName(338532); // Vij (tickets guy)
            InstallName(236437); // Meln (drunk turian)

            // Horizon CR1
            InstallName(341935); // Colonist
            InstallName(342012); // Colonist

            // Gernsback (Jacob Mission)
            InstallName(266999); // Survivor
            InstallName(267000); // Survivor
            InstallName(267001); // Survivor
            InstallName(287918); // Survivor
            InstallName(287919); // Survivor
            InstallName(287920); // Survivor
            FixFirstSurvivorNameBchLmL();

            // Geth Acqusition
            InstallName(342401); // Cerb scientist 1
            InstallName(342402); // Cerb scientist 2

            // illium twrhub
            InstallName(263290); // Colonist
            InstallName(262481); // Slave Broker
            InstallName(262479); // Quarian

            // Overlord
            InstallName(360543); // Engineer 1
            InstallName(361166); // Engineer 2
            InstallName(361165); // Lab Tech

            // Citadel
            InstallName(345666); // Game salesman
            InstallName(252048); // Used ships salesman
            InstallName(229974); // Barkeep
            InstallName(345460); // Gunnery chief

            // Tuchanka
            InstallName(234580); // Sick Krogan

            //N7VIQ2_VI
            InstallName(343549); // The VI name?



            // ??? - Jack mission maybe?
            InstallName(186968); // Technician

            Debug.WriteLine($"COUNT: {count}");
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
                var tlkId = InstallName();
                if (tlkId != 0)
                {
                    beachPathP.GetUExport(700).WriteProperty(new StringRefProperty(tlkId, "ActorGameNameStrRef"));
                    beachPathP.GetUExport(700).ObjectName = "survivor_female_MER";
                    MERFileSystem.SavePackage(beachPathP);
                }
            }
        }

        private static int count = 0;

        private static int InstallName(int stringId = 0)
        {
            count++;
            if (PawnNameListInstanced.Any())
            {
                var newPawnName = PawnNameListInstanced.PullFirstItem();
                if (stringId == 0)
                    stringId = TLKHandler.GetNewTLKID();
                TLKHandler.ReplaceString(stringId, newPawnName);
                return stringId;
            }

            return 0; // Not changed
        }

    }
}
