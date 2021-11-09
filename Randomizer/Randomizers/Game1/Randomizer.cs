#if GAME1
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK.ME2ME3;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using Serilog;
using StringComparison = System.StringComparison;

namespace Randomizer.Randomizers.Game1
{
    public class Randomizer
    {

        private static string[] acceptableTagsForPawnShuffling =
        {
            "HMF_Asari_Captain_",
            "HMF_Asari_Comm_",
            "HMF_Asari_Communication_",
            "HMM_Joker_",
            "Hench_away01_",
            "Hench_away02_",
            "Hench_away_",
            "Joker_",
            "Saren_",
            "WAR30_02I_AsariCommando",
            "WAR30_02b_AsariCommando",
            "WAR30_02g_AsariCommando",
            "WAR30_20a_AsariCommandoF",
            "WAR40_06_Lizbeth",
            "WAR40_EthanJeong",
            "WAR40_Juliana",
            "WAR50_Lizbeth",
            "end95_asari_councilor",
            "end95_human_ambassador",
            "end95_salarian_councilor",
            "end95_turian_councilor",
            "hench_asari",
            "hench_humanMale",
            "hench_humanfemale",
            "hench_humanfemale_cockpit",
            "hench_humanmale",
            "hench_humanmale_cockpit",
            "hench_jenkins",
            "hench_pilot_cockpit",
            "hench_pilot_joker",
            "ice20_anoleisinplaza",
            "ice20_giannainplaza",
            "ice60_deadasari",
            "ice70_tartakovsky",
            "nor10_doctor_medical",
            "nor10_jenkins",
            "nor10_navigator",
            "nor_cutscene_crew2",
            "nor_hmm_crew1",
            "npcf_prop_Captain",
            "npcf_prop_Rescuer",
            "npch_END70B_Saren",
            "npch_JUG8007_Saren",
            "npch_JUG8013_Saren",
            "npch_Sal_Fanatic01",
            "npch_watcher",
            "npcn_Sal_Indoctrinate01",
            "npcn_Sal_Indoctrinate02",
            "npcn_Sal_Indoctrinate03",
            "npcn_Sal_Indoctrinate04",
            "player",
            "prc1_CapSoldier0",
            "prc1_batarian_soldier",
            "prc1_batarian_soldier02",
            "prc1_brother",
            "prc1_capSoldier1",
            "prc1_hmf_sci1",
            "prc1_hmm_sc1",
            "prc1_hmm_sci2",
            "prc1_kate",
            "prc1_leader",
            "prc1_lieutenantactor",
            "prc1_surveyor",
            "prop_END20_Trooper01",
            "prop_END20_Trooper02",
            "prop_end70C_SarenMonster",
            "prop_npcf_NorCrew01",
            "prop_npcf_NorCrew02",
            "prop_npcf_NorCrew03",
            "sta20_avina",
            "sta30_amb_dockworker",
            "sta60_doctor_michel",
            "sta60_fist_thug1",
            "sta60_fist_thug2",
            "sta60_fist_thug3",
            "sta60_garrus",
            "sta60_schells"
        };

        private const string UPDATE_RANDOMIZING_TEXT = "UPDATE_RANDOMIZING_TEXT";
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

        public void randomize()
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
            randomizationWorker.RunWorkerAsync(seed);
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

            foreach (var v in faceFxBoneNames)
            {
                Debug.WriteLine(v);
            }
        }

        private void RandomizeBioWaypointSet(IExportEntry export, Random random)
        {
            Log.Information("Randomizing BioWaypointSet " + export.UIndex + " in " + Path.GetFileName(export.FileRef.FileName));
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
                        IExportEntry currentPoint = export.FileRef.getUExport(nav.Value);
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

        private void RandomizeNoveria(Random random, List<TalkFile> Tlks)
        {
            mainWindow.CurrentOperationText = "Randoming Noveria";

            //Make turrets and ECRS guard hostile
            ME1Package introConfrontation = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\ICE\DSG\BIOA_ICE20_01a_DSG.SFM"));

            //Intro area
            var addToSquads = new[]
            {
                introConfrontation.getUExport(1776), introConfrontation.getUExport(1786), introConfrontation.getUExport(1786)
            };
            foreach (var pawnBehavior in addToSquads)
            {
                pawnBehavior.WriteProperty(new ObjectProperty(1958, "Squad"));
            }
            introConfrontation.save();
            ModifiedFiles[introConfrontation.FileName] = introConfrontation.FileName;
        }


        private void RandomizeFerosColonistBattle(Random random, List<TalkFile> Tlks)
        {
            mainWindow.CurrentOperationText = "Randoming Feros";
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("colonistnames.xml");
            XElement rootElement = XElement.Parse(fileContents);
            var colonistnames = rootElement.Elements("colonistname").Select(x => x.Value).ToList();

            ME1Package colonyBattlePackage = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\WAR\DSG\BIOA_WAR20_03c_DSG.SFM"));
            ME1Package skywayBattlePackage = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\WAR\DSG\BIOA_WAR40_11_DSG.SFM"));
            ME1Package towerBattlePackage = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\WAR\DSG\BIOA_WAR20_04b_DSG.SFM"));

            var battlePackages = new[] { colonyBattlePackage, skywayBattlePackage, towerBattlePackage };

            foreach (var battlePackage in battlePackages)
            {
                var bioChallengeScaledPawns = battlePackage.Exports.Where(x => x.ClassName == "BioPawnChallengeScaledType" && x.ObjectName != "MIN_ZombieThorian" && x.ObjectName != "ELT_GethAssaultDrone").ToList();

                foreach (var export in bioChallengeScaledPawns)
                {
                    var strRef = export.GetProperty<StringRefProperty>("ActorGameNameStrRef");
                    var newStrRef = Tlks[0].findDataByValue(colonistnames[0]).StringID;
                    if (newStrRef == 0)
                    {
                        newStrRef = Tlks[0].getFirstNullString();
                    }
                    Log.Information($"Assigning Feros Colonist name {export.UIndex} => {colonistnames[0]}");
                    strRef.Value = newStrRef;
                    Tlks.ForEach(x => x.replaceString(newStrRef, colonistnames[0]));
                    colonistnames.RemoveAt(0);
                    export.WriteProperty(strRef);
                }
            }

            //Make random amount of thorian zombies attack at the same time
            var maxZombs = skywayBattlePackage.getUExport(5748);
            maxZombs.WriteProperty(new IntProperty(random.Next(3, 11), "IntValue"));

            var getNewLoopDelay = skywayBattlePackage.getUExport(1103);
            getNewLoopDelay.WriteProperty(new FloatProperty(random.NextFloat(0.1, 2), "Duration"));

            var riseFromFeignFinishDelay = skywayBattlePackage.getUExport(1115);
            riseFromFeignFinishDelay.WriteProperty(new FloatProperty(random.NextFloat(0, .7), "Duration"));

            //Randomly disable squadmates from not targeting enemies in Zhu's Hope and Tower
            IExportEntry[] saveTheColonistPMCheckExports = new[] { colonyBattlePackage.getUExport(1434), colonyBattlePackage.getUExport(1437), colonyBattlePackage.getUExport(1440), towerBattlePackage.getUExport(576) };
            foreach (var saveColonist in saveTheColonistPMCheckExports)
            {
                if (random.Next(8) == 0)
                {
                    // 1 in 6 chance your squadmates don't listen to your command
                    var props = saveColonist.GetProperties();
                    props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links").Clear();
                    saveColonist.WriteProperties(props);
                }
            }

            foreach (var package in battlePackages)
            {
                if (package.ShouldSave)
                {
                    package.save();
                    ModifiedFiles[package.FileName] = package.FileName;
                }
            }
        }

        private void PerformRandomization(object sender, DoWorkEventArgs e)
        {
            ModifiedFiles = new ConcurrentDictionary<string, string>(); //this will act as a Set since there is no ConcurrentSet
            Random random = new Random((int)e.Argument);

            //Load TLKs
            mainWindow.CurrentOperationText = "Loading TLKs";
            mainWindow.ProgressBarIndeterminate = true;
            string globalTLKPath = Path.Combine(Utilities.GetGamePath(), "BioGame", "CookedPC", "Packages", "Dialog", "GlobalTlk.upk");
            ME1Package globalTLK = new ME1Package(globalTLKPath);
            List<TalkFile> Tlks = new List<TalkFile>();
            foreach (IExportEntry exp in globalTLK.Exports)
            {
                //TODO: Use BioTlkFileSet or something to only do INT
                if (exp.ClassName == "BioTlkFile")
                {
                    TalkFile tlk = new TalkFile(exp);
                    Tlks.Add(tlk);
                }
            }

            ////Test
            //ME1Package test = new ME1Package(@"D:\Origin Games\Mass Effect\BioGame\CookedPC\Maps\STA\DSG\BIOA_STA60_06_DSG.SFM");
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
                    tlk.replaceString(157152, gameOverText);
                }
            }

            //Randomize BIOC_BASE
            ME1Package bioc_base = new ME1Package(Path.Combine(Utilities.GetGamePath(), "BioGame", "CookedPC", "BIOC_Base.u"));
            bool bioc_base_changed = false;
            if (mainWindow.RANDSETTING_MOVEMENT_MAKO)
            {
                RandomizeMako(bioc_base, random);
                bioc_base_changed = true;
            }

            if (bioc_base_changed)
            {
                ModifiedFiles[bioc_base.FileName] = bioc_base.FileName;
                bioc_base.save();
            }




            //Randomize ENGINE
            ME1Package engine = new ME1Package(Utilities.GetEngineFile());
            IExportEntry talentEffectLevels = null;

            foreach (IExportEntry export in engine.Exports)
            {
                switch (export.ObjectName)
                {
                    case "Music_Music":
                        if (mainWindow.RANDSETTING_MISC_MUSIC)
                        {
                            RandomizeMusic(export, random);
                        }

                        break;
                    case "UISounds_GuiMusic":
                        if (mainWindow.RANDSETTING_MISC_GUIMUSIC)
                        {
                            RandomizeGUISounds(export, random, "Randomizing GUI Sounds - Music", "music");
                        }

                        break;
                    case "UISounds_GuiSounds":
                        if (mainWindow.RANDSETTING_MISC_GUISFX)
                        {
                            RandomizeGUISounds(export, random, "Randomizing GUI Sounds - Sounds", "snd_gui");
                        }

                        break;
                    case "MovementTables_CreatureSpeeds":
                        if (mainWindow.RANDSETTING_MOVEMENT_CREATURESPEED)
                        {
                            RandomizeMovementSpeeds(export, random);
                        }

                        break;
                    case "GalaxyMap_Cluster":
                        if (mainWindow.RANDSETTING_GALAXYMAP_CLUSTERS)
                        {
                            RandomizeClustersXY(export, random, Tlks);
                        }

                        break;
                    case "GalaxyMap_System":
                        if (mainWindow.RANDSETTING_GALAXYMAP_SYSTEMS)
                        {
                            RandomizeSystems(export, random);
                        }

                        break;
                    case "GalaxyMap_Planet":
                        //DumpPlanetTexts(export, Tlks[0]);
                        //return;

                        if (mainWindow.RANDSETTING_GALAXYMAP_PLANETCOLOR)
                        {
                            RandomizePlanets(export, random);
                        }

                        if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION)
                        {
                            RandomizePlanetNameDescriptions(export, random, Tlks);
                        }

                        break;
                    case "Characters_StartingEquipment":
                        if (mainWindow.RANDSETTING_WEAPONS_STARTINGEQUIPMENT)
                        {
                            RandomizeStartingWeapons(export, random);
                        }

                        break;
                    case "Classes_ClassTalents":
                        if (mainWindow.RANDSETTING_TALENTS_SHUFFLECLASSTALENTS)
                        {
                            ShuffleClassTalentsAndPowers(export, random);
                        }

                        break;
                    case "LevelUp_ChallengeScalingVars":
                        //RandomizeLevelUpChallenge(export, random);
                        break;
                    case "Items_ItemEffectLevels":
                        if (mainWindow.RANDSETTING_WEAPONS_EFFECTLEVELS || mainWindow.RANDSETTING_MOVEMENT_MAKO)
                        {
                            RandomizeWeaponStats(export, random);
                        }

                        break;
                    case "Characters_Character":
                        //Has internal checks for types
                        RandomizeCharacter(export, random);
                        break;
                    case "Talent_TalentEffectLevels":
                        if (mainWindow.RANDSETTING_TALENTS_STATS)
                        {
                            RandomizeTalentEffectLevels(export, Tlks, random);
                            talentEffectLevels = export;
                        }

                        break;
                }
            }

            if (talentEffectLevels != null && mainWindow.RANDSETTING_TALENTS_SHUFFLECLASSTALENTS)
            {

            }

            if (engine.ShouldSave)
            {
                engine.save();
                ModifiedFiles[engine.FileName] = engine.FileName;

            }

            //RANDOMIZE ENTRYMENU
            ME1Package entrymenu = new ME1Package(Utilities.GetEntryMenuFile());
            foreach (IExportEntry export in entrymenu.Exports)
            {
                switch (export.ObjectName)
                {
                    case "FemalePregeneratedHeads":
                    case "MalePregeneratedHeads":
                    case "BaseMaleSliders":
                    case "BaseFemaleSliders":
                        if (mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
                        {
                            RandomizePregeneratedHead(export, random);
                        }

                        break;
                    default:
                        if ((export.ClassName == "Bio2DA" || export.ClassName == "Bio2DANumberedRows") && !export.ObjectName.Contains("Default") && mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
                        {
                            RandomizeCharacterCreator2DA(random, export);
                        }

                        break;

                        //RandomizeGalaxyMap(random);
                        //RandomizeGUISounds(random);
                        //RandomizeMusic(random);
                        //RandomizeMovementSpeeds(random);
                        //RandomizeCharacterCreator2DA(random);
                        //Dump2DAToExcel();
                }

                if (mainWindow.RANDSETTING_CHARACTER_ICONICFACE && export.ClassName == "BioMorphFace" && export.ObjectName.StartsWith("Player_"))
                {
                    Log.Information("Randomizing iconic female shepard face by " + mainWindow.RANDSETTING_CHARACTER_ICONICFACE_AMOUNT);
                    RandomizeBioMorphFace(export, random, mainWindow.RANDSETTING_CHARACTER_ICONICFACE_AMOUNT);
                }
            }

            if (mainWindow.RANDSETTING_CHARACTER_CHARCREATOR)
            {
                RandomizeCharacterCreatorSingular(random, Tlks);
            }

            if (mainWindow.RANDSETTING_MISC_SPLASH)
            {
                RandomizeSplash(random, entrymenu);
            }


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
            }

            if (entrymenu.ShouldSave)
            {
                entrymenu.save();
                ModifiedFiles[entrymenu.FileName] = entrymenu.FileName;
            }


            //RANDOMIZE FACES
            if (mainWindow.RANDSETTING_CHARACTER_HENCHFACE)
            {
                RandomizeBioMorphFaceWrapper(Utilities.GetGameFile(@"BioGame\CookedPC\Packages\GameObjects\Characters\Faces\BIOG_Hench_FAC.upk"), random); //Henchmen
                RandomizeBioMorphFaceWrapper(Utilities.GetGameFile(@"BioGame\CookedPC\Packages\BIOG_MORPH_FACE.upk"), random); //Iconic and player (Not sure if this does anything...
            }

            //Map file randomizer
            if (RunMapRandomizerPass)
            {
                mainWindow.CurrentOperationText = "Getting list of files...";

                mainWindow.ProgressBarIndeterminate = true;
                string path = Path.Combine(Utilities.GetGamePath(), "BioGame", "CookedPC", "Maps");
                string bdtspath = Path.Combine(Utilities.GetGamePath(), "DLC", "DLC_UNC", "CookedPC", "Maps");
                string pspath = Path.Combine(Utilities.GetGamePath(), "DLC", "DLC_Vegas", "CookedPC", "Maps");

                var filesEnum = Directory.GetFiles(path, "*.sfm", SearchOption.AllDirectories);
                string[] files = null;
                if (!mainWindow.RANDSETTING_PAWN_FACEFX)
                {
                    files = filesEnum.Where(x => !Path.GetFileName(x).ToLower().Contains("_loc_")).ToArray();
                }
                else
                {
                    files = filesEnum.ToArray();
                }

                if (Directory.Exists(bdtspath))
                {
                    files = files.Concat(Directory.GetFiles(bdtspath, "*.sfm", SearchOption.AllDirectories)).ToArray();
                }

                if (Directory.Exists(pspath))
                {
                    files = files.Concat(Directory.GetFiles(pspath, "*.sfm", SearchOption.AllDirectories)).ToArray();
                }

                mainWindow.ProgressBarIndeterminate = false;
                mainWindow.ProgressBar_Bottom_Max = files.Count();
                mainWindow.ProgressBar_Bottom_Min = 0;
                double morphFaceRandomizationAmount = mainWindow.RANDSETTING_MISC_MAPFACES_AMOUNT;
                double faceFXRandomizationAmount = mainWindow.RANDSETTING_WACK_FACEFX_AMOUNT;
                string[] mapBaseNamesToNotRandomize = { "entrymenu", "biog_uiworld" };
                for (int i = 0; i < files.Length; i++)
                {
                    bool loggedFilename = false;
                    mainWindow.CurrentProgressValue = i;
                    mainWindow.CurrentOperationText = "Randomizing map files [" + i + "/" + files.Count() + "]";
                    var mapBaseName = Path.GetFileNameWithoutExtension(files[i]).ToLower();
                    //Debug.WriteLine(mapBaseName);
                    //if (mapBaseName != "bioa_nor10_03_dsg") continue;
                    if (!mapBaseNamesToNotRandomize.Any(x => x.StartsWith(mapBaseName)))
                    {
                        //if (!mapBaseName.StartsWith("bioa_sta")) continue;
                        bool hasLogged = false;
                        ME1Package package = new ME1Package(files[i]);
                        if (RunMapRandomizerPassAllExports)
                        {
                            foreach (IExportEntry exp in package.Exports)
                            {
                                if (mainWindow.RANDSETTING_PAWN_MAPFACES && exp.ClassName == "BioMorphFace")
                                {
                                    //Face randomizer
                                    if (!loggedFilename)
                                    {
                                        Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilename = true;
                                    }

                                    RandomizeBioMorphFace(exp, random, morphFaceRandomizationAmount);
                                    package.ShouldSave = true;
                                }
                                else if (mainWindow.RANDSETTING_MISC_HAZARDS && exp.ClassName == "SequenceReference")
                                {
                                    //Hazard Randomizer
                                    var seqRef = exp.GetProperty<ObjectProperty>("oSequenceReference");
                                    if (seqRef != null && exp.FileRef.isUExport(seqRef.Value))
                                    {
                                        IExportEntry possibleHazSequence = exp.FileRef.getUExport(seqRef.Value);
                                        var objName = possibleHazSequence.GetProperty<StrProperty>("ObjName");
                                        if (objName != null && objName == "REF_HazardSystem")
                                        {
                                            if (!loggedFilename)
                                            {
                                                Log.Information("Randomizing map file: " + files[i]);
                                                loggedFilename = true;
                                            }

                                            RandomizeHazard(exp, random);
                                            package.ShouldSave = true;
                                        }
                                    }
                                }
                                else if ((exp.ClassName == "BioSunFlareComponent" || exp.ClassName == "BioSunFlareStreakComponent" || exp.ClassName == "BioSunActor") && mainWindow.RANDSETTING_MISC_STARCOLORS)
                                {
                                    if (!loggedFilename)
                                    {
                                        Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilename = true;
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
                                else if (exp.ClassName == "SeqAct_Interp" && mainWindow.RANDSETTING_MISC_INTERPPAWNS)
                                {
                                    if (!loggedFilename)
                                    {
                                        //Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilename = true;
                                    }
                                    RandomizeInterpPawns(exp, random);
                                }
                                else if (exp.ClassName == "BioLookAtDefinition" && mainWindow.RANDSETTING_PAWN_BIOLOOKATDEFINITION)
                                {
                                    if (!loggedFilename)
                                    {
                                        //Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilename = true;
                                    }
                                    RandomizeBioLookAtDefinition(exp, random);
                                }
                                else if (exp.ClassName == "BioPawn")
                                {
                                    if (mainWindow.RANDSETTING_MISC_MAPPAWNSIZES && random.Next(4) == 0)
                                    {
                                        if (!loggedFilename)
                                        {
                                            Log.Information("Randomizing map file: " + files[i]);
                                            loggedFilename = true;
                                        }

                                        //Pawn size randomizer
                                        RandomizeBioPawnSize(exp, random, 0.4);
                                    }

                                    if (mainWindow.RANDSETTING_PAWN_MATERIALCOLORS)
                                    {
                                        if (!loggedFilename)
                                        {
                                            Log.Information("Randomizing map file: " + files[i]);
                                            loggedFilename = true;
                                        }

                                        RandomizePawnMaterialInstances(exp, random);
                                    }
                                }
                                else if (exp.ClassName == "HeightFogComponent" && mainWindow.RANDSETTING_MISC_HEIGHTFOG)
                                {
                                    if (!loggedFilename)
                                    {
                                        Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilename = true;
                                    }
                                    RandomizeHeightFogComponent(exp, random);
                                }
                                else if (mainWindow.RANDSETTING_MISC_INTERPS && exp.ClassName == "InterpTrackMove" /* && random.Next(4) == 0*/)
                                {
                                    if (!loggedFilename)
                                    {
                                        Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilename = true;
                                    }

                                    //Interpolation randomizer
                                    RandomizeInterpTrackMove(exp, random, morphFaceRandomizationAmount);
                                    package.ShouldSave = true;
                                }
                                else if (mainWindow.RANDSETTING_PAWN_FACEFX && exp.ClassName == "FaceFXAnimSet")
                                {
                                    if (!loggedFilename)
                                    {
                                        Log.Information("Randomizing map file: " + files[i]);
                                        loggedFilename = true;
                                    }

                                    //Method contains SHouldSave in it (due to try catch).
                                    RandomizeFaceFX(exp, random, (int)faceFXRandomizationAmount);
                                }
                            }
                        }

                        if (mainWindow.RANDSETTING_MISC_ENEMYAIDISTANCES)
                        {
                            RandomizeAINames(package, random);
                        }

                        if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION && package.LocalTalkFiles.Any())
                        {
                            if (!loggedFilename)
                            {
                                Log.Information("Randomizing map file: " + files[i]);
                                loggedFilename = true;
                            }
                            UpdateGalaxyMapReferencesForTLKs(package.LocalTalkFiles, false, false);
                        }

                        if (mainWindow.RANDSETTING_WACK_SCOTTISH && package.LocalTalkFiles.Any())
                        {
                            if (!loggedFilename)
                            {
                                Log.Information("Randomizing map file: " + files[i]);
                                loggedFilename = true;
                            }

                            MakeTextPossiblyScottish(package.LocalTalkFiles, random, false);
                        }
                        if (mainWindow.RANDSETTING_WACK_UWU && package.LocalTalkFiles.Any())
                        {
                            if (!loggedFilename)
                            {
                                Log.Information("Randomizing map file: " + files[i]);
                                loggedFilename = true;
                            }

                            UwuifyTalkFiles(package.LocalTalkFiles, random, false, mainWindow.RANDSETTING_WACK_UWU_KEEPCASING, mainWindow.RANDSETTING_WACK_UWU_EMOTICONS);
                        }

                        foreach (var talkFile in package.LocalTalkFiles.Where(x => x.Modified))
                        {
                            talkFile.saveToExport();
                        }

                        if (package.ShouldSave || package.TlksModified)
                        {
                            Debug.WriteLine("Saving package: " + package.FileName);
                            ModifiedFiles[package.FileName] = package.FileName;
                            package.save();
                        }
                    }
                }
            }

            if (mainWindow.RANDSETTING_WACK_OPENINGCUTSCENE)
            {
                Log.Information("Randomizing open cutscene");
                mainWindow.ProgressBarIndeterminate = true;
                mainWindow.CurrentOperationText = "Randomizing opening cutscene";
                RandomizeOpeningCrawl(random, Tlks);
                //RandomizeOpeningSequence(random); //this was just sun tint. Part of sun tint randomizer 
                //Log.Information("Applying fly-into-earth interp modification");
                ME1Package p = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\NOR\LAY\BIOA_NOR10_13_LAY.SFM"));
                //p.getUExport(220).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.InterpMoveTrack_EarthCardIntro_220.bin");
                Log.Information("Randomizing earth texture");

                var earthItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.earthbackdrops")).ToList();
                earthItems.Shuffle(random);
                var newAsset = earthItems[0];
                var earthTexture = p.getUExport(508);
                earthTexture.setBinaryData(Utilities.GetEmbeddedStaticFilesBinaryFile(newAsset, true));
                var props = earthTexture.GetProperties();
                props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(newAsset), "SourceFilePath"));
                props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
                earthTexture.WriteProperties(props);
                earthTexture.idxObjectName = p.FindNameOrAdd("earthMER"); //ensure unique name

                //DEBUG ONLY: NO INTRO MUSIC

#if DEBUG
                var director = p.getUExport(206);
                var tracks = director.GetProperty<ArrayProperty<ObjectProperty>>("InterpTracks");
                var o = tracks.FirstOrDefault(x => x.Value == 227); //ME Music
                tracks.Remove(o);
                director.WriteProperty(tracks);
#endif

                Log.Information("Applying shepard-faces-camera modification");
                p.getUExport(219).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.InterpMoveTrack_PlayerFaceCameraIntro_219.bin");
                p.save();
            }

            if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION)
            {
                Log.Information("Apply galaxy map background transparency fix");
                ME1Package p = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\NOR\DSG\BIOA_NOR10_03_DSG.SFM"));
                p.getUExport(1655).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.PC_GalaxyMap_BGFix_1655.bin");
                p.save();
                ModifiedFiles[p.FileName] = p.FileName;
            }

            if (mainWindow.RANDSETTING_WACK_SCOTTISH)
            {
                MakeTextPossiblyScottish(Tlks, random, true);
            }
            else if (mainWindow.RANDSETTING_WACK_UWU)
            {
                UwuifyTalkFiles(Tlks, random, true, mainWindow.RANDSETTING_WACK_UWU_KEEPCASING, mainWindow.RANDSETTING_WACK_UWU_EMOTICONS);
            }


            bool saveGlobalTLK = false;
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
                    ModifiedFiles[tf.export.FileRef.FileName] = tf.export.FileRef.FileName;
                    tf.saveToExport();
                }

                saveGlobalTLK = true;
            }

            if (saveGlobalTLK)
            {
                globalTLK.save();
            }
            mainWindow.CurrentOperationText = "Finishing up";
            AddMERSplash(random);
        }

        private void UwuifyTalkFiles(List<TalkFile> talkfiles, Random random, bool updateProgressbar, bool keepCasing, bool addReactions)
        {
            Log.Information("UwUifying text");

            int currentTlkIndex = 0;
            foreach (TalkFile tf in talkfiles)
            {
                currentTlkIndex++;
                int max = tf.StringRefs.Count();
                int current = 0;
                if (updateProgressbar)
                {
                    mainWindow.CurrentOperationText = $"UwUifying text [{currentTlkIndex}/{talkfiles.Count}]";
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

                            if (word.StartsWith("%") || word.StartsWith("<CUSTOM"))
                            {
                                Debug.WriteLine($"Skipping {word}");
                                continue; // Don't modify tokens
                            }

                            // PUT CODE HERE
                            words[j] = uwuifyString(word, random, keepCasing, addReactions);
                        }

                        string rebuiltStr = string.Join(" ", words);
                        if (addReactions)
                        {
                            rebuiltStr = AddReactionToLine(originalString, rebuiltStr, keepCasing, random);
                        }

                        tf.replaceString(sref.StringID, rebuiltStr);
                    }
                }
            }
        }
        private static void FindSkipRanges(ME1TalkFile.TLKStringRef sref, List<int> skipRanges)
        {
            var str = sref.Data;
            int startPos = -1;
            char openingChar = (char)0x00;
            for (int i = 0; i < sref.Data.Length; i++)
            {
                if (startPos < 0 && (sref.Data[i] == '[' || sref.Data[i] == '<' || sref.Data[i] == '{'))
                {
                    startPos = i;
                    openingChar = sref.Data[i];
                }
                else if (startPos >= 0 && openingChar == '[' && sref.Data[i] == ']') // ui control token
                {
                    var insideStr = sref.Data.Substring(startPos + 1, i - startPos - 1);
                    if (insideStr.StartsWith("Xbox", StringComparison.InvariantCultureIgnoreCase))
                    {
                        skipRanges.Add(startPos);
                        skipRanges.Add(i + 1);
                    }
                    //else
                    //    Debug.WriteLine(insideStr);

                    startPos = -1;
                    openingChar = (char)0x00;

                }
                else if (startPos >= 0 && openingChar == '<' && sref.Data[i] == '>') //cust token
                {
                    var insideStr = sref.Data.Substring(startPos + 1, i - startPos - 1);
                    if (insideStr.StartsWith("CUSTOM") || insideStr.StartsWith("font") || insideStr.StartsWith("/font") || insideStr.Equals("br", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // custom token. Do not modify it
                        skipRanges.Add(startPos);
                        skipRanges.Add(i + 1);
                    }
                    //else
                    //Debug.WriteLine(insideStr);

                    startPos = -1;
                    openingChar = (char)0x00;
                }
                else if (startPos >= 0 && openingChar == '{' && sref.Data[i] == '}') // token for powers (?)
                {
                    //var insideStr = sref.Data.Substring(startPos + 1, i - startPos - 1);
                    //Debug.WriteLine(insideStr);
                    // { } brackets are for ui tokens in powers, saves, I think.
                    skipRanges.Add(startPos);
                    skipRanges.Add(i + 1);

                    startPos = -1;
                    openingChar = (char)0x00;
                }

                // it's nothing.
            }
        }

        private string uwuifyString(string strData, Random random, bool keepCasing, bool addReactions)
        {
            StringBuilder sb = new StringBuilder();
            char previousChar = (char)0x00;
            char currentChar;
            for (int i = 0; i < strData.Length; i++)
            {
                //if (skipRanges.Any() && skipRanges[0] == i)
                //{
                //    sb.Append(strData.Substring(skipRanges[0], skipRanges[1] - skipRanges[0]));
                //    previousChar = (char)0x00;
                //    i = skipRanges[1] - 1; // We subtract one as the next iteration of the loop will +1 it again, which then will make it read the 'next' character
                //    skipRanges.RemoveAt(0); // remove first 2
                //    skipRanges.RemoveAt(0); // remove first 2

                //    if (i >= strData.Length - 1)
                //        break;
                //    continue;
                //}

                currentChar = strData[i];
                if (currentChar == 'L' || currentChar == 'R')
                {
                    sb.Append(keepCasing ? 'W' : 'w');
                }
                else if (currentChar == 'l' || currentChar == 'r')
                {
                    sb.Append('w');
                    if (random.Next(5) == 0)
                    {
                        sb.Append('w'); // append another w 20% of the time
                        if (random.Next(8) == 0)
                        {
                            sb.Append('w'); // append another w 20% of the time
                        }
                    }
                }
                else if (currentChar == 'N' && (previousChar == 0x00 || previousChar == ' '))
                {
                    sb.Append(keepCasing ? "Nyaa" : "nyaa");
                }
                else if (currentChar == 'O' || currentChar == 'o')
                {
                    if (previousChar == 'N' || previousChar == 'n' ||
                        previousChar == 'M' || previousChar == 'm')
                    {
                        sb.Append("yo");
                    }
                    else
                    {
                        sb.Append(keepCasing ? strData[i] : char.ToLower(strData[i]));
                    }
                }
                else if (currentChar == '!' && !addReactions)
                {
                    sb.Append(currentChar);
                    if (random.Next(2) == 0)
                    {
                        sb.Append(currentChar); // append another ! 50% of the time
                    }
                }
                else
                {
                    sb.Append(keepCasing ? strData[i] : char.ToLower(strData[i]));
                }

                previousChar = currentChar;
            }

            var str = sb.ToString();

            if (!addReactions)
            {
                // Does ME1 drop any f-bombs?
                str = str.Replace("fuck", keepCasing ? "UwU" : "uwu");
                str = str.Replace("Fuck", keepCasing ? "UwU" : "uwu");
            }

            return str;
        }

        private static char[] uwuPunctuationDuplicateChars = { '?', '!' };


        #region UwU Reactions

        [XmlType("reaction")]
        public class Reaction
        {
            [XmlAttribute("name")]
            public string name;

            [XmlElement("property")]
            public List<string> properties;

            [XmlElement("face")]
            public List<string> faces;

            [XmlElement("keyword")]
            public List<string> keywords;

            public int keywordScore = 0;

            public void EarnPoint()
            {
                keywordScore += (properties.Contains("doublescore") ? 2 : 1);
            }

            public string GetFace(Random random)
            {
                return faces[random.Next(faces.Count)];
            }
        }

        private static List<Reaction> ReactionList;
        private static Regex regexEndOfSentence;
        private static Regex regexAllLetters;
        private static Regex regexPunctuationRemover;
        private static Regex regexBorkedElipsesFixer;

        private static string AddReactionToLine(string vanillaLine, string modifiedLine, bool keepCasing, Random random)
        {
            string finalString = "";
            bool dangerousLine = false;

            //initialize reactions/regex if this is first run
            if (ReactionList == null)
            {
                string rawReactionDefinitions = Utilities.GetEmbeddedStaticFilesTextFile("reactiondefinitions.xml");
                var reactionXml = new StringReader(rawReactionDefinitions);
                XmlSerializer serializer = new XmlSerializer(typeof(List<Reaction>), new XmlRootAttribute("ReactionDefinitions"));
                ReactionList = (List<Reaction>)serializer.Deserialize(reactionXml);

                regexEndOfSentence = new Regex(@"(?<![M| |n][M|D|r][s|r]\.)(?<!(,""))(?<=[.!?""])(?= [A-Z])", RegexOptions.Compiled);
                regexAllLetters = new Regex("[a-zA-Z]", RegexOptions.Compiled);
                regexPunctuationRemover = new Regex("(?<![D|M|r][w|r|s])[.!?](?!.)", RegexOptions.Compiled);
                regexBorkedElipsesFixer = new Regex("(?<!\\.)\\.\\.(?=\\s|$)", RegexOptions.Compiled);
            }

            if (modifiedLine.Length < 2 || regexAllLetters.Matches(modifiedLine).Count == 0 || vanillaLine.Contains('{'))
            {
                //I should go.
                return modifiedLine;
            }

            char[] dangerousCharacters = { '<', '\n' };
            if (modifiedLine.IndexOfAny(dangerousCharacters) >= 0 || vanillaLine.Length > 200)
            {
                dangerousLine = true;
            }

            //split strings into sentences for processing
            List<string> splitVanilla = new List<string>();
            List<string> splitModified = new List<string>();

            MatchCollection regexMatches = regexEndOfSentence.Matches(vanillaLine);
            int modOffset = 0;

            //for each regex match in the vanilla line:
            for (int matchIndex = 0; matchIndex <= regexMatches.Count; matchIndex++)
            {
                int start;
                int stop;

                //vanilla sentence splitting
                //find indexes for start and stop from surrounding regexMatches
                if (regexMatches.Count == 0)
                {
                    start = 0;
                    stop = vanillaLine.Length;
                }
                else if (matchIndex == 0)
                {
                    start = 0;
                    stop = regexMatches[matchIndex].Index;
                }
                else if (matchIndex == regexMatches.Count)
                {
                    start = regexMatches[matchIndex - 1].Index;
                    stop = vanillaLine.Length;
                }
                else
                {
                    start = regexMatches[matchIndex - 1].Index;
                    stop = regexMatches[matchIndex].Index;
                }

                splitVanilla.Add(vanillaLine.Substring(start, stop - start));

                //modified sentence splitting
                int modStart = start + modOffset;
                int modStop = stop + modOffset;

                //step through sentence looking for punctuation or EOL
                while (!((".!?\"").Contains(modifiedLine[modStop - 1])) && ((modStop - 1) < (modifiedLine.Length - 1)))
                {
                    modOffset++;
                    modStop++;
                }

                //step through sentence looking for next space character or EOL
                while (!(modifiedLine[modStop - 1].Equals(' ')) && ((modStop - 1) < (modifiedLine.Length - 1)))
                {
                    modOffset++;
                    modStop++;
                }

                //if we found a space, step back
                if (modStop < modifiedLine.Length)
                {
                    modOffset--;
                    modStop--;
                }

                splitModified.Add(modifiedLine.Substring(modStart, modStop - modStart));
            }

            //reaction handling loop
            for (int i = 0; i < splitVanilla.Count; i++)
            {
                string sv = splitVanilla[i];
                string sm = splitModified[i];

                //calculate scores
                foreach (Reaction r in ReactionList)
                {
                    r.keywordScore = 0;

                    string s = (r.properties.Contains("comparetomodified") ? sm : sv);
                    foreach (string keyword in r.keywords)
                    {
                        //if the keyword contains a capital letter, it's case sensitive
                        if (s.Contains(keyword, (keyword.Any(char.IsUpper) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)))
                        {
                            r.EarnPoint();
                        }
                    }

                    //question check, done here to make it a semi-random point
                    if (s.Contains('?') && r.properties.Contains("question") && random.Next(10) == 0)
                    {
                        r.EarnPoint();
                    }

                    //exclamation check
                    if (s.Contains('!') && r.properties.Contains("exclamation") && random.Next(10) == 0)
                    {
                        r.EarnPoint();
                    }
                }

                //determine winner
                ReactionList.Shuffle(); //in case of a tie
                Reaction winningReaction = new Reaction();
                foreach (Reaction r in ReactionList)
                {
                    if (r.properties.Contains("nolower") && !keepCasing)
                    {
                        //reaction is not lowercase safe and lowercase is enabled
                        continue;
                    }

                    if (r.properties.Contains("dangerous") && dangerousLine)
                    {
                        //reaction is dangerous and this is a dangerous string
                        continue;
                    }

                    if (!r.properties.Contains("lowpriority"))
                    {
                        if ((r.keywordScore >= winningReaction.keywordScore))
                        {
                            winningReaction = r;
                        }
                    }
                    else
                    {
                        if ((r.keywordScore > winningReaction.keywordScore))
                        {
                            winningReaction = r;
                        }
                    }
                }

                //winner processing if one exists
                if (winningReaction.keywordScore > 0)
                {
                    //we have a winner!! congwatuwations ^_^
                    if (!winningReaction.properties.Contains("easteregg"))
                    {
                        //standard winner processing. remove punctuation, apply face to line
                        sm = regexPunctuationRemover.Replace(sm, "");
                        sm += " " + winningReaction.GetFace(random);

                        if (!keepCasing)
                        {
                            sm = sm.ToLower();
                        }
                    }
                    else
                    {
                        //easter egg processing
                        switch (winningReaction.name)
                        {
                            case "reee":
                                //SAREN REEEEE
                                //technically should be WEEEEEE. Still funny, but WEEEEE isn't the meme.
                                string reee = "Sawen REEEEE";

                                for (int e = 0; e < random.Next(8); e++)
                                {
                                    reee += 'E';
                                }

                                if (!keepCasing)
                                {
                                    reee = reee.ToLower();
                                }

                                sm = Regex.Replace(sm, "(s|S)aw*en", reee);
                                break;

                            case "finger":
                                //Sovereign t('.'t)
                                string finger = "Soveweign t('.'t)";

                                if (!keepCasing)
                                {
                                    finger = finger.ToLower();
                                }

                                sm = Regex.Replace(sm, "(S|s)ovew*eign", finger);
                                break;

                            case "jason":
                                //Jacob? Who?
                                string jason = winningReaction.GetFace(random);

                                if (!keepCasing)
                                {
                                    jason = jason.ToLower();
                                }

                                sm = Regex.Replace(sm, "(J|j)acob", jason);
                                break;

                            case "shitpost":
                                //We ArE hArBiNgEr
                                bool flipflop = true;
                                string sHiTpOsT = "";
                                foreach (char c in sm.ToCharArray())
                                {
                                    if (flipflop)
                                    {
                                        sHiTpOsT += char.ToUpper(c);
                                    }
                                    else
                                    {
                                        sHiTpOsT += char.ToLower(c);
                                    }
                                    flipflop = !flipflop;
                                }
                                sm = sHiTpOsT;
                                break;

                            case "kitty":
                                //nyaaaa =^.^=
                                string nyaSentence = "";
                                string[] words = sm.Split(' ');
                                for (int w = 0; w < words.Length; w++) //each word in sentence
                                {
                                    foreach (string k in winningReaction.keywords) //each keyword in reaction
                                    {
                                        //semi-random otherwise it's EVERYWHERE
                                        if (words[w].Contains(k, StringComparison.OrdinalIgnoreCase) && random.Next(5) == 0)
                                        {
                                            words[w] = Regex.Replace(words[w], "[.!?]", "");
                                            words[w] += " " + winningReaction.GetFace(random);
                                        }
                                    }
                                    nyaSentence += words[w] + " ";
                                }
                                nyaSentence = nyaSentence.Remove(nyaSentence.Length - 1, 1); //string will always have a trailing space
                                sm = nyaSentence;
                                break;

                            default:
                                Debug.WriteLine("Easter egg reaction happened, but it was left unhandled!");
                                break;
                        }
                    }
                }
                finalString += sm;
            }

            //borked elipses removal
            finalString = regexBorkedElipsesFixer.Replace(finalString, "");

            //do punctuation duplication thing because it's funny
            foreach (char c in uwuPunctuationDuplicateChars)
            {
                if (finalString.Contains(c))
                {
                    int rnd = random.Next(4);
                    switch (rnd)
                    {
                        case 0:
                            finalString = finalString.Replace(c.ToString(), String.Concat(c, c));
                            break;
                        case 1:
                            finalString = finalString.Replace(c.ToString(), String.Concat(c, c, c));
                            break;
                        default:
                            break;
                    }
                }
            }

            //Debug.WriteLine("----------\nreaction input:  " + vanillaLine);
            //Debug.WriteLine("reaction output: " + finalString);
            return finalString;
        }

        #endregion

        private void RandomizeCitadel(Random random)
        {
            Log.Information("Randomizing BioWaypointSets for Citadel");
            mainWindow.CurrentOperationText = "Randomizing Citadel";

            int numDone = 0;
            var staDsg = Utilities.GetGameFile(@"BioGame\CookedPC\Maps\STA\DSG");
            var filesToProcess = Directory.GetFiles(staDsg, "*.SFM");

            mainWindow.CurrentProgressValue = 0;
            mainWindow.ProgressBar_Bottom_Max = filesToProcess.Length;
            mainWindow.ProgressBarIndeterminate = false;

            foreach (var packageFile in filesToProcess)
            {
                ME1Package p = new ME1Package(packageFile);
                var waypoints = p.Exports.Where(x => x.ClassName == "BioWaypointSet").ToList();
                foreach (var waypoint in waypoints)
                {
                    RandomizeBioWaypointSet(waypoint, random);
                }
                if (p.ShouldSave)
                {
                    p.save();
                    ModifiedFiles[p.FileName] = p.FileName;
                }
                mainWindow.CurrentProgressValue++;
            }

            mainWindow.CurrentProgressValue = 0;
            mainWindow.ProgressBar_Bottom_Max = filesToProcess.Length;
            mainWindow.ProgressBarIndeterminate = true;

            //Randomize Citadel Tower sky
            ME1Package package = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\STA\LAY\BIOA_STA70_02_LAY.SFM"));
            var skyMaterial = package.getUExport(347);
            var data = skyMaterial.Data;
            data.OverwriteRange(0x168, BitConverter.GetBytes(random.NextFloat(-1.5, 1.5)));
            data.OverwriteRange(0x19A, BitConverter.GetBytes(random.NextFloat(-1.5, 1.5)));
            skyMaterial.Data = data;

            var volumeLighting = package.getUExport(859);
            var props = volumeLighting.GetProperties();

            var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
            if (vectors != null)
            {
                foreach (var vector in vectors)
                {
                    RandomizeTint(random, vector.GetProp<StructProperty>("ParameterValue"), false);
                }
            }

            volumeLighting.WriteProperties(props);

            if (package.ShouldSave)
            {
                package.save();
                ModifiedFiles[package.FileName] = package.FileName;
            }

            //Randomize Scan the Keepers
            Log.Information("Randomizing Scan the Keepers");
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("stakeepers.xml");
            XElement rootElement = XElement.Parse(fileContents);
            var keeperDefinitions = (from e in rootElement.Elements("keeper")
                                     select new KeeperDefinition
                                     {
                                         STAFile = (string)e.Attribute("file"),
                                         KismetTeleportBoolUIndex = (int)e.Attribute("teleportflagexport"),
                                         PawnExportUIndex = (int)e.Attribute("export"),
                                     }).ToList();


            var keeperRandomizationInfo = (from e in rootElement.Elements("keeperlocation")
                                           select new KeeperLocation
                                           {
                                               STAFile = (string)e.Attribute("file"),
                                               Position = new Vector3
                                               {
                                                   X = (float)e.Attribute("positionx"),
                                                   Y = (float)e.Attribute("positiony"),
                                                   Z = (float)e.Attribute("positionz")
                                               },
                                               Yaw = string.IsNullOrEmpty((string)e.Attribute("yaw")) ? 0 : (int)e.Attribute("yaw")
                                           }).ToList();
            keeperRandomizationInfo.Shuffle(random);
            string STABase = Utilities.GetGameFile(@"BIOGame\CookedPC\Maps\STA\DSG");
            var uniqueFiles = keeperDefinitions.Select(x => x.STAFile).Distinct();

            foreach (string staFile in uniqueFiles)
            {
                Log.Information("Randomizing Keepers in " + staFile);
                string filepath = Path.Combine(STABase, staFile);
                ME1Package staPackage = new ME1Package(filepath);
                var keepersToRandomize = keeperDefinitions.Where(x => x.STAFile == staFile).ToList();
                var keeperRandomizationInfoForThisLevel = keeperRandomizationInfo.Where(x => x.STAFile == staFile).ToList();
                foreach (var keeper in keepersToRandomize)
                {
                    //Set location
                    var newRandomizationInfo = keeperRandomizationInfoForThisLevel[0];
                    keeperRandomizationInfoForThisLevel.RemoveAt(0);
                    IExportEntry bioPawn = staPackage.getUExport(keeper.PawnExportUIndex);
                    Utilities.SetLocation(bioPawn, newRandomizationInfo.Position);
                    if (newRandomizationInfo.Yaw != 0)
                    {
                        Utilities.SetRotation(bioPawn, newRandomizationInfo.Yaw);
                    }

                    // Unset the "Teleport to ActionStation" bool
                    if (keeper.KismetTeleportBoolUIndex != 0)
                    {
                        //Has teleport bool
                        IExportEntry teleportBool = staPackage.getUExport(keeper.KismetTeleportBoolUIndex);
                        teleportBool.WriteProperty(new IntProperty(0, "bValue")); //teleport false
                    }
                }


                if (staPackage.ShouldSave)
                {
                    staPackage.save();
                    ModifiedFiles[staPackage.FileName] = staPackage.FileName;
                }
            }
        }

        private void RandomizeEdenPrime(Random random)
        {
            Log.Information("Randomizing Eden Prime");
            mainWindow.CurrentOperationText = "Randomizing Eden Prime";
            mainWindow.ProgressBarIndeterminate = true;

            ME1Package p = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\PRO\DSG\BIOA_PRO10_08_DSG.SFM"));
            Log.Information("Applying sovereign drawscale pre-randomization modifications");
            p.getUExport(5640).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.SovereignInterpTrackFloatDrawScale_5640_PRO08DSG.bin");
            p.getUExport(5643).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.SovereignInterpTrackMove_5643_PRO08DSG.bin");

            IExportEntry drawScaleExport = p.getUExport(5640);
            var floatTrack = drawScaleExport.GetProperty<StructProperty>("FloatTrack");
            {
                var points = floatTrack?.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var s = points[i];
                        var outVal = s.GetProp<FloatProperty>("OutVal");
                        if (outVal != null)
                        {
                            outVal.Value = random.NextFloat(-15, 95);
                        }
                    }
                }
            }

            drawScaleExport.WriteProperty(floatTrack);

            IExportEntry movementExport = p.getUExport(5643);
            var props = movementExport.GetProperties();
            var posTrack = props.GetProp<StructProperty>("PosTrack");
            if (posTrack != null)
            {
                var points = posTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                if (points != null)
                {
                    for (int i = 1; i < 5; i++)
                    {
                        var s = points[i];
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            x.Value += random.NextFloat(-3000, 3000);
                            y.Value += random.NextFloat(-3000, 3000);
                            z.Value = random.NextFloat(-106400, 392000);
                        }
                    }
                }
            }

            movementExport.WriteProperties(props);
            p.save();
            ModifiedFiles[p.FileName] = p.FileName;
        }

        private void RandomizeBioLookAtDefinition(IExportEntry export, Random random)
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

        private void RandomizeEnding(Random random)
        {
            Log.Information("Randomizing ending");
            mainWindow.ProgressBarIndeterminate = true;

            mainWindow.CurrentOperationText = "Randomizing ending";
            ME1Package backdropFile = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\CRD\BIOA_CRD00.SFM"));
            var paragonItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.endingbackdrops.paragon")).ToList();
            var renegadeItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.endingbackdrops.renegade")).ToList();
            paragonItems.Shuffle(random);
            renegadeItems.Shuffle(random);
            var paragonTexture = backdropFile.getUExport(1067);
            var renegadeConversationTexture = backdropFile.getUExport(1068); //For backdrop of anderson/udina conversation
            var renegadeTexture = backdropFile.getUExport(1069);

            var paragonItem = paragonItems[0];
            var renegadeItem = renegadeItems[0];

            paragonTexture.setBinaryData(Utilities.GetEmbeddedStaticFilesBinaryFile(paragonItem, true));
            renegadeTexture.setBinaryData(Utilities.GetEmbeddedStaticFilesBinaryFile(renegadeItem, true));

            Log.Information("Backdrop randomizer, setting paragon backdrop to " + Path.GetFileName(paragonItem));
            Log.Information("Backdrop randomizer, setting renegade backdrop to " + Path.GetFileName(renegadeItem));

            var props = paragonTexture.GetProperties();
            props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(paragonItem), "SourceFilePath"));
            props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
            paragonTexture.WriteProperties(props);

            props = renegadeTexture.GetProperties();
            props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(renegadeItem), "SourceFilePath"));
            props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
            renegadeTexture.WriteProperties(props);

            int texturePackageNameIndex = backdropFile.findName("BIOA_END20_T");
            if (texturePackageNameIndex != -1)
            {
                backdropFile.replaceName(texturePackageNameIndex, "BIOA_END20_MER_T");
            }

            backdropFile.save();
            ModifiedFiles[backdropFile.FileName] = backdropFile.FileName;


            ME1Package finalCutsceneFile = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\CRD\DSG\BIOA_CRD00_00_DSG.SFM"));
            var weaponTypes = (new[] { "STW_AssaultRifle", "STW_Pistol", "STW_SniperRifle", "STW_ShotGun" }).ToList();
            weaponTypes.Shuffle(random);
            Log.Information("Ending randomizer, setting renegade weapon to " + weaponTypes[0]);
            var setWeapon = finalCutsceneFile.getUExport(979);
            props = setWeapon.GetProperties();
            var eWeapon = props.GetProp<EnumProperty>("eWeapon");
            eWeapon.Value = weaponTypes[0];
            setWeapon.WriteProperties(props);

            //Move Executor to Renegade
            var executor = finalCutsceneFile.getUExport(923);
            Utilities.SetLocation(executor, -25365, 20947, 430);
            Utilities.SetRotation(executor, -30);

            //Move SubShep to Paragon
            var subShep = finalCutsceneFile.getUExport(922);
            Utilities.SetLocation(subShep, -21271, 8367, -2347);
            Utilities.SetRotation(subShep, -15);

            //Ensure the disable light environment has references to our new pawns so they are property lit.
            var toggleLightEnviro = finalCutsceneFile.getUExport(985);
            props = toggleLightEnviro.GetProperties();
            var linkedVars = props.GetProp<ArrayProperty<StructProperty>>("VariableLinks")[0].GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables"); //Shared True
            if (linkedVars.Count == 1)
            {
                linkedVars.Add(new ObjectProperty(2621)); //executor
                linkedVars.Add(new ObjectProperty(2620)); //subshep
            }


            toggleLightEnviro.WriteProperties(props);

            finalCutsceneFile.save();
            ModifiedFiles[finalCutsceneFile.FileName] = finalCutsceneFile.FileName;
        }

        private void RandomizeHeightFogComponent(IExportEntry exp, Random random)
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
                    var twentyPercent = random.NextFloat(-density * .05, density * 0.75);
                    density.Value = density + twentyPercent;
                }
                exp.WriteProperties(properties);
            }
        }

        private void RandomizePinnacleScoreboard(Random random)
        {
            Log.Information("Randomizing Pinnacle Station scoreboard");
            mainWindow.ProgressBarIndeterminate = true;

            mainWindow.CurrentOperationText = "Randomizing Pinnacle Station Scoreboard";
            ME1Package pinnacleTextures = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_Vegas\CookedPC\Maps\PRC2\bioa_prc2_ccsim05_dsg_LOC_int.SFM"));
            var resourceItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.pinnaclestationscoreboard")).ToList();
            resourceItems.Shuffle(random);

            for (int i = 104; i < 118; i++)
            {
                var newBinaryResource = resourceItems[0];
                resourceItems.RemoveAt(0);
                var textureExport = pinnacleTextures.getUExport(i);
                var props = textureExport.GetProperties();
                props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(newBinaryResource), "SourceFilePath"));
                props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
                textureExport.WriteProperties(props);
                var bytes = Utilities.GetEmbeddedStaticFilesBinaryFile(newBinaryResource, true);
                textureExport.setBinaryData(bytes);
            }
            pinnacleTextures.save();
            ModifiedFiles[pinnacleTextures.FileName] = pinnacleTextures.FileName;
        }

        private void RandomizePawnMaterialInstances(IExportEntry exp, Random random)
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
                        IExportEntry material = exp.FileRef.getUExport(materialObj.Value);
                        var props = material.GetProperties();

                        {
                            var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                            var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
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
                                        Debug.WriteLine("Randomizing parameter " + scalar.GetProp<NameProperty>("ParameterName"));
                                        scalar.GetProp<FloatProperty>("ParameterValue").Value = random.NextFloat(0, 1);
                                    }
                                }

                                foreach (var vector in vectors)
                                {
                                    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                                    RandomizeTint(random, paramValue, false);
                                }
                            }
                        }
                        material.WriteProperties(props);
                    }
                }
            }
        }

        private void RandomizeBDTS(Random random)
        {
            //Randomize planet in the sky
            ME1Package bdtsPlanetFile = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Maps\UNC52\LAY\BIOA_UNC52_00_LAY.SFM"));
            IExportEntry planetMaterial = bdtsPlanetFile.getUExport(1546); //BIOA_DLC_UNC52_T.GXM_EarthDup
            RandomizePlanetMaterialInstanceConstant(planetMaterial, random, realistic: true);
            bdtsPlanetFile.save();
            ModifiedFiles[bdtsPlanetFile.FileName] = bdtsPlanetFile.FileName;

            //Randomize the Bio2DA talent table for the turrets
            ME1Package bdtsTalents = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\2DAs\BIOG_2DA_UNC_Talents_X.upk"));
            Bio2DA talentEffectLevels = new Bio2DA(bdtsTalents.getUExport(2));

            for (int i = 0; i < talentEffectLevels.RowCount; i++)
            {
                string rowEffect = talentEffectLevels[i, "GameEffect_Label"].DisplayableValue;
                if (rowEffect.EndsWith("Cooldown") || rowEffect.EndsWith("CastingTime"))
                {
                    float newValue = random.NextFloat(0, 1);
                    if (random.Next(2) == 0) newValue = 0.01f;
                    for (int j = 1; j < 12; j++)
                    {
                        talentEffectLevels[i, "Level" + j].DisplayableValue = newValue.ToString();
                    }
                }
                else if (rowEffect.EndsWith("TravelSpeed"))
                {
                    int newValue = random.Next(2000) + 2000;
                    for (int j = 1; j < 12; j++)
                    {
                        talentEffectLevels[i, "Level" + j].DisplayableValue = newValue.ToString();
                    }
                }
            }

            talentEffectLevels.Write2DAToExport();
            bdtsTalents.save();
            ModifiedFiles[bdtsTalents.FileName] = bdtsTalents.FileName;

        }

        private void RandomizeSplash(Random random, ME1Package entrymenu)
        {
            IExportEntry planetMaterial = entrymenu.getUExport(1316);
            RandomizePlanetMaterialInstanceConstant(planetMaterial, random);

            //Corona
            IExportEntry coronaMaterial = entrymenu.getUExport(1317);
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
            IExportEntry cameraInterpData = entrymenu.getUExport(946);
            var interpLength = cameraInterpData.GetProperty<FloatProperty>("InterpLength");
            float animationLength = random.NextFloat(60, 120);
            ;
            interpLength.Value = animationLength;
            cameraInterpData.WriteProperty(interpLength);

            IExportEntry cameraInterpTrackMove = entrymenu.getUExport(967);
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

        private void RandomizeInterpPawns(IExportEntry export, Random random)
        {
            var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");

            List<ObjectProperty> pawnsToShuffle = new List<ObjectProperty>();
            var playerRefs = new List<IExportEntry>();
            foreach (var variableLink in variableLinks)
            {
                var expectedType = variableLink.GetProp<ObjectProperty>("ExpectedType");
                var expectedTypeStr = export.FileRef.getEntry(expectedType.Value).ObjectName;
                if (expectedTypeStr == "SeqVar_Object" || expectedTypeStr == "SeqVar_Player" || expectedTypeStr == "BioSeqVar_ObjectFindByTag")
                {
                    //Investigate the links
                    var linkedVariables = variableLink.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables");
                    foreach (var objRef in linkedVariables)
                    {
                        var linkedObj = export.FileRef.getUExport(objRef.Value).GetProperty<ObjectProperty>("ObjValue");
                        if (linkedObj != null)
                        {
                            var linkedObjectEntry = export.FileRef.getEntry(linkedObj.Value);
                            var linkedObjName = linkedObjectEntry.ObjectName;
                            if (linkedObjName == "BioPawn" && linkedObjectEntry is IExportEntry bioPawnExport)
                            {
                                var flyingpawn = bioPawnExport.GetProperty<BoolProperty>("bCanFly")?.Value;
                                if (flyingpawn == null || flyingpawn == false)
                                {
                                    pawnsToShuffle.Add(objRef); //pointer to this node
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
            }
        }

        private void RandomizePlanetMaterialInstanceConstant(IExportEntry planetMaterial, Random random, bool realistic = false)
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

        private static readonly int[] GalaxyMapImageIdsThatArePlotReserved = { 1, 7, 8, 116, 117, 118, 119, 120, 121, 122, 123, 124 }; //Plot or Sol planets
        private static readonly int[] GalaxyMapImageIdsThatAreAsteroidReserved = { 70 }; //Asteroids
        private static readonly int[] GalaxyMapImageIdsThatAreFactoryReserved = { 6 }; //Asteroids
        private static readonly int[] GalaxyMapImageIdsThatAreMSVReserved = { 76, 79, 82, 85 }; //MSV Ships
        private static readonly int[] GalaxyMapImageIdsToNeverRandomize = { 127, 128 }; //no idea what these are

        private void RandomizePlanetImages(Random random, Dictionary<int, RandomizedPlanetInfo> planetsRowToRPIMapping, Bio2DA planets2DA, ME1Package galaxyMapImagesPackage, IExportEntry galaxyMapImagesUi, Dictionary<string, List<string>> galaxyMapGroupResources)
        {
            mainWindow.CurrentOperationText = "Updating galaxy map images";
            mainWindow.ProgressBarIndeterminate = false;

            var galaxyMapImages2DA = new Bio2DA(galaxyMapImagesUi);
            var ui2DAPackage = galaxyMapImagesUi.FileRef;

            //Get all exports for images
            string swfObjectNamePrefix = Path.GetFileNameWithoutExtension(ui2DAPackage.FileName).Equals("BIOG_2DA_Vegas_UI_X", StringComparison.InvariantCultureIgnoreCase) ? "prc2_galmap" : "galMap";
            var mapImageExports = galaxyMapImagesPackage.Exports.Where(x => x.ObjectName.StartsWith(swfObjectNamePrefix)).ToList(); //Original galaxy map images
            var planet2daRowsWithImages = new List<int>();
            int imageIndexCol = planets2DA.GetColumnIndexByName("ImageIndex");
            int descriptionCol = planets2DA.GetColumnIndexByName("Description");
            int nextAddedImageIndex = int.Parse(galaxyMapImages2DA.RowNames.Last()) + 1000; //we increment from here. //+1000 to ensure we don't have overlap between DLC
                                                                                            //int nextGalaxyMap2DAImageRowIndex = 0; //THIS IS C# BASED

            //var mappedRPIs = planetsRowToRPIMapping.Values.ToList();

            mainWindow.ProgressBar_Bottom_Max = planets2DA.RowCount;

            Debug.WriteLine("----------------DICTIONARY OF PLANET INFO MAPPINGS:============");
            foreach (var kvp in planetsRowToRPIMapping)
            {
                //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                Debug.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value.PlanetName + (kvp.Value.PlanetName2 != null ? $" ({kvp.Value.PlanetName2})" : ""));
            }
            Debug.WriteLine("----------------------------------------------------------------");
            List<int> assignedImageIndexes = new List<int>(); //This is used to generate new indexes for items that vanilla share values with (like MSV ships)
            for (int i = 0; i < planets2DA.RowCount; i++)
            {
                mainWindow.CurrentProgressValue = i;
                if (planets2DA[i, descriptionCol] == null || planets2DA[i, descriptionCol].GetIntValue() == 0)
                {
                    Debug.WriteLine("Skipping tlk -1 or blank row: (0-indexed) " + i);
                    continue; //Skip this row, its an asteroid belt (or liara's dig site)
                }

                //var assignedRPI = mappedRPIs[i];
                int rowName = i;
                //int.Parse(planets2DA.RowNames[i]);
                //Debug.WriteLine("Getting RPI via row #: " + planets2DA.RowNames[i] + ", using dictionary key " + rowName);

                if (planetsRowToRPIMapping.TryGetValue(rowName, out RandomizedPlanetInfo assignedRPI))
                {

                    var hasImageResource = galaxyMapGroupResources.TryGetValue(assignedRPI.ImageGroup.ToLower(), out var newImagePool);
                    if (!hasImageResource)
                    {
                        hasImageResource = galaxyMapGroupResources.TryGetValue("generic", out newImagePool); //DEBUG ONLY! KIND OF?
                        Log.Warning("WARNING: NO IMAGEGROUP FOR GROUP " + assignedRPI.ImageGroup);
                    }
                    if (hasImageResource)
                    {
                        string newImageResource = null;
                        if (newImagePool.Count > 0)
                        {
                            newImageResource = newImagePool[0];
                            if (assignedRPI.ImageGroup.ToLower() != "error")
                            {
                                //We can use error multiple times.
                                newImagePool.RemoveAt(0);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Not enough images in group " + assignedRPI.ImageGroup + " to continue randomization. Skipping row " + rowName);
                            continue;
                        }

                        Bio2DACell imageIndexCell = planets2DA[i, imageIndexCol];
                        bool didntIncrementNextImageIndex = false;
                        if (imageIndexCell == null)
                        {
                            //Generating new cell that used to be blank - not sure if we should do this.
                            imageIndexCell = new Bio2DACell(Bio2DACell.Bio2DADataType.TYPE_INT, BitConverter.GetBytes(++nextAddedImageIndex));
                            planets2DA[i, imageIndexCol] = imageIndexCell;
                        }
                        else if (imageIndexCell.GetIntValue() < 0 || assignedImageIndexes.Contains(imageIndexCell.GetIntValue()))
                        {
                            //Generating new image value
                            imageIndexCell.DisplayableValue = (++nextAddedImageIndex).ToString();
                        }
                        else
                        {
                            didntIncrementNextImageIndex = true;
                        }

                        assignedImageIndexes.Add(imageIndexCell.GetIntValue());

                        var newImageSwf = newImagePool;
                        IExportEntry matchingExport = null;

                        int uiTableRowName = imageIndexCell.GetIntValue();
                        int rowIndex = galaxyMapImages2DA.GetRowIndexByName(uiTableRowName.ToString());
                        if (rowIndex == -1)
                        {
                            //Create export and row first
                            matchingExport = mapImageExports[0].Clone();
                            matchingExport.indexValue = 0;
                            string objectName = "galMapMER" + nextAddedImageIndex;
                            matchingExport.idxObjectName = galaxyMapImagesPackage.FindNameOrAdd(objectName);
                            galaxyMapImagesPackage.addExport(matchingExport);
                            Log.Information("Cloning galaxy map SWF export. New export " + matchingExport.GetFullPath);
                            int newRowIndex = galaxyMapImages2DA.AddRow(nextAddedImageIndex.ToString());
                            int nameIndex = ui2DAPackage.FindNameOrAdd(Path.GetFileNameWithoutExtension(galaxyMapImagesPackage.FileName) + "." + objectName);
                            galaxyMapImages2DA[newRowIndex, "imageResource"] = new Bio2DACell(Bio2DACell.Bio2DADataType.TYPE_NAME, BitConverter.GetBytes((long)nameIndex));
                            if (didntIncrementNextImageIndex)
                            {
                                Debug.WriteLine("Unused image? Row specified but doesn't exist in this table. Repointing to new image row for image value " + nextAddedImageIndex);
                                imageIndexCell.DisplayableValue = nextAddedImageIndex.ToString(); //assign the image cell to point to this export row
                                nextAddedImageIndex++; //next image index was not incremented, but we had to create a new export anyways. Increment the counter.
                            }
                        }
                        else
                        {
                            string swfImageExportObjectName = galaxyMapImages2DA[rowIndex, "imageResource"].DisplayableValueIndexed;
                            //get object name of export inside of GUI_SF_GalaxyMap.upk
                            swfImageExportObjectName = swfImageExportObjectName.Substring(swfImageExportObjectName.IndexOf('.') + 1); //TODO: Need to deal with name instances for Pinnacle Station DLC. Because it's too hard for them to type a new name.
                                                                                                                                      //Fetch export
                            matchingExport = mapImageExports.FirstOrDefault(x => x.ObjectNameIndexed == swfImageExportObjectName);
                        }

                        if (matchingExport != null)
                        {
                            ReplaceSWFFromResource(matchingExport, newImageResource);
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }
                    else
                    {
                        string nameTextForRow = planets2DA[i, 5].DisplayableValue;
                        Debug.WriteLine("Skipped row: " + rowName + ", " + nameTextForRow + ", could not find imagegroup " + assignedRPI.ImageGroup);
                    }
                }
                else
                {
                    string nameTextForRow = planets2DA[i, 5].DisplayableValue;
                    Debug.WriteLine("Skipped row: " + rowName + ", " + nameTextForRow + " due to no RPI for this row.");
                }
            }

            galaxyMapImages2DA.Write2DAToExport();
            ui2DAPackage.save();
            ModifiedFiles[ui2DAPackage.FileName] = ui2DAPackage.FileName;
            galaxyMapImagesPackage.save();
            ModifiedFiles[galaxyMapImagesPackage.FileName] = galaxyMapImagesPackage.FileName;
            planets2DA.Write2DAToExport(); //should save later
        }

        private void GalaxyMapValidationPass(Dictionary<int, RandomizedPlanetInfo> rowRPIMapping, Bio2DA planets2DA, Bio2DA galaxyMapImages2DA, ME1Package galaxyMapImagesPackage)
        {
            mainWindow.CurrentOperationText = "Running tests on galaxy map images";
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = rowRPIMapping.Keys.Count;

            mainWindow.CurrentProgressValue = 0;

            foreach (int i in rowRPIMapping.Keys)
            {
                mainWindow.CurrentProgressValue++;

                //For every row in planets 2DA table
                if (planets2DA[i, "Description"] != null && planets2DA[i, "Description"].DisplayableValue != "-1")
                {
                    int imageRowReference = planets2DA[i, "ImageIndex"].GetIntValue();
                    if (imageRowReference == -1) continue; //We don't have enough images yet to pass this hurdle
                                                           //Use this value to find value in UI table
                    int rowIndex = galaxyMapImages2DA.GetRowIndexByName(imageRowReference.ToString());
                    string exportName = galaxyMapImages2DA[rowIndex, 0].DisplayableValueIndexed;
                    exportName = exportName.Substring(exportName.LastIndexOf('.') + 1);
                    //Use this value to find the export in GUI_SF file
                    var export = galaxyMapImagesPackage.Exports.FirstOrDefault(x => x.ObjectNameIndexed == exportName);
                    if (export == null)
                    {
                        Debugger.Break();
                    }
                    else
                    {
                        string path = export.GetProperty<StrProperty>("SourceFilePath").Value;
                        path = path.Substring(path.LastIndexOf(' ') + 1);
                        string[] parts = path.Split('.');
                        if (parts.Length == 6)
                        {
                            string swfImageGroup = parts[3];
                            var assignedRPI = rowRPIMapping[i];
                            if (assignedRPI.ImageGroup.ToLower() != swfImageGroup)
                            {
                                Debug.WriteLine("WRONG IMAGEGROUP ASSIGNED!");
                                Debugger.Break();
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Source comment not correct format, might not yet be assigned: " + path);
                        }
                    }

                }
                else
                {
                    //Debugger.Break();
                }
            }
        }

        private void ReplaceSWFFromResource(IExportEntry exp, string swfResourcePath)
        {
            Debug.WriteLine($"Replacing {Path.GetFileName(exp.FileRef.FileName)} {exp.UIndex} {exp.ObjectName} SWF with {swfResourcePath}");
            var bytes = Utilities.GetEmbeddedStaticFilesBinaryFile(swfResourcePath, true);
            var props = exp.GetProperties();

            var rawData = props.GetProp<ArrayProperty<ByteProperty>>("Data");
            //Write SWF data
            rawData.Values = bytes.Select(b => new ByteProperty(b)).ToList();

            //Write SWF metadata
            props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(swfResourcePath), "SourceFilePath"));
            props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
            exp.WriteProperties(props);
        }

        private void RandomizeFaceFX(IExportEntry exp, Random random, int amount)
        {
            try
            {
                Log.Information($"Randomizing FaceFX export {exp.UIndex}");
                ME1FaceFXAnimSet animSet = new ME1FaceFXAnimSet(exp);
                for (int i = 0; i < animSet.Data.Data.Count(); i++)
                {
                    var faceFxline = animSet.Data.Data[i];
                    //if (true)
                    if (random.Next(10 - amount) == 0)
                    {
                        //Randomize the names used for animation
                        List<int> usedIndexes = faceFxline.animations.Select(x => x.index).ToList();
                        usedIndexes.Shuffle(random);
                        for (int j = 0; j < faceFxline.animations.Length; j++)
                        {
                            faceFxline.animations[j].index = usedIndexes[j];
                        }
                    }
                    else
                    {
                        //Randomize the points
                        for (int j = 0; j < faceFxline.points.Length; j++)
                        {
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
                                        faceFxline.points[j].weight = random.NextFloat(-10, 10);
                                    }
                                    else
                                    {
                                        faceFxline.points[j].weight *= 8;
                                    }
                                    break;
                                case 4: //Extreme
                                    if (random.Next(6) == 0)
                                    {
                                        faceFxline.points[j].weight = random.NextFloat(-20, 20);
                                    }
                                    else
                                    {
                                        faceFxline.points[j].weight *= 20;
                                    }
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

                Log.Information("Randomized FaceFX for export " + exp.UIndex);
                animSet.Save();
            }
            catch (Exception e)
            {
                //Do nothing for now.
                Log.Error("AnimSet error! " + App.FlattenException((e)));
            }
        }

        public void AddMERSplash(Random random)
        {
            ME1Package entrymenu = new ME1Package(Utilities.GetEntryMenuFile());

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
                File.Copy(merToExtract, Utilities.GetGameFile(@"BioGame\CookedPC\Movies\merintro.bik"), true);
                entrymenu.save();
                //Add to fileindex
                var fileIndex = Utilities.GetGameFile(@"BioGame\CookedPC\FileIndex.txt");
                var filesInIndex = File.ReadAllLines(fileIndex).ToList();
                if (filesInIndex.All(x => x != @"Movies\MERIntro.bik"))
                {
                    filesInIndex.Add(@"Movies\MERIntro.bik");
                    File.WriteAllLines(fileIndex, filesInIndex);
                }
                ModifiedFiles[entrymenu.FileName] = entrymenu.FileName;
            }

        }

        private static string[] hazardTypes = { "Cold", "Heat", "Toxic", "Radiation", "Vacuum" };

        private void RandomizeHazard(IExportEntry export, Random random)
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

        private void scaleHeadMesh(IExportEntry meshRef, float headScale)
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

        private void RandomizeInterpTrackMove(IExportEntry export, Random random, double amount)
        {
            Log.Information("Randomizing movement interpolations for " + export.UIndex + ": " + export.GetIndexedFullPath);
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

        public string GetResourceFileText(string filename, string assemblyName)
        {
            string result = string.Empty;

            using (Stream stream =
                System.Reflection.Assembly.Load(assemblyName).GetManifestResourceStream($"{assemblyName}.{filename}"))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }

            return result;
        }


        private void RandomizeMako(ME1Package package, Random random)
        {
            IExportEntry SVehicleSimTank = package.Exports[23314];
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
                IExportEntry LFWheel = package.Exports[36984];
                IExportEntry RFWheel = package.Exports[36987];
                //Rear
                IExportEntry LRWheel = package.Exports[36986];
                IExportEntry RRWheel = package.Exports[36989];

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
            IExportEntry BioVehicleBehaviorBase = package.Exports[23805];
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

        private void RandomizePlanetNameDescriptions(IExportEntry export, Random random, List<TalkFile> Tlks)
        {
            mainWindow.CurrentOperationText = "Applying entropy to galaxy map";
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("planetinfo.xml");

            XElement rootElement = XElement.Parse(fileContents);
            var allMapRandomizationInfo = (from e in rootElement.Elements("RandomizedPlanetInfo")
                                           select new RandomizedPlanetInfo
                                           {
                                               PlanetName = (string)e.Element("PlanetName"),
                                               PlanetName2 = (string)e.Element("PlanetName2"), //Original name (plot planets only)
                                               PlanetDescription = (string)e.Element("PlanetDescription"),
                                               IsMSV = (bool)e.Element("IsMSV"),
                                               IsAsteroidBelt = (bool)e.Element("IsAsteroidBelt"),
                                               IsAsteroid = e.Element("IsAsteroid") != null && (bool)e.Element("IsAsteroid"),
                                               PreventShuffle = (bool)e.Element("PreventShuffle"),
                                               RowID = (int)e.Element("RowID"),
                                               MapBaseNames = e.Elements("MapBaseNames")
                                                   .Select(r => r.Value).ToList(),
                                               DLC = e.Element("DLC")?.Value,
                                               ImageGroup = e.Element("ImageGroup")?.Value ?? "Generic", //TODO: TURN THIS OFF FOR RELEASE BUILD AND DEBUG ONCE FULLY IMPLEMENTED
                                               ButtonLabel = e.Element("ButtonLabel")?.Value,
                                               Playable = !(e.Element("NotPlayable") != null && (bool)e.Element("NotPlayable")),
                                           }).ToList();

            fileContents = Utilities.GetEmbeddedStaticFilesTextFile("galaxymapclusters.xml");
            rootElement = XElement.Parse(fileContents);
            var suffixedClusterNames = rootElement.Elements("suffixedclustername").Select(x => x.Value).ToList(); //Used for assignments
            var suffixedClusterNamesForPreviousLookup = rootElement.Elements("suffixedclustername").Select(x => x.Value).ToList(); //Used to lookup previous assignments 
            VanillaSuffixedClusterNames = rootElement.Elements("originalsuffixedname").Select(x => x.Value).ToList();
            var nonSuffixedClusterNames = rootElement.Elements("nonsuffixedclustername").Select(x => x.Value).ToList();
            suffixedClusterNames.Shuffle(random);
            nonSuffixedClusterNames.Shuffle(random);

            fileContents = Utilities.GetEmbeddedStaticFilesTextFile("galaxymapsystems.xml");
            rootElement = XElement.Parse(fileContents);
            var shuffledSystemNames = rootElement.Elements("systemname").Select(x => x.Value).ToList();
            shuffledSystemNames.Shuffle(random);


            var everything = new List<string>();
            everything.AddRange(suffixedClusterNames);
            everything.AddRange(allMapRandomizationInfo.Select(x => x.PlanetName));
            everything.AddRange(allMapRandomizationInfo.Where(x => x.PlanetName2 != null).Select(x => x.PlanetName2));
            everything.AddRange(shuffledSystemNames);
            everything.AddRange(nonSuffixedClusterNames);

            //Subset checking
            //foreach (var name1 in everything)
            //{
            //    foreach (var name2 in everything)
            //    {
            //        if (name1.Contains(name2) && name1 != name2)
            //        {
            //            //Debugger.Break();
            //        }
            //    }
            //}

            var msvInfos = allMapRandomizationInfo.Where(x => x.IsMSV).ToList();
            var asteroidInfos = allMapRandomizationInfo.Where(x => x.IsAsteroid).ToList();
            var asteroidBeltInfos = allMapRandomizationInfo.Where(x => x.IsAsteroidBelt).ToList();
            var planetInfos = allMapRandomizationInfo.Where(x => !x.IsAsteroidBelt && !x.IsAsteroid && !x.IsMSV && !x.PreventShuffle).ToList();

            msvInfos.Shuffle(random);
            asteroidInfos.Shuffle(random);
            planetInfos.Shuffle(random);

            List<int> rowsToNotRandomlyReassign = new List<int>();

            IExportEntry systemsExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_System");
            IExportEntry clustersExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_Cluster");
            IExportEntry areaMapExport = export.FileRef.Exports.First(x => x.ObjectName == "AreaMap_AreaMap");
            IExportEntry plotPlanetExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_PlotPlanet");
            IExportEntry mapExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_Map");

            Bio2DA systems2DA = new Bio2DA(systemsExport);
            Bio2DA clusters2DA = new Bio2DA(clustersExport);
            Bio2DA planets2DA = new Bio2DA(export);
            Bio2DA areaMap2DA = new Bio2DA(areaMapExport);
            Bio2DA plotPlanet2DA = new Bio2DA(plotPlanetExport);
            Bio2DA levelMap2DA = new Bio2DA(mapExport);

            //These dictionaries hold the mappings between the old names and new names and will be used in the 
            //map file pass as references to these are also contained in the localized map TLKs.
            systemNameMapping = new Dictionary<string, string>();
            clusterNameMapping = new Dictionary<string, SuffixedCluster>();
            planetNameMapping = new Dictionary<string, string>();


            //Cluster Names
            int nameColumnClusters = clusters2DA.GetColumnIndexByName("Name");
            //Used for resolving %SYSTEMNAME% in planet description and localization VO text
            Dictionary<int, SuffixedCluster> clusterIdToClusterNameMap = new Dictionary<int, SuffixedCluster>();

            for (int i = 0; i < clusters2DA.RowNames.Count; i++)
            {
                int tlkRef = clusters2DA[i, nameColumnClusters].GetIntValue();

                string oldClusterName = "";
                foreach (TalkFile tf in Tlks)
                {
                    oldClusterName = tf.findDataById(tlkRef);
                    if (oldClusterName != "No Data")
                    {
                        SuffixedCluster suffixedCluster = null;
                        if (VanillaSuffixedClusterNames.Contains(oldClusterName) || suffixedClusterNamesForPreviousLookup.Contains(oldClusterName))
                        {
                            suffixedClusterNamesForPreviousLookup.Remove(oldClusterName);
                            suffixedCluster = new SuffixedCluster(suffixedClusterNames[0], true);
                            suffixedClusterNames.RemoveAt(0);
                        }
                        else
                        {
                            suffixedCluster = new SuffixedCluster(nonSuffixedClusterNames[0], false);
                            nonSuffixedClusterNames.RemoveAt(0);
                        }

                        clusterNameMapping[oldClusterName] = suffixedCluster;
                        clusterIdToClusterNameMap[int.Parse(clusters2DA.RowNames[i])] = suffixedCluster;
                        break;
                    }
                }
            }

            //SYSTEMS
            //Used for resolving %SYSTEMNAME% in planet description and localization VO text
            Dictionary<int, (SuffixedCluster clustername, string systemname)> systemIdToSystemNameMap = new Dictionary<int, (SuffixedCluster clustername, string systemname)>();


            BuildSystemClusterMap(systems2DA, Tlks, systemIdToSystemNameMap, clusterIdToClusterNameMap, shuffledSystemNames);


            //BRING DOWN THE SKY (UNC) SYSTEM===================
            if (File.Exists(Utilities.GetGameFile(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\2DAs\BIOG_2DA_UNC_GalaxyMap_X.upk"))))
            {
                ME1Package bdtsGalaxyMapX = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\2DAs\BIOG_2DA_UNC_GalaxyMap_X.upk"));
                Bio2DA bdtsGalMapX_Systems2DA = new Bio2DA(bdtsGalaxyMapX.getUExport(6));
                ME1Package bdtstalkfile = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\Dialog\DLC_UNC_GlobalTlk.upk"));
                var bdtsTlks = bdtstalkfile.Exports.Where(x => x.ClassName == "BioTlkFile").Select(x => new TalkFile(x)).ToList();
                BuildSystemClusterMap(bdtsGalMapX_Systems2DA, bdtsTlks, systemIdToSystemNameMap, clusterIdToClusterNameMap, shuffledSystemNames);
            }
            //END BRING DOWN THE SKY=====================

            //PLANETS
            //mainWindow.CurrentProgressValue = 0;37
            //mainWindow.ProgressBar_Bottom_Max = planets2DA.RowCount;
            //mainWindow.ProgressBarIndeterminate = false;

            Dictionary<string, List<string>> galaxyMapGroupResources = new Dictionary<string, List<string>>();
            var resourceItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.galaxymapimages.")).ToList();
            var uniqueNames = new SortedSet<string>();

            //Get unique image group categories
            foreach (string str in resourceItems)
            {
                string[] parts = str.Split('.');
                if (parts.Length == 6)
                {
                    uniqueNames.Add(parts[3]);
                }
            }

            //Build group lists
            foreach (string groupname in uniqueNames)
            {
                galaxyMapGroupResources[groupname] = resourceItems.Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.galaxymapimages." + groupname)).ToList();
                galaxyMapGroupResources[groupname].Shuffle(random);
            }

            //BASEGAME===================================
            var rowRPIMap = new Dictionary<int, RandomizedPlanetInfo>();
            var AlreadyAssignedMustBePlayableRows = new List<int>();
            for (int i = 0; i < planets2DA.RowCount; i++)
            {
                Bio2DACell mapCell = planets2DA[i, "Map"];
                if (mapCell.GetIntValue() > 0)
                {
                    //must be playable
                    RandomizePlanetText(planets2DA, i, "", Tlks, systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMap, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos, mustBePlayable: true);
                    AlreadyAssignedMustBePlayableRows.Add(i);
                }
            }

            for (int i = 0; i < planets2DA.RowCount; i++)
            {
                if (AlreadyAssignedMustBePlayableRows.Contains(i)) continue;
                RandomizePlanetText(planets2DA, i, "", Tlks, systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMap, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos);
            }
            ME1Package galaxyMapImagesBasegame = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Packages\GUI\GUI_SF_GalaxyMap.upk")); //lol demiurge, what were you doing?
            ME1Package ui2DAPackage = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Packages\2DAs\BIOG_2DA_UI_X.upk")); //lol demiurge, what were you doing?
            IExportEntry galaxyMapImages2DAExport = ui2DAPackage.getUExport(8);
            RandomizePlanetImages(random, rowRPIMap, planets2DA, galaxyMapImagesBasegame, galaxyMapImages2DAExport, galaxyMapGroupResources);
            UpdateGalaxyMapReferencesForTLKs(Tlks, true, true); //Update TLKs.
            planets2DA.Write2DAToExport();
            //END BASEGAME===============================

            //BRING DOWN THE SKY (UNC)===================
            if (File.Exists(Utilities.GetGameFile(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\2DAs\BIOG_2DA_UNC_GalaxyMap_X.upk"))))
            {
                ME1Package bdtsplanets = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\2DAs\BIOG_2DA_UNC_GalaxyMap_X.upk"));
                ME1Package bdtstalkfile = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\Dialog\DLC_UNC_GlobalTlk.upk"));

                Bio2DA bdtsGalMapX_Planets2DA = new Bio2DA(bdtsplanets.getUExport(3));
                var rowRPIMapBdts = new Dictionary<int, RandomizedPlanetInfo>();
                var bdtsTlks = bdtstalkfile.Exports.Where(x => x.ClassName == "BioTlkFile").Select(x => new TalkFile(x)).ToList();

                for (int i = 0; i < bdtsGalMapX_Planets2DA.RowCount; i++)
                {
                    RandomizePlanetText(bdtsGalMapX_Planets2DA, i, "UNC", bdtsTlks, systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMapBdts, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos);
                }
                var galaxyMapImagesBdts = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\GUI\GUI_SF_DLC_GalaxyMap.upk"));
                ui2DAPackage = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_UNC\CookedPC\Packages\2DAs\BIOG_2DA_UNC_UI_X.upk"));
                galaxyMapImages2DAExport = ui2DAPackage.getUExport(2);
                RandomizePlanetImages(random, rowRPIMapBdts, bdtsGalMapX_Planets2DA, galaxyMapImagesBdts, galaxyMapImages2DAExport, galaxyMapGroupResources);
                bdtsplanets.save();
                ModifiedFiles[bdtsplanets.FileName] = bdtsplanets.FileName;
                UpdateGalaxyMapReferencesForTLKs(bdtsTlks, true, false); //Update TLKs
                bdtsTlks.ForEach(x => x.saveToExport());
                bdtstalkfile.save();
                ModifiedFiles[bdtstalkfile.FileName] = bdtstalkfile.FileName;
                GalaxyMapValidationPass(rowRPIMapBdts, bdtsGalMapX_Planets2DA, new Bio2DA(galaxyMapImages2DAExport), galaxyMapImagesBdts);
            }
            //END BRING DOWN THE SKY=====================

            //PINNACE STATION (VEGAS)====================
            if (File.Exists(Utilities.GetGameFile(@"DLC\DLC_Vegas\CookedPC\Packages\2DAs\BIOG_2DA_Vegas_GalaxyMap_X.upk")))
            {
                ME1Package vegasplanets = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_Vegas\CookedPC\Packages\2DAs\BIOG_2DA_Vegas_GalaxyMap_X.upk"));
                ME1Package vegastalkfile = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_Vegas\CookedPC\Packages\Dialog\DLC_Vegas_GlobalTlk.upk"));

                Bio2DA vegasGalMapX_Planets2DA = new Bio2DA(vegasplanets.getUExport(2));
                var rowRPIMapVegas = new Dictionary<int, RandomizedPlanetInfo>();
                var vegasTlks = vegastalkfile.Exports.Where(x => x.ClassName == "BioTlkFile").Select(x => new TalkFile(x)).ToList();

                for (int i = 0; i < vegasGalMapX_Planets2DA.RowCount; i++)
                {
                    RandomizePlanetText(vegasGalMapX_Planets2DA, i, "Vegas", vegasTlks, systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMapVegas, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos);
                }

                var galaxyMapImagesVegas = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_Vegas\CookedPC\Packages\GUI\GUI_SF_PRC2_GalaxyMap.upk"));
                ui2DAPackage = new ME1Package(Utilities.GetGameFile(@"DLC\DLC_Vegas\CookedPC\Packages\2DAs\BIOG_2DA_Vegas_UI_X.upk"));
                galaxyMapImages2DAExport = ui2DAPackage.getUExport(2);
                RandomizePlanetImages(random, rowRPIMapVegas, vegasGalMapX_Planets2DA, galaxyMapImagesVegas, galaxyMapImages2DAExport, galaxyMapGroupResources);
                vegasplanets.save();
                ModifiedFiles[vegasplanets.FileName] = vegasplanets.FileName;
                UpdateGalaxyMapReferencesForTLKs(vegasTlks, true, false); //Update TLKs.
                vegasTlks.ForEach(x => x.saveToExport());
                vegastalkfile.save();
                ModifiedFiles[vegastalkfile.FileName] = vegastalkfile.FileName;

            }
            //END PINNACLE STATION=======================
        }

        private void BuildSystemClusterMap(Bio2DA systems2DA, List<TalkFile> Tlks, Dictionary<int, (SuffixedCluster clustername, string systemname)> systemIdToSystemNameMap, Dictionary<int, SuffixedCluster> clusterIdToClusterNameMap, List<string> shuffledSystemNames)
        {
            int nameColumnSystems = systems2DA.GetColumnIndexByName("Name");
            int clusterColumnSystems = systems2DA.GetColumnIndexByName("Cluster");
            for (int i = 0; i < systems2DA.RowNames.Count; i++)
            {

                string newSystemName = shuffledSystemNames[0];
                shuffledSystemNames.RemoveAt(0);
                int tlkRef = systems2DA[i, nameColumnSystems].GetIntValue();
                int clusterTableRow = systems2DA[i, clusterColumnSystems].GetIntValue();


                string oldSystemName = "";
                foreach (TalkFile tf in Tlks)
                {
                    oldSystemName = tf.findDataById(tlkRef);
                    if (oldSystemName != "No Data")
                    {
                        //tf.replaceString(tlkRef, newSystemName);
                        systemNameMapping[oldSystemName] = newSystemName;
                        systemIdToSystemNameMap[int.Parse(systems2DA.RowNames[i])] = (clusterIdToClusterNameMap[clusterTableRow], newSystemName);
                        break;
                    }
                }
            }
        }

        private void RandomizePlanetText(Bio2DA planets2DA, int tableRow, string dlcName, List<TalkFile> Tlks, Dictionary<int, (SuffixedCluster clustername, string systemname)> systemIdToSystemNameMap,
            List<RandomizedPlanetInfo> allMapRandomizationInfo, Dictionary<int, RandomizedPlanetInfo> rowRPIMap, List<RandomizedPlanetInfo> planetInfos, List<RandomizedPlanetInfo> msvInfos, List<RandomizedPlanetInfo> asteroidInfos,
            List<RandomizedPlanetInfo> asteroidBeltInfos, bool mustBePlayable = false)
        {
            //mainWindow.CurrentProgressValue = i;
            int systemId = planets2DA[tableRow, 1].GetIntValue();
            (SuffixedCluster clusterName, string systemName) systemClusterName = systemIdToSystemNameMap[systemId];

            Bio2DACell descriptionRefCell = planets2DA[tableRow, "Description"];
            Bio2DACell mapCell = planets2DA[tableRow, "Map"];
            bool isMap = mapCell != null && mapCell.GetIntValue() > 0;

            int descriptionReference = descriptionRefCell?.GetIntValue() ?? 0;


            //var rowIndex = int.Parse(planets2DA.RowNames[i]);
            var info = allMapRandomizationInfo.FirstOrDefault(x => x.RowID == tableRow && (dlcName == "" || x.DLC == dlcName)); //get non-shuffled information. this implementation will have to be chagned later to accoutn for additional planets
            if (info != null)
            {
                if (info.IsAsteroidBelt)
                {
                    return; //we don't care.
                }
                //found original info
                RandomizedPlanetInfo rpi = null;
                if (info.PreventShuffle)
                {
                    //Shuffle with items of same rowindex.
                    //Todo post launch.
                    rpi = info;
                    //Do not use shuffled

                }
                else
                {
                    if (info.IsMSV)
                    {
                        rpi = msvInfos[0];
                        msvInfos.RemoveAt(0);
                    }
                    else if (info.IsAsteroid)
                    {
                        rpi = asteroidInfos[0];
                        asteroidInfos.RemoveAt(0);
                    }
                    else
                    {

                        int indexPick = 0;
                        rpi = planetInfos[indexPick];
                        Debug.WriteLine("Assigning MustBePlayable: " + rpi.PlanetName);
                        while (!rpi.Playable && mustBePlayable) //this could error out but since we do things in a specific order it shouldn't
                        {
                            indexPick++;
                            //We need to fetch another RPI
                            rpi = planetInfos[indexPick];
                        }

                        planetInfos.RemoveAt(indexPick);
                        //if (isMap)
                        //{
                        //    Debug.WriteLine("IsMapAssigned: " + rpi.PlanetName);
                        //    numRequiredLandablePlanets--;
                        //    if (remainingLandablePlanets < numRequiredLandablePlanets)
                        //    {
                        //        Debugger.Break(); //we're gonna have a bad time
                        //    }
                        //}
                        //Debug.WriteLine("Assigning planet from pool, is playable: " + rpi.Playable);

                    }
                }


                rowRPIMap[tableRow] = rpi; //Map row in this table to the assigned RPI
                string newPlanetName = rpi.PlanetName;
                if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION_PLOTPLANET && rpi.PlanetName2 != null)
                {
                    newPlanetName = rpi.PlanetName2;
                }

                //if (rename plot missions) planetName = rpi.PlanetName2
                var description = rpi.PlanetDescription;
                if (description != null)
                {
                    SuffixedCluster clusterName = systemClusterName.clusterName;
                    string clusterString = systemClusterName.clusterName.ClusterName;
                    if (!clusterName.Suffixed)
                    {
                        clusterString += " cluster";
                    }
                    description = description.Replace("%CLUSTERNAME%", clusterString).Replace("%SYSTEMNAME%", systemClusterName.systemName).Replace("%PLANETNAME%", newPlanetName).TrimLines();
                }

                //var landableMapID = planets2DA[i, planets2DA.GetColumnIndexByName("Map")].GetIntValue();
                int planetNameTlkId = planets2DA[tableRow, "Name"].GetIntValue();

                //Replace planet description here, as it won't be replaced in the overall pass
                foreach (TalkFile tf in Tlks)
                {
                    //Debug.WriteLine("Setting planet name on row index (not rowname!) " + i + " to " + newPlanetName);
                    string originalPlanetName = tf.findDataById(planetNameTlkId);

                    if (originalPlanetName == "No Data")
                    {
                        continue;
                    }

                    if (!info.IsAsteroid)
                    {
                        //We don't want to do a planet mapping as this might overwrite existing text somewhere, and nothing mentions an asteroid directly.
                        planetNameMapping[originalPlanetName] = newPlanetName;
                    }

                    //if (originalPlanetName == "Ilos") Debugger.Break();
                    if (descriptionReference != 0 && description != null)
                    {
                        tf.TlksIdsToNotUpdate.Add(descriptionReference);
                        //Log.Information($"New planet: {newPlanetName}");
                        //if (descriptionReference == 138077)
                        //{
                        //    Debug.WriteLine($"------------SUBSTITUTING----{tf.export.ObjectName}------------------");
                        //    Debug.WriteLine($"{originalPlanetName} -> {newPlanetName}");
                        //    Debug.WriteLine("New description:\n" + description);
                        //    Debug.WriteLine("----------------------------------");
                        //    Debugger.Break(); //Xawin
                        //}
                        tf.replaceString(descriptionReference, description);

                        if (rpi.ButtonLabel != null)
                        {
                            Bio2DACell actionLabelCell = planets2DA[tableRow, "ButtonLabel"];
                            if (actionLabelCell != null)
                            {
                                var currentTlkId = actionLabelCell.GetIntValue();
                                if (tf.findDataById(currentTlkId) != rpi.ButtonLabel)
                                {
                                    //Value is different
                                    //try to find existing value first
                                    var tlkref = tf.findDataByValue(rpi.ButtonLabel);
                                    if (tlkref.StringID != 0)
                                    {
                                        //We found result
                                        actionLabelCell.DisplayableValue = tlkref.StringID.ToString(); //Assign cell to this TLK ref
                                    }
                                    else
                                    {
                                        int newID = tf.getFirstNullString();
                                        if (newID == -1) Debugger.Break(); //hopefully we never see this, but if user runs it enough, i guess you could.
                                        tf.replaceString(newID, rpi.ButtonLabel);
                                        actionLabelCell.DisplayableValue = newID.ToString(); //Assign cell to new TLK ref
                                    }
                                }


                            }
                        }
                    }

                    if (info.IsAsteroid)
                    {
                        //Since some asteroid names change and/or are shared amongst themselves, we have to add names if they don't exist.
                        if (originalPlanetName != rpi.PlanetName)
                        {
                            var newTlkValue = tf.findDataByValue(rpi.PlanetName);
                            if (newTlkValue.StringID == 0)
                            {
                                //Doesn't exist
                                var newId = tf.getFirstNullString();
                                tf.replaceString(newId, rpi.PlanetName);
                                planets2DA[tableRow, "Name"].DisplayableValue = newId.ToString();
                                Log.Information("Assigned asteroid new TLK ID: " + newId);
                            }
                            else
                            {
                                //Exists - repoint to that TLK value
                                planets2DA[tableRow, "Name"].DisplayableValue = newTlkValue.StringID.ToString();
                                Log.Information("Repointed asteroid new existing string ID: " + newTlkValue.StringID);
                            }
                        }
                    }
                }
            }
            else
            {
                Log.Error("No randomization data for galaxy map planet 2da, row id " + tableRow);
            }
        }

        static readonly List<char> englishVowels = new List<char>(new[] { 'a', 'e', 'i', 'o', 'u' });
        static readonly List<char> upperCaseVowels = new List<char>(new[] { 'A', 'E', 'I', 'O', 'U' });

        /// <summary>
        /// Swap the vowels around
        /// </summary>
        /// <param name="Tlks"></param>
        private void MakeTextPossiblyScottish(List<TalkFile> Tlks, Random random, bool updateProgressbar)
        {
            Log.Information("Randomizing vowels");
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
                    mainWindow.CurrentOperationText = $"Randomizing vowels [{currentTlkIndex}/{Tlks.Count()}]";
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

                            if (word.StartsWith("%") || word.StartsWith("<CUSTOM"))
                            {
                                Debug.WriteLine($"Skipping {word}");
                                continue; // Don't modify tokens
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
            }
        }

        private void UpdateGalaxyMapReferencesForTLKs(List<TalkFile> Tlks, bool updateProgressbar, bool basegame)
        {
            int currentTlkIndex = 0;
            foreach (TalkFile tf in Tlks)
            {
                currentTlkIndex++;
                int max = tf.StringRefs.Count();
                int current = 0;
                if (updateProgressbar)
                {
                    mainWindow.CurrentOperationText = $"Applying entropy to galaxy map [{currentTlkIndex}/{Tlks.Count()}]";
                    mainWindow.ProgressBar_Bottom_Max = tf.StringRefs.Length;
                    mainWindow.ProgressBarIndeterminate = false;
                }

                if (basegame) //this will only be fired on basegame tlk's since they're the only ones that update the progerssbar.
                {

                    //text fixes.
                    //TODO: CHECK IF ORIGINAL VALUE IS BIOWARE - IF IT ISN'T ITS ALREADY BEEN UPDATED.
                    string testStr = tf.findDataById(179694);
                    if (testStr == "")
                    {
                        tf.replaceString(179694, "Head to the Armstrong Nebula to investigate what the geth are up to."); //Remove cluster after Nebula to ensure the text pass works without cluster cluster.

                    }
                    testStr = tf.findDataById(156006);
                    testStr = tf.findDataById(136011);

                    tf.replaceString(156006, "Go to the Newton System in the Kepler Verge and find the one remaining scientist assigned to the secret project.");
                    tf.replaceString(136011, "The geth have begun setting up a number of small outposts in the Armstrong Nebula of the Skyllian Verge. You must eliminate these outposts before the incursion becomes a full-scale invasion.");
                }

                //This is inefficient but not much I can do it about it.
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
                        string newString = sref.Data;
                        foreach (var planetMapping in planetNameMapping)
                        {

                            //Update TLK references to this planet.
                            bool originalPlanetNameIsSingleWord = !planetMapping.Key.Contains(" ");

                            if (originalPlanetNameIsSingleWord)
                            {
                                //This is to filter out things like Inti resulting in Intimidate
                                if (originalString.ContainsWord(planetMapping.Key) /*&& newString.ContainsWord(planetMapping.Key)*/) //second statement is disabled as its the same at this point in execution.
                                {
                                    //Do a replace if the whole word is matched only (no partial matches on words).
                                    newString = newString.Replace(planetMapping.Key, planetMapping.Value);
                                }
                            }
                            else
                            {
                                //Planets with spaces in the names won't (hopefully) match on Contains.
                                if (originalString.Contains(planetMapping.Key) && newString.Contains(planetMapping.Key))
                                {
                                    newString = newString.Replace(planetMapping.Key, planetMapping.Value);
                                }
                            }
                        }


                        foreach (var systemMapping in systemNameMapping)
                        {
                            //Update TLK references to this system.
                            bool originalSystemNameIsSingleWord = !systemMapping.Key.Contains(" ");
                            if (originalSystemNameIsSingleWord)
                            {
                                //This is to filter out things like Inti resulting in Intimidate
                                if (originalString.ContainsWord(systemMapping.Key) && newString.ContainsWord(systemMapping.Key))
                                {
                                    //Do a replace if the whole word is matched only (no partial matches on words).
                                    newString = newString.Replace(systemMapping.Key, systemMapping.Value);
                                }
                            }
                            else
                            {
                                //System with spaces in the names won't (hopefully) match on Contains.
                                if (originalString.Contains(systemMapping.Key) && newString.Contains(systemMapping.Key))
                                {
                                    newString = newString.Replace(systemMapping.Key, systemMapping.Value);
                                }
                            }
                        }



                        string test1 = "The geth must be stopped. Go to the Kepler Verge and stop them!";
                        string test2 = "Protect the heart of the Artemis Tau cluster!";

                        // >> test1 Detect types that end with Verge or Nebula, or types that end with an adjective.
                        // >> >> Determine if new name ends with Verge or Nebula or other terms that have a specific ending type that is an adjective of the area. (Castle for example)
                        // >> >> >> True: Do an exact replacement
                        // >> >> >> False: Check if the match is 100% matching on the whole thing. If it is, just replace the string. If it is not, replace the string but append the word "cluster".

                        // >> test 2 Determine if cluster follows the name of the item being replaced.
                        // >> >> Scan for the original key + cluster appended.
                        // >> >> >> True: If the new item includes an ending adjective, replace the whold thing with the word cluster included.
                        // >> >> >> False: If the new item doesn't end with an adjective, replace only the exact original key.

                        foreach (var clusterMapping in clusterNameMapping)
                        {
                            //Update TLK references to this cluster.
                            bool originalclusterNameIsSingleWord = !clusterMapping.Key.Contains(" ");
                            if (originalclusterNameIsSingleWord)
                            {
                                //Go to the Kepler Verge and end the threat.
                                //Old = Kepler Verge, New = Zoltan Homeworlds
                                if (originalString.ContainsWord(clusterMapping.Key) && newString.ContainsWord(clusterMapping.Key)) //
                                {

                                    //Terribly inefficent
                                    if (originalString.Contains("I'm asking you because the Normandy can get on-site quickly and quietly."))
                                        Debugger.Break();
                                    if (clusterMapping.Value.SuffixedWithCluster && !clusterMapping.Value.Suffixed)
                                    {
                                        //Replacing string like Local Cluster
                                        newString = newString.ReplaceInsensitive(clusterMapping.Key + " Cluster", clusterMapping.Value.ClusterName); //Go to the Voyager Cluster and... 
                                    }
                                    else
                                    {
                                        //Replacing string like Artemis Tau
                                        newString = newString.ReplaceInsensitive(clusterMapping.Key + " Cluster", clusterMapping.Value.ClusterName + " cluster"); //Go to the Voyager Cluster and... 
                                    }

                                    newString = newString.Replace(clusterMapping.Key, clusterMapping.Value.ClusterName); //catch the rest of the items.
                                    Debug.WriteLine(newString);
                                }
                            }
                            else
                            {
                                if (newString.Contains(clusterMapping.Key, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    //Terribly inefficent

                                    if (clusterMapping.Value.SuffixedWithCluster || clusterMapping.Value.Suffixed)
                                    {
                                        //Local Cluster
                                        if (VanillaSuffixedClusterNames.Contains(clusterMapping.Key, StringComparer.InvariantCultureIgnoreCase))
                                        {
                                            newString = newString.ReplaceInsensitive(clusterMapping.Key, clusterMapping.Value.ClusterName); //Go to the Voyager Cluster and... 
                                        }
                                        else
                                        {
                                            newString = newString.ReplaceInsensitive(clusterMapping.Key + " Cluster", clusterMapping.Value.ClusterName); //Go to the Voyager Cluster and... 
                                        }
                                    }
                                    else
                                    {
                                        //Artemis Tau
                                        if (VanillaSuffixedClusterNames.Contains(clusterMapping.Key.ToLower(), StringComparer.InvariantCultureIgnoreCase))
                                        {
                                            newString = newString.ReplaceInsensitive(clusterMapping.Key, clusterMapping.Value.ClusterName + " cluster"); //Go to the Voyager Cluster and... 
                                        }
                                        else
                                        {
                                            newString = newString.ReplaceInsensitive(clusterMapping.Key + " Cluster", clusterMapping.Value.ClusterName + " cluster"); //Go to the Voyager Cluster and... 
                                        }
                                    }

                                    newString = newString.ReplaceInsensitive(clusterMapping.Key, clusterMapping.Value.ClusterName); //catch the rest of the items.
                                    Debug.WriteLine(newString);
                                }
                            }
                        }

                        if (originalString != newString)
                        {
                            tf.replaceString(sref.StringID, newString);
                        }
                    }
                }
            }
        }

        public static void DumpPlanetTexts(IExportEntry export, TalkFile tf)
        {
            Bio2DA planets = new Bio2DA(export);
            var planetInfos = new List<RandomizedPlanetInfo>();

            int nameRefcolumn = planets.GetColumnIndexByName("Name");
            int descColumn = planets.GetColumnIndexByName("Description");

            for (int i = 0; i < planets.RowNames.Count; i++)
            {
                RandomizedPlanetInfo rpi = new RandomizedPlanetInfo();
                rpi.PlanetName = tf.findDataById(planets[i, nameRefcolumn].GetIntValue());

                var descCell = planets[i, descColumn];
                if (descCell != null)
                {
                    rpi.PlanetDescription = tf.findDataById(planets[i, 7].GetIntValue());
                }

                rpi.RowID = i;
                planetInfos.Add(rpi);
            }

            using (StringWriter writer = new StringWriter())
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<RandomizedPlanetInfo>));
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;

                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);

                XmlWriter xmlWriter = XmlWriter.Create(writer, settings);
                xs.Serialize(xmlWriter, planetInfos, namespaces);

                File.WriteAllText(@"C:\users\mgame\desktop\planetinfo.xml", FormatXml(writer.ToString()));
            }
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
            Log.Information($"Randomizing opening crawl text");

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

        }

        private void RandomizeBioPawnSize(IExportEntry export, Random random, double amount)
        {
            Log.Information("Randomizing pawn size for " + export.UIndex + ": " + export.GetIndexedFullPath);
            var props = export.GetProperties();
            StructProperty sp = props.GetProp<StructProperty>("DrawScale3D");
            if (sp == null)
            {
                var structprops = ME1UnrealObjectInfo.getDefaultStructValue("Vector", true);
                sp = new StructProperty("Vector", structprops, "DrawScale3D", ME1UnrealObjectInfo.isImmutableStruct("Vector"));
                props.Add(sp);
            }

            if (sp != null)
            {
                //Debug.WriteLine("Randomizing morph face " + Path.GetFileName(export.FileRef.FileName) + " " + export.UIndex + " " + export.GetFullPath + " vPos");
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
            //            //Debug.WriteLine("Randomizing morph face " + Path.GetFileName(export.FileRef.FileName) + " " + export.UIndex + " " + export.GetFullPath + " offset");
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
                ME1Package package = new ME1Package(file);
                {
                    foreach (IExportEntry export in package.Exports)
                    {
                        if (export.ClassName == "BioMorphFace")
                        {
                            RandomizeBioMorphFace(export, random);
                        }
                    }
                }
                ModifiedFiles[package.FileName] = package.FileName;
                package.save();
            }
        }

        private void RandomizeMovementSpeeds(IExportEntry export, Random random)
        {
            mainWindow.CurrentOperationText = "Randomizing Movement Speeds";

            Bio2DA movementSpeed2DA = new Bio2DA(export);
            int[] colsToRandomize = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15, 16, 17, 18, 19 };
            for (int row = 0; row < movementSpeed2DA.RowNames.Count(); row++)
            {
                for (int i = 0; i < colsToRandomize.Count(); i++)
                {
                    //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(cluster2da[row, colsToRandomize[i]].Data, 0));
                    int randvalue = random.Next(10, 1200);
                    Console.WriteLine("Movement Speed Randomizer [" + row + "][" + colsToRandomize[i] + "] value is now " + randvalue);
                    movementSpeed2DA[row, colsToRandomize[i]].Data = BitConverter.GetBytes(randvalue);
                    movementSpeed2DA[row, colsToRandomize[i]].Type = Bio2DACell.Bio2DADataType.TYPE_INT;
                }
            }

            movementSpeed2DA.Write2DAToExport();
        }

        //private void RandomizeGalaxyMap(Random random)
        //{
        //    ME1Package engine = new ME1Package(Utilities.GetEngineFile());

        //    foreach (IExportEntry export in engine.Exports)
        //    {
        //        switch (export.ObjectName)
        //        {
        //            case "GalaxyMap_Cluster":
        //                //RandomizeClustersXY(export, random);
        //                break;
        //            case "GalaxyMap_System":
        //                //RandomizeSystems(export, random);
        //                break;
        //            case "GalaxyMap_Planet":
        //                //RandomizePlanets(export, random);
        //                break;
        //            case "Characters_StartingEquipment":
        //                //RandomizeStartingWeapons(export, random);
        //                break;
        //            case "Classes_ClassTalents":
        //                int shuffleattempts = 0;
        //                bool reattemptTalentShuffle = false;
        //                while (reattemptTalentShuffle)
        //                {
        //                    if (shuffleattempts > 0)
        //                    {
        //                        mainWindow.CurrentOperationText = "Randomizing Class Talents... Attempt #" + (shuffleattempts + 1)));
        //                    }
        //                    reattemptTalentShuffle = !RandomizeTalentLists(export, random); //true if shuffle is OK, false if it failed
        //                    shuffleattempts++;
        //                }
        //                break;
        //            case "LevelUp_ChallengeScalingVars":
        //                //RandomizeLevelUpChallenge(export, random);
        //                break;
        //            case "Items_ItemEffectLevels":
        //                RandomizeWeaponStats(export, random);
        //                break;
        //            case "Characters_Character":
        //                RandomizeCharacter(export, random);
        //                break;
        //        }
        //    }
        //    mainWindow.CurrentOperationText = "Finishing Galaxy Map Randomizing"));

        //    engine.save();
        //}



        private void RandomizeCharacter(IExportEntry export, Random random)
        {
            bool hasChanges = false;
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
            }
        }

        /// <summary>
        /// Randomizes the highest-level galaxy map view. Values are between 0 and 1 for columns 1 and 2 (X,Y).
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeClustersXY(IExportEntry export, Random random, List<TalkFile> Tlks)
        {
            mainWindow.CurrentOperationText = "Randomizing Galaxy Map - Clusters";

            Bio2DA cluster2da = new Bio2DA(export);
            int xColIndex = cluster2da.GetColumnIndexByName("X");
            int yColIndex = cluster2da.GetColumnIndexByName("Y");

            for (int row = 0; row < cluster2da.RowNames.Count(); row++)
            {
                //Randomize X,Y
                float randvalue = random.NextFloat(0, 1);
                cluster2da[row, xColIndex].Data = BitConverter.GetBytes(randvalue);
                randvalue = random.NextFloat(0, 1);
                cluster2da[row, yColIndex].Data = BitConverter.GetBytes(randvalue);
            }

            cluster2da.Write2DAToExport();
        }


        /// <summary>
        /// Randomizes the mid-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeSystems(IExportEntry export, Random random)
        {
            mainWindow.CurrentOperationText = "Randomizing Galaxy Map - Systems";

            Console.WriteLine("Randomizing Galaxy Map - Systems");
            Bio2DA system2da = new Bio2DA(export);
            int[] colsToRandomize = { 2, 3 }; //X,Y
            for (int row = 0; row < system2da.RowNames.Count(); row++)
            {
                for (int i = 0; i < colsToRandomize.Count(); i++)
                {
                    //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(system2da[row, colsToRandomize[i]].Data, 0));
                    float randvalue = random.NextFloat(0, 1);
                    Console.WriteLine("System Randomizer [" + row + "][" + colsToRandomize[i] + "] value is now " + randvalue);
                    system2da[row, colsToRandomize[i]].Data = BitConverter.GetBytes(randvalue);
                }

                //string value = system2da[row, 9].GetDisplayableValue();
                //Console.WriteLine("Scale: [" + row + "][9] value is " + value);
                float scalerandvalue = random.NextFloat(0.25, 2);
                Console.WriteLine("System Randomizer [" + row + "][9] value is now " + scalerandvalue);
                system2da[row, 9].Data = BitConverter.GetBytes(scalerandvalue);
                system2da[row, 9].Type = Bio2DACell.Bio2DADataType.TYPE_FLOAT;
            }

            system2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the planet-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizePlanets(IExportEntry export, Random random)
        {
            mainWindow.CurrentOperationText = "Randomizing Galaxy Map - Planets";

            Console.WriteLine("Randomizing Galaxy Map - Planets");
            Bio2DA planet2da = new Bio2DA(export);
            int[] colsToRandomize = { 2, 3 }; //X,Y
            for (int row = 0; row < planet2da.RowNames.Count(); row++)
            {
                for (int i = 0; i < planet2da.ColumnNames.Count(); i++)
                {
                    if (planet2da[row, i] != null && planet2da[row, i].Type == Bio2DACell.Bio2DADataType.TYPE_FLOAT)
                    {
                        Console.WriteLine("[" + row + "][" + i + "]  (" + planet2da.ColumnNames[i] + ") value is " + BitConverter.ToSingle(planet2da[row, i].Data, 0));
                        float randvalue = random.NextFloat(0, 1);
                        if (i == 11)
                        {
                            randvalue = random.NextFloat(2.5, 8.0);
                        }

                        Console.WriteLine("Planets Randomizer [" + row + "][" + i + "] (" + planet2da.ColumnNames[i] + ") value is now " + randvalue);
                        planet2da[row, i].Data = BitConverter.GetBytes(randvalue);
                    }
                }
            }

            planet2da.Write2DAToExport();
        }

        private void RandomizeOpeningSequence(Random random)
        {
            Log.Information($"Randomizing opening cutscene");

            ME1Package p = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\PRO\CIN\BIOA_GLO00_A_Opening_Flyby_CIN.SFM"));
            foreach (var ex in p.Exports)
            {
                if (ex.ClassName == "BioSunFlareComponent" || ex.ClassName == "BioSunFlareStreakComponent")
                {
                    var tint = ex.GetProperty<StructProperty>("FlareTint");
                    if (tint != null)
                    {
                        RandomizeTint(random, tint, false);
                        ex.WriteProperty(tint);
                    }
                }
                else if (ex.ClassName == "BioSunActor")
                {
                    var tint = ex.GetProperty<StructProperty>("SunTint");
                    if (tint != null)
                    {
                        RandomizeTint(random, tint, false);
                        ex.WriteProperty(tint);
                    }
                }
            }

            p.save();
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

        /// <summary>
        /// Randomizes the planet-level galaxy map view. 
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeWeaponStats(IExportEntry export, Random random)
        {
            mainWindow.CurrentOperationText = "Randomizing Item Levels (only partially implemented)";


            //Console.WriteLine("Randomizing Items - Item Effect Levels");
            Bio2DA itemeffectlevels2da = new Bio2DA(export);

            if (mainWindow.RANDSETTING_MOVEMENT_MAKO)
            {
                float makoCannonFiringRate = random.NextFloat(0.5, 4);
                float makoCannonForce = random.NextFloat(1000, 5000) + random.NextFloat(0, 2000);
                float makoCannonDamage = 120 / makoCannonFiringRate; //to same damage amount.
                float damageincrement = random.NextFloat(60, 90);
                for (int i = 0; i < 10; i++)
                {
                    itemeffectlevels2da[598, 4 + i].Data = BitConverter.GetBytes(makoCannonFiringRate); //RPS
                    itemeffectlevels2da[604, 4 + i].Data = BitConverter.GetBytes(makoCannonDamage + (i * damageincrement)); //Damage
                    itemeffectlevels2da[617, 4 + i].Data = BitConverter.GetBytes(makoCannonForce);
                }
            }
            //Randomize 
            //for (int row = 0; row < itemeffectlevels2da.RowNames.Count(); row++)
            //{
            //    Bio2DACell propertyCell = itemeffectlevels2da[row, 2];
            //    if (propertyCell != null)
            //    {
            //        int gameEffect = propertyCell.GetIntValue();
            //        switch (gameEffect)
            //        {
            //            case 15:
            //                //GE_Weap_Damage
            //                ItemEffectLevels.Randomize_GE_Weap_Damage(itemeffectlevels2da, row, random);
            //                break;
            //            case 17:
            //                //GE_Weap_RPS
            //                ItemEffectLevels.Randomize_GE_Weap_RPS(itemeffectlevels2da, row, random);
            //                break;
            //            case 447:
            //                //GE_Weap_Projectiles
            //                ItemEffectLevels.Randomize_GE_Weap_PhysicsForce(itemeffectlevels2da, row, random);
            //                break;
            //            case 1199:
            //                //GE_Weap_HeatPerShot
            //                ItemEffectLevels.Randomize_GE_Weap_HeatPerShot(itemeffectlevels2da, row, random);
            //                break;
            //            case 1201:
            //                //GE_Weap_HeatLossRate
            //                ItemEffectLevels.Randomize_GE_Weap_HeatLossRate(itemeffectlevels2da, row, random);
            //                break;
            //            case 1259:
            //                //GE_Weap_HeatLossRateOH
            //                ItemEffectLevels.Randomize_GE_Weap_HeatLossRateOH(itemeffectlevels2da, row, random);
            //                break;
            //        }
            //    }
            //}

            itemeffectlevels2da.Write2DAToExport();
        }



        /// <summary>
        /// Randomizes the 4 guns you get at the start of the game.
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeStartingWeapons(IExportEntry export, Random random)
        {
            /* These are the valid values, invalid ones are removed. They might include some ones not normally accessible but are fully functional
            324	Manf_Armax_Weap
            325	Manf_Devlon_Weap
            326	Manf_Elkoss_Weap
            327	Manf_HK_Weap
            412	Manf_Elanus_Weap
            436	Manf_Geth_Weap
            502	Manf_Spectre01_Weap
            503	Manf_Spectre02_Weap
            504	Manf_Spectre03_Weap
            525	Manf_Haliat_Weap
            582	Manf_Ariake_Weap
            583	Manf_Rosen_Weap
            584	Manf_Kassa_Weap
            598	Manf_Batarian_Weap
            599	Manf_Cerberus_Weap
            600	Manf_Jorman_Weap
            601	Manf_HKShadow_Weap*/

            mainWindow.CurrentOperationText = "Randomizing Starting Weapons";
            bool randomizeLevels = true; //will use better later
            Console.WriteLine("Randomizing Starting Weapons");
            Bio2DA startingitems2da = new Bio2DA(export);
            int[] rowsToRandomize = { 0, 1, 2, 3 };
            int[] manufacturers = { 324, 325, 326, 327, 412, 436, 502, 503, 504, 525, 582, 583, 584, 598, 599, 600, 601 };
            foreach (int row in rowsToRandomize)
            {
                //Columns:
                //0: Item Class - you must have 1 of each or game will crash when swapping to that slot and cutscenes will be super bugged
                //1: Item Sophistication (Level?)
                //2: Manufacturer
                if (randomizeLevels)
                {
                    startingitems2da[row, 2].Data = BitConverter.GetBytes(random.Next(1, 10));
                }

                startingitems2da[row, 2].Data = BitConverter.GetBytes(manufacturers[random.Next(manufacturers.Length)]);
            }

            startingitems2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the talent list
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private bool ShuffleClassTalentsAndPowers(IExportEntry export, Random random)
        {
            //List of talents... i think. Taken from talent_talenteffectlevels
            //int[] talentsarray = { 0, 7, 14, 15, 21, 28, 29, 30, 35, 42, 49, 50, 56, 57, 63, 64, 84, 86, 91, 93, 98, 99, 108, 109, 119, 122, 126, 128, 131, 132, 134, 137, 138, 141, 142, 145, 146, 149, 150, 153, 154, 157, 158, 163, 164, 165, 166, 167, 168, 169, 170, 171, 174, 175, 176, 177, 178, 180, 182, 184, 186, 188, 189, 190, 192, 193, 194, 195, 196, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 305, 306, 307, 310, 312, 313, 315, 317, 318, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332 };
            List<int> talentidstoassign = new List<int>();
            Bio2DA classtalents = new Bio2DA(export);
            mainWindow.CurrentOperationText = "Randomizing Class talents";

            //108 = Charm
            //109 = Intimidate
            //229 = Setup_Player -> Spectre Training
            //228 = Setup_Player_Squad
            int[] powersToNotReassign = { 108, 109 };
            var powersToReassignPlayerMaster = new List<int>();
            var powersToReassignSquadMaster = new List<int>();

            int isVisibleCol = classtalents.GetColumnIndexByName("IsVisible");

            //Get powers list
            for (int row = 0; row < classtalents.RowNames.Count(); row++)
            {
                var classId = classtalents[row, 0].GetIntValue();
                int talentId = classtalents[row, 1].GetIntValue();
                if (powersToNotReassign.Contains(talentId))
                {
                    continue;
                }

                var visibleInt = classtalents[row, isVisibleCol].GetIntValue();
                if (visibleInt != 0)
                {
                    if (classId == 10)
                    {
                        continue; //QA Cheat Class
                    }

                    if (classId < 6)
                    {
                        //Player class
                        powersToReassignPlayerMaster.Add(talentId);
                    }
                    else
                    {
                        //squadmate class
                        powersToReassignSquadMaster.Add(talentId);
                    }
                }
            }

            var playerPowersShuffled = TalentsShuffler.TalentShuffle(powersToReassignPlayerMaster, 6, 9, random);
            var squadPowersShuffled = TalentsShuffler.TalentShuffle(powersToReassignSquadMaster, 6, 9, random);

            //ASSIGN POWERS TO TABLE

            // >> Player
            for (int classId = 0; classId < 6; classId++)
            {
                int assignmentStartRow = (classId * 16) + 5; //16 powers per player, the first 5 of each are setup, the last 2 are charm/intimidate
                var talentList = playerPowersShuffled[classId];
                for (int i = 0; i < talentList.Count; i++)
                {
                    Log.Information("Talent randomizer [PLAYER - CLASSID " + classId + "]: Setting row " + (assignmentStartRow + i) + " to " + talentList[i]);
                    classtalents[assignmentStartRow + i, 1].Data = BitConverter.GetBytes(talentList[i]);
                }
            }

            // >> Squad
            int currentClassId = -1;
            List<int> currentList = null;
            for (int i = 0; i < classtalents.RowNames.Count; i++)
            {
                int rowClassId = classtalents[i, 0].GetIntValue();
                if (rowClassId == 10 || rowClassId < 6) continue; //skip supersoldier, player classes
                int currentTalentId = classtalents[i, 1].GetIntValue();
                if (rowClassId != currentClassId)
                {
                    currentList = squadPowersShuffled[0];
                    squadPowersShuffled.RemoveAt(0);
                    currentClassId = rowClassId;
                    //Krogan only has 2 non-assignable powers
                    if (currentClassId == 7)
                    {
                        i += 2;
                    }
                    else
                    {
                        i += 3;
                    }
                }

                int newPowerToAssign = currentList[0];
                currentList.RemoveAt(0);
                Log.Information("Talent randomizer [SQUAD - CLASSID " + currentClassId + "]: Setting row " + i + " to " + newPowerToAssign);
                classtalents[i, 1].Data = BitConverter.GetBytes(newPowerToAssign);
            }

            //UPDATE UNLOCKS (in reverse)
            int prereqTalentCol = classtalents.GetColumnIndexByName("PrereqTalent0");
            for (int row = classtalents.RowNames.Count() - 1; row > 0; row--)
            {
                var hasPrereq = classtalents[row, prereqTalentCol] != null;
                if (hasPrereq)
                {
                    classtalents[row, prereqTalentCol].Data = BitConverter.GetBytes(classtalents[row - 1, 1].GetIntValue()); //Talent ID of above row
                }
            }

            /*
            //REASSIGN POWERS
            int reassignmentAttemptsRemaining = 200;
            bool attemptingReassignment = true;
            while (attemptingReassignment)
            {
                reassignmentAttemptsRemaining--;
                if (reassignmentAttemptsRemaining < 0) { attemptingReassignment = false; }

                var playerReassignmentList = new List<int>();
                playerReassignmentList.AddRange(powersToReassignPlayerMaster);
                var squadReassignmentList = new List<int>();
                squadReassignmentList.AddRange(powersToReassignSquadMaster);

                playerReassignmentList.Shuffle(random);
                squadReassignmentList.Shuffle(random);

                int previousClassId = -1;
                for (int row = 0; row < classtalents.RowNames.Count(); row++)
                {
                    var classId = classtalents[row, 0].GetIntValue();
                    int existingTalentId = classtalents[row, 1].GetIntValue();
                    if (powersToNotReassign.Contains(existingTalentId)) { continue; }
                    var visibleInt = classtalents[row, isVisibleCol].GetIntValue();
                    if (visibleInt != 0)
                    {
                        if (classId == 10)
                        {
                            continue; //QA Cheat Class
                        }
                        if (classId < 6)
                        {
                            //Player class
                            int talentId = playerReassignmentList[0];
                            playerReassignmentList.RemoveAt(0);
                            classtalents[row, 1].SetData(talentId);
                        }
                        else
                        {

                            //squadmate class
                            int talentId = squadReassignmentList[0];
                            squadReassignmentList.RemoveAt(0);
                            classtalents[row, 1].SetData(talentId);
                        }
                    }
                }

                //Validate

                break;
            }

            if (reassignmentAttemptsRemaining < 0)
            {
                Debugger.Break();
                return false;
            }*/

            //Patch out Destroyer Tutorial as it may cause a softlock as it checks for kaidan throw
            ME1Package Pro10_08_Dsg = new ME1Package(Path.Combine(Utilities.GetGamePath(), "BioGame", "CookedPC", "Maps", "PRO", "DSG", "BIOA_PRO10_08_DSG.SFM"));
            IExportEntry GDInvulnerabilityCounter = (IExportEntry)Pro10_08_Dsg.getEntry(13521);
            var invulnCount = GDInvulnerabilityCounter.GetProperty<IntProperty>("IntValue");
            if (invulnCount != null && invulnCount.Value != 0)
            {
                invulnCount.Value = 0;
                GDInvulnerabilityCounter.WriteProperty(invulnCount);
                Pro10_08_Dsg.save();
            }


            //REASSIGN UNLOCK REQUIREMENTS
            Log.Information("Reassigned talents");
            classtalents.Write2DAToExport();

            return true;










            /*








            //OLD CODE
            for (int row = 0; row < classtalents.RowNames.Count(); row++)
            {
                int baseclassid = classtalents[row, 0].GetIntValue();
                if (baseclassid == 10)
                {
                    continue;
                }
                int isvisible = classtalents[row, 6].GetIntValue();
                if (isvisible == 0)
                {
                    continue;
                }
                talentidstoassign.Add(classtalents[row, 1].GetIntValue());
            }

            int i = 0;
            int spectretrainingid = 259;
            //while (i < 60)
            //{
            //    talentidstoassign.Add(spectretrainingid); //spectre training
            //    i++;
            //}

            //bool randomizeLevels = false; //will use better later
            Console.WriteLine("Randomizing Class talent list");

            int currentClassNum = -1;
            List<int> powersAssignedToThisClass = new List<int>();
            List<int> rowsNeedingPrereqReassignments = new List<int>(); //some powers require a prereq, this will ensure all powers are unlockable for this randomization
            List<int> talentidsNeedingReassignment = new List<int>(); //used only to filter out the list of bad choices, e.g. don't depend on self.
            List<int> powersAssignedAsPrereq = new List<int>(); //only assign 1 prereq to a power tree
            for (int row = 0; row < classtalents.RowNames.Count(); row++)
            {
                int baseclassid = classtalents[row, 0].GetIntValue();
                if (baseclassid == 10)
                {
                    continue;
                }
                if (currentClassNum != baseclassid)
                //this block only executes when we are changing classes in the list, so at this point
                //we have all of the info loaded about the class (e.g. all powers that have been assigned)
                {
                    if (powersAssignedToThisClass.Count() > 0)
                    {
                        List<int> possibleAllowedPrereqs = powersAssignedToThisClass.Except(talentidsNeedingReassignment).ToList();

                        //reassign prereqs now that we have a list of powers
                        foreach (int prereqrow in rowsNeedingPrereqReassignments)
                        {
                            int randomindex = -1;
                            int prereq = -1;
                            //while (true)
                            //{
                            randomindex = random.Next(possibleAllowedPrereqs.Count());
                            prereq = possibleAllowedPrereqs[randomindex];
                            //powersAssignedAsPrereq.Add(prereq);
                            classtalents[prereqrow, 8].Data = BitConverter.GetBytes(prereq);
                            classtalents[prereqrow, 9].Data = BitConverter.GetBytes(random.Next(5) + 4);
                            Console.WriteLine("Class " + baseclassid + "'s power on row " + row + " now depends on " + classtalents[prereqrow, 8].GetIntValue() + " at level " + classtalents[prereqrow, 9].GetIntValue());
                            //}
                        }
                    }
                    rowsNeedingPrereqReassignments.Clear();
                    powersAssignedToThisClass.Clear();
                    powersAssignedAsPrereq.Clear();
                    currentClassNum = baseclassid;

                }
                int isvisible = classtalents[row, 6].GetIntValue();
                if (isvisible == 0)
                {
                    continue;
                }

                if (classtalents[row, 8] != null)
                {
                    //prereq
                    rowsNeedingPrereqReassignments.Add(row);
                }

                if (classtalents[row, 1] != null) //talentid
                {
                    //Console.WriteLine("[" + row + "][" + 1 + "]  (" + classtalents.columnNames[1] + ") value originally is " + classtalents[row, 1].GetDisplayableValue());

                    int randomindex = -1;
                    int talentindex = -1;
                    int reassignattemptsremaining = 250; //attempt 250 random attempts.
                    while (true)
                    {
                        reassignattemptsremaining--;
                        if (reassignattemptsremaining <= 0)
                        {
                            //this isn't going to work.
                            return false;
                        }
                        randomindex = random.Next(talentidstoassign.Count());
                        talentindex = talentidstoassign[randomindex];
                        if (baseclassid <= 5 && talentindex == spectretrainingid)
                        {
                            continue;
                        }
                        if (!powersAssignedToThisClass.Contains(talentindex))
                        {
                            break;
                        }
                    }

                    talentidstoassign.RemoveAt(randomindex);
                    classtalents[row, 1].Data = BitConverter.GetBytes(talentindex);
                    powersAssignedToThisClass.Add(talentindex);
                    //Console.WriteLine("[" + row + "][" + 1 + "]  (" + classtalents.columnNames[1] + ") value is now " + classtalents[row, 1].GetDisplayableValue());
                }
                //if (randomizeLevels)
                //{
                //classtalents[row, 1].Data = BitConverter.GetBytes(random.Next(1, 12));
                //}
            }oi
            classtalents.Write2DAToExport();
            return true;*/
        }

        private static string[] TalentEffectsToRandomize_THROW = { "GE_TKThrow_CastingTime", "GE_TKThrow_Kickback", "GE_TKThrow_CooldownTime", "GE_TKThrow_ImpactRadius", "GE_TKThrow_Force" };
        private static string[] TalentEffectsToRandomize_LIFT = { "GE_TKLift_Force", "GE_TKLift_EffectDuration", "GE_TKLift_ImpactRadius", "GE_TKLift_CooldownTime" };

        public bool RunMapRandomizerPass
        {
            get => mainWindow.RANDSETTING_PAWN_MAPFACES
                   || mainWindow.RANDSETTING_MISC_MAPPAWNSIZES
                   || mainWindow.RANDSETTING_MISC_HAZARDS
                   || mainWindow.RANDSETTING_MISC_INTERPS
                   || mainWindow.RANDSETTING_MISC_INTERPPAWNS
                   || mainWindow.RANDSETTING_MISC_ENEMYAIDISTANCES
                   || mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION
                   || mainWindow.RANDSETTING_MISC_HEIGHTFOG
                   || mainWindow.RANDSETTING_PAWN_FACEFX
                   || mainWindow.RANDSETTING_WACK_SCOTTISH
                   || mainWindow.RANDSETTING_WACK_UWU
                   || mainWindow.RANDSETTING_PAWN_MATERIALCOLORS
                   || mainWindow.RANDSETTING_PAWN_BIOLOOKATDEFINITION
            ;
        }

        public bool RunMapRandomizerPassAllExports
        {
            get => mainWindow.RANDSETTING_PAWN_MAPFACES
                   || mainWindow.RANDSETTING_MISC_MAPPAWNSIZES
                   || mainWindow.RANDSETTING_MISC_HAZARDS
                   | mainWindow.RANDSETTING_MISC_HEIGHTFOG
                   || mainWindow.RANDSETTING_PAWN_FACEFX
                   || mainWindow.RANDSETTING_MISC_INTERPS
                   || mainWindow.RANDSETTING_WACK_SCOTTISH
                   || mainWindow.RANDSETTING_WACK_UWU
                   || mainWindow.RANDSETTING_PAWN_MATERIALCOLORS
                   || mainWindow.RANDSETTING_MISC_INTERPPAWNS
                   || mainWindow.RANDSETTING_PAWN_BIOLOOKATDEFINITION
            ;
        }

        private void RandomizeTalentEffectLevels(IExportEntry export, List<TalkFile> Tlks, Random random)
        {
            mainWindow.CurrentOperationText = "Randomizing Talent and Power stats";
            Bio2DA talentEffectLevels = new Bio2DA(export);
            const int gameEffectLabelCol = 18;

            for (int i = 0; i < talentEffectLevels.RowNames.Count; i++)
            {
                //for each row
                int talentId = talentEffectLevels[i, 0].GetIntValue();
                string rowEffect = talentEffectLevels[i, gameEffectLabelCol].GetDisplayableValue();

                if (talentId == 49 && TalentEffectsToRandomize_THROW.Contains(rowEffect))
                {
                    //THROW = 49 
                    List<int> boostedLevels = new List<int>();
                    boostedLevels.Add(7);
                    boostedLevels.Add(12);
                    switch (rowEffect)
                    {
                        case "GE_TKThrow_Force":
                            Debug.WriteLine("Randomizing GK_TKThrow_Force");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .45, boostedLevels, random, maxValue: 2500f);
                            continue;
                        case "GE_TKThrow_CastingTime":
                            Debug.WriteLine("GE_TKThrow_CastingTime");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .15, boostedLevels, random, directionsAllowed: RandomizationDirection.DownOnly, minValue: .3f);
                            continue;
                        case "GE_TKThrow_Kickback":
                            Debug.WriteLine("GE_TKThrow_Kickback");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .1, boostedLevels, random, minValue: 0.05f);
                            continue;
                        case "GE_TKThrow_CooldownTime":
                            Debug.WriteLine("GE_TKThrow_CooldownTime");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .22, boostedLevels, random, minValue: 5);
                            continue;
                        case "GE_TKThrow_ImpactRadius":
                            Debug.WriteLine("GE_TKThrow_ImpactRadius");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .4, boostedLevels, random, minValue: 100, maxValue: 1200f);
                            continue;
                    }
                }
                else if (talentId == 50 && TalentEffectsToRandomize_LIFT.Contains(rowEffect))
                {
                    List<int> boostedLevels = new List<int>();
                    boostedLevels.Add(7);
                    boostedLevels.Add(12);
                    switch (rowEffect)
                    {
                        //LIFT = 50
                        case "GE_TKLift_Force":
                            Debug.WriteLine("GE_TKLift_Force");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .25, boostedLevels, random, directionsAllowed: RandomizationDirection.UpOnly, minValue: .3f, maxValue: 3500f);
                            continue;
                        case "GE_TKLift_EffectDuration":
                            Debug.WriteLine("GE_TKLift_EffectDuration");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .1, boostedLevels, random, minValue: 1f, directionsAllowed: RandomizationDirection.DownOnly);
                            continue;
                        case "GE_TKLift_ImpactRadius":
                            Debug.WriteLine("GE_TKLift_ImpactRadius");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .22, boostedLevels, random, minValue: 5, maxValue: 4500);
                            continue;
                        case "GE_TKLift_CooldownTime":
                            Debug.WriteLine("GE_TKLift_CooldownTime");
                            TalentEffectLevels.RandomizeRow_FudgeEndpointsEvenDistribution(talentEffectLevels, i, 4, 12, .4, boostedLevels, random, minValue: 15f, maxValue: 60f);
                            continue;
                    }
                }
            }

            talentEffectLevels.Write2DAToExport();
            UpdateTalentStrings(export, Tlks);
        }

        private void UpdateTalentStrings(IExportEntry talentEffectLevelsExport, List<TalkFile> talkFiles)
        {
            IExportEntry talentGUIExport = talentEffectLevelsExport.FileRef.Exports.First(x => x.ObjectName == "Talent_TalentGUI");
            Bio2DA talentGUI2DA = new Bio2DA(talentGUIExport);
            Bio2DA talentEffectLevels2DA = new Bio2DA(talentEffectLevelsExport);
            const int columnPatternStart = 4;
            const int numColumnsPerLevelGui = 4;
            int statTableLevelStartColumn = 4; //Level 1 in TalentEffectLevels
            for (int i = 0; i < talentGUI2DA.RowNames.Count; i++)
            {
                if (int.TryParse(talentGUI2DA.RowNames[i], out int talentID))
                {
                    for (int level = 0; level < 12; level++)
                    {
                        switch (talentID)
                        {
                            case 49: //Throw
                                {
                                    var guitlkcolumn = columnPatternStart + 2 + (level * numColumnsPerLevelGui);
                                    int stringId = talentGUI2DA[i, guitlkcolumn].GetIntValue();

                                    string basicFormat = "%HEADER%\n\nThrows enemies away from the caster with a force of %TOKEN1% Newtons\n\nRadius: %TOKEN2% m\nTime To Cast: %TOKEN3% sec\nRecharge Time: %TOKEN4% sec\nAccuracy Cost: %TOKEN5%%";
                                    int token1row = 175; //Force
                                    int token2row = 173; //impact radius
                                    int token3row = 170; //Casting time
                                    int token4row = 172; //Cooldown
                                    int token5row = 171; //Accuracy cost

                                    string force = talentEffectLevels2DA[token1row, level + statTableLevelStartColumn].GetTlkDisplayableValue();
                                    string radius = talentEffectLevels2DA[token2row, level + statTableLevelStartColumn].GetTlkDisplayableValue(isMeters: true);
                                    string time = talentEffectLevels2DA[token3row, level + statTableLevelStartColumn].GetTlkDisplayableValue();
                                    string cooldown = talentEffectLevels2DA[token4row, level + statTableLevelStartColumn].GetTlkDisplayableValue();
                                    string cost = talentEffectLevels2DA[token5row, level + statTableLevelStartColumn].GetTlkDisplayableValue(isPercent: true);

                                    string header = "Throw";
                                    if (level > 6)
                                    {
                                        header = "Advanced Throw";
                                    }

                                    if (level >= 11)
                                    {
                                        header = "Master Throw";
                                    }

                                    string formatted = FormatString(basicFormat, header, force, radius, time, cooldown, cost);
                                    talkFiles.ForEach(x => x.replaceString(stringId, formatted));
                                }
                                break;
                            case 50: //Lift
                                {
                                    var guitlkcolumn = columnPatternStart + 2 + (level * numColumnsPerLevelGui);
                                    int stringId = talentGUI2DA[i, guitlkcolumn].GetIntValue();

                                    string basicFormat = "%HEADER%\n\nLifts everything within %TOKEN1% m of the target into the air, rendering enemies immobile and unable to attack. Drops them when it expires.\n\nDuration: %TOKEN2% sec\nRecharge Time: %TOKEN3% sec\nAccuracy Cost: %TOKEN4%%\nLift Force: %TOKEN5% Newtons";
                                    int token1row = 175; //impact radius
                                    int token2row = 189; //duration
                                    int token3row = 186; //recharge
                                    int token4row = 185; //accuacy cost
                                    int token5row = 190; //lift force

                                    string radius = talentEffectLevels2DA[token1row, level + statTableLevelStartColumn].GetTlkDisplayableValue();
                                    string duration = talentEffectLevels2DA[token2row, level + statTableLevelStartColumn].GetTlkDisplayableValue();
                                    string cooldown = talentEffectLevels2DA[token3row, level + statTableLevelStartColumn].GetTlkDisplayableValue();
                                    string cost = talentEffectLevels2DA[token4row, level + statTableLevelStartColumn].GetTlkDisplayableValue(isPercent: true);
                                    string force = talentEffectLevels2DA[token5row, level + statTableLevelStartColumn].GetTlkDisplayableValue();


                                    string header = "Lift";
                                    if (level > 6)
                                    {
                                        header = "Advanced Lift";
                                    }

                                    if (level >= 11)
                                    {
                                        header = "Master Lift";
                                    }

                                    string formatted = FormatString(basicFormat, header, radius, duration, cooldown, cost, force);
                                    talkFiles.ForEach(x => x.replaceString(stringId, formatted));
                                }
                                break;
                        }
                    }
                }
            }
        }

        private string FormatString(string unformattedStr, string header, params string[] tokens)
        {
            string retStr = unformattedStr;
            retStr = retStr.Replace("%HEADER%", header);
            for (int i = 1; i <= tokens.Length; i++)
            {
                string token = tokens[i - 1];
                retStr = retStr.Replace($"%TOKEN{i}%", token);
            }

            return retStr;
        }

        /// <summary>
        /// Randomizes the challenge scaling variables used by enemies
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeLevelUpChallenge(IExportEntry export, Random random)
        {
            mainWindow.CurrentOperationText = "Randomizing Class talents list";
            bool randomizeLevels = false; //will use better later
            Console.WriteLine("Randomizing Class talent list");
            Bio2DA challenge2da = new Bio2DA(export);



            for (int row = 0; row < challenge2da.RowNames.Count(); row++)
            {
                for (int col = 0; col < challenge2da.ColumnNames.Count(); col++)
                    if (challenge2da[row, col] != null)
                    {
                        Console.WriteLine("[" + row + "][" + col + "]  (" + challenge2da.ColumnNames[col] + ") value originally is " + challenge2da[row, 1].GetDisplayableValue());
                        //int randomindex = random.Next(talents.Count());
                        //int talentindex = talents[randomindex];
                        //talents.RemoveAt(randomindex);
                        float multiplier = random.NextFloat(0.7, 1.3);
                        if (col % 2 == 0)
                        {
                            //Fraction
                            Bio2DACell cell = challenge2da[row, col];
                            if (cell.Type == Bio2DACell.Bio2DADataType.TYPE_FLOAT)
                            {
                                challenge2da[row, col].Data = BitConverter.GetBytes(challenge2da[row, col].GetFloatValue() * multiplier);
                            }
                            else
                            {
                                challenge2da[row, col].Data = BitConverter.GetBytes(challenge2da[row, col].GetIntValue() * multiplier);
                                challenge2da[row, col].Type = Bio2DACell.Bio2DADataType.TYPE_FLOAT;
                            }
                        }
                        else
                        {
                            //Level Offset
                            challenge2da[row, col].Data = BitConverter.GetBytes((int)(challenge2da[row, col].GetIntValue() * multiplier));
                        }

                        Console.WriteLine("[" + row + "][" + col + "]  (" + challenge2da.ColumnNames[col] + ") value is now " + challenge2da[row, 1].GetDisplayableValue());
                    }

                if (randomizeLevels)
                {
                    challenge2da[row, 1].Data = BitConverter.GetBytes(random.Next(1, 12));
                }
            }

            challenge2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the character creator
        /// </summary>
        /// <param name="random">Random number generator</param>
        private void RandomizeCharacterCreator2DA(Random random, IExportEntry export)
        {
            mainWindow.CurrentOperationText = "Randomizing Charactor Creator";
            //if (headrandomizerclasses.Contains(export.ObjectName))
            //{
            //    RandomizePregeneratedHead(export, random);
            //    continue;
            //}
            Bio2DA export2da = new Bio2DA(export);
            bool hasChanges = false;
            for (int row = 0; row < export2da.RowNames.Count(); row++)
            {
                float numberedscalar = 0;
                for (int col = 0; col < export2da.ColumnNames.Count(); col++)
                {
                    Bio2DACell cell = export2da[row, col];

                    //Extent
                    if (export2da.ColumnNames[col] == "Extent" || export2da.ColumnNames[col] == "Rand_Extent")
                    {
                        float multiplier = random.NextFloat(0.5, 6);
                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value originally is " + export2da[row, col].GetDisplayableValue());

                        if (cell.Type == Bio2DACell.Bio2DADataType.TYPE_FLOAT)
                        {
                            cell.Data = BitConverter.GetBytes(cell.GetFloatValue() * multiplier);
                            hasChanges = true;
                        }
                        else
                        {
                            cell.Data = BitConverter.GetBytes(cell.GetIntValue() * multiplier);
                            cell.Type = Bio2DACell.Bio2DADataType.TYPE_FLOAT;
                            hasChanges = true;
                        }

                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value now is " + cell.GetDisplayableValue());
                        continue;
                    }

                    //Hair Scalars
                    if (export.ObjectName.Contains("MorphHair") && row > 0 && col >= 4 && col <= 8)
                    {

                        float scalarval = random.NextFloat(0, 1);
                        if (col == 5)
                        {
                            numberedscalar = scalarval;
                        }
                        else if (col > 5)
                        {
                            scalarval = numberedscalar;
                        }

                        // Bio2DACell cellX = cell;
                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value originally is " + cell.GetDisplayableValue());
                        cell.Data = BitConverter.GetBytes(scalarval);
                        cell.Type = Bio2DACell.Bio2DADataType.TYPE_FLOAT;
                        Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value now is " + cell.GetDisplayableValue());
                        hasChanges = true;
                        continue;
                    }

                    //Skin Tone
                    if (cell != null && cell.Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
                    {
                        if (export.ObjectName.Contains("Skin_Tone") && !mainWindow.RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE)
                        {
                            continue; //skip
                        }

                        string value = cell.GetDisplayableValue();
                        if (value.StartsWith("RGB("))
                        {
                            //Make new item
                            string rgbNewName = GetRandomColorRBGStr(random);
                            int newValue = export.FileRef.FindNameOrAdd(rgbNewName);
                            cell.Data = BitConverter.GetBytes((ulong)newValue); //name is 8 bytes
                            hasChanges = true;
                        }
                    }

                    string columnName = export2da.GetColumnNameByIndex(col);
                    if (columnName.Contains("Scalar") && cell != null && cell.Type != Bio2DACell.Bio2DADataType.TYPE_NAME)
                    {
                        float currentValue = float.Parse(cell.GetDisplayableValue());
                        cell.Data = BitConverter.GetBytes(currentValue * random.NextFloat(0.5, 2));
                        cell.Type = Bio2DACell.Bio2DADataType.TYPE_FLOAT;
                        hasChanges = true;
                    }

                    //if (export.ObjectName.Contains("Skin_Tone") && mainWindow.RANDSETTING_CHARACTER_CHARCREATOR_SKINTONE && row > 0 && col >= 1 && col <= 5)
                    //{
                    //    if (export.ObjectName.Contains("Female"))
                    //    {
                    //        if (col < 5)
                    //        {
                    //            //Females have one less column
                    //            string rgbNewName = GetRandomColorRBGStr(random);
                    //            int newValue = export.FileRef.FindNameOrAdd(rgbNewName);
                    //            export2da[row, col].Data = BitConverter.GetBytes((ulong)newValue); //name is 8 bytes
                    //            hasChanges = true;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        string rgbNewName = GetRandomColorRBGStr(random);
                    //        int newValue = export.FileRef.FindNameOrAdd(rgbNewName);
                    //        export2da[row, col].Data = BitConverter.GetBytes((ulong)newValue); //name is 8 bytes
                    //        hasChanges = true;
                    //    }
                    //}
                }
            }

            if (hasChanges)
            {
                export2da.Write2DAToExport();
            }


        }

        private void RandomizeCharacterCreatorSingular(Random random, List<TalkFile> Tlks)
        {
            //non-2da character creator changes.

            //Randomize look at targets
            ME1Package biog_uiworld = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\BIOG_UIWorld.sfm"));
            var bioInerts = biog_uiworld.Exports.Where(x => x.ClassName == "BioInert").ToList();
            foreach (IExportEntry ex in bioInerts)
            {
                RandomizeLocation(ex, random);
            }

            //Randomize face-zoom in
            //var zoomInOnFaceInterp = biog_uiworld.getUExport(385);
            //var eulerTrack = zoomInOnFaceInterp.GetProperty<StructProperty>("EulerTrack");
            //var points = eulerTrack?.GetProp<ArrayProperty<StructProperty>>("Points");
            //if (points != null)
            //{
            //    var s = points[2]; //end point
            //    var outVal = s.GetProp<StructProperty>("OutVal");
            //    if (outVal != null)
            //    {
            //        FloatProperty x = outVal.GetProp<FloatProperty>("X");
            //        //FloatProperty y = outVal.GetProp<FloatProperty>("Y");
            //        //FloatProperty z = outVal.GetProp<FloatProperty>("Z");
            //        x.Value = random.NextFloat(0, 360);
            //        //y.Value = y.Value * random.NextFloat(1 - amount * 3, 1 + amount * 3);
            //        //z.Value = z.Value * random.NextFloat(1 - amount * 3, 1 + amount * 3);
            //    }
            //}

            //zoomInOnFaceInterp.WriteProperty(eulerTrack);
            biog_uiworld.save();
            ModifiedFiles[biog_uiworld.FileName] = biog_uiworld.FileName;

            //Psych Profiles
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("psychprofiles.xml");

            XElement rootElement = XElement.Parse(fileContents);
            var childhoods = rootElement.Descendants("childhood").Where(x => x.Value != "").Select(x => (x.Attribute("name").Value, string.Join("\n", x.Value.Split('\n').Select(s => s.Trim())))).ToList();
            var reputations = rootElement.Descendants("reputation").Where(x => x.Value != "").Select(x => (x.Attribute("name").Value, string.Join("\n", x.Value.Split('\n').Select(s => s.Trim())))).ToList();

            childhoods.Shuffle(random);
            reputations.Shuffle(random);

            var backgroundTlkPairs = new List<(int nameId, int descriptionId)>();
            backgroundTlkPairs.Add((45477, 34931)); //Spacer
            backgroundTlkPairs.Add((45508, 34940)); //Earthborn
            backgroundTlkPairs.Add((45478, 34971)); //Colonist
            for (int i = 0; i < 3; i++)
            {
                foreach (var tlk in Tlks)
                {
                    tlk.replaceString(backgroundTlkPairs[i].nameId, childhoods[i].Item1);
                    tlk.replaceString(backgroundTlkPairs[i].descriptionId, childhoods[i].Item2);
                }
            }

            backgroundTlkPairs.Clear();
            backgroundTlkPairs.Add((45482, 34934)); //Sole Survivor
            backgroundTlkPairs.Add((45483, 34936)); //War Hero
            backgroundTlkPairs.Add((45484, 34938)); //Ruthless
            for (int i = 0; i < 3; i++)
            {
                foreach (var tlk in Tlks)
                {
                    tlk.replaceString(backgroundTlkPairs[i].nameId, reputations[i].Item1);
                    tlk.replaceString(backgroundTlkPairs[i].descriptionId, reputations[i].Item2);
                }
            }

        }

        private void RandomizeLocation(IExportEntry e, Random random)
        {
            SetLocation(e, random.NextFloat(-100000, 100000), random.NextFloat(-100000, 100000), random.NextFloat(-100000, 100000));
        }

        public static void SetLocation(IExportEntry export, float x, float y, float z)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, x, y, z);
            export.WriteProperty(prop);
        }

        public static Point3D GetLocation(IExportEntry export)
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

        private void RandomizeBioMorphFace(IExportEntry export, Random random, double amount = 0.3)
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
                        //Debug.WriteLine("Randomizing morph face " + Path.GetFileName(export.FileRef.FileName) + " " + export.UIndex + " " + export.GetFullPath + " offset");
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
                        //Debug.WriteLine("Randomizing morph face " + Path.GetFileName(export.FileRef.FileName) + " " + export.UIndex + " " + export.GetFullPath + " vPos");
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
        }

        private void RandomizePregeneratedHead(IExportEntry export, Random random)
        {
            int[] floatSliderIndexesToRandomize = { 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 19, 20, 21, 22, 24, 25, 26, 27, 29, 30 };
            Dictionary<int, int> columnMaxDictionary = new Dictionary<int, int>();
            columnMaxDictionary[1] = 7; //basehead
            columnMaxDictionary[2] = 6; //skintone
            columnMaxDictionary[3] = 3; //archetype
            columnMaxDictionary[4] = 14; //scar
            columnMaxDictionary[12] = 8; //eyeshape
            columnMaxDictionary[18] = 13; //iriscolor +1
            columnMaxDictionary[23] = 10; //mouthshape
            columnMaxDictionary[28] = 13; //noseshape
            columnMaxDictionary[31] = 14; //beard
            columnMaxDictionary[32] = 7; //brows +1
            columnMaxDictionary[33] = 9; //hair
            columnMaxDictionary[34] = 8; //haircolor
            columnMaxDictionary[35] = 8; //facialhaircolor

            if (export.ObjectName.Contains("Female"))
            {
                floatSliderIndexesToRandomize = new int[] { 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 19, 20, 21, 22, 24, 25, 26, 27, 29, 30 };
                columnMaxDictionary.Clear();
                //there are female specific values that must be used
                columnMaxDictionary[1] = 10; //basehead
                columnMaxDictionary[2] = 6; //skintone
                columnMaxDictionary[3] = 3; //archetype
                columnMaxDictionary[4] = 11; //scar
                columnMaxDictionary[12] = 10; //eyeshape
                columnMaxDictionary[18] = 13; //iriscolor +1
                columnMaxDictionary[23] = 10; //mouthshape
                columnMaxDictionary[28] = 12; //noseshape
                columnMaxDictionary[31] = 8; //haircolor
                columnMaxDictionary[32] = 10; //hair
                columnMaxDictionary[33] = 17; //brows
                columnMaxDictionary[34] = 7; //browcolor
                columnMaxDictionary[35] = 7; //blush
                columnMaxDictionary[36] = 8; //lipcolor
                columnMaxDictionary[37] = 8; //eyemakeupcolor

            }

            Bio2DA export2da = new Bio2DA(export);
            for (int row = 0; row < export2da.RowNames.Count(); row++)
            {
                foreach (int col in floatSliderIndexesToRandomize)
                {
                    export2da[row, col].Data = BitConverter.GetBytes(random.NextFloat(0, 2));
                }
            }

            for (int row = 0; row < export2da.RowNames.Count(); row++)
            {
                foreach (KeyValuePair<int, int> entry in columnMaxDictionary)
                {
                    int col = entry.Key;
                    Console.WriteLine("[" + row + "][" + col + "]  (" + export2da.ColumnNames[col] + ") value originally is " + export2da[row, col].GetDisplayableValue());

                    export2da[row, col].Data = BitConverter.GetBytes(random.Next(0, entry.Value) + 1);
                    export2da[row, col].Type = Bio2DACell.Bio2DADataType.TYPE_INT;
                    Console.WriteLine("Character Creator Randomizer [" + row + "][" + col + "] (" + export2da.ColumnNames[col] + ") value is now " + export2da[row, col].GetDisplayableValue());

                }
            }

            Console.WriteLine("Writing export " + export.ObjectName);
            export2da.Write2DAToExport();
        }

        /// <summary>
        /// Randomizes the the music table
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeMusic(IExportEntry export, Random random, string randomizingtext = null)
        {
            if (randomizingtext == null)
            {
                randomizingtext = "Randomizing Music";
            }

            mainWindow.CurrentOperationText = randomizingtext;
            Console.WriteLine(randomizingtext);
            Bio2DA music2da = new Bio2DA(export);
            List<byte[]> names = new List<byte[]>();
            int[] colsToRandomize = { 0, 5, 6, 7, 8, 9, 10, 11, 12 };
            for (int row = 0; row < music2da.RowNames.Count(); row++)
            {
                foreach (int col in colsToRandomize)
                {
                    if (music2da[row, col] != null && music2da[row, col].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
                    {
                        if (!music2da[row, col].GetDisplayableValue().StartsWith("music"))
                        {
                            continue;
                        }

                        names.Add(music2da[row, col].Data.TypedClone());
                    }
                }
            }

            for (int row = 0; row < music2da.RowNames.Count(); row++)
            {
                foreach (int col in colsToRandomize)
                {
                    if (music2da[row, col] != null && music2da[row, col].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
                    {
                        if (!music2da[row, col].GetDisplayableValue().StartsWith("music"))
                        {
                            continue;
                        }

                        Log.Information("[" + row + "][" + col + "]  (" + music2da.ColumnNames[col] + ") value originally is " + music2da[row, col].GetDisplayableValue());
                        int r = random.Next(names.Count);
                        byte[] pnr = names[r];
                        names.RemoveAt(r);
                        music2da[row, col].Data = pnr;
                        Log.Information("Music Randomizer [" + row + "][" + col + "] (" + music2da.ColumnNames[col] + ") value is now " + music2da[row, col].GetDisplayableValue());

                    }
                }
            }

            music2da.Write2DAToExport();
        }

        private string[] aiTypes =
        {
            "BioAI_Krogan", "BioAI_Assault", "BioAI_AssaultDrone", "BioAI_Charge", "BioAI_Commander", "BioAI_Destroyer", "BioAI_Drone",
            "BioAI_GunShip", "BioAI_HumanoidMinion", "BioAI_Juggernaut", "BioAI_Melee", "BioAI_Mercenary", "BioAI_Rachnii", "BioAI_Sniper"
        };

        private Dictionary<string, string> systemNameMapping;
        private Dictionary<string, SuffixedCluster> clusterNameMapping;
        private Dictionary<string, string> planetNameMapping;
        private List<char> scottishVowelOrdering;
        private List<char> upperScottishVowelOrdering;
        private List<string> VanillaSuffixedClusterNames;

        private void RandomizeAINames(ME1Package pacakge, Random random)
        {
            bool forcedCharge = random.Next(8) == 0;
            for (int i = 0; i < pacakge.NameCount; i++)
            {
                NameReference n = pacakge.getNameEntry(i);

                //Todo: Test Saren Hopper AI. Might be interesting to force him to change types.
                if (aiTypes.Contains(n.Name))
                {
                    string newAiType = forcedCharge ? "BioAI_Charge" : aiTypes[random.Next(aiTypes.Length)];
                    Log.Information("Reassigning AI type in " + Path.GetFileName(pacakge.FileName) + ", " + n + " -> " + newAiType);
                    pacakge.replaceName(i, newAiType);
                    pacakge.ShouldSave = true;
                }
            }

        }

        /// <summary>
        /// Randomizes the sounds and music in GUIs. This is shared between two tables as it contains the same indexing and table format
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        private void RandomizeGUISounds(IExportEntry export, Random random, string randomizingtext = null, string requiredprefix = null)
        {
            if (randomizingtext == null)
            {
                randomizingtext = "Randomizing UI - Sounds";
            }

            mainWindow.CurrentOperationText = randomizingtext;
            Console.WriteLine(randomizingtext);
            Bio2DA guisounds2da = new Bio2DA(export);
            int[] colsToRandomize = { 0 }; //sound name
            List<byte[]> names = new List<byte[]>();

            if (requiredprefix != "music")
            {

                for (int row = 0; row < guisounds2da.RowNames.Count(); row++)
                {
                    if (guisounds2da[row, 0] != null && guisounds2da[row, 0].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
                    {
                        if (requiredprefix != null && !guisounds2da[row, 0].GetDisplayableValue().StartsWith(requiredprefix))
                        {
                            continue;
                        }

                        names.Add(guisounds2da[row, 0].Data.TypedClone());
                    }
                }
            }
            else
            {
                for (int n = 0; n < export.FileRef.Names.Count; n++)
                {
                    string name = export.FileRef.Names[n];
                    if (name.StartsWith("music.mus"))
                    {
                        Int64 nameval = n;
                        names.Add(BitConverter.GetBytes(nameval));
                    }
                }
            }

            for (int row = 0; row < guisounds2da.RowNames.Count(); row++)
            {
                if (guisounds2da[row, 0] != null && guisounds2da[row, 0].Type == Bio2DACell.Bio2DADataType.TYPE_NAME)
                {
                    if (requiredprefix != null && !guisounds2da[row, 0].GetDisplayableValue().StartsWith(requiredprefix))
                    {
                        continue;
                    }

                    Thread.Sleep(20);
                    Console.WriteLine("[" + row + "][" + 0 + "]  (" + guisounds2da.ColumnNames[0] + ") value originally is " + guisounds2da[row, 0].GetDisplayableValue());
                    int r = random.Next(names.Count);
                    byte[] pnr = names[r];
                    names.RemoveAt(r);
                    guisounds2da[row, 0].Data = pnr;
                    Console.WriteLine("Sounds - GUI Sounds Randomizer [" + row + "][" + 0 + "] (" + guisounds2da.ColumnNames[0] + ") value is now " + guisounds2da[row, 0].GetDisplayableValue());

                }
            }

            guisounds2da.Write2DAToExport();
        }

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

        public class SuffixedCluster
        {
            public string ClusterName;
            /// <summary>
            /// string ends with "cluster"
            /// </summary>
            public bool SuffixedWithCluster;
            /// <summary>
            /// string is suffixed with cluster-style word. Doesn't need cluster appended.
            /// </summary>
            public bool Suffixed;

            public SuffixedCluster(string clusterName, bool suffixed)
            {
                this.ClusterName = clusterName;
                this.Suffixed = suffixed;
                this.SuffixedWithCluster = clusterName.EndsWith("cluster", StringComparison.InvariantCultureIgnoreCase);
            }

            public override string ToString()
            {
                return $"SuffixedCluster ({ClusterName})";
            }
        }

        [DebuggerDisplay("KeeperLocation at {Position.X},{Position.Y},{Position.Z}, Rot {Yaw} in {STAFile}")]
        public class KeeperLocation
        {
            public Vector3 Position;
            public int Yaw;
            public string STAFile;
        }
        [DebuggerDisplay("KeeperDefintion | Teleport UIndex: {KismetTeleportBoolUIndex} BioPawn UIndex:{PawnExportUIndex} | {STAFile}")]

        public class KeeperDefinition
        {
            public string STAFile;
            public int PawnExportUIndex;
            public int KismetTeleportBoolUIndex;
        }
    }
}
#endif