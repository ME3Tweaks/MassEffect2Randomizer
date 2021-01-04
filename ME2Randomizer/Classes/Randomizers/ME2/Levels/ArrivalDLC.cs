using System;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers
{
    public static class ArrivalDLC
    {
        public static void RandomizeAsteroidRelayColor(Randomizer randomizer, Random random)
        {
            var asteroidf = randomizer.MERFS.GetGameFile(@"BioGame\DLC\DLC_EXP_Part02\CookedPC\BioD_ArvLvl5_110_Asteroid.pcc");
            if (asteroidf != null && File.Exists(asteroidf))
            {
                var asteroidp = MEPackageHandler.OpenMEPackage(asteroidf);
                var randColorR = random.Next(256);
                var randColorG = random.Next(256);
                var randColorB = random.Next(256);

                var stringsGlowMatInst = asteroidp.GetUExport(170);
                var props = stringsGlowMatInst.GetProperties();
                var linearColor = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues")[0].GetProp<StructProperty>("ParameterValue");
                linearColor.GetProp<FloatProperty>("R").Value = randColorR;
                linearColor.GetProp<FloatProperty>("G").Value = randColorG;
                linearColor.GetProp<FloatProperty>("B").Value = randColorB;
                stringsGlowMatInst.WriteProperties(props);

                var lensFlare = asteroidp.GetUExport(97);
                var sourceColor = lensFlare.GetProperty<StructProperty>("SourceColor");
                sourceColor.GetProp<FloatProperty>("R").Value = randColorR / 90.0f;
                sourceColor.GetProp<FloatProperty>("G").Value = randColorG / 90.0f;
                sourceColor.GetProp<FloatProperty>("B").Value = randColorB / 90.0f;
                lensFlare.WriteProperty(sourceColor);

                // lighting on the relay
                var pointlight = asteroidp.GetUExport(710);
                var pointlightColor = pointlight.GetProperty<StructProperty>("LightColor");
                var lightR = randColorR / 2 + 170;
                var lightG = randColorG / 2 + 170;
                var lightB = randColorB / 2 + 170;
                pointlightColor.GetProp<ByteProperty>("R").Value = (byte) lightR;
                pointlightColor.GetProp<ByteProperty>("G").Value = (byte) lightG;
                pointlightColor.GetProp<ByteProperty>("B").Value = (byte) lightB;
                pointlight.WriteProperty(pointlightColor);

                //Shield Impact Ring (?)
                var sir = asteroidp.GetUExport(112);
                var data = sir.Data;
                data.OverwriteRange(0x418, BitConverter.GetBytes(randColorR / 80.0f));
                data.OverwriteRange(0x41C, BitConverter.GetBytes(randColorG / 80.0f));
                data.OverwriteRange(0x420, BitConverter.GetBytes(randColorB / 80.0f));
                sir.Data = data;

                //Particle effect
                var pe = asteroidp.GetUExport(339);
                props = pe.GetProperties();
                var lookupTable = props.GetProp<StructProperty>("ColorOverLife").GetProp<ArrayProperty<FloatProperty>>("LookupTable");
                float[] allColors = { randColorR / 255.0f, randColorG / 255.0f, randColorB / 255.0f };
                lookupTable.Clear();
                lookupTable.Add(new FloatProperty(allColors.Min()));
                lookupTable.Add(new FloatProperty(allColors.Max()));

                lookupTable.Add(new FloatProperty(allColors[0])); //r
                lookupTable.Add(new FloatProperty(allColors[1])); //g
                lookupTable.Add(new FloatProperty(allColors[2])); //b

                int numToAdd = random.Next(3);

                //flare brightens as it fades out
                lookupTable.Add(new FloatProperty(allColors[0] + numToAdd == 0 ? .3f : 0f)); //r
                lookupTable.Add(new FloatProperty(allColors[1] + numToAdd == 1 ? .3f : 0f)); //g
                lookupTable.Add(new FloatProperty(allColors[2] + numToAdd == 2 ? .3f : 0f)); //b
                pe.WriteProperties(props);

                randomizer.SavePackage(asteroidp);
            }


        }
    }
}
