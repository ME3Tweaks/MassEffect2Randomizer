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
            // Load the planet data and shuffle it
            option.CurrentOperation = GALAXY_MAP_RANDOMIZING_MESSAGE;
            string fileContents = MERUtilities.GetEmbeddedTextAsset("galaxymapplanets.xml", true);
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
            var possibilities = MERUtilities.ListEmbeddedAssets("Images", "GalaxyMap").Where(x => x.EndsWith(".jpg")).ToList();
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


            // Open the galaxy map
            var galaxyMapPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_Nor_203aGalaxyMap.pcc"));
            var galaxyMap = galaxyMapPackage.FindExport("biog_galaxymap.GalaxyMap");

            // 203CIC also has it - depending on if player loads into map or walks into area first determines which one will actually be used
            // We don't want to force ours or we might break EGM or other mods.
            var galaxyMapPackage2 = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_Nor_203CIC.pcc"));

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
                        if (!availablePlanets.Any())
                        {
                            numNeeded++;
                            MERLog.Warning($@"There is not enough randomized planet infos to handle all planets! Skipping planet. Num needed: {numNeeded}");
                            continue;
                        }

                        var planet = (ExportEntry)planetRef.ResolveToEntry(galaxyMapPackage);

                        if (planet.ClassName != "BioPlanet")
                            continue; // Don't rename Mass Relays

                        var planet2 = galaxyMapPackage2.FindExport(planet.InstancedFullPath);
                        var props = planet.GetProperties();
                        // Enumerate over individual entries here.
                        if (props.GetProp<StringRefProperty>("Description") == null)
                            continue; // Don't do things that don't have a description

                        var newPlanetInfo = availablePlanets.PullFirstItem();
                        
                        // Game 3 doesn't support things like asteroid zoom ins without text
                        // so we just skip these
                        while (newPlanetInfo.PlanetDescription == null && availablePlanets.Any())
                            newPlanetInfo = availablePlanets.PullFirstItem();


                        // Change the name
                        var newTlkStr = TLKBuilder.GetNewTLKID();
                        TLKBuilder.ReplaceString(newTlkStr, newPlanetInfo.PlanetName?.Trim());
                        props.AddOrReplaceProp(new StringRefProperty(newTlkStr, "DisplayName"));

                        // Change the description
                        newTlkStr = TLKBuilder.GetNewTLKID();
                        TLKBuilder.ReplaceString(newTlkStr, newPlanetInfo.PlanetDescription?.Trim());
                        props.AddOrReplaceProp(new StringRefProperty(newTlkStr, "Description"));

                        // Change the image File 1
                        if (categoryImages.TryGetValue(newPlanetInfo.ImageGroup, out var imageOptions) && imageOptions.Any())
                        {
                            var imageToUse = imageOptions.PullFirstItem();

                            Image imageCache = null;

                            var sourceItem = galaxyMapPackage.FindExport(@"BIOA_GalaxyMap_Previews.PlanetOrbitalPreview_Citadel_512x256");
                            var newTexture = EntryCloner.CloneEntry(sourceItem);
                            newTexture.ObjectName = new NameReference($"MERGalaxyMap_TEST{done}", 0);
                            TextureTools.ReplaceTexture(newTexture, MERUtilities.GetEmbeddedAssetByFullPath(imageToUse), true, out imageCache);
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
                                props.AddOrReplaceProp(new StringRefProperty(newTlkStr, "Description"));
                                props.AddOrReplaceProp(new StringRefProperty(newTlkStr, "DisplayName"));
                                props.AddOrReplaceProp(new ObjectProperty(newTexture.UIndex, "PreviewImage"));
                                planet2.WriteProperties(props);
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

            MERFileSystem.SavePackage(galaxyMapPackage);
            MERFileSystem.SavePackage(galaxyMapPackage2);
            return true;
        }

        private static string PrepareDescription(string xmlText)
        {
            if (xmlText == null) return null;
            StringBuilder sb = new StringBuilder();
            var lines = xmlText.Split('\n');

            foreach (var v in lines)
            {
                // Trim each line
                sb.Append(v.Trim()); // It apparently has newline already so don't do AppendLine, just append
            }

            return sb.ToString();
        }
    }
}
