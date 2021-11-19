using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Serilog;

namespace Randomizer.Randomizers.Levels
{
    [DebuggerDisplay("KeeperLocation at {Position.X},{Position.Y},{Position.Z}, Rot {Yaw} in {STAFile}")]
    public class KeeperLocation
    {
        public Vector3 Position;
        public int Yaw;
        public string STAFile;
    }

    [DebuggerDisplay("KeeperDefinition | Teleport UIndex: {KismetTeleportBoolUIndex} BioPawn UIndex:{PawnExportUIndex} | {STAFile}")]
    public class KeeperDefinition
    {
        public string STAFile;
        public int PawnExportUIndex;
        public int KismetTeleportBoolUIndex;
    }
    class RCitadel
    {
        private static void RandomizeCitadel(GameTarget target, RandomizationOption option)
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
            data.OverwriteRange(0x168, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(-1.5, 1.5)));
            data.OverwriteRange(0x19A, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(-1.5, 1.5)));
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
                                     select new Game1.Randomizer.KeeperDefinition
                                     {
                                         STAFile = (string)e.Attribute("file"),
                                         KismetTeleportBoolUIndex = (int)e.Attribute("teleportflagexport"),
                                         PawnExportUIndex = (int)e.Attribute("export"),
                                     }).ToList();


            var keeperRandomizationInfo = (from e in rootElement.Elements("keeperlocation")
                                           select new Game1.Randomizer.KeeperLocation
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
                    ExportEntry bioPawn = staPackage.getUExport(keeper.PawnExportUIndex);
                    Utilities.SetLocation(bioPawn, newRandomizationInfo.Position);
                    if (newRandomizationInfo.Yaw != 0)
                    {
                        Utilities.SetRotation(bioPawn, newRandomizationInfo.Yaw);
                    }

                    // Unset the "Teleport to ActionStation" bool
                    if (keeper.KismetTeleportBoolUIndex != 0)
                    {
                        //Has teleport bool
                        ExportEntry teleportBool = staPackage.getUExport(keeper.KismetTeleportBoolUIndex);
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

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            RandomizeCitadel(target, option);

            return true;
        }
    }
}
