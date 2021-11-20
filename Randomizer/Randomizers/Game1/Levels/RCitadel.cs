using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game1.ExportTypes;
using Randomizer.Randomizers.Utility;
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
            MERLog.Information("Randomizing BioWaypointSets for Citadel");
            option.CurrentOperation = "Randomizing Citadel";

            int numDone = 0;
            var staDsg = MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\STA\DSG");
            var filesToProcess = Directory.GetFiles(staDsg, "*.SFM");

            option.ProgressValue = 0;
            option.ProgressMax = filesToProcess.Length;
            option.ProgressIndeterminate = false;
            foreach (var packageFile in filesToProcess)
            {
                var p = MERFileSystem.OpenMEPackage(packageFile);
                var waypoints = p.Exports.Where(x => x.ClassName == "BioWaypointSet").ToList();
                foreach (var waypoint in waypoints)
                {
                    RBioWaypointSet.RandomizeExport(target, waypoint, option);
                }
                MERFileSystem.SavePackage(p);

                option.ProgressValue++;
            }

            option.ProgressValue = 0;
            option.ProgressMax = filesToProcess.Length;
            option.ProgressIndeterminate = true;


            option.ProgressValue = 0;
            option.ProgressMax = filesToProcess.Length;
            option.ProgressIndeterminate = true;

            //Randomize Citadel Tower sky
            var package = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\STA\LAY\BIOA_STA70_02_LAY.SFM"));
            var skyMaterial = package.GetUExport(347);
            var data = skyMaterial.Data;
            data.OverwriteRange(0x168, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(-1.5, 1.5)));
            data.OverwriteRange(0x19A, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(-1.5, 1.5)));
            skyMaterial.Data = data;

            var volumeLighting = package.GetUExport(859);
            var props = volumeLighting.GetProperties();

            var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
            if (vectors != null)
            {
                foreach (var vector in vectors)
                {
                    StructTools.RandomizeTint(vector.GetProp<StructProperty>("ParameterValue"), false);
                }
            }

            volumeLighting.WriteProperties(props);

            MERFileSystem.SavePackage(package);

            //Randomize Scan the Keepers
            MERLog.Information("Randomizing Scan the Keepers");
            string fileContents = MERUtilities.GetStaticTextFile("stakeepers.xml");
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
            keeperRandomizationInfo.Shuffle();
            string STABase = MERFileSystem.GetPackageFile(target, @"BIOGame\CookedPC\Maps\STA\DSG");
            var uniqueFiles = keeperDefinitions.Select(x => x.STAFile).Distinct();

            foreach (string staFile in uniqueFiles)
            {
                MERLog.Information("Randomizing Keepers in " + staFile);
                string filepath = Path.Combine(STABase, staFile);
                var staPackage = MERFileSystem.OpenMEPackage(filepath);
                var keepersToRandomize = keeperDefinitions.Where(x => x.STAFile == staFile).ToList();
                var keeperRandomizationInfoForThisLevel = keeperRandomizationInfo.Where(x => x.STAFile == staFile).ToList();
                foreach (var keeper in keepersToRandomize)
                {
                    //Set location
                    var newRandomizationInfo = keeperRandomizationInfoForThisLevel[0];
                    keeperRandomizationInfoForThisLevel.RemoveAt(0);
                    ExportEntry bioPawn = staPackage.GetUExport(keeper.PawnExportUIndex);
                    LocationTools.SetLocation(bioPawn, newRandomizationInfo.Position);
                    if (newRandomizationInfo.Yaw != 0)
                    {
                        LocationTools.SetRotation(bioPawn, newRandomizationInfo.Yaw);
                    }

                    // Unset the "Teleport to ActionStation" bool
                    if (keeper.KismetTeleportBoolUIndex != 0)
                    {
                        //Has teleport bool
                        ExportEntry teleportBool = staPackage.GetUExport(keeper.KismetTeleportBoolUIndex);
                        teleportBool.WriteProperty(new IntProperty(0, "bValue")); //teleport false
                    }
                }

                MERFileSystem.SavePackage(staPackage);
            }
        }

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            RandomizeCitadel(target, option);

            return true;
        }
    }
}
