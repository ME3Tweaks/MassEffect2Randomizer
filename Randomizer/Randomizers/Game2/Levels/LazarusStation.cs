using ME3TweaksCore.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;
using Randomizer.Shared;
using Randomizer.Randomizers.Handlers;

namespace Randomizer.Randomizers.Game2.Levels
{
    /// <summary>
    /// ProCer randomizer
    /// </summary>
    internal class LazarusStation
    {

        internal static bool PerformRandomization(GameTarget target, RandomizationOption notUsed)
        {
            InstallRandomFirstWeapon(target);
            MakeItCreepy(target);
            return true;
        }

        /// <summary>
        /// Changes the weapon you get from the locker.
        /// </summary>
        /// <param name="target"></param>
        private static void InstallRandomFirstWeapon(GameTarget target)
        {
            var rezRoom = MERFileSystem.GetPackageFile(target, "BioD_ProCer_100RezRoom.pcc");
            var rezRoomP = MERFileSystem.OpenMEPackage(rezRoom);

            var getPistolSeq = rezRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS5_Go_Get_Pistol");

            // Install new object
            var giveRandWeapon = SequenceObjectCreator.CreateSequenceObject(rezRoomP, "MERSeqAct_GiveRandomWeapon");
            KismetHelper.AddObjectToSequence(giveRandWeapon, getPistolSeq);

            // Hook new object target to player
            KismetHelper.CreateVariableLink(giveRandWeapon, "Target", rezRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS5_Go_Get_Pistol.SeqVar_Player_1"));

            // Output from new inventory object to the correct next item
            KismetHelper.CreateOutputLink(giveRandWeapon, "Out", rezRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS5_Go_Get_Pistol.BioSeqAct_SetIsInCombat_1"));

            var seqObjs = KismetHelper.GetSequenceObjects(getPistolSeq).OfType<ExportEntry>().Where(x => x.ClassName == "SeqAct_GiveInventory");
            foreach (var giveInv in seqObjs)
            {
                // Remove the weapon given to the player in this object, we have assigned it ourselves assign it ourselves
                var invList = giveInv.GetProperty<ArrayProperty<ObjectProperty>>("InventoryList");

                // We do this in case another mod changes the initial weapon
                // Hopefully they use name including weapon
                invList.RemoveAll(x => x.ResolveToEntry(rezRoomP).ObjectName.Instanced.Contains("Weapon"));
                giveInv.WriteProperty(invList);
                KismetHelper.RemoveOutputLinks(giveInv);
                KismetHelper.CreateOutputLink(giveInv, "Out", giveRandWeapon);
            }

            // Patch out the reload tutorial
            var mirandaLookForThermalClip = rezRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS10_Reload_Heatsink.BioSeqAct_FaceOnlyVO_11");
            var gotoLS15PlotStatus = rezRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS10_Reload_Heatsink.SequenceReference_1");
            KismetHelper.RemoveOutputLinks(mirandaLookForThermalClip);
            KismetHelper.CreateOutputLink(mirandaLookForThermalClip, "Done", gotoLS15PlotStatus);


            MERFileSystem.SavePackage(rezRoomP);


            // Add weapon options to config file

            var bioWeapon = CoalescedHandler.GetIniFile(@"BioWeapon.ini");
            var merGiveRandomWeapon = bioWeapon.GetOrAddSection(@"MERGameContentKismet.MERSeqAct_GiveRandomWeapon");
            var playerWeaponsList = MEREmbedded.GetEmbeddedTextAsset(@"playerweaponifps.txt")
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var ew in playerWeaponsList)
            {
                // Lines starting with // are 'commented' out - mainly so during dev
                // we can force specific lines
                if (!ew.StartsWith("//"))
                {
                    merGiveRandomWeapon.AddEntryIfUnique(new CoalesceProperty(@"WeaponsList", new CoalesceValue(ew, CoalesceParseAction.AddUnique)));
                }
            }
        }

        private static void MakeItCreepy(GameTarget target)
        {
            var procerBioDFiles = MERFileSystem.LoadedFiles.Where(x => x.Key.StartsWith("BioD_ProCer_", StringComparison.InvariantCultureIgnoreCase) && x.Key.GetUnrealLocalization() == MELocalization.None).ToList();
            var creepyPrefab = MEPackageHandler.OpenMEPackageFromStream(MEREmbedded.GetEmbeddedPackage(MEGame.LE2, "SeqPrefabs.MakeItCreepy.pcc"), @"MakeItCreepy.pcc");
            var creepySeq = creepyPrefab.FindExport("MakeItCreepy");

            foreach (var procerFile in procerBioDFiles)
            {
                if (
                    procerFile.Key.Equals("BioD_ProCer_300ShuttleBay.pcc", StringComparison.InvariantCultureIgnoreCase)
                    || procerFile.Key.Equals("BioD_ProCer_350BriefRoom.pcc",
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    // These seem to crash game...?
                    continue;
                }
                var fileToUse = MERFileSystem.GetPackageFile(target, procerFile.Key);
                var proCerP = MERFileSystem.OpenMEPackage(fileToUse);

                // Get all the biopawns in the level
                var actors = proCerP.Exports.Where(x => x.InstancedFullPath.StartsWith("TheWorld.PersistentLevel") && x.ClassName is @"BioPawn" or "SFXSkeletalMeshActor" or "SFXSkeletalMeshActorMAT").ToList();

                if (actors.Any())
                {
                    var newSeq = MERSeqTools.InstallSequenceStandalone(creepySeq, proCerP);
                    var objListIFP = newSeq.InstancedFullPath + ".SeqVar_ObjectList_0";
                    var objList = proCerP.FindExport(objListIFP);
                    var list = objList.GetProperty<ArrayProperty<ObjectProperty>>("ObjList");
                    list.AddRange(actors.Select(x => new ObjectProperty(x.UIndex))); // Add the items to the list
                    objList.WriteProperty(list);
                }

                MERFileSystem.SavePackage(proCerP);
            }
        }
    }
}
