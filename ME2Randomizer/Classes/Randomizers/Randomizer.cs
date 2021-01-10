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

namespace ME2Randomizer.Classes
{
    public class Randomizer
    {
        private MainWindow mainWindow;
        private BackgroundWorker randomizationWorker;
        private ConcurrentDictionary<string, string> ModifiedFiles;
        private SortedSet<string> faceFxBoneNames = new SortedSet<string>();

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
            string backupPath = Utilities.GetGameBackupPath();
            string gamePath = Utilities.GetGamePath();
            if (backupPath != null)
            {
                foreach (KeyValuePair<string, string> kvp in ModifiedFiles)
                {
                    string filepathrel = kvp.Key.Substring(gamePath.Length + 1);

                    Debug.WriteLine($"copy /y \"{Path.Combine(backupPath, filepathrel)}\" \"{Path.Combine(gamePath, filepathrel)}\"");
                }
            }

            //foreach (var v in faceFxBoneNames)
            //{
            //    Debug.WriteLine(v);
            //}
        }

        private void PerformRandomization(object sender, DoWorkEventArgs e)
        {
            // Init
            ModifiedFiles = new ConcurrentDictionary<string, string>(); //this will act as a Set since there is no ConcurrentSet
            MERFileSystem.InitMERFS(SelectedOptions.UseMERFS);
            Random random = new Random(SelectedOptions.Seed);

            //Load TLKs
            mainWindow.CurrentOperationText = "Loading TLKs";
            mainWindow.ProgressBarIndeterminate = true;
            var Tlks = Directory.GetFiles(Path.Combine(Utilities.GetGamePath(), "BioGame"), "*_INT.tlk", SearchOption.AllDirectories).Select(x =>
            {
                TalkFile tf = new TalkFile();
                tf.LoadTlkData(x);
                return tf;
            }).ToList();

            // Pass 1: All randomizers that are file specific
            var specificRandomizers = SelectedOptions.SelectedOptions.Where(x => !x.IsExportRandomizer).ToList();
            foreach (var sr in specificRandomizers)
            {
                mainWindow.CurrentOperationText = $"Randomizing {sr.HumanName}";
                sr.PerformSpecificRandomizationDelegate?.Invoke(random, sr);
            }

            //return;

            ///svar testp = MERFS.GetBasegameFile("");

            //var animseqs = p.Exports.Where(x => x.ClassName == "AnimSequence").ToList();
            //foreach (var v in animseqs)
            //{
            //    RandomizeAnimSequence(v, random);
            //    //Enum.TryParse(v.GetProperty<EnumProperty>("RotationCompressionFormat").Value.Name, out AnimationCompressionFormat rotCompression);

            //    //var ms = new MemoryStream(v.Data);
            //    //ms.Position = v.propsEnd();
            //    //ms.Position += 32;
            //    //while (ms.Position + 4 < ms.Length)
            //    //{
            //    //    var currentData = BitConverter.ToSingle(ms.ReadToBuffer(4),0);
            //    //    ms.Position -= 4;
            //    //    var randomizedFloat = random.NextFloat(currentData - (currentData * .2), currentData + (currentData * .2));
            //    //    switch (cf)
            //    //    {
            //    //        case 
            //    //    }
            //    //    ms.WriteBytes(BitConverter.GetBytes());
            //    //}

            //    //v.Data = ms.ToArray();
            //}
            //p.save();
            //Debugger.Break();

            //RANDOMIZE TEXTS
            if (mainWindow.RANDSETTING_MISC_GAMEOVERTEXT)
            {
                mainWindow.CurrentOperationText = "Randoming Game Over text";
                string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("gameovertexts.xml");
                XElement rootElement = XElement.Parse(fileContents);
                var gameoverTexts = rootElement.Elements("gameovertext").Select(x => x.Value).ToList();
                var gameOverText = gameoverTexts[random.Next(gameoverTexts.Count)];
                foreach (TalkFile tlk in Tlks)
                {
                    var replaced = tlk.ReplaceString(157152, gameOverText); //Todo: Update game over text ID
                    //tlk.
                    //    var hc = new HuffmanCompression();
                }
            }

#if DEBUG
            //Restore ini files first
            var backupPath = Utilities.GetGameBackupPath();
            if (backupPath != null)
            {
                var buDlcDir = Path.Combine(backupPath, "BioGame", "DLC");
                var relativeInis = Directory.EnumerateFiles(buDlcDir, "*.ini", SearchOption.AllDirectories).Select(x => x.Substring(buDlcDir.Length + 1)).ToList();
                var gamepath = Path.Combine(Utilities.GetGamePath(), "BioGame", "DLC");
                foreach (var rel in relativeInis)
                {
                    var bu = Path.Combine(buDlcDir, rel);
                    var ig = Path.Combine(gamepath, rel);
                    if (Directory.Exists(Path.GetDirectoryName(ig)))
                    {
                        File.Copy(bu, ig, true);
                    }
                }

                var basegameCoal = Path.Combine(Path.Combine(Utilities.GetGamePath(), @"BIOGame\Config\PC\Cooked\Coalesced.ini"));
                var buCoal = Path.Combine(backupPath, @"BIOGame\Config\PC\Cooked\Coalesced.ini");
                File.Copy(buCoal, basegameCoal, true);
            }
#endif

            // ME2 ----
            /*
            if (mainWindow.RANDSETTING_MOVEMENT_SPEED)
            {
                RandomizePlayerMovementSpeed(random);
            }

            if (mainWindow.RANDSETTING_LEVEL_LONGWALK)
            {
                RandomizeTheLongWalk(random);
            }

            if (mainWindow.RANDSETTING_LEVEL_ARRIVAL)
            {
                RandomizeArrivalDLC(random);
            }

            if (mainWindow.RANDSETTING_LEVEL_NORMANDY)
            {
                RandomizeNormandyHolo(random);
            }



            Log.Information("Saving Coalesced.ini file");
            me2basegamecoalesced.Serialize();
            */

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
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (file) =>
#else
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (file) =>
#endif
                {
                    // Todo: Filter out BioD_Nor_103aGalaxyMap.pcc
                    bool loggedFilePath = false;
                    mainWindow.CurrentProgressValue = Interlocked.Increment(ref currentFileNumber);
                    mainWindow.CurrentOperationText = $"Randomizing game files [{currentFileNumber}/{files.Count()}]";

                    // Debug
                    if (!file.Contains("_pro", StringComparison.InvariantCultureIgnoreCase))
                        return;

                    var package = MEPackageHandler.OpenMEPackage(file);
                    foreach (var exp in package.Exports)
                    {
                        foreach (var r in perExportRandomizers)
                        {
                            r.PerformRandomizationOnExportDelegate(exp, random, r);
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

                            RandomizeBioMorphFace(exp, random, morphFaceRandomizationAmount);
                        }
                        else if ((exp.ClassName == "BioSunFlareComponent" || exp.ClassName == "BioSunFlareStreakComponent" || exp.ClassName == "BioSunActor") && mainWindow.RANDSETTING_MISC_STARCOLORS)
                        {
                            if (!loggedFilePath)
                            {
                                Log.Information("Randomizing map file: " + file);
                                loggedFilePath = true;
                            }

                            
                        }
                        else if (exp.ClassName == "BioAnimSetData" && mainWindow.RANDSETTING_SHUFFLE_CUTSCENE_ACTORS) //UPDATE THIS TO DO BIOANIMDATA!!
                        {
                            RandomizeBioAnimSetData(exp, random);
                        }
                        else if (exp.ClassName == "AnimSequence" && mainWindow.RANDSETTING_PAWN_ANIMSEQUENCE)
                        {
                            if (!loggedFilePath)
                            {
                                //Log.Information("Randomizing map file: " + files[i]);
                                loggedFilePath = true;
                            }

                            RandomizeAnimSequence(exp, random);
                        }
                        else if (exp.ClassName == "BioLookAtDefinition" && mainWindow.RANDSETTING_PAWN_BIOLOOKATDEFINITION)
                        {
                            if (!loggedFilePath)
                            {
                                //Log.Information("Randomizing map file: " + files[i]);
                                loggedFilePath = true;
                            }

                            RandomizeBioLookAtDefinition(exp, random);
                        }
                        else if (exp.ClassName == "BioPawn")
                        {
                            if (mainWindow.RANDSETTING_MISC_MAPPAWNSIZES && random.Next(4) == 0)
                            {
                                if (!loggedFilePath)
                                {
                                    Log.Information("Randomizing file: " + file);
                                    loggedFilePath = true;
                                }

                                //Pawn size randomizer
                                RandomizeBioPawnSize(exp, random, 0.4);
                            }

                            if (mainWindow.RANDSETTING_PAWN_MATERIALCOLORS)
                            {
                                if (!loggedFilePath)
                                {
                                    Log.Information("Randomizing file: " + file);
                                    loggedFilePath = true;
                                }

                                RandomizePawnMaterialInstances(exp, random);
                            }
                        }
                        else if (exp.ClassName == "HeightFogComponent" && mainWindow.RANDSETTING_MISC_HEIGHTFOG)
                        {
                            if (!loggedFilePath)
                            {
                                Log.Information("Randomizing file: " + file);
                                loggedFilePath = true;
                            }

                            RandomizeHeightFogComponent(exp, random);
                        }
                        else if (mainWindow.RANDSETTING_MISC_INTERPS && exp.ClassName == "InterpTrackMove" && random.Next(4) == 0)
                        {
                            if (!loggedFilePath)
                            {
                                Log.Information("Randomizing file: " + file);
                                loggedFilePath = true;
                            }

                            //Interpolation randomizer
                            RandomizeInterpTrackMove(exp, random, morphFaceRandomizationAmount);
                        }
                        else if (mainWindow.RANDSETTING_PAWN_FACEFX && exp.ClassName == "FaceFXAnimSet")
                        {
                            if (!loggedFilePath)
                            {
                                Log.Information("Randomizing file: " + file);
                                loggedFilePath = true;
                            }

                            //Method contains SHouldSave in it (due to try catch).
                            RandomizeFaceFX(exp, random, (int)faceFXRandomizationAmount);
                        }
                        else if (mainWindow.RANDSETTING_MOVEMENT_SPEED && exp.ClassName == "SFXMovementData" && !exp.FileRef.FilePath.EndsWith("_Player_C.pcc"))
                        {
                            RandomizeMovementSpeed2DA(exp, random);
                        }
                        else if (mainWindow.RANDSETTING_HOLOGRAM_COLORS && exp.ClassName == "MaterialInstanceConstant" && exp.ObjectName.Name.StartsWith("Holo"))
                        {
                            Debug.WriteLine("RAndomizing hologram colors");
                            RandomizeMaterialInstance(exp, random);
                        }
                        */
                    }
                    MERFileSystem.SavePackage(package);
                });

                //if (mainWindow.RANDSETTING_MISC_ENEMYAIDISTANCES)
                //{
                //    RandomizeAINames(package, random);
                //}

                //if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION && package.LocalTalkFiles.Any())
                //{
                //    if (!loggedFilePath)
                //    {
                //        Log.Information("Randomizing map file: " + files[i]);
                //        loggedFilePath = true;
                //    }
                //    UpdateGalaxyMapReferencesForTLKs(package.LocalTalkFiles, false, false);
                //}

                //if (mainWindow.RANDSETTING_WACK_SCOTTISH && package.LocalTalkFiles.Any())
                //{
                //    if (!loggedFilePath)
                //    {
                //        Log.Information("Randomizing map file: " + files[i]);
                //        loggedFilePath = true;
                //    }

                //    MakeTextPossiblyScottish(package.LocalTalkFiles, random, false);
                //}

                //foreach (var talkFile in package.LocalTalkFiles.Where(x => x.Modified))
                //{
                //    talkFile.saveToExport();
                //}


            }

            //if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION)
            //{
            //    Log.Information("Apply galaxy map background transparency fix");
            //    MEPackage p = MEPackageHandler.OpenMEPackage(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\NOR\DSG\BIOA_NOR10_03_DSG.SFM"));
            //    p.GetUExport(1655).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.PC_GalaxyMap_BGFix_1655.bin");
            //    p.save();
            //    ModifiedFiles[p.FilePath] = p.FilePath;
            //}

            if (mainWindow.RANDSETTING_WACK_SCOTTISH)
            {
                MakeTextPossiblyScottish(Tlks, random, true);
            }



            mainWindow.ProgressBarIndeterminate = true;
            foreach (TalkFile tf in Tlks)
            {
                if (tf.IsModified)
                {
                    //string xawText = tf.findDataById(138077); //Earth.
                    //Debug.WriteLine($"------------AFTER REPLACEMENT----{tf.export.ObjectName}------------------");
                    //Debug.WriteLine("New description:\n" + xawText);
                    //Debug.WriteLine("----------------------------------");
                    //Debugger.Break(); //Xawin
                    mainWindow.CurrentOperationText = "Saving TLKs";
                    ModifiedFiles[tf.path] = tf.path;
                    //HuffmanCompression hc = new ME3Explorer.HuffmanCompression();
                    // hc.SavetoFile(tf.path);
                }
            }

            mainWindow.CurrentOperationText = "Finishing up";
            //AddMERSplash(random);
        }

        /// <summary>
        /// Sets the options up that can be selected and their methods they call
        /// </summary>
        /// <param name="RandomizationGroups"></param>
        internal static void SetupOptions(ObservableCollectionExtended<RandomizationGroup> RandomizationGroups)
        {
            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Faces & Characters",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() { HumanName = "FaceFX animation", Ticks = "1,2,3,4,5", HasSliderOption = true, IsRecommended = true, SliderToTextConverter = rSetting =>
                        rSetting switch
                        {
                            1 => "Oblivion",
                            2 => "Knights of the old Republic",
                            3 => "Sonic Adventure",
                            4 => "Source filmmaker",
                            5 => "Total madness",
                            _ => "Error"
                        },
                        SliderValue = 2, // This must come after the converter
                        PerformRandomizationOnExportDelegate = RFaceFXAnimSet.RandomizeExport
                    },
                    new RandomizationOption() { HumanName = "Squadmate faces"},
                    new RandomizationOption() { HumanName = "NPC faces", Ticks = "0.1,0.2,0.3,0.4,0.5,0.6,0.7", HasSliderOption = true, IsRecommended = true, SliderToTextConverter =
                        rSetting => $"Randomization amount: {rSetting}",
                        SliderValue = .3,// This must come after the converter

                    },
                    new RandomizationOption() { HumanName = "NPC head colors"},
                    new RandomizationOption() { HumanName = "Eyes (exluding Illusive Man)", IsRecommended = true, PerformRandomizationOnExportDelegate = REyes.RandomizeExport},
                    new RandomizationOption() { HumanName = "Illusive Man eyes", IsRecommended = true, PerformRandomizationOnExportDelegate = RIllusiveEyes.RandomizeExport},
                    new RandomizationOption() { HumanName = "Character creator premade faces", IsRecommended=true, PerformSpecificRandomizationDelegate=CharacterCreator.RandomizeCharacterCreator},
                    new RandomizationOption() { HumanName = "Character creator skin tones"},
                    new RandomizationOption() { HumanName = "Iconic FemShep face"},
                    new RandomizationOption() { HumanName = "Look At Definitions", PerformRandomizationOnExportDelegate = RBioLookAtDefinition.RandomizeExport},
                    new RandomizationOption() { HumanName = "Look At Targets", PerformRandomizationOnExportDelegate = RBioLookAtTarget.RandomizeExport},
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Miscellaneous",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                                 new RandomizationOption() {HumanName = "Game over text"},
                    new RandomizationOption() {HumanName = "Drone colors", PerformRandomizationOnExportDelegate = CombatDrone.RandomizeExport}
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Movement & pawns",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Enemy movement speeds"},
                    new RandomizationOption() {HumanName = "Player movement speeds"},
                    new RandomizationOption() {HumanName = "Hammerhead"}
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Weapons",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() { HumanName = "Weapon stats" },
                    new RandomizationOption() { HumanName = "Squadmate weapon types" },
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Level-specific",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() { HumanName = "Galaxy Map" },
                    new RandomizationOption() { HumanName = "Normandy", PerformSpecificRandomizationDelegate = Normandy.PerformRandomization },
                    new RandomizationOption() { HumanName = "Prologue" },
                    new RandomizationOption() { HumanName = "Arrival", PerformSpecificRandomizationDelegate = ArrivalDLC.PerformRandomization },
                    new RandomizationOption() { HumanName = "Collector Base", PerformSpecificRandomizationDelegate = CollectorBase.PerformRandomization },
                }
            });

            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Level components",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Star colors", IsRecommended = true, PerformRandomizationOnExportDelegate=RBioSun.PerformRandomization},
                    new RandomizationOption() {HumanName = "Fog colors", IsRecommended=true, PerformRandomizationOnExportDelegate=RHeightFogComponent.RandomizeExport},
                    new RandomizationOption() {HumanName = "Post Processing volumes", PerformRandomizationOnExportDelegate=RPostProcessingVolume.RandomizeExport},
                    new RandomizationOption() {HumanName = "Light colors", PerformRandomizationOnExportDelegate=RLighting.RandomizeExport},
                }
            });


            RandomizationGroups.Add(new RandomizationGroup()
            {
                GroupName = "Wackadoodle",
                Options = new ObservableCollectionExtended<RandomizationOption>()
                {
                    new RandomizationOption() {HumanName = "Actors in cutscenes"},
                    new RandomizationOption() {HumanName = "Animation data", PerformRandomizationOnExportDelegate = RAnimSequence.RandomizeExport},
                    new RandomizationOption() {HumanName = "Random movement interpolations"},
                    new RandomizationOption() {HumanName = "Hologram colors"},
                    new RandomizationOption() {HumanName = "Vowels"},
                    new RandomizationOption() {HumanName = "Game over text"},
                    new RandomizationOption() {HumanName = "Drone colors", PerformRandomizationOnExportDelegate = CombatDrone.RandomizeExport}
                }
            });
        }

        public enum AnimationCompressionFormat
        {
            ACF_None,
            ACF_Float96NoW,
            ACF_Fixed48NoW,
            ACF_IntervalFixed32NoW,
            ACF_Fixed32NoW,
            ACF_Float32NoW,
            ACF_BioFixed48,
        }







        private void randomizeMorphTarget(Random random, ExportEntry morphTarget)
        {
            MemoryStream ms = new MemoryStream(morphTarget.Data);
            ms.Position = morphTarget.propsEnd();
            var numLods = ms.ReadInt32();

            for (int i = 0; i < numLods; i++)
            {
                var numVertices = ms.ReadInt32();
                var diff = random.NextFloat(-0.2, 0.2);
                for (int k = 0; k < numVertices; k++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var fVal = ms.ReadFloat();
                        ms.Position -= 4;
                        ms.WriteFloat(fVal + diff);
                    }
                    ms.WriteByte((byte)random.Next(256));
                    ms.ReadByte();
                    ms.ReadByte();
                    ms.ReadByte();
                    ms.SkipInt16(); //idx
                }
                ms.SkipInt32(); //Vertices?/s
            }

            morphTarget.Data = ms.ToArray();
        }




        private void RandomizePawnMaterialInstances(ExportEntry exp, Random random)
        {
            //Don't know if this works
            var hairMeshObj = exp.GetProperty<ObjectProperty>("m_oHairMesh");
            if (hairMeshObj != null)
            {
                var headMesh = exp.FileRef.GetUExport(hairMeshObj.Value);
                var materials = headMesh.GetProperty<ArrayProperty<ObjectProperty>>("Materials");
                if (materials != null)
                {
                    foreach (var materialObj in materials)
                    {
                        //MaterialInstanceConstant
                        ExportEntry material = exp.FileRef.GetUExport(materialObj.Value);
                        RMaterialInstance.RandomizeExport(material, null, random);
                    }
                }
            }
        }

        private void RandomizePlanetMaterialInstanceConstant(ExportEntry planetMaterial, Random random, bool realistic = false)
        {
            var props = planetMaterial.GetProperties();
            {
                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                scalars[0].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0, 1.0); //Horizon Atmosphere Intensity
                if (random.Next(4) == 0)
                {
                    scalars[2].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0, 0.7); //Atmosphere Min (how gas-gianty it looks)
                }
                else
                {
                    scalars[2].GetProp<FloatProperty>("ParameterValue").Value = 0; //Atmosphere Min (how gas-gianty it looks)
                }

                scalars[3].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(.5, 1.5); //Atmosphere Tiling U
                scalars[4].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(.5, 1.5); //Atmosphere Tiling V
                scalars[5].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(.5, 4); //Atmosphere Speed
                scalars[6].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0.5, 12); //Atmosphere Fall off...? seems like corona intensity

                foreach (var vector in vectors)
                {
                    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                    RStructs.RandomizeTint(random, paramValue, false);
                }
            }
            planetMaterial.WriteProperties(props);
        }

        private void RandomizeMovementSpeed2DA(ExportEntry exp, Random random)
        {
            var props = exp.GetProperties();
            foreach (var prop in props)
            {
                if (prop is FloatProperty fp)
                {
                    var min = fp.Value / 3;
                    var max = fp.Value * 3;

                    fp.Value = random.NextFloat(min, max);
                }
            }
            exp.WriteProperties(props);
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

        private void RandomizeInterpTrackMove(ExportEntry export, Random random, double amount)
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
                            x.Value = x.Value * random.NextFloat(1 - amount, 1 + amount);
                            y.Value = y.Value * random.NextFloat(1 - amount, 1 + amount);
                            z.Value = z.Value * random.NextFloat(1 - amount, 1 + amount);
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
                                x.Value = x.Value * random.NextFloat(1 - amount * 3, 1 + amount * 3);
                            }
                            else
                            {
                                x.Value = random.NextFloat(0, 360);
                            }

                            if (y.Value != 0)
                            {
                                y.Value = y.Value * random.NextFloat(1 - amount * 3, 1 + amount * 3);
                            }
                            else
                            {
                                y.Value = random.NextFloat(0, 360);
                            }

                            if (z.Value != 0)
                            {
                                z.Value = z.Value * random.NextFloat(1 - amount * 3, 1 + amount * 3);
                            }
                            else
                            {
                                z.Value = random.NextFloat(0, 360);
                            }
                        }
                    }
                }
            }

            export.WriteProperties(props);
        }

        public string GetResourceFileText(string FilePath, string assemblyName)
        {
            string result = string.Empty;

            using (Stream stream =
                System.Reflection.Assembly.Load(assemblyName).GetManifestResourceStream($"{assemblyName}.{FilePath}"))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }

            return result;
        }






        static readonly List<char> englishVowels = new List<char>(new[] { 'a', 'e', 'i', 'o', 'u' });
        static readonly List<char> upperCaseVowels = new List<char>(new[] { 'A', 'E', 'I', 'O', 'U' });

        /// <summary>
        /// Swap the vowels around
        /// </summary>
        /// <param name="Tlks"></param>
        private void MakeTextPossiblyScottish(List<TalkFile> Tlks, Random random, bool updateProgressbar)
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


        static string FormatXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch (Exception)
            {
                // Handle and throw if fatal exception here; don't just ignore them
                return xml;
            }
        }

        private void RandomizeOpeningCrawl(Random random, List<TalkFile> Tlks)
        {
            /* Log.Information($"Randomizing opening crawl text");

             string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("openingcrawls.xml");

             XElement rootElement = XElement.Parse(fileContents);
             var crawls = (from e in rootElement.Elements("CrawlText")
                           select new OpeningCrawl()
                           {
                               CrawlText = e.Value,
                               RequiresFaceRandomizer = e.Element("requiresfacerandomizer") != null && ((bool)e.Element("requiresfacerandomizer"))
                           }).ToList();
             crawls = crawls.Where(x => x.CrawlText != "").ToList();

             if (!mainWindow.RANDSETTING_PAWN_MAPFACES)
             {
                 crawls = crawls.Where(x => !x.RequiresFaceRandomizer).ToList();
             }

             string crawl = crawls[random.Next(crawls.Count)].CrawlText;
             crawl = crawl.TrimLines();
             //For length testing.
             //crawl = "It is a period of civil war. Rebel spaceships, striking from a hidden base, " +
             //        "have won their first victory against the evil Galactic Empire. During the battle, Rebel spies " +
             //        "managed to steal secret plans to the Empire's ultimate weapon, the DEATH STAR, an armored space station " +
             //        "with enough power to destroy an entire planet.\n\n" +
             //        "Pursued by the Empire's sinister agents, Princess Leia races home aboard her starship, custodian of the stolen plans that can " +
             //        "save her people and restore freedom to the galaxy.....";
             foreach (TalkFile tf in Tlks)
             {
                 tf.replaceString(153106, crawl);
             }
             */
        }

        private void RandomizeBioPawnSize(ExportEntry export, Random random, double amount)
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
                x.Value = x.Value * random.NextFloat(1 - amount, 1 + amount);
                y.Value = y.Value * random.NextFloat(1 - amount, 1 + amount);
                z.Value = z.Value * random.NextFloat(1 - amount, 1 + amount);
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
            //            offset.Value = offset.Value * random.NextFloat(1 - (amount / 3), 1 + (amount / 3));
            //        }
            //    }
            //}
        }

        private void RandomizeCharacter(ExportEntry export, Random random)
        {
            /*bool hasChanges = false;
            int[] humanLightArmorManufacturers = { 373, 374, 375, 379, 383, 451 };
            int[] bioampManufacturers = { 341, 342, 343, 345, 410, 496, 497, 498, 526 };
            int[] omnitoolManufacturers = { 362, 363, 364, 366, 411, 499, 500, 501, 527 };
            List<string> actorTypes = new List<string>();
            actorTypes.Add("BIOG_HumanFemale_Hench_C.hench_humanFemale");
            actorTypes.Add("BIOG_HumanMale_Hench_C.hench_humanmale");
            actorTypes.Add("BIOG_Asari_Hench_C.hench_asari");
            actorTypes.Add("BIOG_Krogan_Hench_C.hench_krogan");
            actorTypes.Add("BIOG_Turian_Hench_C.hench_turian");
            actorTypes.Add("BIOG_Quarian_Hench_C.hench_quarian");
            //actorTypes.Add("BIOG_Jenkins_Hench_C.hench_jenkins");

            Bio2DA character2da = new Bio2DA(export);
            for (int row = 0; row < character2da.RowNames.Count(); row++)
            {
                //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(cluster2da[row, colsToRandomize[i]].Data, 0));


                if (mainWindow.RANDSETTING_CHARACTER_HENCH_ARCHETYPES)
                {
                    if (character2da[row, 0].GetDisplayableValue().StartsWith("hench") && !character2da[row, 0].GetDisplayableValue().Contains("jenkins"))
                    {
                        //Henchman
                        int indexToChoose = random.Next(actorTypes.Count);
                        var actorNameVal = actorTypes[indexToChoose];
                        actorTypes.RemoveAt(indexToChoose);
                        Console.WriteLine("Character Randomizer HENCH ARCHETYPE [" + row + "][2] value is now " + actorNameVal);
                        character2da[row, 2].Data = BitConverter.GetBytes((ulong)export.FileRef.findName(actorNameVal));
                        hasChanges = true;
                    }
                }

                if (mainWindow.RANDSETTING_CHARACTER_INVENTORY)
                {
                    int randvalue = random.Next(humanLightArmorManufacturers.Length);
                    int manf = humanLightArmorManufacturers[randvalue];
                    Console.WriteLine("Character Randomizer ARMOR [" + row + "][21] value is now " + manf);
                    character2da[row, 21].Data = BitConverter.GetBytes(manf);

                    if (character2da[row, 24] != null)
                    {
                        randvalue = random.Next(bioampManufacturers.Length);
                        manf = bioampManufacturers[randvalue];
                        Console.WriteLine("Character Randomizer BIOAMP [" + row + "][24] value is now " + manf);
                        character2da[row, 24].Data = BitConverter.GetBytes(manf);
                        hasChanges = true;
                    }

                    if (character2da[row, 29] != null)
                    {
                        randvalue = random.Next(omnitoolManufacturers.Length);
                        manf = omnitoolManufacturers[randvalue];
                        Console.WriteLine("Character Randomizer OMNITOOL [" + row + "][29] value is now " + manf);
                        character2da[row, 29].Data = BitConverter.GetBytes(manf);
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                Debug.WriteLine("Writing Character_Character to export");
                character2da.Write2DAToExport();
            }*/
        }




        private List<char> scottishVowelOrdering;
        private List<char> upperScottishVowelOrdering;

        static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-3, 20));
            return (float)(mantissa * exponent);
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
                new EnumProperty("BIO_Relation_Hostile","EBioFactionRelationship",MEGame.ME2),
                new EnumProperty("BIO_Relation_Friendly","EBioFactionRelationship",MEGame.ME2),
                new EnumProperty("BIO_Relation_Neutral","EBioFactionRelationship",MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile","EBioFactionRelationship",MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile","EBioFactionRelationship",MEGame.ME2),
                new EnumProperty("BIO_Relation_Friendly","EBioFactionRelationship",MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile","EBioFactionRelationship",MEGame.ME2),
                new EnumProperty("BIO_Relation_Hostile","EBioFactionRelationship",MEGame.ME2)
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
    }
}