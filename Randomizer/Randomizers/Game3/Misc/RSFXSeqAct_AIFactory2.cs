using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
                // Make sure we aren't spawning dummy pawns
                var ss = export.GetProperty<ArrayProperty<StructProperty>>("SpawnSets");
                if (ss != null)
                {
                    foreach (var structProperty in ss)
                    {
                        var types = structProperty.GetProp<ArrayProperty<ObjectProperty>>("Types");
                        foreach (var type in types)
                        {
                            if (type.Value == 0)
                                continue; // Skip an empty type
                            var r = type.ResolveToEntry(export.FileRef);
                            if (r.ObjectName.Name.Contains("Dummy", StringComparison.InvariantCultureIgnoreCase))
                            {
                                Debug.WriteLine($@">>>> SKIPPING RSFXAIFactory {r.ObjectName}");
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            return false;
        }

        public static bool Init(GameTarget target, RandomizationOption option)
        {
            // Setup functions to allow custom enemies
            var sfxGame = ScriptTools.InstallScriptToPackage(target, "SFXGame.pcc", "SFXModule_DamagePlayer.SFXTakeDamage", "PlayerTeamDominate.SFXTakeDamage.uc", false, false);

            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXAI_Henchman.AddOrder"), "PlayerTeamDominate.AddOrder.uc");
            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXAI_Henchman.CanInstantlyUsePowers"), "PlayerTeamDominate.CanInstantlyUsePowers.uc");
            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXAI_Henchman.CanQueueOrder"), "PlayerTeamDominate.CanQueueOrder.uc");
            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXAI_Henchman.CanUsePowers"), "PlayerTeamDominate.CanUsePowers.uc");
            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXAI_Henchman.HasAnyEnemies"), "PlayerTeamDominate.HasAnyEnemies.uc");
            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXAI_Henchman.MoveToCoverNearHoldLocation"), "PlayerTeamDominate.MoveToCoverNearHoldLocation.uc");
            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXAI_Henchman.ShouldAttack"), "PlayerTeamDominate.ShouldAttack.uc");

            ScriptTools.InstallScriptToExport(sfxGame.FindExport("SFXPawn_Henchman.Downed.BeginState"), "PlayerTeamDominate.DownedBeginState.uc");
            // Setup functions to allow teammates to change teams against the player

            MERFileSystem.SavePackage(sfxGame);

            // Ensure we have prepared the custom action assets and seek free for boost/climb
            RPawnStats.PrepareDynamicResources(target);

            // Split out packages to prepare them for use in seek free
            var decooksRequired = new List<ObjectDecookInfo>()
            {
                // OMEGA DLC
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_OMG003_125LitExtra.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Omega_Enemies.Archetypes.Adjutant_Combat",
                        SeekFreePackage = "SFXPawn_Adjutant"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_Omg004_300Fan.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Omega_Enemies.Archetypes.Rampart_Combat",
                        SeekFreePackage = "SFXPawn_Rampart"
                    }
                },

                // CITADEL - MP ENHANCEMENTS
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_CrbrsC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Phoenix",
                        SeekFreePackage = "SFXPawn_Phoenix"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_GthB.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.GethBomber",
                        SeekFreePackage = "SFXPawn_GethBomber"
                    }
                },

                // CITADEL - MP COLLECTORS
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Abomination",
                        SeekFreePackage = "SFXPawn_Abomination"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrB.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.CollectorTrooper",
                        SeekFreePackage = "SFXPawn_CollectorTrooper"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.CollectorCaptain",
                        SeekFreePackage = "SFXPawn_CollectorCaptain"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Scion",
                        SeekFreePackage = "SFXPawn_Scion"
                    }
                },
                new ObjectDecookInfo()
                {
                    SourceFileName = "BioD_CitSim_CllctrC.pcc",
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_Shared.Praetorian",
                        SeekFreePackage = "SFXPawn_Praetorian"
                    }
                },


                // CUSTOM SPECIAL
                new ObjectDecookInfo()
                {
                    SourceFileName = null,
                    SeekFreeInfo = new SeekFreeInfo()
                    {
                        EntryPath = "Char_Enemies_MER.Archetypes.Reapers.CorruptBanshee",
                        SeekFreePackage = "SFXPawn_CorruptBanshee"
                    }
                },
            };
            MERDecooker.DecookObjectsToPackages(target, option, decooksRequired, "Decooking enemy pawn files", true);
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

                if (connectionsToAIFactory.Any())
                {
                    // Install randomization
                    var aiFactoryTypeShuffler = SequenceObjectCreator.CreateSequenceObject(export.FileRef, MERCustomClasses.SpawnModifier, MERCaches.GlobalCommonLookupCache);
                    var aiFactorySeqObj = SequenceObjectCreator.CreateSequenceObject(export.FileRef, "SeqVar_Object", MERCaches.GlobalCommonLookupCache);
                    var spawnedSeqObj = SequenceObjectCreator.CreateSequenceObject(export.FileRef, "SeqVar_Object", MERCaches.GlobalCommonLookupCache);
                    aiFactorySeqObj.WriteProperty(new ObjectProperty(export, "ObjValue"));

                    // Install the randomization object and point to AIFactory2
                    KismetHelper.AddObjectsToSequence(sequence, false, aiFactoryTypeShuffler, aiFactorySeqObj, spawnedSeqObj);
                    KismetHelper.CreateOutputLink(aiFactoryTypeShuffler, "Out", export, 0); // Connect from Out to Spawn
                    KismetHelper.CreateVariableLink(aiFactoryTypeShuffler, "ActorFactory", aiFactorySeqObj); // Connect variable link to ActorFactory reference object
                    KismetHelper.CreateVariableLink(export, "Last Spawned", spawnedSeqObj); // Link spawned to Pawn
                    KismetHelper.CreateVariableLink(aiFactoryTypeShuffler, "Pawn", spawnedSeqObj); // Link spawned to Pawn

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


                    // Change outputs for Spawned to link to our OnSpawned

                    var outLinksT = export.GetProperty<ArrayProperty<StructProperty>>("OutputLinks").FirstOrDefault(x => x.GetProp<StrProperty>("LinkDesc") == "Spawned");
                    if (outLinksT != null)
                    {
                        var outLinks = outLinksT.GetProp<ArrayProperty<StructProperty>>("Links");
                        MERSeqTools.RemoveAllNamedOutputLinks(export, "Spawned"); // Remove all existing nodes
                        KismetHelper.CreateOutputLink(export, "Spawned", aiFactoryTypeShuffler, 1); // Link to our OnSpawned

                        var aifProps = aiFactoryTypeShuffler.GetProperties();
                        aifProps.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[1].Properties.AddOrReplaceProp(outLinks);
                        aiFactoryTypeShuffler.WriteProperties(aifProps);
                    }
                }
            }

            return true;
        }
    }
}
