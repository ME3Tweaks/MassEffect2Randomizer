using System;
using System.IO;
using System.Linq;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public static class ArrivalDLC
    {
        private static void RandomizeAsteroidRelayColor()
        {
            // Relay at the end of the DLC
            var shuttleFile = MERFileSystem.GetPackageFile(@"BioD_ArvLvl5_110_Asteroid.pcc");
            if (shuttleFile != null && File.Exists(shuttleFile))
            {
                var shuttleP = MEPackageHandler.OpenMEPackage(shuttleFile);
                var randColorR = ThreadSafeRandom.Next(256);
                var randColorG = ThreadSafeRandom.Next(256);
                var randColorB = ThreadSafeRandom.Next(256);

                var stringsGlowMatInst = shuttleP.GetUExport(170);
                var props = stringsGlowMatInst.GetProperties();
                var linearColor = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues")[0].GetProp<StructProperty>("ParameterValue");
                linearColor.GetProp<FloatProperty>("R").Value = randColorR;
                linearColor.GetProp<FloatProperty>("G").Value = randColorG;
                linearColor.GetProp<FloatProperty>("B").Value = randColorB;
                stringsGlowMatInst.WriteProperties(props);

                var lensFlare = shuttleP.GetUExport(97);
                var sourceColor = lensFlare.GetProperty<StructProperty>("SourceColor");
                sourceColor.GetProp<FloatProperty>("R").Value = randColorR / 90.0f;
                sourceColor.GetProp<FloatProperty>("G").Value = randColorG / 90.0f;
                sourceColor.GetProp<FloatProperty>("B").Value = randColorB / 90.0f;
                lensFlare.WriteProperty(sourceColor);

                // lighting on the relay
                var pointlight = shuttleP.GetUExport(710);
                var pointlightColor = pointlight.GetProperty<StructProperty>("LightColor");
                var lightR = randColorR / 2 + 170;
                var lightG = randColorG / 2 + 170;
                var lightB = randColorB / 2 + 170;
                pointlightColor.GetProp<ByteProperty>("R").Value = (byte)lightR;
                pointlightColor.GetProp<ByteProperty>("G").Value = (byte)lightG;
                pointlightColor.GetProp<ByteProperty>("B").Value = (byte)lightB;
                pointlight.WriteProperty(pointlightColor);

                //Shield Impact Ring (?)
                var sir = shuttleP.GetUExport(112);
                var data = sir.Data;
                data.OverwriteRange(0x418, BitConverter.GetBytes(randColorR / 80.0f));
                data.OverwriteRange(0x41C, BitConverter.GetBytes(randColorG / 80.0f));
                data.OverwriteRange(0x420, BitConverter.GetBytes(randColorB / 80.0f));
                sir.Data = data;

                //Particle effect
                var pe = shuttleP.GetUExport(339);
                props = pe.GetProperties();
                var lookupTable = props.GetProp<StructProperty>("ColorOverLife").GetProp<ArrayProperty<FloatProperty>>("LookupTable");
                float[] allColors = { randColorR / 255.0f, randColorG / 255.0f, randColorB / 255.0f };
                lookupTable.Clear();
                lookupTable.Add(new FloatProperty(allColors.Min()));
                lookupTable.Add(new FloatProperty(allColors.Max()));

                lookupTable.Add(new FloatProperty(allColors[0])); //r
                lookupTable.Add(new FloatProperty(allColors[1])); //g
                lookupTable.Add(new FloatProperty(allColors[2])); //b

                int numToAdd = ThreadSafeRandom.Next(3);

                //flare brightens as it fades out
                lookupTable.Add(new FloatProperty(allColors[0] + numToAdd == 0 ? .3f : 0f)); //r
                lookupTable.Add(new FloatProperty(allColors[1] + numToAdd == 1 ? .3f : 0f)); //g
                lookupTable.Add(new FloatProperty(allColors[2] + numToAdd == 2 ? .3f : 0f)); //b
                pe.WriteProperties(props);

                MERFileSystem.SavePackage(shuttleP);
            }

            // Relay shown in the shuttle at the end of act 1
            var asteroidf = MERFileSystem.GetPackageFile(@"BioD_ArvLvl1_710Shuttle.pcc");
            if (asteroidf != null && File.Exists(asteroidf))
            {
                var skhuttleP = MEPackageHandler.OpenMEPackage(asteroidf);
                var randColorR = ThreadSafeRandom.Next(256);
                var randColorG = ThreadSafeRandom.Next(256);
                var randColorB = ThreadSafeRandom.Next(256);

                var stringsGlowMatInst = skhuttleP.GetUExport(4715); //strings glow
                var props = stringsGlowMatInst.GetProperties();
                var linearColor = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues")[0].GetProp<StructProperty>("ParameterValue");
                linearColor.GetProp<FloatProperty>("R").Value = randColorR;
                linearColor.GetProp<FloatProperty>("G").Value = randColorG;
                linearColor.GetProp<FloatProperty>("B").Value = randColorB;
                stringsGlowMatInst.WriteProperties(props);

                var lensFlare = skhuttleP.GetUExport(2230);
                var sourceColor = lensFlare.GetProperty<StructProperty>("SourceColor");
                sourceColor.GetProp<FloatProperty>("R").Value = randColorR / 90.0f;
                sourceColor.GetProp<FloatProperty>("G").Value = randColorG / 90.0f;
                sourceColor.GetProp<FloatProperty>("B").Value = randColorB / 90.0f;
                lensFlare.WriteProperty(sourceColor);

                // lighting on the relay
                var pointlight = skhuttleP.GetUExport(8425);
                var pointlightColor = pointlight.GetProperty<StructProperty>("LightColor");
                var lightR = randColorR / 2 + 170;
                var lightG = randColorG / 2 + 170;
                var lightB = randColorB / 2 + 170;
                pointlightColor.GetProp<ByteProperty>("R").Value = (byte)lightR;
                pointlightColor.GetProp<ByteProperty>("G").Value = (byte)lightG;
                pointlightColor.GetProp<ByteProperty>("B").Value = (byte)lightB;
                pointlight.WriteProperty(pointlightColor);

                //Shield Impact Ring (?)
                var sir = skhuttleP.GetUExport(2278);
                var data = sir.Data;
                data.OverwriteRange(0x418, BitConverter.GetBytes(randColorR / 80.0f));
                data.OverwriteRange(0x41C, BitConverter.GetBytes(randColorG / 80.0f));
                data.OverwriteRange(0x420, BitConverter.GetBytes(randColorB / 80.0f));
                sir.Data = data;

                //Particle effect
                var pe = skhuttleP.GetUExport(6078);
                props = pe.GetProperties();
                var lookupTable = props.GetProp<StructProperty>("ColorOverLife").GetProp<ArrayProperty<FloatProperty>>("LookupTable");
                float[] allColors = { randColorR / 255.0f, randColorG / 255.0f, randColorB / 255.0f };
                lookupTable.Clear();
                lookupTable.Add(new FloatProperty(allColors.Min()));
                lookupTable.Add(new FloatProperty(allColors.Max()));

                lookupTable.Add(new FloatProperty(allColors[0])); //r
                lookupTable.Add(new FloatProperty(allColors[1])); //g
                lookupTable.Add(new FloatProperty(allColors[2])); //b

                int numToAdd = ThreadSafeRandom.Next(3);

                //flare brightens as it fades out
                lookupTable.Add(new FloatProperty(allColors[0] + numToAdd == 0 ? .3f : 0f)); //r
                lookupTable.Add(new FloatProperty(allColors[1] + numToAdd == 1 ? .3f : 0f)); //g
                lookupTable.Add(new FloatProperty(allColors[2] + numToAdd == 2 ? .3f : 0f)); //b
                pe.WriteProperties(props);

                MERFileSystem.SavePackage(skhuttleP);
            }
        }

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            RandomizeAsteroidRelayColor();
            return true;
        }
    }
}
