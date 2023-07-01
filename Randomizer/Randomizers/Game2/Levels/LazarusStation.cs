using ME3TweaksCore.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game2.Levels
{
    /// <summary>
    /// ProCer randomizer
    /// </summary>
    internal class LazarusStation
    {

        internal static bool PerformRandomization(GameTarget target, RandomizationOption notUsed)
        {
            MakeItCreepy(target);
            return true;
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
