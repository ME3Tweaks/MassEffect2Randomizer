using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class HammerHead
    {
        public static bool PerformRandomization(Random random, RandomizationOption option)
        {
            // This code appears to still be for ME1.
            var package = MEPackageHandler.OpenMEPackage("");
            ExportEntry SVehicleSimTank = package.Exports[23314];
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

            //if (mainWindow.RANDSETTING_MOVEMENT_MAKO_WHEELS)
            //{
            //    //Reverse the steering to back wheels
            //    //Front
            //    ExportEntry LFWheel = package.Exports[36984];
            //    ExportEntry RFWheel = package.Exports[36987];
            //    //Rear
            //    ExportEntry LRWheel = package.Exports[36986];
            //    ExportEntry RRWheel = package.Exports[36989];

            //    var LFSteer = LFWheel.GetProperty<FloatProperty>("SteerFactor");
            //    var LRSteer = LRWheel.GetProperty<FloatProperty>("SteerFactor");
            //    var RFSteer = RFWheel.GetProperty<FloatProperty>("SteerFactor");
            //    var RRSteer = RRWheel.GetProperty<FloatProperty>("SteerFactor");

            //    LFSteer.Value = 0f;
            //    LRSteer.Value = 4f;
            //    RFSteer.Value = 0f;
            //    RRSteer.Value = 4f;

            //    LFWheel.WriteProperty(LFSteer);
            //    RFWheel.WriteProperty(RFSteer);
            //    LRWheel.WriteProperty(LRSteer);
            //    RRWheel.WriteProperty(RRSteer);
            //}

            //Randomize the jumpjets
            ExportEntry BioVehicleBehaviorBase = package.Exports[23805];
            var behaviorProps = BioVehicleBehaviorBase.GetProperties();
            foreach (var prop in behaviorProps)
            {
                if (prop.Name.Name.StartsWith("m_fThrusterScalar"))
                {
                    var floatprop = prop as FloatProperty;
                    floatprop.Value = random.NextFloat(.1, 6);
                }
            }

            BioVehicleBehaviorBase.WriteProperties(behaviorProps);

            return true;
        }
    }
}
