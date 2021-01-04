using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class GalaxyMap
    {
        public void RandomizeGalaxyMap(Random random)
        {
            var package = MERFileSystem.GetPackageFile(@"BioD_Nor_103aGalaxyMap.pcc");
            foreach (ExportEntry export in package.Exports)
            {
                switch (export.ClassName)
                {
                    case "SFXCluster":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("StarColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }
                            starColor = props.GetProp<StructProperty>("StarColor2");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }

                            props.GetProp<IntProperty>("PosX").Value = random.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = random.Next(1000);


                            var intensity = props.GetProp<FloatProperty>("SphereIntensity");
                            if (intensity != null) intensity.Value = random.NextFloat(0, 6);
                            intensity = props.GetProp<FloatProperty>("NebularDensity");
                            if (intensity != null) intensity.Value = random.NextFloat(0, 6);
                            intensity = props.GetProp<FloatProperty>("SphereSize");
                            if (intensity != null) intensity.Value = random.NextFloat(0, 6);

                            export.WriteProperties(props);
                        }
                        //RandomizeClustersXY(export, random);

                        break;
                    case "SFXSystem":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("StarColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }

                            starColor = props.GetProp<StructProperty>("FlareTint");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }


                            starColor = props.GetProp<StructProperty>("SunColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }

                            props.GetProp<IntProperty>("PosX").Value = random.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = random.Next(1000);


                            var scale = props.GetProp<FloatProperty>("Scale");
                            if (scale != null) scale.Value = random.NextFloat(.1, 2);


                            export.WriteProperties(props);
                        }
                        break;
                    case "BioPlanet":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("SunColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }

                            starColor = props.GetProp<StructProperty>("FlareTint");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }


                            starColor = props.GetProp<StructProperty>("CloudColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(random, starColor, false);
                            }

                            var resourceRichness = props.GetProp<FloatProperty>("ResourceRichness");
                            if (resourceRichness != null)
                            {
                                resourceRichness.Value = random.NextFloat(0, 1.2);
                            }
                            else
                            {
                                props.AddOrReplaceProp(new FloatProperty(random.NextFloat(0, .6), "ResourceRichness"));
                            }

                            props.GetProp<IntProperty>("PosX").Value = random.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = random.Next(1000);


                            var scale = props.GetProp<FloatProperty>("Scale");
                            if (scale != null) scale.Value = random.NextFloat(.1, 6);


                            export.WriteProperties(props);
                        }
                        break;
                    case "MaterialInstanceConstant":
                        RandomizeMaterialInstance(export, random);
                        break;
                }
            }
            MERFileSystem.SavePackage(package);
        }
    }
}
