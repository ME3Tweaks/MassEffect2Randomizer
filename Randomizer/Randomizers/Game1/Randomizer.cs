#if __GAME1__
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Numerics;
//using System.Reflection;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Xml;
//using System.Xml.Linq;
//using System.Xml.Serialization;
//using LegendaryExplorerCore.Packages;
//using LegendaryExplorerCore.TLK.ME1;
//using LegendaryExplorerCore.TLK.ME2ME3;
//using LegendaryExplorerCore.Unreal;
//using LegendaryExplorerCore.Unreal.BinaryConverters;
//using LegendaryExplorerCore.Unreal.ObjectInfo;
//using Serilog;
//using StringComparison = System.StringComparison;

//namespace Randomizer.Randomizers.Game1
//{
//    public class Randomizer
//    {

        /*
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
            foreach (ExportEntry exp in globalTLK.Exports)
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
            ExportEntry talentEffectLevels = null;

            foreach (ExportEntry export in engine.Exports)
            {
                
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

                        break;
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
            foreach (ExportEntry export in entrymenu.Exports)
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
                            foreach (ExportEntry exp in package.Exports)
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
                                        ExportEntry possibleHazSequence = exp.FileRef.getUExport(seqRef.Value);
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
                                    if (mainWindow.RANDSETTING_MISC_MAPPAWNSIZES && ThreadSafeRandom.Next(4) == 0)
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
                                else if (mainWindow.RANDSETTING_MISC_INTERPS && exp.ClassName == "InterpTrackMove" /* && ThreadSafeRandom.Next(4) == 0*//*)
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

            }

            if (mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION)
            {
                // REMOVE FROM ME1R
                Log.Information("Apply galaxy map background transparency fix");
                ME1Package p = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\NOR\DSG\BIOA_NOR10_03_DSG.SFM"));
                p.getUExport(1655).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.PC_GalaxyMap_BGFix_1655.bin");
                p.save();
                ModifiedFiles[p.FileName] = p.FileName;
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
                    if (ThreadSafeRandom.Next(5) == 0)
                    {
                        sb.Append('w'); // append another w 20% of the time
                        if (ThreadSafeRandom.Next(8) == 0)
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
                    if (ThreadSafeRandom.Next(2) == 0)
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
                return faces[ThreadSafeRandom.Next(faces.Count)];
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
                    if (s.Contains('?') && r.properties.Contains("question") && ThreadSafeRandom.Next(10) == 0)
                    {
                        r.EarnPoint();
                    }

                    //exclamation check
                    if (s.Contains('!') && r.properties.Contains("exclamation") && ThreadSafeRandom.Next(10) == 0)
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

                                for (int e = 0; e < ThreadSafeRandom.Next(8); e++)
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
                                        if (words[w].Contains(k, StringComparison.OrdinalIgnoreCase) && ThreadSafeRandom.Next(5) == 0)
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
                    int rnd = ThreadSafeRandom.Next(4);
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

        */

// I think this is done better with ME2R code
/*
        private void RandomizeInterpPawns(ExportEntry export, Random random)
        {
            var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");

            List<ObjectProperty> pawnsToShuffle = new List<ObjectProperty>();
            var playerRefs = new List<ExportEntry>();
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
                            if (linkedObjName == "BioPawn" && linkedObjectEntry is ExportEntry bioPawnExport)
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
*/
        
/*
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
/*
                meshRef.WriteProperty(scale);
            }
        }
        /*
        private void RandomizeInterpTrackMove(ExportEntry export, Random random, double amount)
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


        */

        

        


        //static readonly List<char> englishVowels = new List<char>(new[] { 'a', 'e', 'i', 'o', 'u' });
        //static readonly List<char> upperCaseVowels = new List<char>(new[] { 'A', 'E', 'I', 'O', 'U' });

        ///// <summary>
        ///// Swap the vowels around
        ///// </summary>
        ///// <param name="Tlks"></param>
        //private void MakeTextPossiblyScottish(List<TalkFile> Tlks, Random random, bool updateProgressbar)
        //{
        //    Log.Information("Randomizing vowels");
        //    if (scottishVowelOrdering == null)
        //    {
        //        scottishVowelOrdering = new List<char>(new char[] { 'a', 'e', 'i', 'o', 'u' });
        //        scottishVowelOrdering.Shuffle(random);
        //        upperScottishVowelOrdering = new List<char>();
        //        foreach (var c in scottishVowelOrdering)
        //        {
        //            upperScottishVowelOrdering.Add(char.ToUpper(c, CultureInfo.InvariantCulture));
        //        }
        //    }

        //    int currentTlkIndex = 0;
        //    foreach (TalkFile tf in Tlks)
        //    {
        //        currentTlkIndex++;
        //        int max = tf.StringRefs.Count();
        //        int current = 0;
        //        if (updateProgressbar)
        //        {
        //            mainWindow.CurrentOperationText = $"Randomizing vowels [{currentTlkIndex}/{Tlks.Count()}]";
        //            mainWindow.ProgressBar_Bottom_Max = tf.StringRefs.Length;
        //            mainWindow.ProgressBarIndeterminate = false;
        //        }

        //        foreach (var sref in tf.StringRefs)
        //        {
        //            current++;
        //            if (tf.TlksIdsToNotUpdate.Contains(sref.StringID)) continue; //This string has already been updated and should not be modified.
        //            if (updateProgressbar)
        //            {
        //                mainWindow.CurrentProgressValue = current;
        //            }

        //            if (!string.IsNullOrWhiteSpace(sref.Data))
        //            {
        //                string originalString = sref.Data;
        //                if (originalString.Length == 1)
        //                {
        //                    continue; //Don't modify I, A
        //                }

        //                string[] words = originalString.Split(' ');
        //                for (int j = 0; j < words.Length; j++)
        //                {
        //                    string word = words[j];
        //                    if (word.Length == 1)
        //                    {
        //                        continue; //Don't modify I, A
        //                    }

        //                    if (word.StartsWith("%") || word.StartsWith("<CUSTOM"))
        //                    {
        //                        Debug.WriteLine($"Skipping {word}");
        //                        continue; // Don't modify tokens
        //                    }

        //                    char[] newStringAsChars = word.ToArray();
        //                    for (int i = 0; i < word.Length; i++)
        //                    {
        //                        //Undercase
        //                        var vowelIndex = englishVowels.IndexOf(word[i]);
        //                        if (vowelIndex >= 0)
        //                        {
        //                            if (i + 1 < word.Length && englishVowels.Contains(word[i + 1]))
        //                            {
        //                                continue; //don't modify dual vowel first letters.
        //                            }
        //                            else
        //                            {
        //                                newStringAsChars[i] = scottishVowelOrdering[vowelIndex];
        //                            }
        //                        }
        //                        else
        //                        {
        //                            var upperVowelIndex = upperCaseVowels.IndexOf(word[i]);
        //                            if (upperVowelIndex >= 0)
        //                            {
        //                                if (i + 1 < word.Length && upperCaseVowels.Contains(word[i + 1]))
        //                                {
        //                                    continue; //don't modify dual vowel first letters.
        //                                }
        //                                else
        //                                {
        //                                    newStringAsChars[i] = upperScottishVowelOrdering[upperVowelIndex];
        //                                }
        //                            }
        //                        }
        //                    }

        //                    words[j] = new string(newStringAsChars);
        //                }

        //                string rebuiltStr = string.Join(" ", words);
        //                tf.replaceString(sref.StringID, rebuiltStr);
        //            }
        //        }
        //    }
        //}








        /// <summary>
        /// Randomizes bio morph faces in a specified file. Will check if file exists first
        /// </summary>
        /// <param name="file"></param>
        /// <param name="random"></param>
        //private void RandomizeBioMorphFaceWrapper(string file, Random random)
        //{
        //    if (File.Exists(file))
        //    {
        //        ME1Package package = new ME1Package(file);
        //        {
        //            foreach (ExportEntry export in package.Exports)
        //            {
        //                if (export.ClassName == "BioMorphFace")
        //                {
        //                    RandomizeBioMorphFace(export, random);
        //                }
        //            }
        //        }
        //        ModifiedFiles[package.FileName] = package.FileName;
        //        package.save();
        //    }
        //}

        
 /*
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

            randomOrderChooser[0].Value = ThreadSafeRandom.NextFloat(0, totalTintValue);
            totalTintValue -= randomOrderChooser[0].Value;

            randomOrderChooser[1].Value = ThreadSafeRandom.NextFloat(0, totalTintValue);
            totalTintValue -= randomOrderChooser[1].Value;

            randomOrderChooser[2].Value = totalTintValue;
            if (randomizeAlpha)
            {
                a.Value = ThreadSafeRandom.NextFloat(0, 1);
            }
        }
 */

        ///// <summary>
        ///// Randomizes the planet-level galaxy map view. 
        ///// </summary>
        ///// <param name="export">2DA Export</param>
        ///// <param name="random">Random number generator</param>
        //private void RandomizeWeaponStats(ExportEntry export, Random random)
        //{
        //    mainWindow.CurrentOperationText = "Randomizing Item Levels (only partially implemented)";


        //    //Console.WriteLine("Randomizing Items - Item Effect Levels");
        //    Bio2DA itemeffectlevels2da = new Bio2DA(export);

            
        //    //Randomize 
        //    //for (int row = 0; row < itemeffectlevels2da.RowNames.Count(); row++)
        //    //{
        //    //    Bio2DACell propertyCell = itemeffectlevels2da[row, 2];
        //    //    if (propertyCell != null)
        //    //    {
        //    //        int gameEffect = propertyCell.GetIntValue();
        //    //        switch (gameEffect)
        //    //        {
        //    //            case 15:
        //    //                //GE_Weap_Damage
        //    //                ItemEffectLevels.Randomize_GE_Weap_Damage(itemeffectlevels2da, row, random);
        //    //                break;
        //    //            case 17:
        //    //                //GE_Weap_RPS
        //    //                ItemEffectLevels.Randomize_GE_Weap_RPS(itemeffectlevels2da, row, random);
        //    //                break;
        //    //            case 447:
        //    //                //GE_Weap_Projectiles
        //    //                ItemEffectLevels.Randomize_GE_Weap_PhysicsForce(itemeffectlevels2da, row, random);
        //    //                break;
        //    //            case 1199:
        //    //                //GE_Weap_HeatPerShot
        //    //                ItemEffectLevels.Randomize_GE_Weap_HeatPerShot(itemeffectlevels2da, row, random);
        //    //                break;
        //    //            case 1201:
        //    //                //GE_Weap_HeatLossRate
        //    //                ItemEffectLevels.Randomize_GE_Weap_HeatLossRate(itemeffectlevels2da, row, random);
        //    //                break;
        //    //            case 1259:
        //    //                //GE_Weap_HeatLossRateOH
        //    //                ItemEffectLevels.Randomize_GE_Weap_HeatLossRateOH(itemeffectlevels2da, row, random);
        //    //                break;
        //    //        }
        //    //    }
        //    //}

        //    itemeffectlevels2da.Write2DAToExport();
        //}



      

        
        //public bool RunMapRandomizerPass
        //{
        //    get => mainWindow.RANDSETTING_PAWN_MAPFACES
        //           || mainWindow.RANDSETTING_MISC_MAPPAWNSIZES
        //           || mainWindow.RANDSETTING_MISC_HAZARDS
        //           || mainWindow.RANDSETTING_MISC_INTERPS
        //           || mainWindow.RANDSETTING_MISC_INTERPPAWNS
        //           || mainWindow.RANDSETTING_MISC_ENEMYAIDISTANCES
        //           || mainWindow.RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION
        //           || mainWindow.RANDSETTING_MISC_HEIGHTFOG
        //           || mainWindow.RANDSETTING_PAWN_FACEFX
        //           || mainWindow.RANDSETTING_WACK_SCOTTISH
        //           || mainWindow.RANDSETTING_WACK_UWU
        //           || mainWindow.RANDSETTING_PAWN_MATERIALCOLORS
        //           || mainWindow.RANDSETTING_PAWN_BIOLOOKATDEFINITION
        //    ;
        //}

        //public bool RunMapRandomizerPassAllExports
        //{
        //    get => mainWindow.RANDSETTING_PAWN_MAPFACES
        //           || mainWindow.RANDSETTING_MISC_MAPPAWNSIZES
        //           || mainWindow.RANDSETTING_MISC_HAZARDS
        //           | mainWindow.RANDSETTING_MISC_HEIGHTFOG
        //           || mainWindow.RANDSETTING_PAWN_FACEFX
        //           || mainWindow.RANDSETTING_MISC_INTERPS
        //           || mainWindow.RANDSETTING_WACK_SCOTTISH
        //           || mainWindow.RANDSETTING_WACK_UWU
        //           || mainWindow.RANDSETTING_PAWN_MATERIALCOLORS
        //           || mainWindow.RANDSETTING_MISC_INTERPPAWNS
        //           || mainWindow.RANDSETTING_PAWN_BIOLOOKATDEFINITION
        //    ;
        //}




        //private void RandomizeLocation(ExportEntry e, Random random)
        //{
        //    SetLocation(e, ThreadSafeRandom.NextFloat(-100000, 100000), ThreadSafeRandom.NextFloat(-100000, 100000), ThreadSafeRandom.NextFloat(-100000, 100000));
        //}


        //public static Point3D GetLocation(ExportEntry export)
        //{
        //    float x = 0, y = 0, z = int.MinValue;
        //    var prop = export.GetProperty<StructProperty>("location");
        //    if (prop != null)
        //    {
        //        foreach (var locprop in prop.Properties)
        //        {
        //            switch (locprop)
        //            {
        //                case FloatProperty fltProp when fltProp.Name == "X":
        //                    x = fltProp;
        //                    break;
        //                case FloatProperty fltProp when fltProp.Name == "Y":
        //                    y = fltProp;
        //                    break;
        //                case FloatProperty fltProp when fltProp.Name == "Z":
        //                    z = fltProp;
        //                    break;
        //            }
        //        }

        //        return new Point3D(x, y, z);
        //    }

        //    return null;
        //}

        //public class Point3D
        //{
        //    public double X { get; set; }
        //    public double Y { get; set; }
        //    public double Z { get; set; }

        //    public Point3D()
        //    {

        //    }

        //    public Point3D(double X, double Y, double Z)
        //    {
        //        this.X = X;
        //        this.Y = Y;
        //        this.Z = Z;
        //    }

        //    public double getDistanceToOtherPoint(Point3D other)
        //    {
        //        double deltaX = X - other.X;
        //        double deltaY = Y - other.Y;
        //        double deltaZ = Z - other.Z;

        //        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        //    }

        //    public override string ToString()
        //    {
        //        return $"{X},{Y},{Z}";
        //    }
        //}

        /*
        
        private string[] aiTypes =
        {
            "BioAI_Krogan", "BioAI_Assault", "BioAI_AssaultDrone", "BioAI_Charge", "BioAI_Commander", "BioAI_Destroyer", "BioAI_Drone",
            "BioAI_GunShip", "BioAI_HumanoidMinion", "BioAI_Juggernaut", "BioAI_Melee", "BioAI_Mercenary", "BioAI_Rachnii", "BioAI_Sniper"
        };



        private void RandomizeAINames(ME1Package pacakge, Random random)
        {
            bool forcedCharge = ThreadSafeRandom.Next(8) == 0;
            for (int i = 0; i < pacakge.NameCount; i++)
            {
                NameReference n = pacakge.getNameEntry(i);

                //Todo: Test Saren Hopper AI. Might be interesting to force him to change types.
                if (aiTypes.Contains(n.Name))
                {
                    string newAiType = forcedCharge ? "BioAI_Charge" : aiTypes[ThreadSafeRandom.Next(aiTypes.Length)];
                    Log.Information("Reassigning AI type in " + Path.GetFileName(pacakge.FileName) + ", " + n + " -> " + newAiType);
                    pacakge.replaceName(i, newAiType);
                    pacakge.ShouldSave = true;
                }
            }
        }/*


        //static float NextFloat(Random random)
        //{
        //    double mantissa = (ThreadSafeRandom.NextDouble() * 2.0) - 1.0;
        //    double exponent = Math.Pow(2.0, ThreadSafeRandom.Next(-3, 20));
        //    return (float)(mantissa * exponent);
        //}

        static string GetRandomColorRBGStr(Random random)
        {
            return $"RGB({ThreadSafeRandom.Next(255)},{ThreadSafeRandom.Next(255)},{ThreadSafeRandom.Next(255)})";
        }

        


    }
}*/
#endif