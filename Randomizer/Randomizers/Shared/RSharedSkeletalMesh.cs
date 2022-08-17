using System;
using System.Collections.Generic;
using System.Linq;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Shared
{
    class RSharedSkeletalMesh
    {
        public static bool InstallRandomMICKismetObject(GameTarget target, IMEPackage package, RandomizationOption option)
        {
            var mainSeq = package.FindExport("TheWorld.PersistentLevel.Main_Sequence");
            if (mainSeq == null)
                return true; // Nothing to do here

            var actors = package.GetLevelActors();
            var actorsToLink = new List<ExportEntry>();
#if __GAME1__
            foreach (var actor in actors.Where(x => x.IsA("BioPawn")))
#elif __GAME2__
            foreach (var actor in actors.Where(x => x.IsA("SFXSkeletalMeshActor"))) // BioPawn, SkelActorMAT
#elif __GAME3__
            foreach (var actor in actors.Where(x => x.IsA("SFXSkeletalMeshActor") || x.IsA("SFXStuntActor")|| x.IsA("SFXPawn")))
#endif
            {
                actorsToLink.Add(actor);
            }

            if (actorsToLink.Count > 0)
            {
                List<ExportEntry> addedSeqObjs = new List<ExportEntry>();
                // 1. Create LevelLoaded - our own, for simplicity.
                var levelLoaded = SequenceObjectCreator.CreateSequenceObject(package, "SeqEvent_LevelLoaded", MERCaches.GlobalCommonLookupCache);
                addedSeqObjs.Add(levelLoaded);

                // 2. Link the randomizer sequence object
                var randomizerObj = SequenceObjectCreator.CreateSequenceObject(package, "MERSeqAct_RandomizeMaterials", MERCaches.GlobalCommonLookupCache);
                addedSeqObjs.Add(randomizerObj);


                // 3. Create the child objects
                var varLinks = SeqTools.GetVariableLinksOfNode(randomizerObj);
                foreach (var a in actorsToLink)
                {
                    var pawnRef = SequenceObjectCreator.CreateSequenceObject(package, "SeqVar_Object", null);
                    pawnRef.WriteProperty(new ObjectProperty(a.UIndex, "ObjValue"));
                    addedSeqObjs.Add(pawnRef);
                    varLinks[0].LinkedNodes.Add(pawnRef);
                }

                // 4. Link the child objects
                SeqTools.WriteVariableLinksToNode(randomizerObj, varLinks);

                // 5. Add everything to the sequence
                KismetHelper.AddObjectsToSequence(mainSeq, false, addedSeqObjs.ToArray());

                // 6. Link the loaded event to the randomier
                KismetHelper.CreateOutputLink(levelLoaded, "Loaded and Visible", randomizerObj);
            }

            return true;
        }

        public static SkeletalMesh FuzzSkeleton(ExportEntry export, RandomizationOption option)
        {
            // Goofs up the RefSkeleton values
            var objBin = ObjectBinary.From<SkeletalMesh>(export);
            foreach (var bone in objBin.RefSkeleton)
            {
                if (!bone.Name.Name.Contains("eye", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("sneer", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("nose", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("brow", StringComparison.InvariantCultureIgnoreCase)
                    && !bone.Name.Name.Contains("jaw", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                var v3 = bone.Position;
                v3.X *= ThreadSafeRandom.NextFloat(1 - (option.SliderValue / 2), 1 + option.SliderValue);
                v3.Y *= ThreadSafeRandom.NextFloat(1 - (option.SliderValue / 2), 1 + option.SliderValue);
                v3.Z *= ThreadSafeRandom.NextFloat(1 - (option.SliderValue / 2), 1 + option.SliderValue);
                bone.Position = v3;
            }
            export.WriteBinary(objBin);
            return objBin;
        }
    }
}
