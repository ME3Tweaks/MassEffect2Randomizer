using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Gibbed.IO;
using Serilog;
using ME3Explorer.Unreal;
using ME3Explorer.Packages;
using ME3Explorer;
using ME2Explorer.Unreal;
using ME3Explorer.FaceFX;
using MassEffectModManagerCore.modmanager.gameini;
using MassEffectModManagerCore.modmanager.helpers;

namespace MassEffectRandomizer.Classes
{
    class Randomizer
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

        public bool Busy => randomizationWorker != null && randomizationWorker.IsBusy;
        private MERFileSystem MERFS;

        public void Randomize(bool usingDLCModFS)
        {
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
            randomizationWorker.RunWorkerAsync((usingDLCModFS, seed));
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate, mainWindow);
        }


        private void Randomization_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, mainWindow);
            mainWindow.CurrentOperationText = "Randomization complete";
            mainWindow.AllowOptionsChanging = true;

            mainWindow.ProgressPanelVisible = System.Windows.Visibility.Collapsed;
            mainWindow.ButtonPanelVisible = System.Windows.Visibility.Visible;
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

        private void RandomizeBioWaypointSet(ExportEntry export, Random random)
        {
            Log.Information("Randomizing BioWaypointSet " + export.UIndex + " in " + Path.GetFileName(export.FileRef.FilePath));
            var waypointReferences = export.GetProperty<ArrayProperty<StructProperty>>("WaypointReferences");
            if (waypointReferences != null)
            {
                //Get list of valid targets
                var pcc = export.FileRef;
                var waypoints = pcc.Exports.Where(x => x.ClassName == "BioPathPoint" || x.ClassName == "PathNode").ToList();
                waypoints.Shuffle(random);

                foreach (var waypoint in waypointReferences)
                {
                    var nav = waypoint.GetProp<ObjectProperty>("Nav");
                    if (nav != null && nav.Value > 0)
                    {
                        ExportEntry currentPoint = export.FileRef.getUExport(nav.Value);
                        if (currentPoint.ClassName == "BioPathPoint" || currentPoint.ClassName == "PathNode")
                        {
                            nav.Value = waypoints[0].UIndex;
                            waypoints.RemoveAt(0);
                        }
                        else
                        {
                            Debug.WriteLine("SKIPPING NODE TYPE " + currentPoint.ClassName);
                        }
                    }
                }
            }

            export.WriteProperty(waypointReferences);
        }

        private void PerformRandomization(object sender, DoWorkEventArgs e)
        {
            ModifiedFiles = new ConcurrentDictionary<string, string>(); //this will act as a Set since there is no ConcurrentSet
            var parms = ((bool usingMerFS, int seed))e.Argument;
            MERFS = new MERFileSystem(Utilities.GetGamePath(), parms.usingMerFS);
            Random random = new Random(parms.seed);
            if (!ME3ExplorerMinified.DLL.Booted)
            {
                mainWindow.CurrentOperationText = "Loading ME3Explorer Library";
                mainWindow.ProgressBarIndeterminate = true;
                ME3ExplorerMinified.DLL.Startup();
            }

            //Load TLKs
            mainWindow.CurrentOperationText = "Loading TLKs";
            mainWindow.ProgressBarIndeterminate = true;
            var Tlks = Directory.GetFiles(Path.Combine(Utilities.GetGamePath(), "BioGame"), "*_INT.tlk", SearchOption.AllDirectories).Select(x =>
            {
                TalkFile tf = new TalkFile();
                tf.LoadTlkData(x);
                return tf;
            }).ToList();

            acceptableTagsForPawnShuffling = Utilities.GetEmbeddedStaticFilesTextFile("allowedcutscenerandomizationtags.txt").Split('\n').ToList();

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

            ////Test
            //MEPackage test = MEPackageHandler.OpenMEPackage(@"D:\Origin Games\Mass Effect\BioGame\CookedPC\Maps\STA\DSG\BIOA_STA60_06_DSG.SFM");
            //var morphFaces = test.Exports.Where(x => x.ClassName == "BioMorphFace").ToList();
            //morphFaces.ForEach(x => RandomizeBioMorphFace(x, random));
            //test.save();
            //return;

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
                    var replaced = tlk.replaceString(157152, gameOverText); //Todo: Update game over text ID
                    //tlk.
                    //    var hc = new HuffmanCompression();
                }
            }

            //Randomize BIOC_BASE


            if (mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
            {
                RandomizeCharacterCreator(random, Tlks);
            }




            //RANDOMIZE ENTRYMENU
            //var entrymenu = MEPackageHandler.OpenMEPackage(Utilities.GetEntryMenuFile());
            //foreach (ExportEntry export in entrymenu.Exports)
            //{
            //    switch (export.ObjectName)
            //    {
            //        case "FemalePregeneratedHeads":
            //        case "MalePregeneratedHeads":
            //        case "BaseMaleSliders":
            //        case "BaseFemaleSliders":
            //            if (mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
            //            {
            //                RandomizePregeneratedHead(export, random);
            //            }

            //            break;
            //        default:
            //            if ((export.ClassName == "Bio2DA" || export.ClassName == "Bio2DANumberedRows") && !export.ObjectName.Contains("Default") && mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
            //            {
            //                RandomizeCharacterCreator2DA(random, export);
            //            }

            //            break;

            //            //RandomizeGalaxyMap(random);
            //            //RandomizeGUISounds(random);
            //            //RandomizeMusic(random);
            //            //RandomizeMovementSpeeds(random);
            //            //RandomizeCharacterCreator2DA(random);
            //            //Dump2DAToExcel();
            //    }

            //    if (mainWindow.RANDSETTING_CHARACTER_ICONICFACE && export.ClassName == "BioMorphFace" && export.ObjectName.StartsWith("Player_"))
            //    {
            //        Log.Information("Randomizing iconic female shepard face by " + mainWindow.RANDSETTING_CHARACTER_ICONICFACE_AMOUNT);
            //        RandomizeBioMorphFace(export, random, mainWindow.RANDSETTING_CHARACTER_ICONICFACE_AMOUNT);
            //    }
            //}


            //if (mainWindow.RANDSETTING_MISC_SPLASH)
            //{
            //    RandomizeSplash(random, entrymenu);
            //}

            /*
            if (mainWindow.RANDSETTING_MAP_EDENPRIME)
            {
                RandomizeEdenPrime(random);
            }

            if (mainWindow.RANDSETTING_MAP_FEROS)
            {
                RandomizeFerosColonistBattle(random, Tlks);
            }

            if (mainWindow.RANDSETTING_MAP_NOVERIA)
            {
                RandomizeNoveria(random, Tlks);
            }

            if (mainWindow.RANDSETTING_MAP_PINNACLESTATION)
            {
                RandomizePinnacleScoreboard(random);
            }

            if (mainWindow.RANDSETTING_MAP_BDTS)
            {
                RandomizeBDTS(random);
            }

            if (mainWindow.RANDSETTING_MAP_CITADEL)
            {
                RandomizeCitadel(random);
            }

            if (mainWindow.RANDSETTING_MISC_ENDINGART)
            {
                RandomizeEnding(random);
            }*/

            //if (entrymenu.IsModified)
            //{
            //    entrymenu.save();
            //    ModifiedFiles[entrymenu.FilePath] = entrymenu.FilePath;
            //}


            //RANDOMIZE FACESRANDSETTING_PAWN_CLOWNMODE
            //if (mainWindow.RANDSETTING_CHARACTER_HENCHFACE)
            //{
            //    RandomizeBioMorphFaceWrapper(Utilities.GetGameFile(@"BioGame\CookedPC\Packages\GameObjects\Characters\Faces\BIOG_Hench_FAC.upk"), random); //Henchmen
            //    RandomizeBioMorphFaceWrapper(Utilities.GetGameFile(@"BioGame\CookedPC\Packages\BIOG_MORPH_FACE.upk"), random); //Iconic and player (Not sure if this does anything...
            //}

#if DEBUG
            //Restore ini files first
            var backupPath = Utilities.GetGameBackupPath();
            var buDlcDir = Path.Combine(backupPath, "BioGame", "DLC");
            var relativeInis = Directory.EnumerateFiles(buDlcDir, "*.ini", SearchOption.AllDirectories).Select(x => x.Substring(buDlcDir.Length + 1)).ToList();
            var gamepath = Path.Combine(Utilities.GetGamePath(), "BioGame", "DLC");
            foreach (var rel in relativeInis)
            {
                var bu = Path.Combine(buDlcDir, rel);
                var ig = Path.Combine(gamepath, rel);
                File.Copy(bu, ig, true);
            }

            var basegameCoal = Path.Combine(Path.Combine(Utilities.GetGamePath(), @"BIOGame\Config\PC\Cooked\Coalesced.ini"));
            var buCoal = Path.Combine(backupPath, @"BIOGame\Config\PC\Cooked\Coalesced.ini");
            File.Copy(buCoal, basegameCoal, true);
#endif

            string cookedPC = Path.Combine(Utilities.GetGamePath(), "BioGame");
            ME2Coalesced me2basegamecoalesced = new ME2Coalesced(MERFS.GetGameFile(@"BIOGame\Config\PC\Cooked\Coalesced.ini"));

            if (mainWindow.RANDSETTING_ILLUSIVEEYES)
            {
                //var headmorphpro = MEPackageHandler.OpenMEPackage(Utilities.GetBasegameFile("BIOG_HMM_HED_PROMorph.pcc"));
                var headmorphpro = MEPackageHandler.OpenMEPackage(MERFS.GetBasegameFile("BioD_ProCer_350BriefRoom.pcc"));
                var eyes = headmorphpro.getUExport(2234); //Illusive man eyes
                RandomizeMaterialInstance(eyes, random);
                ModifiedFiles[headmorphpro.FilePath] = headmorphpro.FilePath;
            }

            if (mainWindow.RANDSETTING_WEAPONS)
            {
                DuplicatingIni dlcModIni = new DuplicatingIni();
                Log.Information("Randomizing basegame weapon ini");
                var bioweapon = me2basegamecoalesced.Inis.FirstOrDefault(x => Path.GetFileName(x.Key) == "BIOWeapon.ini").Value;
                var bioweaponUnmodified = bioweapon.ToString();
                RandomizeWeaponIni(bioweapon, random, dlcModIni);
                if (mainWindow.UseMERFS)
                {
                    bioweapon = DuplicatingIni.ParseIni(bioweaponUnmodified); //reset the coalesced file to vanilla as it'll be stored in DLC instead
                }
                var weaponInis = Directory.GetFiles(Path.Combine(cookedPC, "DLC"), "BIOWeapon.ini", SearchOption.AllDirectories).ToList();
                foreach (var wi in weaponInis)
                {
                    //Log.Information("Randomizing weapons in ini: " + wi);
                    var dlcWeapIni = DuplicatingIni.LoadIni(wi);
                    RandomizeWeaponIni(dlcWeapIni, random, dlcModIni);
                    if (!mainWindow.UseMERFS)
                    {
                        Log.Information("Writing DLC BioWeapon: " + wi);
                        File.WriteAllText(wi, dlcWeapIni.ToString());
                    }
                }

                if (mainWindow.UseMERFS)
                {
                    //Write out BioWeapon.ini file
                    var bioweaponPath = Path.Combine(MERFS.dlcModCookedPath, "BioWeapon.ini");
                    Log.Information("Writing MERFS DLC BioWeapon: " + bioweaponPath);
                    File.WriteAllText(bioweaponPath, dlcModIni.ToString());
                }
            }

            if (mainWindow.RANDSETTING_MOVEMENT_SPEED)
            {
                RandomizeMovementSpeed(random);
            }

            Log.Information("Saving Coalesced.ini file");
            me2basegamecoalesced.Serialize();

            if (RunAllFilesRandomizerPass)
            {
                mainWindow.CurrentOperationText = "Getting list of files...";

                mainWindow.ProgressBarIndeterminate = true;

                var files = Directory.GetFiles(cookedPC, "*.pcc", SearchOption.AllDirectories).ToList();

                mainWindow.ProgressBarIndeterminate = false;
                mainWindow.ProgressBar_Bottom_Max = files.Count();
                mainWindow.ProgressBar_Bottom_Min = 0;
                double morphFaceRandomizationAmount = mainWindow.RANDSETTING_MISC_MAPFACES_AMOUNT;
                double faceFXRandomizationAmount = mainWindow.RANDSETTING_WACK_FACEFX_AMOUNT;
                int currentFileNumber = 0;
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (file) =>
                {
                    //for (int i = 0; i < files.Count; i++)
                    //{
                    bool loggedFilePath = false;
                    mainWindow.CurrentProgressValue = Interlocked.Increment(ref currentFileNumber);
                    mainWindow.CurrentOperationText = "Randomizing game files [" + currentFileNumber + "/" + files.Count() + "]";
                    //if (!file.Contains("BioD_Pro", StringComparison.InvariantCultureIgnoreCase))
                    //    return;
                    var package = MEPackageHandler.OpenMEPackage(file);

                    if (Path.GetFileName(file).Equals("BioD_Nor_103aGalaxyMap.pcc", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (mainWindow.RANDSETTING_GALAXYMAP)
                        {
                            RandomizeGalaxyMap(package, random);
                        }
                    }
                    else
                    {
                        //if (!mapBaseName.StartsWith("bioa_sta")) continue;
                        bool hasLogged = false;
                        if (RunAllFilesRandomizerPass)
                        {
                            foreach (ExportEntry exp in package.Exports)
                            {
                                if (exp.IsDefaultObject)
                                    continue; //Do not randomize default objects
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
                                    if (exp.ClassName == "BioSunFlareComponent" || exp.ClassName == "BioSunFlareStreakComponent")
                                    {
                                        var tint = exp.GetProperty<StructProperty>("FlareTint");
                                        if (tint != null)
                                        {
                                            RandomizeTint(random, tint, false);
                                            exp.WriteProperty(tint);
                                        }
                                    }
                                    else if (exp.ClassName == "BioSunActor")
                                    {
                                        var tint = exp.GetProperty<StructProperty>("SunTint");
                                        if (tint != null)
                                        {
                                            RandomizeTint(random, tint, false);
                                            exp.WriteProperty(tint);
                                        }
                                    }
                                }
                                else if (exp.ClassName == "SeqAct_Interp" && mainWindow.RANDSETTING_SHUFFLE_CUTSCENE_ACTORS)
                                {
                                    if (!loggedFilePath)
                                    {
                                        //Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilePath = true;
                                    }

                                    ShuffleCutscenePawns(exp, random);
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
                            }
                        }
                    }

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

                    if (package.IsModified)
                    {
                        Debug.WriteLine("Saving package: " + package.FilePath);
                        ModifiedFiles[package.FilePath] = package.FilePath;
                        package.save();
                    }
                });
            }

            //if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION)
            //{
            //    Log.Information("Apply galaxy map background transparency fix");
            //    MEPackage p = MEPackageHandler.OpenMEPackage(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\NOR\DSG\BIOA_NOR10_03_DSG.SFM"));
            //    p.getUExport(1655).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.PC_GalaxyMap_BGFix_1655.bin");
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
                if (tf.Modified)
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

        private void RandomizeMovementSpeed(Random random)
        {
            var femaleFile = MERFS.GetBasegameFile("BIOG_Female_Player_C.pcc");
            var maleFile = MERFS.GetBasegameFile("BIOG_Male_Player_C.pcc");
            var femalepackage = MEPackageHandler.OpenMEPackage(femaleFile);
            var malepackage = MEPackageHandler.OpenMEPackage(femaleFile);
            RandomizeMovementData(femalepackage.getUExport(2917), random);
            RandomizeMovementData(malepackage.getUExport(2672), random);
            femalepackage.save();
            malepackage.save();
        }

        private void RandomizeMovementData(ExportEntry export, Random random)
        {
            var props = export.GetProperties();
            foreach (var prop in props)
            {
                if (prop is FloatProperty fp)
                {
                    fp.Value = random.NextFloat(fp.Value - (fp * .75), fp.Value + (fp * .75));
                }

            }
            export.WriteProperties(props);
        }

        public enum AnimationKeyFormat
        {
            AKF_ConstantKeyLerp,
            AKF_VariableKeyLerp,
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

        private static string[] bonesToNotRandomize = { "root", "God" };
        public void RandomizeBioAnimSetData(ExportEntry export, Random random)
        {
            var trackbonenames = export.GetProperty<ArrayProperty<NameProperty>>("TrackBoneNames").Select(x => x.Value.Name).ToList(); //Make new list object.

            trackbonenames.RemoveAll(x => bonesToNotRandomize.Contains(x));

            trackbonenames.Shuffle(random);
            var bones = export.GetProperty<ArrayProperty<NameProperty>>("TrackBoneNames");
            foreach (var bonename in bones)
            {
                if (bonesToNotRandomize.Contains(bonename.Value.Name)) continue; //skip
                bonename.Value = trackbonenames[0];
                trackbonenames.RemoveAt(0);
            }
            export.WriteProperty(bones);
        }

        private bool shouldRandomizeBone(string boneName)
        {
            if (boneName.Contains("finger", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("eye", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("mouth", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("jaw", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("sneer", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("brow", StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }

        private void RandomizeAnimSequence(ExportEntry export, Random random)
        {
            var game = export.FileRef.Game;
            byte[] data = export.Data;
            try
            {
                var TrackOffsets = export.GetProperty<ArrayProperty<IntProperty>>("CompressedTrackOffsets");
                var animsetData = export.GetProperty<ObjectProperty>("m_pBioAnimSetData");
                if (animsetData.Value <= 0)
                {
                    Debug.WriteLine("trackdata is an import skipping");
                    return;
                } // don't randomize;

                var boneList = export.FileRef.getUExport(animsetData.Value).GetProperty<ArrayProperty<NameProperty>>("TrackBoneNames");
                Enum.TryParse(export.GetProperty<EnumProperty>("RotationCompressionFormat").Value.Name, out AnimationCompressionFormat rotCompression);
                int offset = export.propsEnd();

                int binLength = BitConverter.ToInt32(data, offset);
                //var LengthNode = new BinInterpNode
                //{
                //    Header = $"0x{offset:X4} AnimBinary length: {binLength}",
                //    Name = "_" + offset,
                //    Tag = NodeType.StructLeafInt
                //};
                //offset += 4;
                //subnodes.Add(LengthNode);
                var animBinStart = offset;

                int bone = 0;

                for (int i = 0; i < TrackOffsets.Count; i++)
                {
                    var bonePosOffset = TrackOffsets[i].Value;
                    i++;
                    var bonePosCount = TrackOffsets[i].Value;
                    var boneName = boneList[bone].Value;
                    bool doSomething = shouldRandomizeBone(boneName);
                    //POSKEYS
                    for (int j = 0; j < bonePosCount; j++)
                    {
                        offset = animBinStart + bonePosOffset + j * 12;
                        //Key #
                        //var PosKeys = new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} PosKey {j}",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.Unknown
                        //};
                        //BoneID.Items.Add(PosKeys);


                        var posX = BitConverter.ToSingle(data, offset);
                        if (doSomething)
                            data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(posX - (posX * .3f), posX + (posX * .3f))));

                        //PosKeys.Items.Add(new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} X: {posX} ",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.StructLeafFloat
                        //});
                        offset += 4;

                        var posY = BitConverter.ToSingle(data, offset);
                        if (doSomething)
                            data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(posY - (posY * .3f), posY + (posY * .3f))));
                        //PosKeys.Items.Add(new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} Y: {posY} ",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.StructLeafFloat
                        //});
                        offset += 4;

                        var posZ = BitConverter.ToSingle(data, offset);
                        if (doSomething)
                            data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(posZ - (posZ * .3f), posZ + (posZ * .3f))));

                        //PosKeys.Items.Add(new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} Z: {posZ} ",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.StructLeafFloat
                        //});
                        offset += 4;
                    }

                    var lookat = boneName.Name.Contains("lookat");

                    i++;
                    var boneRotOffset = TrackOffsets[i].Value;
                    i++;
                    var boneRotCount = TrackOffsets[i].Value;
                    int l = 12; // 12 length of rotation by default
                    var offsetRotX = boneRotOffset;
                    var offsetRotY = boneRotOffset;
                    var offsetRotZ = boneRotOffset;
                    var offsetRotW = boneRotOffset;
                    for (int j = 0; j < boneRotCount; j++)
                    {
                        float rotX = 0;
                        float rotY = 0;
                        float rotZ = 0;
                        float rotW = 0;

                        switch (rotCompression)
                        {
                            case AnimationCompressionFormat.ACF_None:
                                l = 16;
                                offset = animBinStart + boneRotOffset + j * l;
                                offsetRotX = offset;
                                rotX = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotX - (rotX * .1f), rotX + (rotX * .1f))));
                                offset += 4;
                                offsetRotY = offset;
                                rotY = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotY - (rotY * .1f), rotY + (rotY * .1f))));
                                offset += 4;
                                offsetRotZ = offset;
                                rotZ = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotZ - (rotZ * .1f), rotZ + (rotZ * .1f))));
                                offset += 4;
                                offsetRotW = offset;
                                rotW = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotW - (rotW * .1f), rotW + (rotW * .1f))));
                                offset += 4;
                                break;
                            case AnimationCompressionFormat.ACF_Float96NoW:
                                offset = animBinStart + boneRotOffset + j * l;
                                offsetRotX = offset;
                                rotX = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotX - (rotX * .1f), rotX + (rotX * .1f))));

                                offset += 4;
                                offsetRotY = offset;
                                rotY = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotY - (rotY * .1f), rotY + (rotY * .1f))));

                                offset += 4;
                                offsetRotZ = offset;
                                rotZ = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotZ - (rotZ * .1f), rotZ + (rotZ * .1f))));

                                offset += 4;
                                break;
                            case AnimationCompressionFormat.ACF_Fixed48NoW: // normalized quaternion with 3 16-bit fixed point fields
                                                                            //FQuat r;
                                                                            //r.X = (X - 32767) / 32767.0f;
                                                                            //r.Y = (Y - 32767) / 32767.0f;
                                                                            //r.Z = (Z - 32767) / 32767.0f;
                                                                            //RESTORE_QUAT_W(r);
                                                                            //break;
                            case AnimationCompressionFormat.ACF_Fixed32NoW:// normalized quaternion with 11/11/10-bit fixed point fields
                                                                           //FQuat r;
                                                                           //r.X = X / 1023.0f - 1.0f;
                                                                           //r.Y = Y / 1023.0f - 1.0f;
                                                                           //r.Z = Z / 511.0f - 1.0f;
                                                                           //RESTORE_QUAT_W(r);
                                                                           //break;
                            case AnimationCompressionFormat.ACF_IntervalFixed32NoW:
                            //FQuat r;
                            //r.X = (X / 1023.0f - 1.0f) * Ranges.X + Mins.X;
                            //r.Y = (Y / 1023.0f - 1.0f) * Ranges.Y + Mins.Y;
                            //r.Z = (Z / 511.0f - 1.0f) * Ranges.Z + Mins.Z;
                            //RESTORE_QUAT_W(r);
                            //break;
                            case AnimationCompressionFormat.ACF_Float32NoW:
                                //FQuat r;

                                //int _X = data >> 21;            // 11 bits
                                //int _Y = (data >> 10) & 0x7FF;  // 11 bits
                                //int _Z = data & 0x3FF;          // 10 bits

                                //*(unsigned*)&r.X = ((((_X >> 7) & 7) + 123) << 23) | ((_X & 0x7F | 32 * (_X & 0xFFFFFC00)) << 16);
                                //*(unsigned*)&r.Y = ((((_Y >> 7) & 7) + 123) << 23) | ((_Y & 0x7F | 32 * (_Y & 0xFFFFFC00)) << 16);
                                //*(unsigned*)&r.Z = ((((_Z >> 6) & 7) + 123) << 23) | ((_Z & 0x3F | 32 * (_Z & 0xFFFFFE00)) << 17);

                                //RESTORE_QUAT_W(r);


                                break;
                            case AnimationCompressionFormat.ACF_BioFixed48:
                                offset = animBinStart + boneRotOffset + j * l;
                                const float shift = 0.70710678118f;
                                const float scale = 1.41421356237f;
                                offsetRotX = offset;
                                rotX = (data[0] & 0x7FFF) / 32767.0f * scale - shift;
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotX - (rotX * .1f), rotX + (rotX * .1f))));

                                offset += 4;
                                offsetRotY = offset;
                                rotY = (data[1] & 0x7FFF) / 32767.0f * scale - shift;
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotY - (rotY * .1f), rotY + (rotY * .1f))));

                                offset += 4;
                                offsetRotZ = offset;
                                rotZ = (data[2] & 0x7FFF) / 32767.0f * scale - shift;
                                if (lookat)
                                    data.OverwriteRange(offset, BitConverter.GetBytes(random.NextFloat(rotZ - (rotZ * .1f), rotZ + (rotZ * .1f))));



                                //float w = 1.0f - (rotX * rotX + rotY * rotY + rotZ * rotZ);
                                //w = w >= 0.0f ? (float)Math.Sqrt(w) : 0.0f;
                                //int s = ((data[0] >> 14) & 2) | ((data[1] >> 15) & 1);
                                break;
                        }

                        if (rotCompression == AnimationCompressionFormat.ACF_BioFixed48 || rotCompression == AnimationCompressionFormat.ACF_Float96NoW || rotCompression == AnimationCompressionFormat.ACF_None)
                        {
                            //randomize here?
                            //var RotKeys = new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotX:X5} RotKey {j}",
                            //    Name = "_" + offsetRotX,
                            //    Tag = NodeType.Unknown
                            //};
                            //BoneID.Items.Add(RotKeys);
                            //RotKeys.Items.Add(new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotX:X5} RotX: {rotX} ",
                            //    Name = "_" + offsetRotX,
                            //    Tag = NodeType.StructLeafFloat
                            //});
                            //RotKeys.Items.Add(new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotY:X5} RotY: {rotY} ",
                            //    Name = "_" + offsetRotY,
                            //    Tag = NodeType.StructLeafFloat
                            //});
                            //RotKeys.Items.Add(new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotZ:X5} RotZ: {rotZ} ",
                            //    Name = "_" + offsetRotZ,
                            //    Tag = NodeType.StructLeafFloat
                            //});
                            if (rotCompression == AnimationCompressionFormat.ACF_None)
                            {
                                //RotKeys.Items.Add(new BinInterpNode
                                //{
                                //    Header = $"0x{offsetRotW:X5} RotW: {rotW} ",
                                //    Name = "_" + offsetRotW,
                                //    Tag = NodeType.StructLeafFloat
                                //});
                            }
                        }
                    }
                    bone++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading animsequence: " + ex.Message + ". Skipping");
            }

            export.Data = data; //write back
        }

        private void RandomizeBioDyanmicAnimSet(ExportEntry export)
        {

        }

        private void RandomizeWeaponIni(DuplicatingIni bioweapon, Random random, DuplicatingIni merfsOut)
        {
            foreach (var section in bioweapon.Sections)
            {
                var sectionsplit = section.Header.Split('.').ToList();
                if (sectionsplit.Count > 1)
                {
                    var objectname = sectionsplit[1];
                    if (objectname.StartsWith("SFXWeapon") || objectname.StartsWith("SFXHeavyWeapon"))
                    {
                        //We can randomize this section of the ini.
                        Debug.WriteLine($"Randomizing weapon {objectname}");

                        foreach (var entry in section.Entries)
                        {
                            if (entry.HasValue)
                            {
                                // if (entry.Key == "Damage") Debugger.Break();
                                string value = entry.Value;

                                //range check
                                if (value.StartsWith("("))
                                {
                                    value = value.Substring(0, value.IndexOf(')') + 1); //trim off trash on end (like ; comment )
                                    var p = StringStructParser.GetCommaSplitValues(value);
                                    if (p.Count == 2)
                                    {
                                        try
                                        {
                                            bool isInt = false;
                                            float x = 0;
                                            float y = 0;
                                            bool isZeroed = false;
                                            if (int.TryParse(p["X"].TrimEnd('f'), out var intX) && int.TryParse(p["Y"].TrimEnd('f'), out var intY))
                                            {
                                                //integers
                                                if (intX < 0 && intY < 0)
                                                {
                                                    Debug.WriteLine($" BELOW ZERO INT: {entry.Key} for {objectname}: {entry.RawText}");
                                                }

                                                bool validValue = false;
                                                for (int i = 0; i < 10; i++)
                                                {

                                                    bool isMaxMin = intX > intY;
                                                    //bool isMinMax = intY < intX;
                                                    isZeroed = intX == 0 && intY == 0;
                                                    if (isZeroed)
                                                    {
                                                        validValue = true;
                                                        break;
                                                    }; //skip
                                                    bool isSame = intX == intY;
                                                    bool belowzeroInt = intX < 0 || intY < 0;
                                                    bool abovezeroInt = intX > 0 || intY > 0;

                                                    int Max = isMaxMin ? intX : intY;
                                                    int Min = isMaxMin ? intY : intX;

                                                    int range = Max - Min;
                                                    if (range == 0) range = Max;
                                                    if (range == 0)
                                                        Debug.WriteLine("Range still 0");
                                                    int rangeExtension = range / 2; //50%

                                                    int newMin = Math.Max(0, random.Next(Min - rangeExtension, Min + rangeExtension));
                                                    int newMax = random.Next(Max - rangeExtension, Max + rangeExtension);
                                                    intX = isMaxMin ? newMax : newMin;
                                                    intY = isMaxMin ? newMin : newMax; //might need to check zeros
                                                    //if (entry.Key.Contains("MagSize")) Debugger.Break();

                                                    if (intX != 0 || intY != 0)
                                                    {
                                                        x = intX;
                                                        y = intY;
                                                        if (isSame) x = intY;
                                                        if (!belowzeroInt && (x <= 0 || y <= 0))
                                                        {
                                                            continue; //not valid. Redo this loop
                                                        }
                                                        if (abovezeroInt && (x <= 0 || y <= 0))
                                                        {
                                                            continue; //not valid. Redo this loop
                                                        }

                                                        validValue = true;
                                                        break; //break loop
                                                    }
                                                }

                                                if (!validValue)
                                                {
                                                    Debug.WriteLine($"Failed rerolls: {entry.Key} for {objectname}: {entry.RawText}");
                                                }
                                            }
                                            else
                                            {
                                                //if (section.Header.Contains("SFXWeapon_GethShotgun")) Debugger.Break();

                                                //floats
                                                //Fix error in bioware's coalesced file
                                                if (p["X"] == "0.65.0f") p["X"] = "0.65f";
                                                float floatx = float.Parse(p["X"].TrimEnd('f'));
                                                float floaty = float.Parse(p["Y"].TrimEnd('f'));
                                                bool belowzeroFloat = false;
                                                if (floatx < 0 || floaty < 0)
                                                {
                                                    Debug.WriteLine($" BELOW ZERO FLOAT: {entry.Key} for {objectname}: {entry.RawText}");
                                                    belowzeroFloat = true;
                                                }

                                                bool isMaxMin = floatx > floaty;
                                                bool isMinMax = floatx < floaty;
                                                bool isSame = floatx == floaty;
                                                isZeroed = floatx == 0 && floaty == 0;
                                                if (isZeroed)
                                                {
                                                    continue;
                                                }; //skip

                                                float Max = isMaxMin ? floatx : floaty;
                                                float Min = isMaxMin ? floaty : floatx;

                                                float range = Max - Min;
                                                if (range == 0) range = 0.1f * Max;
                                                float rangeExtension = range * 15f; //50%

                                                float newMin = Math.Max(0, random.NextFloat(Min - rangeExtension, Min + rangeExtension));
                                                float newMax = random.NextFloat(Max - rangeExtension, Max + rangeExtension);
                                                if (!belowzeroFloat)
                                                {
                                                    //ensure they don't fall below 0
                                                    if (newMin < 0)
                                                        newMin = Math.Max(newMin, Min / 2);

                                                    if (newMax < 0)
                                                        newMax = Math.Max(newMax, Max / 2);

                                                    //i have no idea what i'm doing
                                                }
                                                floatx = isMaxMin ? newMax : newMin;
                                                floaty = isMaxMin ? newMin : newMax; //might need to check zeros
                                                x = floatx;
                                                y = floaty;
                                                if (isSame) x = y;
                                            }

                                            if (isZeroed)
                                            {
                                                continue; //skip
                                            }
                                            entry.Value = $"(X={x},Y={y})";
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Error($"Cannot randomize weapon stat {objectname} {entry.Key}: {e.Message}");
                                        }
                                    }
                                }
                                else
                                {
                                    //Debug.WriteLine(entry.Key);
                                    var isInt = int.TryParse(entry.Value, out var burstVal);
                                    var isFloat = float.TryParse(entry.Value, out var floatVal);
                                    switch (entry.Key)
                                    {
                                        case "BurstRounds":
                                            {
                                                var burstMax = burstVal * 2;
                                                entry.Value = (random.Next(burstMax) + 1).ToString();
                                            }
                                            break;
                                        case "RateOfFireAI":
                                        case "DamageAI":
                                            {
                                                entry.Value = random.NextFloat(.1, 2).ToString();
                                            }
                                            break;
                                            //case 
                                    }
                                }
                            }
                        }

                        if (section.Entries.All(x => x.Key != "Damage"))
                        {
                            float X = random.NextFloat(2, 7);
                            float Y = random.NextFloat(2, 7);
                            section.Entries.Add(new DuplicatingIni.IniEntry($"Damage=(X={X},Y={Y})"));
                        }
                        merfsOut.Sections.Add(section); //add this section for out-writing
                    }
                }
            }
        }

        private void RandomizeCharacterCreator(Random random, List<TalkFile> tlks)
        {
            var biop_char = MEPackageHandler.OpenMEPackage(MERFS.GetBasegameFile("BioP_Char.pcc"));
            foreach (var export in biop_char.Exports)
            {
                if (export.ClassName == "BioMorphFace")
                {
                    RandomizeBioMorphFace(export, random); //.3 default
                    continue;
                }

                if (export.ClassName == "BioMorphFaceFESliderColour")
                {

                }
                else if (export.ClassName == "BioMorphFaceFESliderMorph")
                {

                }
                else if (export.ClassName == "BioMorphFaceFESliderScalar")
                {

                }
                else if (export.ClassName == "BioMorphFaceFESliderSetMorph")
                {

                }
                else if (export.ClassName == "BioMorphFaceFESliderTexture")
                {

                }
            }

            biop_char.save();
            ModifiedFiles[biop_char.FilePath] = biop_char.FilePath;
        }

        private void RandomizeBioLookAtDefinition(ExportEntry export, Random random)
        {
            Log.Information("Randomizing BioLookAtDefinition " + export.UIndex);
            var boneDefinitions = export.GetProperty<ArrayProperty<StructProperty>>("BoneDefinitions");
            if (boneDefinitions != null)
            {
                foreach (var item in boneDefinitions)
                {
                    if (item.GetProp<NameProperty>("m_nBoneName").Value.Name.StartsWith("Eye"))
                    {
                        item.GetProp<FloatProperty>("m_fLimit").Value = random.Next(1, 5);
                        item.GetProp<FloatProperty>("m_fUpDownLimit").Value = random.Next(1, 5);
                    }
                    else
                    {
                        item.GetProp<FloatProperty>("m_fLimit").Value = random.Next(1, 170);
                        item.GetProp<FloatProperty>("m_fUpDownLimit").Value = random.Next(70, 170);
                    }

                }
            }
            export.WriteProperty(boneDefinitions);
        }


        private void RandomizeHeightFogComponent(ExportEntry exp, Random random)
        {
            var properties = exp.GetProperties();
            var lightColor = properties.GetProp<StructProperty>("LightColor");
            if (lightColor != null)
            {
                lightColor.GetProp<ByteProperty>("R").Value = (byte)random.Next(256);
                lightColor.GetProp<ByteProperty>("G").Value = (byte)random.Next(256);
                lightColor.GetProp<ByteProperty>("B").Value = (byte)random.Next(256);

                var density = properties.GetProp<FloatProperty>("Density");
                if (density != null)
                {
                    var thicknessRandomizer = random.NextFloat(-density * .03, density * 1.15);
                    density.Value = density + thicknessRandomizer;
                }

                exp.WriteProperties(properties);
            }
        }


        private void RandomizePawnMaterialInstances(ExportEntry exp, Random random)
        {
            //Don't know if this works
            var hairMeshObj = exp.GetProperty<ObjectProperty>("m_oHairMesh");
            if (hairMeshObj != null)
            {
                var headMesh = exp.FileRef.getUExport(hairMeshObj.Value);
                var materials = headMesh.GetProperty<ArrayProperty<ObjectProperty>>("Materials");
                if (materials != null)
                {
                    foreach (var materialObj in materials)
                    {
                        //MaterialInstanceConstant
                        ExportEntry material = exp.FileRef.getUExport(materialObj.Value);
                        RandomizeMaterialInstance(material, random);

                    }
                }
            }
        }

        private void RandomizeMaterialInstance(ExportEntry material, Random random)
        {
            var props = material.GetProperties();

            {
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                if (vectors != null)
                {
                    foreach (var vector in vectors)
                    {
                        var pc = vector.GetProp<StructProperty>("ParameterValue");
                        if (pc != null)
                        {
                            RandomizeTint(random, pc, false);
                        }
                    }
                }

                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                if (scalars != null)
                {
                    for (int i = 0; i < scalars.Count; i++)
                    {
                        var scalar = scalars[i];
                        var parameter = scalar.GetProp<NameProperty>("ParameterName");
                        var currentValue = scalar.GetProp<FloatProperty>("ParameterValue");
                        if (currentValue > 1)
                        {
                            scalar.GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0, currentValue * 1.3);
                        }
                        else
                        {
                            //Debug.WriteLine("Randomizing parameter " + scalar.GetProp<NameProperty>("ParameterName"));
                            scalar.GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0, 1);
                        }
                    }

                    //foreach (var scalar in vectors)
                    //{
                    //    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                    //    RandomizeTint(random, paramValue, false);
                    //}
                }
            }
            material.WriteProperties(props);
        }

        private void RandomizeSplash(Random random, MEPackage entrymenu)
        {
            ExportEntry planetMaterial = entrymenu.getUExport(1316);
            RandomizePlanetMaterialInstanceConstant(planetMaterial, random);

            //Corona
            ExportEntry coronaMaterial = entrymenu.getUExport(1317);
            var props = coronaMaterial.GetProperties();
            {
                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                scalars[0].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0.01, 0.05); //Bloom
                scalars[1].GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(1, 10); //Opacity
                RandomizeTint(random, vectors[0].GetProp<StructProperty>("ParameterValue"), false);
            }
            coronaMaterial.WriteProperties(props);

            //CameraPan
            ExportEntry cameraInterpData = entrymenu.getUExport(946);
            var interpLength = cameraInterpData.GetProperty<FloatProperty>("InterpLength");
            float animationLength = random.NextFloat(60, 120);
            ;
            interpLength.Value = animationLength;
            cameraInterpData.WriteProperty(interpLength);

            ExportEntry cameraInterpTrackMove = entrymenu.getUExport(967);
            cameraInterpTrackMove.Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.InterpTrackMove967_EntryMenu_CameraPan.bin");
            props = cameraInterpTrackMove.GetProperties(forceReload: true);
            var posTrack = props.GetProp<StructProperty>("PosTrack");
            bool ZUp = false;
            if (posTrack != null)
            {
                var points = posTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                float startx = random.NextFloat(-5100, -4800);
                float starty = random.NextFloat(13100, 13300);
                float startz = random.NextFloat(-39950, -39400);

                startx = -4930;
                starty = 13212;
                startz = -39964;

                float peakx = random.NextFloat(-5100, -4800);
                float peaky = random.NextFloat(13100, 13300);
                float peakz = random.NextFloat(-39990, -39920); //crazy small Z values here for some reason.
                ZUp = peakz > startz;

                if (points != null)
                {
                    int i = 0;
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            if (i != 1) x.Value = startx;
                            y.Value = i == 1 ? peaky : starty;
                            z.Value = i == 1 ? peakz : startz;
                        }

                        if (i > 0)
                        {
                            s.GetProp<FloatProperty>("InVal").Value = i == 1 ? (animationLength / 2) : animationLength;
                        }

                        i++;
                    }
                }
            }

            var eulerTrack = props.GetProp<StructProperty>("EulerTrack");
            if (eulerTrack != null)
            {
                var points = eulerTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                //float startx = random.NextFloat(, -4800);
                float startPitch = random.NextFloat(25, 35);
                float startYaw = random.NextFloat(-195, -160);

                //startx = 1.736f;
                //startPitch = 31.333f;
                //startYaw = -162.356f;

                float peakx = 1.736f; //Roll
                float peakPitch = ZUp ? random.NextFloat(0, 30) : random.NextFloat(-15, 10); //Pitch
                float peakYaw = random.NextFloat(-315, -150);
                if (points != null)
                {
                    int i = 0;
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            //x.Value = i == 1 ? peakx : startx;
                            y.Value = i == 1 ? peakPitch : startPitch;
                            z.Value = i == 1 ? peakYaw : startYaw;
                        }

                        if (i > 0)
                        {
                            s.GetProp<FloatProperty>("InVal").Value = i == 1 ? (animationLength / 2) : animationLength;
                        }

                        i++;
                    }

                }
            }

            cameraInterpTrackMove.WriteProperties(props);

            var fovCurve = entrymenu.getUExport(964);
            fovCurve.Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.InterpTrackMove964_EntryMenu_CameraFOV.bin");
            props = fovCurve.GetProperties(forceReload: true);
            //var pi = props.GetProp<ArrayProperty<StructProperty>>("Points");
            //var pi2 = props.GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<FloatProperty>("OutVal");
            props.GetProp<StructProperty>("FloatTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<FloatProperty>("OutVal").Value = random.NextFloat(65, 90); //FOV
            props.GetProp<StructProperty>("FloatTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<FloatProperty>("InVal").Value = random.NextFloat(1, animationLength - 1);
            props.GetProp<StructProperty>("FloatTrack").GetProp<ArrayProperty<StructProperty>>("Points")[2].GetProp<FloatProperty>("InVal").Value = animationLength;
            fovCurve.WriteProperties(props);

            var menuTransitionAnimation = entrymenu.getUExport(968);
            props = menuTransitionAnimation.GetProperties();
            props.AddOrReplaceProp(new EnumProperty("IMF_RelativeToInitial", "EInterpTrackMoveFrame", MEGame.ME1, "MoveFrame"));
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[0].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("X").Value = 0;
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[0].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Y").Value = 0;
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[0].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Z").Value = 0;

            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("X").Value = random.NextFloat(-180, 180);
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Y").Value = random.NextFloat(-180, 180);
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Z").Value = random.NextFloat(-180, 180);

            menuTransitionAnimation.WriteProperties(props);

            var dbStandard = entrymenu.getUExport(730);
            props = dbStandard.GetProperties();
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[1].GetProp<ArrayProperty<StructProperty>>("Links")[1].GetProp<ObjectProperty>("LinkedOp").Value = 2926; //Bioware logo
            dbStandard.WriteProperties(props);
        }

        private List<string> acceptableTagsForPawnShuffling = new List<string>();

        private void ShuffleCutscenePawns(ExportEntry export, Random random)
        {
            var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");

            List<ObjectProperty> pawnsToShuffle = new List<ObjectProperty>();
            var playerRefs = new List<ExportEntry>();
            foreach (var variableLink in variableLinks)
            {
                var expectedType = variableLink.GetProp<ObjectProperty>("ExpectedType");
                var expectedTypeStr = export.FileRef.getEntry(expectedType.Value).ObjectName;
                var DEBUG = variableLink.GetProp<StrProperty>("LinkDesc");
                if (expectedTypeStr == "SeqVar_Object" || expectedTypeStr == "SeqVar_Player" || expectedTypeStr == "BioSeqVar_ObjectFindByTag")
                {
                    //Investigate the links
                    var linkedVariables = variableLink.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                    foreach (var objRef in linkedVariables)
                    {
                        var linkedObj = export.FileRef.getUExport(objRef.Value).GetProperty<ObjectProperty>("ObjValue");
                        if (linkedObj != null)
                        {
                            //This is the data the node is referencing
                            var linkedObjectEntry = export.FileRef.getEntry(linkedObj.Value);
                            var linkedObjName = linkedObjectEntry.ObjectName;
                            if (linkedObjName == "BioPawn" && linkedObjectEntry is ExportEntry bioPawnExport)
                            {
                                var flyingpawn = bioPawnExport.GetProperty<BoolProperty>("bCanFly")?.Value;
                                if (flyingpawn == null || flyingpawn == false)
                                {
                                    pawnsToShuffle.Add(objRef); //pointer to this node
                                }
                            }
                        }
                        else if (expectedTypeStr == "SeqVar_Object")
                        {
                            //We might be assigned to. We need to look at the parent sequence
                            //and find what assigns me
                            var node = export.FileRef.getUExport(objRef.Value);
                            var parentRef = node.GetProperty<ObjectProperty>("ParentSequence");
                            if (parentRef != null)
                            {
                                var parent = export.FileRef.getUExport(parentRef.Value);
                                var sequenceObjects = parent.GetProperty<ArrayProperty<ObjectProperty>>("SequenceObjects");
                                if (sequenceObjects != null)
                                {
                                    foreach (var obj in sequenceObjects)
                                    {
                                        if (obj.Value <= 0) continue;
                                        var sequenceObject = export.FileRef.getUExport(obj.Value);
                                        if (sequenceObject.InheritsFrom("SequenceAction") && sequenceObject.ClassName == "SeqAct_SetObject" && sequenceObject != export)
                                        {
                                            //check if target is my node
                                            var varlinqs = sequenceObject.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
                                            if (varlinqs != null)
                                            {
                                                var targetLink = varlinqs.FirstOrDefault(x =>
                                                {
                                                    var linkdesc = x.GetProp<StrProperty>("LinkDesc");
                                                    return linkdesc != null && linkdesc == "Target";
                                                });
                                                var targetLinkedVariables = targetLink?.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                                                if (targetLinkedVariables != null)
                                                {
                                                    //see if target is node we are investigating for setting.
                                                    foreach (var targetLinkedVariable in targetLinkedVariables)
                                                    {
                                                        var potentialTarget = export.FileRef.getUExport(targetLinkedVariable.Value);
                                                        if (potentialTarget == node)
                                                        {
                                                            Debug.WriteLine("FOUND TARGET!");
                                                            //See what value this is set to. If it inherits from BioPawn we can use it in the shuffling.
                                                            var valueLink = varlinqs.FirstOrDefault(x =>
                                                            {
                                                                var linkdesc = x.GetProp<StrProperty>("LinkDesc");
                                                                return linkdesc != null && linkdesc == "Value";
                                                            });
                                                            var valueLinkedVariables = valueLink?.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                                                            if (valueLinkedVariables != null && valueLinkedVariables.Count == 1)
                                                            {
                                                                var linkedNode = export.FileRef.getUExport(valueLinkedVariables[0].Value);
                                                                var linkedNodeType = linkedNode.GetProperty<ObjectProperty>("ObjValue");
                                                                if (linkedNodeType != null)
                                                                {
                                                                    var linkedNodeData = export.FileRef.getUExport(linkedNodeType.Value);
                                                                    if (linkedNodeData.InheritsFrom("BioPawn"))
                                                                    {
                                                                        //We can shuffle this item.
                                                                        Debug.WriteLine("Adding shuffle item: " + objRef.Value);
                                                                        pawnsToShuffle.Add(objRef); //pointer to this node
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        string className = export.FileRef.getUExport(objRef.Value).ClassName;
                        if (className == "SeqVar_Player")
                        {
                            playerRefs.Add(export.FileRef.getUExport(objRef.Value));
                            pawnsToShuffle.Add(objRef); //pointer to this node
                        }
                        else if (className == "BioSeqVar_ObjectFindByTag")
                        {
                            var tagToFind = export.FileRef.getUExport(objRef.Value).GetProperty<StrProperty>("m_sObjectTagToFind")?.Value;
                            if (tagToFind != null && acceptableTagsForPawnShuffling.Contains(tagToFind))
                            {
                                pawnsToShuffle.Add(objRef); //pointer to this node
                            }
                        }
                    }
                }
            }

            if (pawnsToShuffle.Count > 1)
            {
                int reshuffleAttemptsRemaining = 3;
                while (reshuffleAttemptsRemaining > 0)
                {
                    reshuffleAttemptsRemaining--;
                    Log.Information("Randomizing pawns in interp: " + export.GetFullPath);
                    foreach (var refx in playerRefs)
                    {
                        refx.WriteProperty(new BoolProperty(true, "bReturnsPawns")); //Ensure the object returns pawns. It should, but maybe it doesn't.
                    }

                    var newAssignedValues = pawnsToShuffle.Select(x => x.Value).ToList();
                    newAssignedValues.Shuffle(random);
                    for (int i = 0; i < pawnsToShuffle.Count; i++)
                    {
                        pawnsToShuffle[i].Value = newAssignedValues[i];
                    }

                    export.WriteProperty(variableLinks);
                    if (export.EntryHasPendingChanges)
                    {
                        break;
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
                    RandomizeTint(random, paramValue, false);
                }
            }
            planetMaterial.WriteProperties(props);
        }




        private void RandomizeFaceFX(ExportEntry exp, Random random, int amount)
        {
            try
            {
                Log.Information($"[{Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}] Randomizing FaceFX export {exp.UIndex}");
                var d = exp.Data;
                ME2FaceFXAnimSet animSet = new ME2FaceFXAnimSet(exp);
                for (int i = 0; i < animSet.Data.Data.Count(); i++)
                {
                    var faceFxline = animSet.Data.Data[i];
                    //if (true)

                    bool randomizedBoneList = false;
                    if (random.Next(10 - amount) == 0)
                    {
                        //Randomize the names used for animation
                        List<int> usedIndexes = faceFxline.animations.Select(x => x.index).ToList();
                        usedIndexes.Shuffle(random);
                        for (int j = 0; j < faceFxline.animations.Length; j++)
                        {
                            faceFxline.animations[j].index = usedIndexes[j];
                        }

                        randomizedBoneList = true;
                    }
                    if (!randomizedBoneList || random.Next(16 - amount) == 0)
                    {
                        //Randomize the points
                        for (int j = 0; j < faceFxline.points.Length; j++)
                        {
                            bool isLast = j == faceFxline.points.Length;
                            var currentWeight = faceFxline.points[j].weight;
                            switch (amount)
                            {
                                case 1: //A few broken bones
                                    faceFxline.points[j].weight += random.NextFloat(-.25, .25);
                                    break;
                                case 2: //A significant amount of broken bones
                                    faceFxline.points[j].weight += random.NextFloat(-.5, .5);
                                    break;
                                case 3: //That's not how the face is supposed to work
                                    if (random.Next(5) == 0)
                                    {
                                        faceFxline.points[j].weight = random.NextFloat(-3, 3);
                                    }
                                    else if (isLast && random.Next(3) == 0)
                                    {
                                        faceFxline.points[j].weight = random.NextFloat(.7, .7);
                                    }
                                    else
                                    {
                                        faceFxline.points[j].weight *= 8;
                                    }

                                    break;
                                case 4: //:O
                                    if (random.Next(12) == 0)
                                    {
                                        faceFxline.points[j].weight = random.NextFloat(-5, 5);
                                    }
                                    else if (isLast && random.Next(3) == 0)
                                    {
                                        faceFxline.points[j].weight = random.NextFloat(.9, .9);
                                    }
                                    else
                                    {
                                        faceFxline.points[j].weight *= 5;
                                    }
                                    //Debug.WriteLine(faceFxline.points[j].weight);

                                    break;
                                case 5: //Utter madness
                                    faceFxline.points[j].weight = random.NextFloat(-10, 10);
                                    break;
                                default:
                                    Debugger.Break();
                                    break;
                            }
                        }
                    }

                    //Debugging only: Get list of all animation names
                    //for (int j = 0; j < faceFxline.animations.Length; j++)
                    //{
                    //    var animationName = animSet.Header.Names[faceFxline.animations[j].index]; //animation name
                    //    faceFxBoneNames.Add(animationName);
                    //}
                }

                Log.Information($"[{Path.GetFileNameWithoutExtension(exp.FileRef.FilePath)}] Randomized FaceFX for export " + exp.UIndex);
                animSet.Save();
            }
            catch (Exception e)
            {
                //Do nothing for now.
                Log.Error("AnimSet error! " + App.FlattenException((e)));
            }
        }

        public void RandomizeWallText(ExportEntry entry, Random random, List<TalkFile> Tlks)
        {
            var modules = entry.GetProperty<ArrayProperty<ObjectProperty>>("Modules");
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    var signModule = entry.FileRef.getUExport(module.Value);
                    var srRef = signModule.GetProperty<StringRefProperty>("m_srGameName");
                    if (srRef != null)
                    {
                        int newId = 0;
                        var container = Tlks.FirstOrDefault(x => x.findDataById(srRef.Value) != "No data");
                        var origStr = container?.findDataById(srRef.Value);
                        if (origStr != null)
                        {
                            foreach (var tlk in Tlks)
                            {
                                var availableItems = tlk.StringRefs.Where(x => x.Data != null && x.Data.Length == origStr.Length).ToList();
                                if (availableItems.Count > 0)
                                {
                                    srRef.Value = availableItems[random.Next(availableItems.Count)].StringID;
                                    signModule.WriteProperty(srRef);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddMERSplash(Random random)
        {
            IMEPackage entrymenu = MEPackageHandler.OpenMEPackage(Utilities.GetEntryMenuFile());

            //Connect attract to BWLogo
            var attractMovie = entrymenu.getUExport(729);
            var props = attractMovie.GetProperties();
            var movieName = props.GetProp<StrProperty>("m_sMovieName");
            movieName.Value = "merintro";
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[1].GetProp<ArrayProperty<StructProperty>>("Links")[0].GetProp<ObjectProperty>("LinkedOp").Value = 732; //Bioware logo
            attractMovie.WriteProperties(props);

            //Rewrite ShowSplash to BWLogo to point to merintro instead
            var showSplash = entrymenu.getUExport(736);
            props = showSplash.GetProperties();
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links")[1].GetProp<ObjectProperty>("LinkedOp").Value = 729; //attractmovie logo
            showSplash.WriteProperties(props);

            //Visual only (for debugging): Remove connection to 

            //Update inputs to point to merintro comparebool
            var guiinput = entrymenu.getUExport(738);
            props = guiinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                outlink.GetProp<ArrayProperty<StructProperty>>("Links")[0].GetProp<ObjectProperty>("LinkedOp").Value = 2936; //Comparebool
            }

            guiinput.WriteProperties(props);

            var playerinput = entrymenu.getUExport(739);
            props = playerinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                var links = outlink.GetProp<ArrayProperty<StructProperty>>("Links");
                foreach (var link in links)
                {
                    link.GetProp<ObjectProperty>("LinkedOp").Value = 2936; //Comparebool
                }
            }

            playerinput.WriteProperties(props);

            //Clear old unused inputs for attract
            guiinput = entrymenu.getUExport(737);
            props = guiinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                outlink.GetProp<ArrayProperty<StructProperty>>("Links").Clear();
            }

            guiinput.WriteProperties(props);

            playerinput = entrymenu.getUExport(740);
            props = playerinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                outlink.GetProp<ArrayProperty<StructProperty>>("Links").Clear();
            }

            playerinput.WriteProperties(props);

            //Connect CompareBool outputs
            var mercomparebool = entrymenu.getUExport(2936);
            props = mercomparebool.GetProperties();
            var outlinks = props.GetProp<ArrayProperty<StructProperty>>("OutputLinks");
            //True
            var outlink1 = outlinks[0].GetProp<ArrayProperty<StructProperty>>("Links");
            StructProperty newLink = null;
            if (outlink1.Count == 0)
            {
                PropertyCollection p = new PropertyCollection();
                p.Add(new ObjectProperty(2938, "LinkedOp"));
                p.Add(new IntProperty(0, "InputLinkIdx"));
                p.Add(new NoneProperty());
                newLink = new StructProperty("SeqOpOutputInputLink", p);
                outlink1.Add(newLink);
            }
            else
            {
                newLink = outlink1[0];
            }

            newLink.GetProp<ObjectProperty>("LinkedOp").Value = 2938;

            //False
            var outlink2 = outlinks[1].GetProp<ArrayProperty<StructProperty>>("Links");
            newLink = null;
            if (outlink2.Count == 0)
            {
                PropertyCollection p = new PropertyCollection();
                p.Add(new ObjectProperty(2934, "LinkedOp"));
                p.Add(new IntProperty(0, "InputLinkIdx"));
                p.Add(new NoneProperty());
                newLink = new StructProperty("SeqOpOutputInputLink", p);
                outlink2.Add(newLink);
            }
            else
            {
                newLink = outlink2[0];
            }

            newLink.GetProp<ObjectProperty>("LinkedOp").Value = 2934;

            mercomparebool.WriteProperties(props);

            //Update output of setbool to next comparebool, point to shared true value
            var setBool = entrymenu.getUExport(2934);
            props = setBool.GetProperties();
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links")[0].GetProp<ObjectProperty>("LinkedOp").Value = 729; //CompareBool (step 2)
            props.GetProp<ArrayProperty<StructProperty>>("VariableLinks")[1].GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables")[0].Value = 2952; //Shared True
            setBool.WriteProperties(props);


            //Default setbool should be false, not true
            var boolValueForMERSkip = entrymenu.getUExport(2955);
            var bValue = boolValueForMERSkip.GetProperty<IntProperty>("bValue");
            bValue.Value = 0;
            boolValueForMERSkip.WriteProperty(bValue);

            //Extract MER Intro
            var merIntroDir = Path.Combine(Utilities.GetAppDataFolder(), "merintros");
            if (Directory.Exists(merIntroDir))
            {
                var merIntros = Directory.GetFiles(merIntroDir, "*.bik").ToList();
                string merToExtract = merIntros[random.Next(merIntros.Count)];
                File.Copy(merToExtract, MERFS.GetGameFile(@"BioGame\CookedPC\Movies\merintro.bik"), true);
                entrymenu.save();
                //Add to fileindex
                var fileIndex = MERFS.GetGameFile(@"BioGame\CookedPC\FileIndex.txt");
                var filesInIndex = File.ReadAllLines(fileIndex).ToList();
                if (filesInIndex.All(x => x != @"Movies\MERIntro.bik"))
                {
                    filesInIndex.Add(@"Movies\MERIntro.bik");
                    File.WriteAllLines(fileIndex, filesInIndex);
                }

                ModifiedFiles[entrymenu.FilePath] = entrymenu.FilePath;
            }

        }

        private static string[] hazardTypes = { "Cold", "Heat", "Toxic", "Radiation", "Vacuum" };

        private void RandomizeHazard(ExportEntry export, Random random)
        {
            Log.Information("Randomizing hazard sequence objects for " + export.UIndex + ": " + export.GetIndexedFullPath);
            var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
            if (variableLinks != null)
            {
                foreach (var variableLink in variableLinks)
                {
                    var expectedType = export.FileRef.getEntry(variableLink.GetProp<ObjectProperty>("ExpectedType").Value).ObjectName;
                    var linkedVariable = export.FileRef.getUExport(variableLink.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables")[0].Value); //hoochie mama that is one big statement.

                    switch (expectedType)
                    {
                        case "SeqVar_Name":
                            //Hazard type
                            var hazardTypeProp = linkedVariable.GetProperty<NameProperty>("NameValue");
                            hazardTypeProp.Value = hazardTypes[random.Next(hazardTypes.Length)];
                            Log.Information(" >> Hazard type: " + hazardTypeProp.Value);
                            linkedVariable.WriteProperty(hazardTypeProp);
                            break;
                        case "SeqVar_Bool":
                            //Force helmet
                            var hazardHelmetProp = new IntProperty(random.Next(2), "bValue");
                            Log.Information(" >> Force helmet on: " + hazardHelmetProp.Value);
                            linkedVariable.WriteProperty(hazardHelmetProp);
                            break;
                        case "SeqVar_Int":
                            //Hazard level
                            var hazardLevelProp = new IntProperty(random.Next(4), "IntValue");
                            if (random.Next(8) == 0) //oof, for the player
                            {
                                hazardLevelProp.Value = 4;
                            }

                            Log.Information(" >> Hazard level: " + hazardLevelProp.Value);
                            linkedVariable.WriteProperty(hazardLevelProp);
                            break;
                    }
                }
            }
        }

        private void scaleHeadMesh(ExportEntry meshRef, float headScale)
        {
            Log.Information("Randomizing headmesh for " + meshRef.GetIndexedFullPath);
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
            Log.Information($"[{Path.GetFileNameWithoutExtension(export.FileRef.FilePath)}] Randomizing movement interpolations for " + export.UIndex + ": " + export.GetIndexedFullPath);
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


        private void RandomizerHammerHead(MEPackage package, Random random)
        {
            ExportEntry SVehicleSimTank = package.Exports[23314];
            var props = SVehicleSimTank.GetProperties();
            StructProperty torqueCurve = SVehicleSimTank.GetProperty<StructProperty>("m_TorqueCurve");
            ArrayProperty<StructProperty> points = torqueCurve.GetProp<ArrayProperty<StructProperty>>("Points");
            var minOut = random.Next(4000, 5600);
            var maxOut = random.Next(6000, 22000);
            minOut = 5600;
            maxOut = 20000;
            var stepping = (maxOut - minOut) / 3; //starts at 0 with 3 upgrades
            for (int i = 0; i < points.Count; i++)
            {
                float newVal = minOut + (stepping * i);
                Log.Information($"Setting MakoTorque[{i}] to {newVal}");
                points[i].GetProp<FloatProperty>("OutVal").Value = newVal;
            }

            SVehicleSimTank.WriteProperty(torqueCurve);

            if (mainWindow.RANDSETTING_MOVEMENT_MAKO_WHEELS)
            {
                //Reverse the steering to back wheels
                //Front
                ExportEntry LFWheel = package.Exports[36984];
                ExportEntry RFWheel = package.Exports[36987];
                //Rear
                ExportEntry LRWheel = package.Exports[36986];
                ExportEntry RRWheel = package.Exports[36989];

                var LFSteer = LFWheel.GetProperty<FloatProperty>("SteerFactor");
                var LRSteer = LRWheel.GetProperty<FloatProperty>("SteerFactor");
                var RFSteer = RFWheel.GetProperty<FloatProperty>("SteerFactor");
                var RRSteer = RRWheel.GetProperty<FloatProperty>("SteerFactor");

                LFSteer.Value = 0f;
                LRSteer.Value = 4f;
                RFSteer.Value = 0f;
                RRSteer.Value = 4f;

                LFWheel.WriteProperty(LFSteer);
                RFWheel.WriteProperty(RFSteer);
                LRWheel.WriteProperty(LRSteer);
                RRWheel.WriteProperty(RRSteer);
            }

            //Randomize the jumpjets
            ExportEntry BioVehicleBehaviorBase = package.Exports[23805];
            var behaviorProps = BioVehicleBehaviorBase.GetProperties();
            foreach (UProperty prop in behaviorProps)
            {
                if (prop.Name.Name.StartsWith("m_fThrusterScalar"))
                {
                    var floatprop = prop as FloatProperty;
                    floatprop.Value = random.NextFloat(.1, 6);
                }
            }

            BioVehicleBehaviorBase.WriteProperties(behaviorProps);
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
            Log.Information($"[{Path.GetFileNameWithoutExtension(export.FileRef.FilePath)}] Randomizing pawn size for " + export.UIndex + ": " + export.GetIndexedFullPath);
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
                //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.GetFullPath + " vPos");
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
            //            //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.GetFullPath + " offset");
            //            offset.Value = offset.Value * random.NextFloat(1 - (amount / 3), 1 + (amount / 3));
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Randomizes bio morph faces in a specified file. Will check if file exists first
        /// </summary>
        /// <param name="file"></param>
        /// <param name="random"></param>
        private void RandomizeBioMorphFaceWrapper(string file, Random random)
        {
            if (File.Exists(file))
            {
                var package = MEPackageHandler.OpenMEPackage(file);
                {
                    foreach (ExportEntry export in package.Exports)
                    {
                        if (export.ClassName == "BioMorphFace")
                        {
                            RandomizeBioMorphFace(export, random);
                        }
                    }
                }
                ModifiedFiles[package.FilePath] = package.FilePath;
                package.save();
            }
        }


        private void RandomizeGalaxyMap(IMEPackage package, Random random)
        {
            foreach (ExportEntry export in package.Exports)
            {
                switch (export.ClassName)
                {
                    case "SFXCluster":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("StarColor");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }
                            starColor = props.GetProp<StructProperty>("StarColor2");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }

                            props.GetProp<IntProperty>("PosX").Value = random.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = random.Next(1000);


                            var intensity = props.GetProp<FloatProperty>("SphereIntensity");
                            if (intensity != null) intensity.Value = random.NextFloat(0, 6);
                            intensity = props.GetProp<FloatProperty>("NebularDensity");
                            if (intensity != null) intensity.Value = random.NextFloat(0, 6);
                            intensity = props.GetProp<FloatProperty>("SphereSize");
                            if (intensity != null) intensity.Value = random.NextFloat(0, 6);

                            export.WriteProperties(props);
                        }
                        //RandomizeClustersXY(export, random);

                        break;
                    case "SFXSystem":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("StarColor");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }

                            starColor = props.GetProp<StructProperty>("FlareTint");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }


                            starColor = props.GetProp<StructProperty>("SunColor");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }

                            props.GetProp<IntProperty>("PosX").Value = random.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = random.Next(1000);


                            var scale = props.GetProp<FloatProperty>("Scale");
                            if (scale != null) scale.Value = random.NextFloat(.1, 2);


                            export.WriteProperties(props);
                        }
                        break;
                    case "BioPlanet":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("SunColor");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }

                            starColor = props.GetProp<StructProperty>("FlareTint");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }


                            starColor = props.GetProp<StructProperty>("CloudColor");
                            if (starColor != null)
                            {
                                RandomizeTint(random, starColor, false);
                            }

                            var resourceRichness = props.GetProp<FloatProperty>("ResourceRichness");
                            if (resourceRichness != null)
                            {
                                resourceRichness.Value = random.NextFloat(0, 1.2);
                            }
                            else
                            {
                                props.AddOrReplaceProp(new FloatProperty(random.NextFloat(0, .6), "ResourceRichness"));
                            }

                            props.GetProp<IntProperty>("PosX").Value = random.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = random.Next(1000);


                            var scale = props.GetProp<FloatProperty>("Scale");
                            if (scale != null) scale.Value = random.NextFloat(.1, 6);


                            export.WriteProperties(props);
                        }
                        break;
                    case "MaterialInstanceConstant":
                        RandomizeMaterialInstance(export, random);
                        break;
                }
            }

            if (package.IsModified)
            {
                package.save();
            }
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

        private void RandomizeTint(Random random, StructProperty tint, bool randomizeAlpha)
        {
            var a = tint.GetProp<FloatProperty>("A");
            var r = tint.GetProp<FloatProperty>("R");
            var g = tint.GetProp<FloatProperty>("G");
            var b = tint.GetProp<FloatProperty>("B");

            float totalTintValue = r + g + b;

            //Randomizing hte pick order will ensure we get a random more-dominant first color (but only sometimes).
            //e.g. if e went in R G B order red would always have a chance at a higher value than the last picked item
            List<FloatProperty> randomOrderChooser = new List<FloatProperty>();
            randomOrderChooser.Add(r);
            randomOrderChooser.Add(g);
            randomOrderChooser.Add(b);
            randomOrderChooser.Shuffle(random);

            randomOrderChooser[0].Value = random.NextFloat(0, totalTintValue);
            totalTintValue -= randomOrderChooser[0].Value;

            randomOrderChooser[1].Value = random.NextFloat(0, totalTintValue);
            totalTintValue -= randomOrderChooser[1].Value;

            randomOrderChooser[2].Value = totalTintValue;
            if (randomizeAlpha)
            {
                a.Value = random.NextFloat(0, 1);
            }
        }

        public bool RunAllFilesRandomizerPass
        {
            get => mainWindow.RANDSETTING_BIOMORPHFACES
                   || mainWindow.RANDSETTING_MISC_MAPPAWNSIZES
                   || mainWindow.RANDSETTING_MISC_INTERPS
                   || mainWindow.RANDSETTING_SHUFFLE_CUTSCENE_ACTORS
                   || mainWindow.RANDSETTING_MISC_ENEMYAIDISTANCES
                   || mainWindow.RANDSETTING_MISC_HEIGHTFOG
                   || mainWindow.RANDSETTING_PAWN_FACEFX
                   || mainWindow.RANDSETTING_WACK_SCOTTISH
                   || mainWindow.RANDSETTING_PAWN_MATERIALCOLORS
                   || mainWindow.RANDSETTING_MISC_WALLTEXT
                   || mainWindow.RANDSETTING_PAWN_BIOLOOKATDEFINITION
            ;
        }

        public static void SetLocation(ExportEntry export, float x, float y, float z)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, x, y, z);
            export.WriteProperty(prop);
        }

        public static Point3D GetLocation(ExportEntry export)
        {
            float x = 0, y = 0, z = int.MinValue;
            var prop = export.GetProperty<StructProperty>("location");
            if (prop != null)
            {
                foreach (var locprop in prop.Properties)
                {
                    switch (locprop)
                    {
                        case FloatProperty fltProp when fltProp.Name == "X":
                            x = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Y":
                            y = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Z":
                            z = fltProp;
                            break;
                    }
                }

                return new Point3D(x, y, z);
            }

            return null;
        }

        public class Point3D
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public Point3D()
            {

            }

            public Point3D(double X, double Y, double Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }

            public double getDistanceToOtherPoint(Point3D other)
            {
                double deltaX = X - other.X;
                double deltaY = Y - other.Y;
                double deltaZ = Z - other.Z;

                return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            }

            public override string ToString()
            {
                return $"{X},{Y},{Z}";
            }
        }

        public static void SetLocation(StructProperty prop, float x, float y, float z)
        {
            prop.GetProp<FloatProperty>("X").Value = x;
            prop.GetProp<FloatProperty>("Y").Value = y;
            prop.GetProp<FloatProperty>("Z").Value = z;
        }

        private void RandomizeBioMorphFace(ExportEntry export, Random random, double amount = 0.3)
        {
            var props = export.GetProperties();
            ArrayProperty<StructProperty> m_aMorphFeatures = props.GetProp<ArrayProperty<StructProperty>>("m_aMorphFeatures");
            if (m_aMorphFeatures != null)
            {
                foreach (StructProperty morphFeature in m_aMorphFeatures)
                {
                    FloatProperty offset = morphFeature.GetProp<FloatProperty>("Offset");
                    if (offset != null)
                    {
                        //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.GetFullPath + " offset");
                        offset.Value = offset.Value * random.NextFloat(1 - (amount / 3), 1 + (amount / 3));
                    }
                }
            }

            ArrayProperty<StructProperty> m_aFinalSkeleton = props.GetProp<ArrayProperty<StructProperty>>("m_aFinalSkeleton");
            if (m_aFinalSkeleton != null)
            {
                foreach (StructProperty offsetBonePos in m_aFinalSkeleton)
                {
                    StructProperty vPos = offsetBonePos.GetProp<StructProperty>("vPos");
                    if (vPos != null)
                    {
                        //Debug.WriteLine("Randomizing morph face " + Path.GetFilePath(export.FileRef.FilePath) + " " + export.UIndex + " " + export.GetFullPath + " vPos");
                        FloatProperty x = vPos.GetProp<FloatProperty>("X");
                        FloatProperty y = vPos.GetProp<FloatProperty>("Y");
                        FloatProperty z = vPos.GetProp<FloatProperty>("Z");
                        x.Value = x.Value * random.NextFloat(1 - amount, 1 + amount);
                        y.Value = y.Value * random.NextFloat(1 - amount, 1 + amount);
                        z.Value = z.Value * random.NextFloat(1 - (amount / .85), 1 + (amount / .85));
                    }
                }
            }

            export.WriteProperties(props);
            if (mainWindow.RANDSETTING_PAWN_CLOWNMODE)
            {
                var materialoverride = props.GetProp<ObjectProperty>("m_oMaterialOverrides");
                if (materialoverride != null)
                {
                    var overrides = export.FileRef.getUExport(materialoverride.Value);
                    RandomizeMaterialOverride(overrides, random);
                }
            }
        }

        private void RandomizeMaterialOverride(ExportEntry export, Random random)
        {
            PropertyCollection props = export.GetProperties();
            var colorOverrides = props.GetProp<ArrayProperty<StructProperty>>("m_aColorOverrides");
            if (colorOverrides != null)
            {
                foreach (StructProperty colorParameter in colorOverrides)
                {
                    //Debug.WriteLine("Randomizing Color Parameter");
                    RandomizeTint(random, colorParameter.GetProp<StructProperty>("cValue"), false);
                }
            }
            var scalarOverrides = props.GetProp<ArrayProperty<StructProperty>>("m_aScalarOverrides");
            if (scalarOverrides != null)
            {
                foreach (StructProperty scalarParameter in scalarOverrides)
                {
                    var name = scalarParameter.GetProp<NameProperty>("nName");
                    if (name != null)
                    {
                        if (name.Value.Name.Contains("_Frek_") || name.Value.Name.StartsWith("HAIR") || name.Value.Name.StartsWith("HED_Scar"))
                        {

                            var currentValue = scalarParameter.GetProp<FloatProperty>("sValue");
                            if (currentValue != null)
                            {
                                Debug.WriteLine("Randomizing FREK HAIR HEDSCAR");

                                if (currentValue > 1)
                                {
                                    scalarParameter.GetProp<FloatProperty>("sValue").Value = random.NextFloat(0, currentValue * 1.3);
                                }
                                else
                                {
                                    scalarParameter.GetProp<FloatProperty>("sValue").Value = random.NextFloat(0, 1);
                                }
                            }
                        }

                    }
                }
            }

            export.WriteProperties(props);
        }

        private List<char> scottishVowelOrdering;
        private List<char> upperScottishVowelOrdering;

        static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-3, 20));
            return (float)(mantissa * exponent);
        }

        static string GetRandomColorRBGStr(Random random)
        {
            return $"RGB({random.Next(255)},{random.Next(255)},{random.Next(255)})";
        }

    }
}