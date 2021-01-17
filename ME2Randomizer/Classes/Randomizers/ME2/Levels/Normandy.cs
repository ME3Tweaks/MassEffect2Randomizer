using System;
using System.Collections.Generic;
using System.Linq;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public class Normandy
    {
        public static bool PerformRandomization(RandomizationOption option)
        {
            RandomizeNormandyHolo();
            RandomizeRomance();
            return true;
        }

        /// <summary>
        /// Technically this is not part of Nor (It's EndGm). But it takes place on normandy so users
        /// will think it is part of the normandy.
        /// </summary>
        /// <param name="random"></param>
        private static void RandomizeRomance()
        {
            var romChooserPackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile("BioD_EndGm1_110Romance.pcc"));

            var names = new List<(int index, string name)>();

            for(int i = 0; i < romChooserPackage.NameCount; i++)
            {
                var nv = romChooserPackage.GetNameEntry(i);
                if (nv.StartsWith("SS_") && nv.EndsWith("ROMANCE") && !nv.Contains("ACTIVE") && !nv.Contains("POST") && !nv.Contains("ME1"))
                {
                    names.Add((i, nv));
                }
            }
            var indicies = names.Select(x => x.index).ToList();
            var nameValues = names.Select(x => x.name).ToList();
            indicies.Shuffle();

            foreach(var index in indicies)
            {
                var name = nameValues[0];
                nameValues.RemoveAt(0);
                romChooserPackage.replaceName(index, name);
            }

            // OLD CODE
            /*
            var uindexesOfStates = new[] {108, 109, 110, 111, 106, 107 };

            List<NameProperty> stateNames = new List<NameProperty>();

            foreach(var uindex in uindexesOfStates)
            {
                var exp = romChooserPackage.GetUExport(uindex);
                stateNames.Add(exp.GetProperty<NameProperty>("NameValue"));
            }

            stateNames.Shuffle();
            foreach (var uindex in uindexesOfStates)
            {
                var exp = romChooserPackage.GetUExport(uindex);
                var nsn = stateNames[0];
                exp.WriteProperty(nsn);
                stateNames.RemoveAt(0);
            }*/

            MERFileSystem.SavePackage(romChooserPackage);
        }

        private static void RandomizeNormandyHolo()
        {
            string[] packages = { "BioD_Nor_104Comm.pcc", "BioA_Nor_110.pcc" };
            foreach (var packagef in packages)
            {
                var package = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(packagef));

                //WIREFRAME COLOR
                var wireframeMaterial = package.Exports.First(x => x.ObjectName == "Wireframe_mat_Master");
                var data = wireframeMaterial.Data;

                var wireColorR = ThreadSafeRandom.NextFloat(0.01, 2);
                var wireColorG = ThreadSafeRandom.NextFloat(0.01, 2);
                var wireColorB = ThreadSafeRandom.NextFloat(0.01, 2);

                List<float> allColors = new List<float>();
                allColors.Add(wireColorR);
                allColors.Add(wireColorG);
                allColors.Add(wireColorB);

                data.OverwriteRange(0x33C, BitConverter.GetBytes(wireColorR)); //R
                data.OverwriteRange(0x340, BitConverter.GetBytes(wireColorG)); //G
                data.OverwriteRange(0x344, BitConverter.GetBytes(wireColorB)); //B
                wireframeMaterial.Data = data;

                //INTERNAL HOLO
                var norHoloLargeMat = package.Exports.First(x => x.ObjectName == "Nor_Hologram_Large");
                data = norHoloLargeMat.Data;

                float holoR = 0, holoG = 0, holoB = 0;
                holoR = wireColorR * 5;
                holoG = wireColorG * 5;
                holoB = wireColorB * 5;

                data.OverwriteRange(0x314, BitConverter.GetBytes(holoR)); //R
                data.OverwriteRange(0x318, BitConverter.GetBytes(holoG)); //G
                data.OverwriteRange(0x31C, BitConverter.GetBytes(holoB)); //B
                norHoloLargeMat.Data = data;

                if (packagef == "BioA_Nor_110.pcc")
                {
                    //need to also adjust the glow under the CIC. It's controlled by a interp apparently
                    var lightColorInterp = package.GetUExport(300);
                    var vectorTrack = lightColorInterp.GetProperty<StructProperty>("VectorTrack");
                    var blueToOrangePoints = vectorTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                    //var maxColor = allColors.Max();
                    blueToOrangePoints[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("X").Value = wireColorR;
                    blueToOrangePoints[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Y").Value = wireColorG;
                    blueToOrangePoints[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Z").Value = wireColorB;
                    lightColorInterp.WriteProperty(vectorTrack);
                }
                MERFileSystem.SavePackage(package);
            }
        }
    }
}
