using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game3.Misc
{
    /// <summary>
    /// Randomizer for changing what spawns from a SFXSeqAct_AIFactory2
    /// Probably pretty dangerous in some parts of game if fat enemy can't spawn
    /// in skinny land
    /// </summary>
    internal class RSFXSeqAct_AIFactory2
    {
        private static bool CanRandomize(ExportEntry export)
        {
            // Look for AIFactory2 objects
            if (!export.IsDefaultObject && export.ClassName == "SFXSeqAct_AIFactory2")
            {
                return true;
            }
            return false;
        }

        private class DecookerInfo
        {
            /// <summary>
            /// Which file to open to find the data in
            /// </summary>
            public string SourceFileName { get; set; }

            /// <summary>
            /// Contains the asset path and the destination package name
            /// </summary>
            public SeekFreeInfo SeekFreeInfo { get; set; }
        }

        private class SeekFreeInfo
        {
            /// <summary>
            /// The entry path to map to a package
            /// </summary>
            public string EntryPath { get; set; }
            /// <summary>
            /// The package that contains the specified EntryPath for loading if not in memory
            /// </summary>
            public string SeekFreePackage { get; set; }
            /// <summary>
            /// Generates the struct text used in Coalesced files
            /// </summary>
            public string GetSeekFreeStructText() => $"(ObjectName=\"{EntryPath}\",SeekFreePackageName=\"{SeekFreePackage}\", bReplicate=true)";
        }

        public static bool Init(GameTarget target, RandomizationOption option)
        {
            // Split out packages to prepare them for use in seek free
            var decooksRequired = new List<DecookerInfo>()
            {
                // OMEGA DLC
                new DecookerInfo()
                {
                    SourceFileName = "BioD_OMG003_125LitExtra.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Omega_Enemies.Archetypes.Adjutant_Combat",
                        SeekFreePackage = "SFXPawn_Adjutant"
                    }
                },
                new DecookerInfo()
                {
                    SourceFileName = "BioD_Omg004_300Fan.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Omega_Enemies.Archetypes.Rampart_Combat",
                        SeekFreePackage = "SFXPawn_Rampart"
                    }
                },

                // CITADEL - MP ENHANCEMENTS
                new DecookerInfo()
                {
                    SourceFileName = "BioD_CitSim_CrbrsC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Phoenix",
                        SeekFreePackage = "SFXPawn_Phoenix"
                    }
                },
                new DecookerInfo()
                {
                    SourceFileName = "BioD_CitSim_GthB.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.GethBomber",
                        SeekFreePackage = "SFXPawn_GethBomber"
                    }
                },

                // CITADEL - MP COLLECTORS
                new DecookerInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Abomination",
                        SeekFreePackage = "SFXPawn_Abomination"
                    }
                },
                new DecookerInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrB.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.CollectorTrooper",
                        SeekFreePackage = "SFXPawn_CollectorTrooper"
                    }
                },
                new DecookerInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.CollectorCaptain",
                        SeekFreePackage = "SFXPawn_CollectorCaptain"
                    }
                },
                new DecookerInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Scion",
                        SeekFreePackage = "SFXPawn_Scion"
                    }
                },
                new DecookerInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Praetorian",
                        SeekFreePackage = "SFXPawn_Praetorian"
                    }
                },
            };

            MERPackageCache gc = new MERPackageCache(target);
            MERPackageCache c = new MERPackageCache(target);
            option.ProgressMax = decooksRequired.Count;
            option.ProgressValue = 0;
            option.ProgressIndeterminate = false;
            option.CurrentOperation = "Decooking pawn files";

            var engine = CoalescedHandler.GetIniFile("BioEngine.xml");
            var sfxengine = engine.GetOrAddSection("sfxgame.sfxengine");
            List<CoalesceValue> mappings = new List<CoalesceValue>();

            foreach (var di in decooksRequired)
            {
                var cachedPacakge = c.GetCachedPackage(di.SourceFileName);
                var objRef = MEREasyPorts.PortExportIntoPackage(target, "ObjectReferencer_0", cachedPacakge); // Add the object reference if it 
                objRef.WriteProperty(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(cachedPacakge.FindExport(di.SeekFreeInfo.EntryPath).UIndex) }, "ReferencedObjects")); // Write the reference - overwrite if same cached package

                var outPath = Path.Combine(MERFileSystem.DLCModCookedPath, $"{di.SeekFreeInfo.SeekFreePackage}.pcc");
                var results = EntryExporter.ExportExportToFile(objRef, outPath, out _, globalCache: gc, pc: c);
                if (results != null)
                {
                    foreach (var v in results)
                    {
                        Debug.WriteLine(v.Message);
                    }
                }

                mappings.Add(new CoalesceValue(di.SeekFreeInfo.GetSeekFreeStructText(), CoalesceParseAction.AddUnique));
                option.ProgressValue++;
            }

            // Add seek free info
            sfxengine.AddEntry(new CoalesceProperty("dynamicloadmapping", mappings));
            return true;
        }

        public static bool RandomizeSpawnSets(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var sequence = SeqTools.GetParentSequence(export);
            if (sequence != null)
            {
                var nodesInSeq = SeqTools.GetAllSequenceElements(sequence);
                var connectionsToAIFactory = SeqTools.FindOutboundConnectionsToNode(export, nodesInSeq.OfType<ExportEntry>());

                if (Enumerable.Any(connectionsToAIFactory))
                {
                    // Install randomization
                    var aiFactoryTypeShuffler = SequenceObjectCreator.CreateSequenceObject(export.FileRef, MERCustomClasses.RandomizeSpawnSets);
                    var aiFactorySeqObj = SequenceObjectCreator.CreateSequenceObject(export.FileRef, "SeqVar_Object");
                    aiFactorySeqObj.WriteProperty(new ObjectProperty(export, "ObjValue"));

                    // Install the randomization object and point to AIFactory2
                    KismetHelper.AddObjectsToSequence(sequence, false, aiFactoryTypeShuffler, aiFactorySeqObj);
                    KismetHelper.CreateOutputLink(aiFactoryTypeShuffler, "Out", export, 0); // Connect from Out to Spawn
                    KismetHelper.CreateVariableLink(aiFactoryTypeShuffler, "ActorFactory", aiFactorySeqObj); // Connect variable link to ActorFactory reference object

                    // Repoint all the original inputs to Spawn to ours.
                    foreach (var connection in connectionsToAIFactory)
                    {
                        var outbound = SeqTools.GetOutboundLinksOfNode(connection);

                        // Enumerate output links
                        bool changed = false;
                        foreach (var outL in outbound)
                        {
                            // Enumerate each link from that output
                            foreach (var outLL in outL)
                            {
                                if (outLL.LinkedOp == export && outLL.InputLinkIdx == 0) // SPAWN
                                {
                                    // Repoint to our shuffler
                                    outLL.LinkedOp = aiFactoryTypeShuffler;
                                    changed = true;
                                }
                            }
                        }

                        // Commit the changes
                        if (changed)
                        {
                            SeqTools.WriteOutboundLinksToNode(connection, outbound);
                        }
                    }


                }

            }

            return true;
        }
    }
}
