using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;
using Serilog;

namespace Randomizer.Randomizers.Game1.Misc
{
    class RLevelHazard
    {
        private static string[] hazardTypes = { "Cold", "Heat", "Toxic", "Radiation", "Vacuum" };

        private void RandomizeHazard(ExportEntry export, Random random)
        {
            MERLog.Information("Randomizing hazard sequence objects for " + export.UIndex + ": " + export.InstancedFullPath);
            
            /*var variableLinks = export.GetProperty<ArrayProperty<StructProperty>>("VariableLinks");
            if (variableLinks != null)
            {
                foreach (var variableLink in variableLinks)
                {
                    var expectedType = export.FileRef.getEntry(variableLink.GetProp<ObjectProperty>("ExpectedType").Value).ObjectName;
                    var linkedVariable = export.FileRef.GetUExport(variableLink.GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables")[0].Value); //hoochie mama that is one big statement.

                    switch (expectedType)
                    {
                        case "SeqVar_Name":
                            //Hazard type
                            var hazardTypeProp = linkedVariable.GetProperty<NameProperty>("NameValue");
                            hazardTypeProp.Value = hazardTypes[ThreadSafeRandom.Next(hazardTypes.Length)];
                            MERLog.Information(" >> Hazard type: " + hazardTypeProp.Value);
                            linkedVariable.WriteProperty(hazardTypeProp);
                            break;
                        case "SeqVar_Bool":
                            //Force helmet
                            var hazardHelmetProp = new IntProperty(ThreadSafeRandom.Next(2), "bValue");
                            MERLog.Information(" >> Force helmet on: " + hazardHelmetProp.Value);
                            linkedVariable.WriteProperty(hazardHelmetProp);
                            break;
                        case "SeqVar_Int":
                            //Hazard level
                            var hazardLevelProp = new IntProperty(ThreadSafeRandom.Next(4), "IntValue");
                            if (ThreadSafeRandom.Next(8) == 0) //oof, for the player
                            {
                                hazardLevelProp.Value = 4;
                            }

                            MERLog.Information(" >> Hazard level: " + hazardLevelProp.Value);
                            linkedVariable.WriteProperty(hazardLevelProp);
                            break;
                    }
                }
            }*/
        }
    }
}
