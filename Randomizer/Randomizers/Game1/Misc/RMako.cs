using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Serilog;

namespace Randomizer.Randomizers.Game1.Misc
{
    class RMako
    {
        public const string RANDSETTING_MOVEMENT_MAKO_WHEELS = "RANDSETTING_MOVEMENT_MAKO_WHEELS";

        private static void RandomizeMakoMovement()
        {
            //if (mainWindow.RANDSETTING_MOVEMENT_MAKO)
            //{
            //    float makoCannonFiringRate = ThreadSafeRandom.NextFloat(0.5, 4);
            //    float makoCannonForce = ThreadSafeRandom.NextFloat(1000, 5000) + ThreadSafeRandom.NextFloat(0, 2000);
            //    float makoCannonDamage = 120 / makoCannonFiringRate; //to same damage amount.
            //    float damageincrement = ThreadSafeRandom.NextFloat(60, 90);
            //    for (int i = 0; i < 10; i++)
            //    {
            //        itemeffectlevels2da[598, 4 + i].Data = BitConverter.GetBytes(makoCannonFiringRate); //RPS
            //        itemeffectlevels2da[604, 4 + i].Data = BitConverter.GetBytes(makoCannonDamage + (i * damageincrement)); //Damage
            //        itemeffectlevels2da[617, 4 + i].Data = BitConverter.GetBytes(makoCannonForce);
            //    }
            //}
        }

        public static bool RandomizeMako(GameTarget target, IMEPackage package, RandomizationOption option)
        {
            ExportEntry SVehicleSimTank = package.Exports[23314]; // WHAT
            var props = SVehicleSimTank.GetProperties();
            StructProperty torqueCurve = SVehicleSimTank.GetProperty<StructProperty>("m_TorqueCurve");
            ArrayProperty<StructProperty> points = torqueCurve.GetProp<ArrayProperty<StructProperty>>("Points");
            var minOut = ThreadSafeRandom.Next(4000, 5600);
            var maxOut = ThreadSafeRandom.Next(6000, 22000);
            minOut = 5600;
            maxOut = 20000;
            var stepping = (maxOut - minOut) / 3; //starts at 0 with 3 upgrades
            for (int i = 0; i < points.Count; i++)
            {
                float newVal = minOut + (stepping * i);
                MERLog.Information($"Setting MakoTorque[{i}] to {newVal}");
                points[i].GetProp<FloatProperty>("OutVal").Value = newVal;
            }

            SVehicleSimTank.WriteProperty(torqueCurve);

            if (option.HasSubOptionSelected(RANDSETTING_MOVEMENT_MAKO_WHEELS))
            {
                //Reverse the steering to back wheels
                //Front
                ExportEntry LFWheel = package.Exports[36984];
                ExportEntry RFWheel = package.Exports[36987];
                //Rear
                ExportEntry LRWheel = package.Exports[36986];
                ExportEntry RRWheel = package.Exports[36989];

                var LFSteer = LFWheel.GetProperty<FloatProperty>("SteerFactor");
                var LRSteer = LRWheel.GetProperty<FloatProperty>("SteerFactor");
                var RFSteer = RFWheel.GetProperty<FloatProperty>("SteerFactor");
                var RRSteer = RRWheel.GetProperty<FloatProperty>("SteerFactor");

                LFSteer.Value = 0f;
                LRSteer.Value = 4f;
                RFSteer.Value = 0f;
                RRSteer.Value = 4f;

                LFWheel.WriteProperty(LFSteer);
                RFWheel.WriteProperty(RFSteer);
                LRWheel.WriteProperty(LRSteer);
                RRWheel.WriteProperty(RRSteer);
            }

            //Randomize the jumpjets
            ExportEntry BioVehicleBehaviorBase = package.Exports[23805];
            var behaviorProps = BioVehicleBehaviorBase.GetProperties();
            foreach (var prop in behaviorProps)
            {
                if (prop.Name.Name.StartsWith("m_fThrusterScalar"))
                {
                    var floatprop = prop as FloatProperty;
                    floatprop.Value = ThreadSafeRandom.NextFloat(.1, 6);
                }
            }

            if (target.Game.IsLEGame())
            {
                // BOOSTERS
                // TODO: BOOSTERS
            }

            BioVehicleBehaviorBase.WriteProperties(behaviorProps);

            return true;
        }

    }
}
