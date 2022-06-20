using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
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
                                        PlanetDescription = (string)e.Element("PlanetDescription"),
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

            // Open the galaxy map
            var galaxyMapPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_Nor_203aGalaxyMap.pcc"));
            var galaxyMap = galaxyMapPackage.FindExport("biog_galaxymap.GalaxyMap");

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
                            MERLog.Warning(@"There is not enough randomized planet infos to handle all planets! Skipping planet");
                            continue;
                        }
                        var newPlanetInfo = availablePlanets.PullFirstItem();

                        var planet = (ExportEntry)planetRef.ResolveToEntry(galaxyMapPackage);
                        var props = planet.GetProperties();
                        // Enumerate over individual entries here.

                        // Change the name
                        var newTlkStr = TLKBuilder.GetNewTLKID();
                        TLKBuilder.ReplaceString(newTlkStr, newPlanetInfo.PlanetName);
                        props.AddOrReplaceProp(new StringRefProperty(newTlkStr, "DisplayName"));

                        // Change the description
                        newTlkStr = TLKBuilder.GetNewTLKID();
                        TLKBuilder.ReplaceString(newTlkStr, newPlanetInfo.PlanetDescription);
                        props.AddOrReplaceProp(new StringRefProperty(newTlkStr, "Description"));

                        // Change the image
                        var sourceItem = galaxyMapPackage.FindExport(@"BIOA_GalaxyMap_Previews.PlanetOrbitalPreview_Citadel_512x256");
                        var newTexture = EntryCloner.CloneEntry(sourceItem);
                        newTexture.ObjectName = new NameReference($"MERGalaxyMap_TEST{done}", 0);
                        TextureTools.ReplaceTexture(newTexture, MERUtilities.GetEmbeddedAsset("Images", "GalaxyMap.test.png"), true);
                        props.AddOrReplaceProp(new ObjectProperty(newTexture.UIndex, "PreviewImage"));
                        planet.WriteProperties(props);

                        done++;
                        option.CurrentOperation = $"{GALAXY_MAP_RANDOMIZING_MESSAGE} ({done} processed)";
                    }
                }
            }

            MERFileSystem.SavePackage(galaxyMapPackage);
            return true;
        }
    }
}
