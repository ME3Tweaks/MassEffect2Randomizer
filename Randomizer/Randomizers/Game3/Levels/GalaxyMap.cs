using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Textures;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;
using Octokit;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Shared.Classes;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game3.Levels
{
    internal class GalaxyMap
    {
        private static string GALAXY_MAP_RANDOMIZING_MESSAGE = "Applying entropy to galaxy map";
        public static bool InstallGalaxyMapRewrite(GameTarget target, RandomizationOption option)
        {

            // Open the galaxy map
            var galaxyMapPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_Nor_203aGalaxyMap.pcc"));

            // 203CIC also has it - depending on if player loads into map or walks into area first determines which one will actually be used
            // We don't want to force ours or we might break EGM or other mods.
            var galaxyMapPackage2 = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_Nor_203CIC.pcc"));

            RandomizeGalaxyMapClusters(target, option, galaxyMapPackage, galaxyMapPackage2);
            RandomizeGalaxyMapSystems(target, option, galaxyMapPackage, galaxyMapPackage2);
            RandomizeGalaxyMapPlanets(target, option, galaxyMapPackage, galaxyMapPackage2);

            MERFileSystem.SavePackage(galaxyMapPackage);
            MERFileSystem.SavePackage(galaxyMapPackage2);
            return true;
        }

        private static void RandomizeGalaxyMapSystems(GameTarget target, RandomizationOption option, IMEPackage galaxyMapPackage, IMEPackage galaxyMapPackage2)
        {
            string fileContents = MEREmbedded.GetEmbeddedTextAsset("galaxymapsystems.xml", true);
            XElement rootElement = XElement.Parse(fileContents);

            var systemnames = rootElement.Elements("systemname").Select(x => x.Value).ToList(); //Used for assignments
            systemnames.Shuffle();

            var galaxyMap = galaxyMapPackage.FindExport("biog_galaxymap.GalaxyMap");
            // Enumerate the galaxy map
            var clusters = galaxyMap.GetProperty<ArrayProperty<ObjectProperty>>("Children");
            foreach (var clusterRef in clusters.Where(x => x.Value != 0))
            {
                var cluster = (ExportEntry)clusterRef.ResolveToEntry(galaxyMapPackage);
                var systems = cluster.GetProperty<ArrayProperty<ObjectProperty>>("Children");
                if (systems == null)
                    continue;
                foreach (var systemRef in systems.Where(x => x.Value != 0))
                {
                    var system = (ExportEntry)systemRef.ResolveToEntry(galaxyMapPackage);
                    var dn = system.GetProperty<StringRefProperty>("DisplayName");
                    if (dn != null)
                    {
                        if (systemnames.Any())
                        {
                            TLKBuilder.ReplaceString(dn.Value, systemnames.PullFirstItem());
                        }
                    }
                }
            }
        }

        private static void RandomizeGalaxyMapClusters(GameTarget target, RandomizationOption option, IMEPackage galaxyMapPackage, IMEPackage galaxyMapPackage2)
        {
            string fileContents = MEREmbedded.GetEmbeddedTextAsset("galaxymapclusters.xml", true);
            XElement rootElement = XElement.Parse(fileContents);

            var originalSuffixedNames = rootElement.Elements("originalsuffixedname").Select(x => x.Value).ToList(); //Used for assignments
            var suffixedClusterNames = rootElement.Elements("suffixedclustername").Select(x => x.Value).ToList(); //Used for assignments
            var nonSuffixedClusterNames = rootElement.Elements("nonsuffixedclustername").Select(x => x.Value).ToList();
            suffixedClusterNames.Shuffle();
            nonSuffixedClusterNames.Shuffle();

            var galaxyMap = galaxyMapPackage.FindExport("biog_galaxymap.GalaxyMap");
            // Enumerate the galaxy map
            var clusters = galaxyMap.GetProperty<ArrayProperty<ObjectProperty>>("Children");
            foreach (var clusterRef in clusters.Where(x => x.Value != 0))
            {
                var cluster = (ExportEntry)clusterRef.ResolveToEntry(galaxyMapPackage);

                var dn = cluster.GetProperty<StringRefProperty>("DisplayName");
                if (dn != null)
                {
                    var currentValue = TLKBuilder.TLKLookup(dn.Value, null);
                    if (originalSuffixedNames.Contains(currentValue))
                    {
                        if (suffixedClusterNames.Any())
                            TLKBuilder.ReplaceString(dn.Value, suffixedClusterNames.PullFirstItem());
                    }
                    else
                    {
                        if (nonSuffixedClusterNames.Any())
                            TLKBuilder.ReplaceString(dn.Value, nonSuffixedClusterNames.PullFirstItem());
                    }
                }
            }
        }

        private static void RandomizeGalaxyMapPlanets(GameTarget target, RandomizationOption option, IMEPackage galaxyMapPackage, IMEPackage galaxyMapPackage2)
        {
            var galaxyMap = galaxyMapPackage.FindExport("biog_galaxymap.GalaxyMap");

            // Load the planet data and shuffle it
            option.CurrentOperation = GALAXY_MAP_RANDOMIZING_MESSAGE;
            string fileContents = MEREmbedded.GetEmbeddedTextAsset("galaxymapplanets.xml", true);
            XElement rootElement = XElement.Parse(fileContents);
            var availablePlanets = (from e in rootElement.Elements("RandomizedPlanetInfo")
                                    select new RandomizedPlanetInfo
                                    {
                                        PlanetName = (string)e.Element("PlanetName"),
                                        PlanetName2 = (string)e.Element("PlanetName2"), //Original name (plot planets only)
                                        PlanetDescription = PrepareDescription((string)e.Element("PlanetDescription")),
                                        IsMSV = (bool)e.Element("IsMSV"),
                                        IsAsteroidBelt = (bool)e.Element("IsAsteroidBelt"),
                                        IsAsteroid = e.Element("IsAsteroid") != null && (bool)e.Element("IsAsteroid"),
                                        PreventShuffle = (bool)e.Element("PreventShuffle"),
                                        RowID = (int)e.Element("RowID"),
                                        MapBaseNames = e.Elements("MapBaseNames")
                                            .Select(r => r.Value).ToList(),
                                        DLC = e.Element("DLC")?.Value,
                                        ImageGroup =
                                            e.Element("ImageGroup")?.Value ??
                                            "Generic", //TODO: TURN THIS OFF FOR RELEASE BUILD AND DEBUG ONCE FULLY IMPLEMENTED
                                        ButtonLabel = e.Element("ButtonLabel")?.Value,
                                        Playable = !(e.Element("NotPlayable") != null && (bool)e.Element("NotPlayable")),
                                    }).ToList();
            availablePlanets.Shuffle();

            // Inventory images we can use
            var categoryImages = new CaseInsensitiveDictionary<List<string>>();
            var possibilities = MEREmbedded.ListEmbeddedAssets("Images", "GalaxyMap").Where(x => x.EndsWith(".jpg")).ToList();
            foreach (var fullAssetPath in possibilities)
            {
                var split = fullAssetPath.Split('.');
                var category = split[^3]; // Skip extension and filename
                if (!categoryImages.TryGetValue(category, out var imageList))
                {
                    imageList = new List<string>();
                    categoryImages[category] = imageList;
                }
                imageList.Add(fullAssetPath);
            }

            // Shuffle images
            foreach (var ioption in categoryImages)
            {
                ioption.Value.Shuffle();
            }

            // Number of needed stories/infos
            int numNeeded = 0;

            int done = 0;

            // Enumerate the galaxy map
            var clusters = galaxyMap.GetProperty<ArrayProperty<ObjectProperty>>("Children");
            foreach (var clusterRef in clusters.Where(x => x.Value != 0))
            {
                var cluster = (ExportEntry)clusterRef.ResolveToEntry(galaxyMapPackage);
                var systems = cluster.GetProperty<ArrayProperty<ObjectProperty>>("Children");
                if (systems == null)
                    continue;
                foreach (var systemRef in systems.Where(x => x.Value != 0))
                {
                    var system = (ExportEntry)systemRef.ResolveToEntry(galaxyMapPackage);
                    var planets = system.GetProperty<ArrayProperty<ObjectProperty>>("Children");
                    if (planets == null)
                        continue;
                    foreach (var planetRef in planets.Where(x => x.Value != 0))
                    {
                        var planet = (ExportEntry)planetRef.ResolveToEntry(galaxyMapPackage);

                        if (planet.ClassName != "BioPlanet")
                            continue; // Don't rename Mass Relays

                        var texParamVal = planet.GetProperty<ObjectProperty>("TextureParam")?.Value;
                        if (texParamVal.HasValue && texParamVal != 0)
                        {
                            Debug.WriteLine($"Skipping planet with texture parameter for now: {planet.FileRef.GetEntry(texParamVal.Value).InstancedFullPath}");
                            continue; // Don't rename Mass Relays

                        }

                        var planet2 = galaxyMapPackage2.FindExport(planet.InstancedFullPath);
                        var props = planet.GetProperties();
                        // Enumerate over individual entries here.
                        if (props.GetProp<StringRefProperty>("Description") == null)
                            continue; // Don't do things that don't have a description

                        if (!availablePlanets.Any())
                        {
                            numNeeded++;
                            MERLog.Warning($@"There is not enough randomized planet infos to handle all planets! Skipping planet. Num needed: {numNeeded}");
                            continue;
                        }

                        var newPlanetInfo = availablePlanets.PullFirstItem();

                        // Game 3 doesn't support things like asteroid zoom ins without text
                        // so we just skip these
                        while (newPlanetInfo.PlanetDescription == null && availablePlanets.Any())
                        {
                            Debug.WriteLine("SKIPPING BLANK DESCRIPTION PLANET (IN OUR LIST)");
                            newPlanetInfo = availablePlanets.PullFirstItem();
                        }


                        // Change the name
                        // TODO: ADD OPTION BACK TO NOT CHANGE PLOT PLANET NAMES
                        var usedPlanetName = newPlanetInfo.PlanetName?.Trim();

                        var newTlkStrName = TLKBuilder.GetNewTLKID();
                        TLKBuilder.ReplaceString(newTlkStrName, usedPlanetName);
                        props.AddOrReplaceProp(new StringRefProperty(newTlkStrName, "DisplayName"));

                        // Change the description
                        var newTlkStrDesc = TLKBuilder.GetNewTLKID();

                        var finalDescription = newPlanetInfo.PlanetDescription?.Trim();
                        finalDescription = "%PLANETNAME% %SYSTEMNAME% %CLUSTERNAME% "+ finalDescription;
                        finalDescription = finalDescription.Replace("%PLANETNAME%", usedPlanetName);
                        finalDescription = finalDescription.Replace("%SYSTEMNAME%", $"${(planet.Parent as ExportEntry).GetProperty<StringRefProperty>("DisplayName").Value}");
                        finalDescription = finalDescription.Replace("%CLUSTERNAME%", $"${(planet.Parent.Parent as ExportEntry).GetProperty<StringRefProperty>("DisplayName").Value}");

                        TLKBuilder.ReplaceString(newTlkStrDesc, finalDescription);
                        props.AddOrReplaceProp(new StringRefProperty(newTlkStrDesc, "Description"));

                        // Change the image File 1
                        if (categoryImages.TryGetValue(newPlanetInfo.ImageGroup, out var imageOptions))
                        {
                            if (!imageOptions.Any())
                            {
                                Debug.WriteLine($@"Not enough images in the {newPlanetInfo.ImageGroup} category! Skipping");
                            }
                            else
                            {
                                var imageToUse = imageOptions.PullFirstItem();

                                Image imageCache = null;

                                var sourceItem = galaxyMapPackage.FindExport(@"BIOA_GalaxyMap_Previews.PlanetOrbitalPreview_Citadel_512x256");
                                var newTexture = EntryCloner.CloneEntry(sourceItem);
                                newTexture.ObjectName = new NameReference($"MERGalaxyMap_TEST{done}", 0);
                                TextureTools.ReplaceTexture(newTexture, MEREmbedded.GetEmbeddedAsset("FULLPATH", imageToUse, false, true), true, out imageCache);
                                props.AddOrReplaceProp(new ObjectProperty(newTexture.UIndex, "PreviewImage"));
                                planet.WriteProperties(props);

                                // Change the image File 2
                                sourceItem = galaxyMapPackage2.FindExport(@"BIOA_GalaxyMap_Previews.PlanetOrbitalPreview_Citadel_512x256");
                                if (sourceItem != null)
                                {
                                    newTexture = EntryCloner.CloneEntry(sourceItem);
                                    newTexture.ObjectName = new NameReference($"MERGalaxyMap_TEST{done}", 0);
                                    TextureTools.ReplaceTexture(newTexture, null, true, out imageCache, imageCache); // Use cached version to improve speed
                                    props = sourceItem.GetProperties();
                                    props.AddOrReplaceProp(new StringRefProperty(newTlkStrDesc, "Description"));
                                    props.AddOrReplaceProp(new StringRefProperty(newTlkStrName, "DisplayName"));
                                    props.AddOrReplaceProp(new ObjectProperty(newTexture.UIndex, "PreviewImage"));
                                    planet2.WriteProperties(props);
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine($@"No image category found for {newPlanetInfo.ImageGroup}");
                        }

                        done++;
                        option.CurrentOperation = $"{GALAXY_MAP_RANDOMIZING_MESSAGE} ({done} processed)";
                    }
                }
            }
        }

        private static string PrepareDescription(string xmlText)
        {
            if (xmlText == null) return null;
            StringBuilder sb = new StringBuilder();
            foreach (var v in xmlText.Trim().SplitToLines())
            {
                // Trim each line
                sb.AppendLine(v.Trim()); // It apparently has newline already so don't do AppendLine, just append
            }

            return sb.ToString().Replace("\r", "");
        }
    }
}
