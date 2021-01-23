using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Enemy;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.TLK.ME2ME3;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Unreal;
using Microsoft.WindowsAPICodePack.Taskbar;
using Serilog;
using ME3ExplorerCore.Misc;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.ME2.TextureAssets;
using ME2Randomizer.Classes.Randomizers.Utility;

namespace ME2Randomizer.Classes
{
    public class Randomizer
    {
        private MainWindow mainWindow;
        private BackgroundWorker randomizationWorker;
        private List<char> scottishVowelOrdering;
        private List<char> upperScottishVowelOrdering;

        // Files that should not be generally passed over
#if __ME2__
        private static List<string> SpecializedFiles { get; } = new List<string>()
        {
            "BioP_Char"
        };
#elif __ME3__

#endif

        public Randomizer(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            scottishVowelOrdering = null; //will be set when needed.
            upperScottishVowelOrdering = null;
        }

        /// <summary>
        /// Are we busy randomizing?
        /// </summary>
        public bool Busy => randomizationWorker != null && randomizationWorker.IsBusy;

        /// <summary>
        /// The options selected by the user that will be used to determine what the randomizer does
        /// </summary>
        public OptionsPackage SelectedOptions { get; set; }


        public void Randomize(OptionsPackage op)
        {
            SelectedOptions = op;
            ThreadSafeRandom.Reset();
            if (!SelectedOptions.UseMultiThread)
            {
                ThreadSafeRandom.SetSingleThread(SelectedOptions.Seed);
            }

            randomizationWorker = new BackgroundWorker();
            randomizationWorker.DoWork += PerformRandomization;
            randomizationWorker.RunWorkerCompleted += Randomization_Completed;

            var seedStr = mainWindow.SeedTextBox.Text;
            if (!int.TryParse(seedStr, out int seed))
            {
                seed = new Random().Next();
                mainWindow.SeedTextBox.Text = seed.ToString();
            }

            Log.Information("-------------------------STARTING RANDOMIZER WITH SEED " + seed + "--------------------------");
            randomizationWorker.RunWorkerAsync();
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate, mainWindow);
        }



        private void Randomization_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, mainWindow);
            mainWindow.CurrentOperationText = "Randomization complete";
            mainWindow.AllowOptionsChanging = true;
            mainWindow.ShowProgressPanel = false;
        }

        private void PerformRandomization(object sender, DoWorkEventArgs e)
        {
            mainWindow.CurrentOperationText = "Initializing randomizer";
            mainWindow.ProgressBarIndeterminate = true;
            var specificRandomizers = SelectedOptions.SelectedOptions.Where(x => !x.IsExportRandomizer).ToList();

            MERFileSystem.InitMERFS(SelectedOptions.UseMERFS, SelectedOptions.SelectedOptions.Any(x => x.RequiresTLK));


            // Prepare the TLK
#if __ME2__
            ME2Textures.SetupME2Textures();
#elif __ME3__
            ME3Textures.SetupME3Textures();
#endif

            // Pass 1: All randomizers that are file specific
            foreach (var sr in specificRandomizers)
            {
                mainWindow.CurrentOperationText = $"Randomizing {sr.HumanName}";
                sr.PerformSpecificRandomizationDelegate?.Invoke(sr);
            }


            // Pass 2: All exports
            var perExportRandomizers = SelectedOptions.SelectedOptions.Where(x => x.IsExportRandomizer).ToList();
            if (perExportRandomizers.Any())
            {
                mainWindow.CurrentOperationText = "Getting list of files...";
                mainWindow.ProgressBarIndeterminate = true;

                var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false, false).Values.ToList();

                mainWindow.ProgressBarIndeterminate = false;
                mainWindow.ProgressBar_Bottom_Max = files.Count();
                mainWindow.ProgressBar_Bottom_Min = 0;
                int currentFileNumber = 0;
#if DEBUG
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = SelectedOptions.UseMultiThread ? 3 : 1 }, (file) =>
#else
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = SelectedOptions.UseMultiThread ? 4 : 1 }, (file) =>
#endif
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (SpecializedFiles.Contains(name)) return; // Do not run randomization on this file as it's only done by specialized randomizers (e.g. char creator)
                    // Todo: Filter out BioD_Nor_103aGalaxyMap.pcc so we don't randomize galaxy map by accident
                    // Todo: Filter out BioP_Char so we don't randomize it by accident

                    bool loggedFilePath = false;
                    mainWindow.CurrentProgressValue = Interlocked.Increment(ref currentFileNumber);
                    mainWindow.CurrentOperationText = $"Randomizing game files [{currentFileNumber}/{files.Count()}]";

                    if (//!file.Contains("SFXGame", StringComparison.InvariantCultureIgnoreCase)
                    //&& !file.Contains("Cit", StringComparison.InvariantCultureIgnoreCase)
                    !file.Contains("Exp1", StringComparison.InvariantCultureIgnoreCase)
                        &&
                    !file.Contains("SFXGame", StringComparison.InvariantCultureIgnoreCase)
                    //&& !file.Contains("BIOG_", StringComparison.InvariantCultureIgnoreCase)
                    //&& !file.Contains("startup", StringComparison.InvariantCultureIgnoreCase)
                    )
                        return;

                    var package = MEPackageHandler.OpenMEPackage(file);
                    foreach (var exp in package.Exports.ToList()) //Tolist cause if we add export it will cause modification
                    {
                        foreach (var r in perExportRandomizers)
                        {
                            r.PerformRandomizationOnExportDelegate(exp, r);
                        }

                        /*

                        // NO MORE DEFAULT OBJECTS AFTER THIS LINE
                        //Randomize faces
                        if (mainWindow.RANDSETTING_BIOMORPHFACES && exp.ClassName == "BioMorphFace")
                        {
                            //Face randomizer
                            if (!loggedFilePath)
                            {
                                Log.Information("Randomizing file: " + file);
                                loggedFilePath = true;
                            }

                            RandomizeBioMorphFace(exp,  morphFaceRandomizationAmount);
                        }

                        else if (exp.ClassName == "BioPawn")
                        {
                            if (mainWindow.RANDSETTING_MISC_MAPPAWNSIZES && ThreadSafeRandom.Next(4) == 0)
                            {
                                if (!loggedFilePath)
                                {
                                    Log.Information("Randomizing file: " + file);
                                    loggedFilePath = true;
                                }

                                //Pawn size randomizer
                                RandomizeBioPawnSize(exp,  0.4);
                            }


                        }

                        else if (mainWindow.RANDSETTING_MISC_INTERPS && exp.ClassName == "InterpTrackMove" && ThreadSafeRandom.Next(4) == 0)
                        {
                            if (!loggedFilePath)
                            {
                                Log.Information("Randomizing file: " + file);
                                loggedFilePath = true;
                            }

                            //Interpolation randomizer
                            RandomizeInterpTrackMove(exp,  morphFaceRandomizationAmount);
                        }
                        */
                    }

                    MERFileSystem.SavePackage(package);
                });

                //if (mainWindow.RANDSETTING_MISC_ENEMYAIDISTANCES)
                //{
                //    RandomizeAINames(package, random);
                //}
            }

            mainWindow.ProgressBarIndeterminate = true;
            mainWindow.CurrentOperationText = "Finishing up";

            // Close out files and free memory
            TFCBuilder.EndTFCs();
            CoalescedHandler.EndHandler();
            TLKHandler.EndHandler();
            NonSharedPackageCache.Cache.ReleasePackages();
        }


        /// <summary>
        /// Sets the options up that can be selected and their methods they call
        /// </summary>
        /// <param name="RandomizationGroups"></param>
        internal static void SetupOptions(ObservableCollectionExtended<RandomizationGroup> RandomizationGroups)
        {
#if __ME2__
            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Faces",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption()
                    {
                        Description="Changes facial animation. The best feature of MER",
                        HumanName = "FaceFX animation", Ticks = "1,2,3,4,5", HasSliderOption = true, IsRecommended = true, SliderToTextConverter = rSetting =>
                            rSetting switch
                            {
                                1 => "Oblivion",
                                2 => "Knights of the old Republic",
                                3 => "Sonic Adventure",
                                4 => "Source filmmaker",
                                5 => "Total madness",
                                _ => "Error"
                            },
                        SliderValue = 4, // This must come after the converter
                        PerformRandomizationOnExportDelegate = RFaceFXAnimSet.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                    new RandomizationOption() {HumanName = "Squadmate faces",
                        Description = "Only works on Wilson and Jacob, unfortunately. Other squadmates are fully modeled",
                        PerformSpecificRandomizationDelegate = RBioMorphFace.RandomizeSquadmateFaces,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                    new RandomizationOption()
                    {
                        HumanName = "NPC faces",
                        Ticks = "0.1,0.2,0.3,0.4,0.5,0.6,0.7",
                        HasSliderOption = true,
                        IsRecommended = true,
                        SliderToTextConverter = rSetting => $"Randomization amount: {rSetting}",
                        SliderValue = .3, // This must come after the converter
                        PerformRandomizationOnExportDelegate = RBioMorphFace.RandomizeExport,
                        Description="Changes the BioFaceMorph used by some pawns",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                    },
                    new RandomizationOption()
                    {
                        HumanName = "NPC Faces - Extra jacked up",
                        Description = "Changes the MorphTargets that map bones to the face morph system",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        PerformRandomizationOnExportDelegate = RMorphTarget.RandomizeGlobalExport
                    },
                    new RandomizationOption() {HumanName = "Eyes (excluding Illusive Man)",
                        Description="Changes the colors of eyes",
                        IsRecommended = true,
                        PerformRandomizationOnExportDelegate = REyes.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                    new RandomizationOption() {HumanName = "Illusive Man eyes",
                        Description="Changes the Illusive Man's eye color",
                        IsRecommended = true, PerformRandomizationOnExportDelegate = RIllusiveEyes.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                    }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Characters",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption()
                    {
                        HumanName = "Animation Set Bones",
                        PerformRandomizationOnExportDelegate = RBioAnimSetData.RandomizeExport,
                        SliderToTextConverter = RBioAnimSetData.UIConverter,
                        HasSliderOption = true,
                        SliderValue = 1,
                        Ticks = "1,2,3,4,5",
                        Description = "Changes the order of animations mapped to bones. E.g. arm rotation will be swapped with eyes",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal
                    },
                    new RandomizationOption() {HumanName = "NPC colors", Description="Changes NPC colors such as skin tone, hair, etc",
                        PerformRandomizationOnExportDelegate = RMaterialInstance.RandomizeNPCExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal},
                    new RandomizationOption() {
                        HumanName = "Romance",
                        Description="Randomizes which romance you will get",
                        PerformSpecificRandomizationDelegate = Romance.PerformRandomization,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning},
                    new RandomizationOption() {
                        HumanName = "Look At Definitions",
                        Description="Changes how pawns look at things",
                        PerformRandomizationOnExportDelegate = RBioLookAtDefinition.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {
                        HumanName = "Look At Targets",
                        Description="Changes where pawns look",
                        PerformRandomizationOnExportDelegate = RBioLookAtTarget.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe
                    },
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Character Creator",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Premade faces", IsRecommended = true,
                        Description = "Completely randomizes settings including skin tones and slider values. Adds extra premade faces",
                        PerformSpecificRandomizationDelegate = CharacterCreator.RandomizeCharacterCreator,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Iconic FemShep face",
                        Description="Changes the default FemShep face. Iconic Maleshep is modeled",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                        Ticks = "0.1,0.2,0.3,0.4,0.5,0.6,0.7",
                        HasSliderOption = true,
                        IsRecommended = true,
                        SliderToTextConverter = rSetting => $"Randomization amount: {rSetting}",
                        SliderValue = .3, // This must come after the converter
                        PerformSpecificRandomizationDelegate = CharacterCreator.RandomizeIconicFemShep
                    },
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Miscellaneous",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Hologram colors", Description="Changes colors of holograms",PerformRandomizationOnExportDelegate = RHolograms.RandomizeExport, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Drone colors", Description="Changes colors of drones",PerformRandomizationOnExportDelegate = CombatDrone.RandomizeExport},
                    //new RandomizationOption() {HumanName = "Omnitool", Description="Changes colors of omnitools",PerformRandomizationOnExportDelegate = ROmniTool.RandomizeExport},
                    new RandomizationOption() {HumanName = "Specific textures",Description="Changes specific textures to more fun ones", PerformRandomizationOnExportDelegate = TFCBuilder.RandomizeExport, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Skip minigames", Description="Skip all minigames. Doesn't even load the UI, just skips them entirely", PerformRandomizationOnExportDelegate = SkipMiniGames.DetectAndSkipMiniGameSeqRefs, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal}
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Movement & pawns",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "NPC movement speeds", Description = "Changes non-player movement stats", PerformRandomizationOnExportDelegate = PawnMovementSpeed.RandomizeMovementSpeed, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Player movement speeds", Description = "Changes player movement stats", PerformSpecificRandomizationDelegate = PawnMovementSpeed.RandomizePlayerMovementSpeed, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal},
                    //new RandomizationOption() {HumanName = "NPC walking routes", PerformRandomizationOnExportDelegate = RRoute.RandomizeExport}, // Seems very specialized in ME2
                    new RandomizationOption() {HumanName = "Hammerhead", Description = "Changes HammerHead stats",PerformSpecificRandomizationDelegate = HammerHead.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal}
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Weapons & Enemies",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Weapon stats", Description = "Attempts to change gun stats in a way that makes game still playable"},
                    new RandomizationOption() {HumanName = "Usable weapon classes", Description = "Changes what guns the player and squad can use. Requires DLC option for Zaeed and Kasumi", PerformSpecificRandomizationDelegate = Weapons.RandomizeSquadmateWeapons},
                    new RandomizationOption() {HumanName = "Enemy AI", Description = "Changes enemy AI so they behave differently", PerformRandomizationOnExportDelegate = PawnAI.RandomizeExport},
                    new RandomizationOption() {HumanName = "Enemy loadouts",Description = "Gives enemies different guns", PerformRandomizationOnExportDelegate = EnemyWeaponChanger.RandomizeExport, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning},
                    new RandomizationOption() {HumanName = "Enemy powers", Description = "Gives enemies different powers", PerformRandomizationOnExportDelegate = EnemyPowerChanger.RandomizeExport, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning},
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Level-specific",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {
                        HumanName = "Galaxy Map",
                        Description = "Moves things around the map, speeds up normandy",
                        PerformSpecificRandomizationDelegate = GalaxyMap.RandomizeGalaxyMap,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning,
                        SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                        {
                            new RandomizationOption()
                            {
                                SubOptionKey = GalaxyMap.SUBOPTIONKEY_INFINITEGAS,
                                HumanName = "Infinite fuel",
                                Description = "Prevents the Normandy from running out of fuel. Prevents possible softlock due to randomization",
                                Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe,
                                IsOptionOnly = true
                            }
                        }
                    },
                    new RandomizationOption() {HumanName = "Normandy", Description = "Changes various things around the ship, including one sidequest", PerformSpecificRandomizationDelegate = Normandy.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    //new RandomizationOption() {HumanName = "Prologue"},
                    new RandomizationOption() {HumanName = "Citadel", Description = "Changes various things", PerformSpecificRandomizationDelegate = Citadel.PerformRandomization, RequiresTLK = true},
                    new RandomizationOption() {HumanName = "Freedom's Progress", Description = "Changes the monster", PerformSpecificRandomizationDelegate = FreedomsProgress.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Archangel Acquisition", Description = "Makes ArchAngel deadly", PerformSpecificRandomizationDelegate = ArchangelAcquisition.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Overlord DLC", Description = "Changes many things across the DLC", PerformSpecificRandomizationDelegate = OverlordDLC.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal},
                    new RandomizationOption() {HumanName = "Arrival DLC", Description = "Changes the relay colors", PerformSpecificRandomizationDelegate = ArrivalDLC.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Kasumi DLC", Description = "Changes the art gallery", PerformSpecificRandomizationDelegate = KasumiDLC.PerformRandomization, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Suicide Mission", Description = "Changes a few things in-level and post-level (renegade)", PerformSpecificRandomizationDelegate = CollectorBase.PerformRandomization},
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Level components",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Star colors", IsRecommended = true, PerformRandomizationOnExportDelegate = RBioSun.PerformRandomization},
                    new RandomizationOption() {HumanName = "Fog colors", Description = "Changes colors of fog", IsRecommended = true, PerformRandomizationOnExportDelegate = RHeightFogComponent.RandomizeExport},
                    new RandomizationOption() {
                        HumanName = "Post Processing volumes",
                        Description = "Changes postprocessing. Likely will make some areas of game unplayable",
                        PerformRandomizationOnExportDelegate = RPostProcessingVolume.RandomizeExport,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_RIP
                    },
                    new RandomizationOption() {HumanName = "Light colors", Description = "Changes colors of dynamic lighting", PerformRandomizationOnExportDelegate = RLighting.RandomizeExport},
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Text",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Game over text", PerformSpecificRandomizationDelegate = RTexts.RandomizeGameOverText, RequiresTLK = true, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Intro Crawl", PerformSpecificRandomizationDelegate = RTexts.RandomizeIntroText, RequiresTLK = true, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Safe},
                    new RandomizationOption() {HumanName = "Vowels", Description="Swaps vowels in text", PerformSpecificRandomizationDelegate = RTexts.RandomizeVowels, RequiresTLK = true, Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Warning},
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Wackadoodle",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {
                        HumanName = "Actors in cutscenes",
                        Description="Swaps pawns around in animated cutscenes. May break some due to complexity, but often hilarious",
                        PerformRandomizationOnExportDelegate = Cutscene.ShuffleCutscenePawns,
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal
                    },
                    new RandomizationOption() {
                            HumanName = "Animation data",
                            PerformRandomizationOnExportDelegate = RAnimSequence.RandomizeExport,
                            SliderToTextConverter = RAnimSequence.UIConverter,
                            HasSliderOption = true,
                            SliderValue = 1,
                            Ticks = "1,2",
                            Description="Shifts rigged bone positions",
                            Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Random interpolations",
                        Description = "Randomly fuzzes interpolation data. Can break the game, but can also be entertaining",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Unsafe
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Conversation Wheel", PerformRandomizationOnExportDelegate = RBioConversation.RandomizeExport,
                        Description = "Changes replies in wheel. Can make conversations hard to exit",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Unsafe
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Actors in conversations",
                        PerformRandomizationOnExportDelegate = RSFXSeqAct_StartConversation.RandomizeExport,
                        Description = "Changes pawn roles in conversations"
                    },
                    new RandomizationOption()
                    {
                        HumanName = "Enable basic friendly fire",
                        PerformSpecificRandomizationDelegate = SFXGame.TurnOnFriendlyFire,
                        Description = "Enables weapons to damage friendlies",
                        Dangerousness = RandomizationOption.EOptionDangerousness.Danger_Normal,
                        SubOptions = new ObservableCollectionExtended<RandomizationOption>()
                        {
                            new RandomizationOption()
                            {
                                IsOptionOnly = true,
                                SubOptionKey = SFXGame.SUBOPTIONKEY_CARELESSFF,
                                HumanName = "Careless mode",
                                Description = "Attack enemies, regardless of friendly casualties"
                            }
                        }
                    }
                }
            });

            foreach (var g in RandomizationGroups)
            {
                g.Options.Sort(x => x.HumanName);
            }
            RandomizationGroups.Sort(x => x.GroupName);
#endif

        }



        private void randomizeMorphTarget(ExportEntry morphTarget)
        {
            MemoryStream ms = new MemoryStream(morphTarget.Data);
            ms.Position = morphTarget.propsEnd();
            var numLods = ms.ReadInt32();

            for (int i = 0; i < numLods; i++)
            {
                var numVertices = ms.ReadInt32();
                var diff = ThreadSafeRandom.NextFloat(-0.2, 0.2);
                for (int k = 0; k < numVertices; k++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var fVal = ms.ReadFloat();
                        ms.Position -= 4;
                        ms.WriteFloat(fVal + diff);
                    }

                    ms.WriteByte((byte)ThreadSafeRandom.Next(256));
                    ms.ReadByte();
                    ms.ReadByte();
                    ms.ReadByte();
                    ms.SkipInt16(); //idx
                }

                ms.SkipInt32(); //Vertices?/s
            }

            morphTarget.Data = ms.ToArray();
        }


        private void RandomizePlanetMaterialInstanceConstant(ExportEntry planetMaterial, bool realistic = false)
        {
            var props = planetMaterial.GetProperties();
            {
                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                scalars[0].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, 1.0); //Horizon Atmosphere Intensity
                if (ThreadSafeRandom.Next(4) == 0)
                {
                    scalars[2].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, 0.7); //Atmosphere Min (how gas-gianty it looks)
                }
                else
                {
                    scalars[2].GetProp<FloatProperty>("ParameterValue").Value = 0; //Atmosphere Min (how gas-gianty it looks)
                }

                scalars[3].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(.5, 1.5); //Atmosphere Tiling U
                scalars[4].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(.5, 1.5); //Atmosphere Tiling V
                scalars[5].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(.5, 4); //Atmosphere Speed
                scalars[6].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0.5, 12); //Atmosphere Fall off...? seems like corona intensity

                foreach (var vector in vectors)
                {
                    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                    RStructs.RandomizeTint(paramValue, false);
                }
            }
            planetMaterial.WriteProperties(props);
        }


        private void scaleHeadMesh(ExportEntry meshRef, float headScale)
        {
            Log.Information("Randomizing headmesh for " + meshRef.InstancedFullPath);
            var drawScale = meshRef.GetProperty<FloatProperty>("Scale");
            var drawScale3D = meshRef.GetProperty<StructProperty>("Scale3D");
            if (drawScale != null)
            {
                drawScale.Value = headScale * drawScale.Value;
                meshRef.WriteProperty(drawScale);
            }
            else if (drawScale3D != null)
            {
                PropertyCollection p = drawScale3D.Properties;
                p.AddOrReplaceProp(new FloatProperty(headScale, "X"));
                p.AddOrReplaceProp(new FloatProperty(headScale, "Y"));
                p.AddOrReplaceProp(new FloatProperty(headScale, "Z"));
                meshRef.WriteProperty(drawScale3D);
            }
            else
            {
                FloatProperty scale = new FloatProperty(headScale, "Scale");
                /*
                PropertyCollection p = new PropertyCollection();
                p.AddOrReplaceProp(new FloatProperty(headScale, "X"));
                p.AddOrReplaceProp(new FloatProperty(headScale, "Y"));
                p.AddOrReplaceProp(new FloatProperty(headScale, "Z"));
                meshRef.WriteProperty(new StructProperty("Vector", p, "Scale3D", true));*/
                meshRef.WriteProperty(scale);
            }
        }

        private void RandomizeInterpTrackMove(ExportEntry export, double amount)
        {
            Log.Information($"[{Path.GetFileNameWithoutExtension(export.FileRef.FilePath)}] Randomizing movement interpolations for " + export.UIndex + ": " + export.InstancedFullPath);
            var props = export.GetProperties();
            var posTrack = props.GetProp<StructProperty>("PosTrack");
            if (posTrack != null)
            {
                var points = posTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            x.Value = x.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
                            y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
                            z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
                        }
                    }
                }
            }

            var eulerTrack = props.GetProp<StructProperty>("EulerTrack");
            if (eulerTrack != null)
            {
                var points = eulerTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            if (x.Value != 0)
                            {
                                x.Value = x.Value * ThreadSafeRandom.NextFloat(1 - amount * 3, 1 + amount * 3);
                            }
                            else
                            {
                                x.Value = ThreadSafeRandom.NextFloat(0, 360);
                            }

                            if (y.Value != 0)
                            {
                                y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - amount * 3, 1 + amount * 3);
                            }
                            else
                            {
                                y.Value = ThreadSafeRandom.NextFloat(0, 360);
                            }

                            if (z.Value != 0)
                            {
                                z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - amount * 3, 1 + amount * 3);
                            }
                            else
                            {
                                z.Value = ThreadSafeRandom.NextFloat(0, 360);
                            }
                        }
                    }
                }
            }

            export.WriteProperties(props);
        }

        private void RandomizeBioPawnSize(ExportEntry export, double amount)
        {
            Log.Information($"[{Path.GetFileNameWithoutExtension(export.FileRef.FilePath)}] Randomizing pawn size for " + export.UIndex + ": " + export.InstancedFullPath);
            var props = export.GetProperties();
            StructProperty sp = props.GetProp<StructProperty>("DrawScale3D");
            if (sp == null)
            {
                var structprops = ME2UnrealObjectInfo.getDefaultStructValue("Vector", true);
                sp = new StructProperty("Vector", structprops, "DrawScale3D", ME2UnrealObjectInfo.IsImmutableStruct("Vector"));
                props.Add(sp);
            }

            if (sp != null)
            {
                //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.FullPath + " vPos");
                FloatProperty x = sp.GetProp<FloatProperty>("X");
                FloatProperty y = sp.GetProp<FloatProperty>("Y");
                FloatProperty z = sp.GetProp<FloatProperty>("Z");
                if (x.Value == 0) x.Value = 1;
                if (y.Value == 0) y.Value = 1;
                if (z.Value == 0) z.Value = 1;
                x.Value = x.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
                y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
                z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - amount, 1 + amount);
            }

            export.WriteProperties(props);
            //export.GetProperties(true);
            //ArrayProperty<StructProperty> m_aMorphFeatures = props.GetProp<ArrayProperty<StructProperty>>("m_aMorphFeatures");
            //if (m_aMorphFeatures != null)
            //{
            //    foreach (StructProperty morphFeature in m_aMorphFeatures)
            //    {
            //        FloatProperty offset = morphFeature.GetProp<FloatProperty>("Offset");
            //        if (offset != null)
            //        {
            //            //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.FullPath + " offset");
            //            offset.Value = offset.Value * ThreadSafeRandom.NextFloat(1 - (amount / 3), 1 + (amount / 3));
            //        }
            //    }
            //}
        }

        public void AddHostileSquadToPackage(IMEPackage package)
        {
            if (package.Exports.Any(x => x.ClassName == "BioFaction_Hostile")) return; //already has it
            if (package.Imports.All(x => x.ObjectName != "SFXGame")) return; //need SFXGame import
            if (package.Imports.All(x => x.FullPath != "Core.Package")) return; //need SFXGame import

            // Add required imports: SFXGame.BioFaction, SFXGame.Default__BioFaction

            ImportEntry sfxgameimp = package.Imports.First(x => x.FullPath == "SFXGame");
            ImportEntry coreobj = package.Imports.First(x => x.FullPath == "Core.Object");
            ImportEntry packageimp = package.Imports.First(x => x.FullPath == "Core.Package");
            ImportEntry biofaction = package.Imports.FirstOrDefault(x => x.FullPath == "SFXGame.BioFaction");
            ImportEntry biofactionDefaults = package.Imports.FirstOrDefault(x => x.FullPath == "SFXGame.BioFaction");
            ImportEntry sfxai_humanoid = package.Imports.FirstOrDefault(x => x.FullPath == "SFXGame.SFXAI_Humanoid");
            ImportEntry biosquadcombatimp = package.Imports.FirstOrDefault(x => x.FullPath == "SFXGameContent.BioSquadCombat");

            if (biofaction == null)
            {
                //add
                biofaction = new ImportEntry(package)
                {
                    ClassName = "Class",
                    ObjectName = "BioFaction",
                    idxLink = sfxgameimp.UIndex,
                    PackageFile = "Core"
                };
                package.AddImport(biofaction);
            }

            if (biofactionDefaults == null)
            {
                //add
                biofactionDefaults = new ImportEntry(package)
                {
                    ClassName = "BioFaction",
                    ObjectName = "Default__BioFaction",
                    idxLink = sfxgameimp.UIndex,
                    PackageFile = "SFXGame"
                };
                package.AddImport(biofactionDefaults);
            }

            if (sfxai_humanoid == null)
            {
                //add
                sfxai_humanoid = new ImportEntry(package)
                {
                    ClassName = "Class",
                    ObjectName = "SFXAI_Humanoid",
                    idxLink = sfxgameimp.UIndex,
                    PackageFile = "SFXGame"
                };
                package.AddImport(sfxai_humanoid);
            }


            //Add required package export
            ExportEntry sfxGameContent = package.Exports.FirstOrDefault(x => x.FullPath == "SFXGameContent");
            if (sfxGameContent == null)
            {
                sfxGameContent = new ExportEntry(package);
                sfxGameContent.Class = packageimp;
                sfxGameContent.ObjectName = "SFXGameContent";
                //do we need to set a GUID? This is SP so no?
                package.AddExport(sfxGameContent);
            }

            if (biosquadcombatimp == null)
            {
                //add
                biosquadcombatimp = new ImportEntry(package)
                {
                    ClassName = "Class",
                    ObjectName = "BioSquadCombat",
                    idxLink = sfxGameContent.UIndex,
                    PackageFile = "Core"
                };
                package.AddImport(biosquadcombatimp);
            }


            MemoryStream classBin = new MemoryStream();
            ExportEntry hostileClassEntry = new ExportEntry(package)
            {
                ObjectName = "BioFaction_Hostile1",
                SuperClass = biofaction,
                idxLink = sfxGameContent.UIndex
            };

            classBin.WriteInt32(0); //unreal index?
            classBin.WriteInt32(biofaction.UIndex); //Superclass <<<<
            classBin.WriteInt32(0); //unknown 1
            classBin.WriteInt32(0); //childlist (none)
            classBin.WriteInt64(0); //ignoremask

            //State block (empty)
            classBin.WriteInt32(-1);
            classBin.WriteInt32(-1);
            classBin.WriteInt32(0);
            classBin.WriteInt32(0);
            classBin.WriteInt32(0);
            classBin.WriteInt32(-1);
            classBin.WriteInt32(-1);
            classBin.WriteInt16(-1);

            classBin.WriteInt32(2); //state flags
            classBin.WriteInt32(0); //local functions count

            //class flags
            classBin.WriteInt32(0x1210); //class flags main chunk
            classBin.WriteByte(0); //extra byte?

            classBin.WriteInt32(coreobj.UIndex); //Outer Class <<<<<
            classBin.WriteInt32(package.FindNameOrAdd("None")); //Implemented interfaces
            classBin.WriteInt32(0); //0index
            classBin.WriteInt32(0); //0count of interfaces
            classBin.WriteInt32(0); //components
            classBin.WriteInt32(0); //UNK1
            classBin.WriteInt32(0); //UNK2
            classBin.WriteInt32(0); //Class Defaults, will rewrite later

            hostileClassEntry.Data = classBin.ToArray();
            package.AddExport(hostileClassEntry);


            ExportEntry hostileClassDefaults = new ExportEntry(package) { ObjectName = "Default__BioFaction_Hostile1", Class = hostileClassEntry, idxLink = sfxGameContent.UIndex, Archetype = biofactionDefaults };

            PropertyCollection props = new PropertyCollection(hostileClassDefaults, "BioFaction_Hostile1");
            props.Add(new EnumProperty("BIO_Faction_Hostile1", "EBioFactionTypes", MEGame.ME2, "SquadFaction"));
            var values = new List<EnumProperty>(new[]
            {
                new EnumProperty("BIO_Relation_Hostile", "EBioFactionRelationship", MEGame.ME2),
                new EnumProperty("BIO_Relation_Friendly", "EBioFactionRelationship", MEGame.ME2),
                new EnumProperty("BIO_Relation_Neutral", "EBioFactionRelationship", MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile", "EBioFactionRelationship", MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile", "EBioFactionRelationship", MEGame.ME2),
                new EnumProperty("BIO_Relation_Friendly", "EBioFactionRelationship", MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile", "EBioFactionRelationship", MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile", "EBioFactionRelationship", MEGame.ME2)
            });
            var relations = (new ArrayProperty<EnumProperty>(values, "SquadRelations"));
            props.Add(relations);
            hostileClassDefaults.WriteProperties(props);
            package.AddExport(hostileClassDefaults);

            var rewrittenData = hostileClassEntry.Data;
            rewrittenData.OverwriteRange(rewrittenData.Length - 4, BitConverter.GetBytes(hostileClassDefaults.UIndex));
            hostileClassEntry.Data = rewrittenData;

            //write squad
            ExportEntry squad = new ExportEntry(package)
            {
                ObjectName = "BioSquadCombat",
                Class = biosquadcombatimp,
                idxLink = package.Exports.First(x => x.ObjectName == "PersistentLevel").UIndex
            };

            //debug only
            var loadouts = package.Exports.Where(x => x.ClassName == "SFXLoadoutData").ToList();
            if (loadouts.Any())
            {
                var pawns = package.Exports.Where(x => x.ObjectName == "BioPawn");
                foreach (var p in pawns)
                {
                    var pprops = p.GetProperties();
                    pprops.AddOrReplaceProp(new ObjectProperty(hostileClassEntry.UIndex, "FactionClass")); //make hostile
                    pprops.AddOrReplaceProp(new ObjectProperty(loadouts[0].UIndex, "Loadout"));
                    pprops.AddOrReplaceProp(new ObjectProperty(sfxai_humanoid, "AIController"));
                    p.WriteProperties(pprops);
                }
            }

            package.Save();
        }

        /// <summary>
        /// Swap the vowels around
        /// </summary>
        /// <param name="Tlks"></param>
        private void MakeTextPossiblyScottish(bool updateProgressbar)
        {
            /*Log.Information("Making text possibly scottish");
            if (scottishVowelOrdering == null)
            {
                scottishVowelOrdering = new List<char>(new char[] { 'a', 'e', 'i', 'o', 'u' });
                scottishVowelOrdering.Shuffle(random);
                upperScottishVowelOrdering = new List<char>();
                foreach (var c in scottishVowelOrdering)
                {
                    upperScottishVowelOrdering.Add(char.ToUpper(c, CultureInfo.InvariantCulture));
                }
            }

            int currentTlkIndex = 0;
            foreach (TalkFile tf in Tlks)
            {
                currentTlkIndex++;
                int max = tf.StringRefs.Count();
                int current = 0;
                if (updateProgressbar)
                {
                    mainWindow.CurrentOperationText = $"Applying Scottish accent [{currentTlkIndex}/{Tlks.Count()}]";
                    mainWindow.ProgressBar_Bottom_Max = tf.StringRefs.Length;
                    mainWindow.ProgressBarIndeterminate = false;
                }

                foreach (var sref in tf.StringRefs)
                {
                    current++;
                    if (tf.TlksIdsToNotUpdate.Contains(sref.StringID)) continue; //This string has already been updated and should not be modified.
                    if (updateProgressbar)
                    {
                        mainWindow.CurrentProgressValue = current;
                    }

                    if (!string.IsNullOrWhiteSpace(sref.Data))
                    {
                        string originalString = sref.Data;
                        if (originalString.Length == 1)
                        {
                            continue; //Don't modify I, A
                        }

                        string[] words = originalString.Split(' ');
                        for (int j = 0; j < words.Length; j++)
                        {
                            string word = words[j];
                            if (word.Length == 1)
                            {
                                continue; //Don't modify I, A
                            }

                            char[] newStringAsChars = word.ToArray();
                            for (int i = 0; i < word.Length; i++)
                            {
                                //Undercase
                                var vowelIndex = englishVowels.IndexOf(word[i]);
                                if (vowelIndex >= 0)
                                {
                                    if (i + 1 < word.Length && englishVowels.Contains(word[i + 1]))
                                    {
                                        continue; //don't modify dual vowel first letters.
                                    }
                                    else
                                    {
                                        newStringAsChars[i] = scottishVowelOrdering[vowelIndex];
                                    }
                                }
                                else
                                {
                                    var upperVowelIndex = upperCaseVowels.IndexOf(word[i]);
                                    if (upperVowelIndex >= 0)
                                    {
                                        if (i + 1 < word.Length && upperCaseVowels.Contains(word[i + 1]))
                                        {
                                            continue; //don't modify dual vowel first letters.
                                        }
                                        else
                                        {
                                            newStringAsChars[i] = upperScottishVowelOrdering[upperVowelIndex];
                                        }
                                    }
                                }
                            }

                            words[j] = new string(newStringAsChars);
                        }

                        string rebuiltStr = string.Join(" ", words);
                        tf.replaceString(sref.StringID, rebuiltStr);
                    }
                }
            }*/
        }
    }
}