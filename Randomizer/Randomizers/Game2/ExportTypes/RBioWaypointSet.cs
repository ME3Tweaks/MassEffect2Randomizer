using System.Diagnostics;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    public class RBioWaypointSet
    {
        private bool CanRandomize(ExportEntry export) => export.ClassName == @"BioWaypointSet";

        public bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            MERLog.Information("Randomizing BioWaypointSet " + export.UIndex + " in " + Path.GetFileName(export.FileRef.FilePath));
            var waypointReferences = export.GetProperty<ArrayProperty<StructProperty>>("WaypointReferences");
            if (waypointReferences != null)
            {
                //Get list of valid targets
                var pcc = export.FileRef;
                var waypoints = pcc.Exports.Where(x => x.ClassName == "BioPathPoint" || x.ClassName == "PathNode").ToList();
                waypoints.Shuffle();

                foreach (var waypoint in waypointReferences)
                {
                    var nav = waypoint.GetProp<ObjectProperty>("Nav");
                    if (nav != null && nav.Value > 0)
                    {
                        ExportEntry currentPoint = export.FileRef.GetUExport(nav.Value);
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
            return true;
        }
    }
}
