using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK.ME2ME3;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Serilog;

namespace Randomizer.Randomizers.Game1.GalaxyMap
{
    class GalaxyMapRandomizer
    {
        private Dictionary<string, string> systemNameMapping;
        private Dictionary<string, SuffixedCluster> clusterNameMapping;
        private Dictionary<string, string> planetNameMapping;
        private List<char> scottishVowelOrdering;
        private List<char> upperScottishVowelOrdering;
        private List<string> VanillaSuffixedClusterNames;

        private static readonly int[] GalaxyMapImageIdsThatArePlotReserved = { 1, 7, 8, 116, 117, 118, 119, 120, 121, 122, 123, 124 }; //Plot or Sol planets
        private static readonly int[] GalaxyMapImageIdsThatAreAsteroidReserved = { 70 }; //Asteroids
        private static readonly int[] GalaxyMapImageIdsThatAreFactoryReserved = { 6 }; //Asteroids
        private static readonly int[] GalaxyMapImageIdsThatAreMSVReserved = { 76, 79, 82, 85 }; //MSV Ships
        private static readonly int[] GalaxyMapImageIdsToNeverRandomize = { 127, 128 }; //no idea what these are

        private void RandomizePlanetImages(GameTarget target, RandomizationOption option, Dictionary<int, RandomizedPlanetInfo> planetsRowToRPIMapping, Bio2DA planets2DA, IMEPackage galaxyMapImagesPackage, ExportEntry galaxyMapImagesUi, Dictionary<string, List<string>> galaxyMapGroupResources)
        {
            option.CurrentOperation = "Updating galaxy map images";
            option.ProgressIndeterminate = false;

            var galaxyMapImages2DA = new Bio2DA(galaxyMapImagesUi);
            var ui2DAPackage = galaxyMapImagesUi.FileRef;

            //Get all exports for images
            string swfObjectNamePrefix = Path.GetFileNameWithoutExtension(ui2DAPackage.FilePath).Equals("BIOG_2DA_Vegas_UI_X", StringComparison.InvariantCultureIgnoreCase) ? "prc2_galmap" : "galMap";
            var mapImageExports = galaxyMapImagesPackage.Exports.Where(x => x.ObjectName.Name.StartsWith(swfObjectNamePrefix)).ToList(); //Original galaxy map images
            var planet2daRowsWithImages = new List<int>();
            int imageIndexCol = planets2DA.GetColumnIndexByName("ImageIndex");
            int descriptionCol = planets2DA.GetColumnIndexByName("Description");
            int nextAddedImageIndex = int.Parse(galaxyMapImages2DA.RowNames.Last()) + 1000; //we increment from here. //+1000 to ensure we don't have overlap between DLC
                                                                                            //int nextGalaxyMap2DAImageRowIndex = 0; //THIS IS C# BASED

            //var mappedRPIs = planetsRowToRPIMapping.Values.ToList();

            option.ProgressMax = planets2DA.RowCount;
            option.ProgressMax = planets2DA.RowCount;

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
                option.ProgressValue = i;
                if (planets2DA[i, descriptionCol] == null || planets2DA[i, descriptionCol].IntValue == 0)
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
                        MERLog.Warning("WARNING: NO IMAGEGROUP FOR GROUP " + assignedRPI.ImageGroup);
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
                            imageIndexCell = new Bio2DACell(++nextAddedImageIndex);
                            planets2DA[i, imageIndexCol] = imageIndexCell;
                        }
                        else if (imageIndexCell.IntValue < 0 || assignedImageIndexes.Contains(imageIndexCell.IntValue))
                        {
                            //Generating new image value
                            imageIndexCell.DisplayableValue = (++nextAddedImageIndex).ToString();
                        }
                        else
                        {
                            didntIncrementNextImageIndex = true;
                        }

                        assignedImageIndexes.Add(imageIndexCell.IntValue);

                        var newImageSwf = newImagePool;
                        ExportEntry matchingExport = null;

                        int uiTableRowName = imageIndexCell.IntValue;
                        int rowIndex = galaxyMapImages2DA.GetRowIndexByName(uiTableRowName.ToString());
                        if (rowIndex == -1)
                        {
                            //Create export and row first
                            matchingExport = mapImageExports[0].Clone();
                            string objectName = "galMapMER" + nextAddedImageIndex;
                            matchingExport.ObjectName = objectName;
                            matchingExport.indexValue = 0;
                            galaxyMapImagesPackage.AddExport(matchingExport);
                            MERLog.Information("Cloning galaxy map SWF export. New export " + matchingExport.InstancedFullPath);
                            int newRowIndex = galaxyMapImages2DA.AddRow(nextAddedImageIndex.ToString());
                            //int nameIndex = ui2DAPackage.FindNameOrAdd(Path.GetFileNameWithoutExtension(galaxyMapImagesPackage.FileName) + "." + objectName);
                            galaxyMapImages2DA[newRowIndex, "imageResource"].NameValue = Path.GetFileNameWithoutExtension(galaxyMapImagesPackage.FilePath) + "." + objectName;
                            //new Bio2DACell(Bio2DACell.Bio2DADataType.TYPE_NAME, BitConverter.GetBytes((long)nameIndex));
                            if (didntIncrementNextImageIndex)
                            {
                                Debug.WriteLine("Unused image? Row specified but doesn't exist in this table. Repointing to new image row for image value " + nextAddedImageIndex);
                                imageIndexCell.DisplayableValue = nextAddedImageIndex.ToString(); //assign the image cell to point to this export row
                                nextAddedImageIndex++; //next image index was not incremented, but we had to create a new export anyways. Increment the counter.
                            }
                        }
                        else
                        {
                            var swfImageExportObjectName = galaxyMapImages2DA[rowIndex, "imageResource"].NameValue.Name;
                            //get object name of export inside of GUI_SF_GalaxyMap
                            swfImageExportObjectName = swfImageExportObjectName.Substring(swfImageExportObjectName.IndexOf('.') + 1); //TODO: Need to deal with name instances for Pinnacle Station DLC. Because it's too hard for them to type a new name.
                                                                                                                                      //Fetch export
                            matchingExport = mapImageExports.FirstOrDefault(x => x.ObjectName == swfImageExportObjectName);
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
            MERFileSystem.SavePackage(ui2DAPackage);
            MERFileSystem.SavePackage(galaxyMapImagesPackage);
            planets2DA.Write2DAToExport(); //should save later
        }


        private void GalaxyMapValidationPass(GameTarget target, RandomizationOption option, Dictionary<int, RandomizedPlanetInfo> rowRPIMapping, Bio2DA planets2DA, Bio2DA galaxyMapImages2DA, IMEPackage galaxyMapImagesPackage)
        {
            option.CurrentOperation = "Running tests on galaxy map images";
            option.ProgressIndeterminate = false;
            option.ProgressMax = rowRPIMapping.Keys.Count;
            option.ProgressValue = 0;

            foreach (int i in rowRPIMapping.Keys)
            {
                option.ProgressValue++;

                //For every row in planets 2DA table
                if (planets2DA[i, "Description"] != null && planets2DA[i, "Description"].DisplayableValue != "-1")
                {
                    int imageRowReference = planets2DA[i, "ImageIndex"].IntValue;
                    if (imageRowReference == -1) continue; //We don't have enough images yet to pass this hurdle
                                                           //Use this value to find value in UI table
                    int rowIndex = galaxyMapImages2DA.GetRowIndexByName(imageRowReference.ToString());
                    string exportName = galaxyMapImages2DA[rowIndex, 0].NameValue;
                    exportName = exportName.Substring(exportName.LastIndexOf('.') + 1);
                    //Use this value to find the export in GUI_SF file
                    var export = galaxyMapImagesPackage.Exports.FirstOrDefault(x => x.ObjectName == exportName);
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

        private void ReplaceSWFFromResource(ExportEntry exp, string swfResourcePath)
        {
            Debug.WriteLine($"Replacing {Path.GetFileName(exp.FileRef.FilePath)} {exp.UIndex} {exp.ObjectName} SWF with {swfResourcePath}");
            var bytes = MERUtilities.GetEmbeddedStaticFilesBinaryFile(swfResourcePath, true);
            var props = exp.GetProperties();

            var rawData = props.GetProp<ArrayProperty<ByteProperty>>("Data");
            //Write SWF data
            rawData.Values = bytes.Select(b => new ByteProperty(b)).ToList();

            //Write SWF metadata
            props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(swfResourcePath), "SourceFilePath"));
            props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
            exp.WriteProperties(props);
        }


        private void UpdateGalaxyMapReferencesForTLKs(GameTarget target, RandomizationOption option, bool updateProgressbar, bool basegame)
        {
            int currentTlkIndex = 0;
            currentTlkIndex++;
            var gameTlks = TLKBuilder.GetOfficialTLKs().ToList();
            foreach (var tf in gameTlks)
            {
                int current = 0;
                if (updateProgressbar)
                {
                    option.CurrentOperation = $"Applying entropy to galaxy map [{currentTlkIndex}/{gameTlks.Count()}]";
                    option.ProgressMax = tf.StringRefs.Count;
                    option.ProgressIndeterminate = false;
                }

                if (basegame) //this will only be fired on basegame tlk's since they're the only ones that update the progerssbar.
                {

                    //text fixes.
                    //TODO: CHECK IF ORIGINAL VALUE IS BIOWARE - IF IT ISN'T ITS ALREADY BEEN UPDATED.
                    string testStr = tf.FindDataById(179694);
                    if (testStr == "")
                    {
                        tf.ReplaceString(179694, "Head to the Armstrong Nebula to investigate what the geth are up to."); //Remove cluster after Nebula to ensure the text pass works without cluster cluster.

                    }
                    testStr = tf.FindDataById(156006);
                    testStr = tf.FindDataById(136011);

                    tf.ReplaceString(156006, "Go to the Newton System in the Kepler Verge and find the one remaining scientist assigned to the secret project.");
                    tf.ReplaceString(136011, "The geth have begun setting up a number of small outposts in the Armstrong Nebula of the Skyllian Verge. You must eliminate these outposts before the incursion becomes a full-scale invasion.");
                }

                //This is inefficient but not much I can do it about it.
                foreach (var sref in tf.StringRefs)
                {
                    current++;
                    if (TLKBuilder.UpdatedTlkStrings.Contains(sref.StringID)) continue; //This string has already been updated and should not be modified.
                    if (updateProgressbar)
                    {
                        option.ProgressValue = current;
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
                            tf.ReplaceString(sref.StringID, newString);
                        }
                    }
                }
            }
        }


        private void RandomizePlanetText(GameTarget target, RandomizationOption option, Bio2DA planets2DA, int tableRow, string dlcName, Dictionary<int, (SuffixedCluster clustername, string systemname)> systemIdToSystemNameMap,
        List<RandomizedPlanetInfo> allMapRandomizationInfo, Dictionary<int, RandomizedPlanetInfo> rowRPIMap, List<RandomizedPlanetInfo> planetInfos, List<RandomizedPlanetInfo> msvInfos, List<RandomizedPlanetInfo> asteroidInfos,
        List<RandomizedPlanetInfo> asteroidBeltInfos, bool mustBePlayable = false)
        {
            //option.ProgressValue = i;
            int systemId = planets2DA[tableRow, 1].IntValue;
            (SuffixedCluster clusterName, string systemName) systemClusterName = systemIdToSystemNameMap[systemId];

            Bio2DACell descriptionRefCell = planets2DA[tableRow, "Description"];
            Bio2DACell mapCell = planets2DA[tableRow, "Map"];
            bool isMap = mapCell != null && mapCell.IntValue > 0;

            int descriptionReference = descriptionRefCell?.IntValue ?? 0;


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
                if (option.HasSubOptionSelected(RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION_PLOTPLANET) && rpi.PlanetName2 != null)
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

                //var landableMapID = planets2DA[i, planets2DA.GetColumnIndexByName("Map")].IntValue;
                int planetNameTlkId = planets2DA[tableRow, "Name"].IntValue;

                //Replace planet description here, as it won't be replaced in the overall pass
                foreach (var tf in TLKBuilder.GetOfficialTLKs())
                {
                    //Debug.WriteLine("Setting planet name on row index (not rowname!) " + i + " to " + newPlanetName);
                    string originalPlanetName = tf.FindDataById(planetNameTlkId);

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
                        TLKBuilder.AddUpdatedTlk(descriptionReference);
                        //Log.Information($"New planet: {newPlanetName}");
                        //if (descriptionReference == 138077)
                        //{
                        //    Debug.WriteLine($"------------SUBSTITUTING----{tf.export.ObjectName}------------------");
                        //    Debug.WriteLine($"{originalPlanetName} -> {newPlanetName}");
                        //    Debug.WriteLine("New description:\n" + description);
                        //    Debug.WriteLine("----------------------------------");
                        //    Debugger.Break(); //Xawin
                        //}
                        tf.ReplaceString(descriptionReference, description);

                        if (rpi.ButtonLabel != null)
                        {
                            Bio2DACell actionLabelCell = planets2DA[tableRow, "ButtonLabel"];
                            if (actionLabelCell != null)
                            {
                                var currentTlkId = actionLabelCell.IntValue;
                                if (tf.FindDataById(currentTlkId) != rpi.ButtonLabel)
                                {
                                    //Value is different
                                    //try to find existing value first
                                    var tlkref = tf.FindIdByData(rpi.ButtonLabel);
                                    if (tlkref >= 0)
                                    {
                                        //We found result
                                        actionLabelCell.IntValue = tlkref;
                                    }
                                    else
                                    {
                                        // Did not find a result. Add a new string
                                        int newID = TLKBuilder.GetNewTLKID(); // WE PROBABLY NEED TO CHANGE THIS FOR DLC SYSTEM IN GAME 1.......
                                        if (newID == -1) Debugger.Break(); //hopefully we never see this, but if user runs it enough, i guess you could.
                                        tf.ReplaceString(newID, rpi.ButtonLabel);
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
                            var newTlkValue = tf.FindIdByData(rpi.PlanetName);
                            if (newTlkValue == -1)
                            {
                                //Doesn't exist
                                int newId = TLKBuilder.GetNewTLKID(); // WE PROBABLY NEED TO CHANGE THIS FOR DLC SYSTEM IN GAME 1.......
                                tf.ReplaceString(newId, rpi.PlanetName);
                                planets2DA[tableRow, "Name"].IntValue = newId;
                                MERLog.Information("Assigned asteroid new TLK ID: " + newId);
                            }
                            else
                            {
                                //Exists - repoint to that TLK value
                                planets2DA[tableRow, "Name"].IntValue = newTlkValue;
                                MERLog.Information("Repointed asteroid new existing string ID: " + newTlkValue);
                            }
                        }
                    }
                }
            }
            else
            {
                MERLog.Error("No randomization data for galaxy map planet 2da, row id " + tableRow);
            }
        }

        public const string RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION_PLOTPLANET = "RANDSETTING_GALAXYMAP_PLANETNAMEDESCRIPTION_PLOTPLANET";

        private void BuildSystemClusterMap(GameTarget target, RandomizationOption option, Bio2DA systems2DA, Dictionary<int, (SuffixedCluster clustername, string systemname)> systemIdToSystemNameMap, Dictionary<int, SuffixedCluster> clusterIdToClusterNameMap, List<string> shuffledSystemNames)
        {
            int nameColumnSystems = systems2DA.GetColumnIndexByName("Name");
            int clusterColumnSystems = systems2DA.GetColumnIndexByName("Cluster");
            for (int i = 0; i < systems2DA.RowNames.Count; i++)
            {

                string newSystemName = shuffledSystemNames[0];
                shuffledSystemNames.RemoveAt(0);
                int tlkRef = systems2DA[i, nameColumnSystems].IntValue;
                int clusterTableRow = systems2DA[i, clusterColumnSystems].IntValue;


                string oldSystemName = "";
                foreach (var tf in TLKBuilder.GetOfficialTLKs())
                {
                    oldSystemName = tf.FindDataById(tlkRef);
                    if (oldSystemName != "No Data")
                    {
                        //tf.ReplaceString(tlkRef, newSystemName);
                        systemNameMapping[oldSystemName] = newSystemName;
                        systemIdToSystemNameMap[int.Parse(systems2DA.RowNames[i])] = (clusterIdToClusterNameMap[clusterTableRow], newSystemName);
                        break;
                    }
                }
            }
        }

        private void RandomizePlanetNameDescriptions(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            option.CurrentOperation = "Applying entropy to galaxy map";
            string fileContents = MERUtilities.GetStaticTextFile("planetinfo.xml");

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

            fileContents = MERUtilities.GetStaticTextFile("galaxymapclusters.xml");
            rootElement = XElement.Parse(fileContents);
            var suffixedClusterNames = rootElement.Elements("suffixedclustername").Select(x => x.Value).ToList(); //Used for assignments
            var suffixedClusterNamesForPreviousLookup = rootElement.Elements("suffixedclustername").Select(x => x.Value).ToList(); //Used to lookup previous assignments 
            VanillaSuffixedClusterNames = rootElement.Elements("originalsuffixedname").Select(x => x.Value).ToList();
            var nonSuffixedClusterNames = rootElement.Elements("nonsuffixedclustername").Select(x => x.Value).ToList();
            suffixedClusterNames.Shuffle();
            nonSuffixedClusterNames.Shuffle();

            fileContents = MERUtilities.GetStaticTextFile("galaxymapsystems.xml");
            rootElement = XElement.Parse(fileContents);
            var shuffledSystemNames = rootElement.Elements("systemname").Select(x => x.Value).ToList();
            shuffledSystemNames.Shuffle();


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

            msvInfos.Shuffle();
            asteroidInfos.Shuffle();
            planetInfos.Shuffle();

            List<int> rowsToNotRandomlyReassign = new List<int>();

            ExportEntry systemsExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_System");
            ExportEntry clustersExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_Cluster");
            ExportEntry areaMapExport = export.FileRef.Exports.First(x => x.ObjectName == "AreaMap_AreaMap");
            ExportEntry plotPlanetExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_PlotPlanet");
            ExportEntry mapExport = export.FileRef.Exports.First(x => x.ObjectName == "GalaxyMap_Map");

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
                int tlkRef = clusters2DA[i, nameColumnClusters].IntValue;

                string oldClusterName = "";
                oldClusterName = TLKBuilder.TLKLookupByLang(tlkRef, MELocalization.INT);
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

            //SYSTEMS
            //Used for resolving %SYSTEMNAME% in planet description and localization VO text
            Dictionary<int, (SuffixedCluster clustername, string systemname)> systemIdToSystemNameMap = new Dictionary<int, (SuffixedCluster clustername, string systemname)>();


            BuildSystemClusterMap(target, option, systems2DA, systemIdToSystemNameMap, clusterIdToClusterNameMap, shuffledSystemNames);


            //BRING DOWN THE SKY (UNC) SYSTEM===================
            if (File.Exists(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_UNC_GalaxyMap_X")))
            {
                var bdtsGalaxyMapX = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_UNC_GalaxyMap_X"));
                Bio2DA bdtsGalMapX_Systems2DA = new Bio2DA(bdtsGalaxyMapX.GetUExport(6));
                var bdtstalkfile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"DLC_UNC_GlobalTlk"));
                var bdtsTlks = bdtstalkfile.Exports.Where(x => x.ClassName == "BioTlkFile").Select(x => new ME1TalkFile(x)).ToList();
                BuildSystemClusterMap(target, option, bdtsGalMapX_Systems2DA, systemIdToSystemNameMap, clusterIdToClusterNameMap, shuffledSystemNames);
            }
            //END BRING DOWN THE SKY=====================

            //PLANETS
            //option.ProgressValue = 0;37
            //option.ProgressMax = planets2DA.RowCount;
            //option.ProgressIndeterminate = false;

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
                // NEEDS UPDATED
                galaxyMapGroupResources[groupname] = resourceItems.Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.galaxymapimages." + groupname)).ToList();
                galaxyMapGroupResources[groupname].Shuffle();
            }

            //BASEGAME===================================
            var rowRPIMap = new Dictionary<int, RandomizedPlanetInfo>();
            var AlreadyAssignedMustBePlayableRows = new List<int>();
            for (int i = 0; i < planets2DA.RowCount; i++)
            {
                Bio2DACell mapCell = planets2DA[i, "Map"];
                if (mapCell.IntValue > 0)
                {
                    //must be playable
                    RandomizePlanetText(target, option, planets2DA, i, "", systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMap, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos, mustBePlayable: true);
                    AlreadyAssignedMustBePlayableRows.Add(i);
                }
            }

            for (int i = 0; i < planets2DA.RowCount; i++)
            {
                if (AlreadyAssignedMustBePlayableRows.Contains(i)) continue;
                RandomizePlanetText(target, option, planets2DA, i, "", systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMap, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos);
            }
            var galaxyMapImagesBasegame = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"GUI_SF_GalaxyMap")); //lol demiurge, what were you doing?
            var ui2DAPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_UI_X")); //lol demiurge, what were you doing?
            ExportEntry galaxyMapImages2DAExport = ui2DAPackage.GetUExport(8);
            RandomizePlanetImages(target, option, rowRPIMap, planets2DA, galaxyMapImagesBasegame, galaxyMapImages2DAExport, galaxyMapGroupResources);
            UpdateGalaxyMapReferencesForTLKs(target, option, true, true); //Update TLKs.
            planets2DA.Write2DAToExport();
            //END BASEGAME===============================

            //BRING DOWN THE SKY (UNC)===================
            if (File.Exists(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_UNC_GalaxyMap_X")))
            {
                var bdtsplanets = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_UNC_GalaxyMap_X"));
                var bdtstalkfile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"DLC_UNC_GlobalTlk"));

                Bio2DA bdtsGalMapX_Planets2DA = new Bio2DA(bdtsplanets.GetUExport(3));
                var rowRPIMapBdts = new Dictionary<int, RandomizedPlanetInfo>();
                var bdtsTlks = bdtstalkfile.Exports.Where(x => x.ClassName == "BioTlkFile").Select(x => new ME1TalkFile(x)).ToList();

                for (int i = 0; i < bdtsGalMapX_Planets2DA.RowCount; i++)
                {
                    RandomizePlanetText(target, option, bdtsGalMapX_Planets2DA, i, "UNC", systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMapBdts, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos);
                }
                var galaxyMapImagesBdts = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"GUI_SF_DLC_GalaxyMap"));
                ui2DAPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_UNC_UI_X"));
                galaxyMapImages2DAExport = ui2DAPackage.GetUExport(2);
                RandomizePlanetImages(target, option, rowRPIMapBdts, bdtsGalMapX_Planets2DA, galaxyMapImagesBdts, galaxyMapImages2DAExport, galaxyMapGroupResources);
                MERFileSystem.SavePackage(bdtsplanets);
                UpdateGalaxyMapReferencesForTLKs(target, option, true, false); //Update TLKs
                //bdtsTlks.ForEach(x => x.saveToExport(x.E)); // TODO: REIMPLEMENT
                MERFileSystem.SavePackage(bdtstalkfile);
                GalaxyMapValidationPass(target, option, rowRPIMapBdts, bdtsGalMapX_Planets2DA, new Bio2DA(galaxyMapImages2DAExport), galaxyMapImagesBdts);
            }
            //END BRING DOWN THE SKY=====================

            //PINNACE STATION (VEGAS)====================
            if (File.Exists(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_Vegas_GalaxyMap_X")))
            {
                var vegasplanets = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_Vegas_GalaxyMap_X"));
                var vegastalkfile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"DLC_Vegas_GlobalTlk"));

                Bio2DA vegasGalMapX_Planets2DA = new Bio2DA(vegasplanets.GetUExport(2));
                var rowRPIMapVegas = new Dictionary<int, RandomizedPlanetInfo>();
                var vegasTlks = vegastalkfile.Exports.Where(x => x.ClassName == "BioTlkFile").Select(x => new ME1TalkFile(x)).ToList();

                for (int i = 0; i < vegasGalMapX_Planets2DA.RowCount; i++)
                {
                    RandomizePlanetText(target, option, vegasGalMapX_Planets2DA, i, "Vegas", systemIdToSystemNameMap, allMapRandomizationInfo, rowRPIMapVegas, planetInfos, msvInfos, asteroidInfos, asteroidBeltInfos);
                }

                var galaxyMapImagesVegas = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"GUI_SF_PRC2_GalaxyMap"));
                ui2DAPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BIOG_2DA_Vegas_UI_X"));
                galaxyMapImages2DAExport = ui2DAPackage.GetUExport(2);
                RandomizePlanetImages(target, option, rowRPIMapVegas, vegasGalMapX_Planets2DA, galaxyMapImagesVegas, galaxyMapImages2DAExport, galaxyMapGroupResources);
                MERFileSystem.SavePackage(vegasplanets);
                UpdateGalaxyMapReferencesForTLKs(target, option, true, false); //Update TLKs.
                //vegasTlks.ForEach(x => x.saveToExport()); //todo: renable
                MERFileSystem.SavePackage(vegastalkfile);
            }
            //END PINNACLE STATION=======================
        }
    }
}
