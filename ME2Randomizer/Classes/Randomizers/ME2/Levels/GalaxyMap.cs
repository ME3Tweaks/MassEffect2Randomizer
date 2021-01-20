using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class GalaxyMap
    {
        #region OptionKeys
        public const string SUBOPTIONKEY_INFINITEGAS = "InfiniteGas";
        #endregion

        public static bool RandomizeGalaxyMap(RandomizationOption option)
        {
            // Make the ship faster because otherwise it takes ages to do stuff
            // And can also consume more fuel

            var sfxgame = MERFileSystem.GetPackageFile("SFXGame.pcc");
            if (sfxgame != null && File.Exists(sfxgame))
            {
                var sfxgameP = MEPackageHandler.OpenMEPackage(sfxgame);
                var galaxyModCamDefaults = sfxgameP.GetUExport(3899);
                var props = galaxyModCamDefaults.GetProperties();

                props.AddOrReplaceProp(new FloatProperty(150, "m_fMovementScalarGalaxy")); // is this used?
                props.AddOrReplaceProp(new FloatProperty(75, "m_fMovementScalarCluster"));
                props.AddOrReplaceProp(new FloatProperty(125, "m_fMovementScalarSystem"));

                galaxyModCamDefaults.WriteProperties(props);

                // Make it so you can't run out of a gas.
                if (option.HasSubOptionSelected(SUBOPTIONKEY_INFINITEGAS))
                {
                    var BurnFuel = sfxgameP.GetUExport(3877);
                    if (BurnFuel.ObjectName == "BurnFuel")
                    {
                        var bfData = BurnFuel.Data;
                        bfData.OverwriteRange(0x9C, BitConverter.GetBytes(50f)); // Make it so we don't run out of gas
                        BurnFuel.Data = bfData;
                    }
                }


                MERFileSystem.SavePackage(sfxgameP);
            }

            // Give a bit more starting gas
            // IDK why it's in Weapon
            // This doesn't seem to actually do anything. But i'll leave it here anyways
            var weaponini = CoalescedHandler.GetIniFile("BIOWeapon.ini");
            var sfxinvmgr = weaponini.GetOrAddSection("SFXGame.SFXInventoryManager");
            sfxinvmgr.SetSingleEntry("FuelEfficiency", 5);

            // Make faster deceleration cause its hard to stop right
            var biogameini = CoalescedHandler.GetIniFile("BIOGame.ini");
            var camgalaxy = biogameini.GetOrAddSection("SFXGame.BioCameraBehaviorGalaxy");
            camgalaxy.SetSingleEntry("m_fShipSystemDeccel", 25);
            camgalaxy.SetSingleEntry("m_fShipClusterDeccel", .7f);

            var packageF = MERFileSystem.GetPackageFile(@"BioD_Nor_103aGalaxyMap.pcc");
            var package = MEPackageHandler.OpenMEPackage(packageF);
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
                                RStructs.RandomizeTint(starColor, false);
                            }
                            starColor = props.GetProp<StructProperty>("StarColor2");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(starColor, false);
                            }

                            props.GetProp<IntProperty>("PosX").Value = ThreadSafeRandom.Next(800);
                            props.GetProp<IntProperty>("PosY").Value = ThreadSafeRandom.Next(800);


                            var intensity = props.GetProp<FloatProperty>("SphereIntensity");
                            if (intensity != null) intensity.Value = ThreadSafeRandom.NextFloat(0, 6);
                            intensity = props.GetProp<FloatProperty>("NebularDensity");
                            if (intensity != null) intensity.Value = ThreadSafeRandom.NextFloat(0, 6);
                            intensity = props.GetProp<FloatProperty>("SphereSize");
                            if (intensity != null) intensity.Value = ThreadSafeRandom.NextFloat(0, 6);

                            export.WriteProperties(props);
                        }
                        //RandomizeClustersXY(export);

                        break;
                    case "SFXSystem":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("StarColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(starColor, false);
                            }

                            starColor = props.GetProp<StructProperty>("FlareTint");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(starColor, false);
                            }


                            starColor = props.GetProp<StructProperty>("SunColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(starColor, false);
                            }

                            props.GetProp<IntProperty>("PosX").Value = ThreadSafeRandom.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = ThreadSafeRandom.Next(1000);


                            var scale = props.GetProp<FloatProperty>("Scale");
                            if (scale != null) scale.Value = ThreadSafeRandom.NextFloat(.1, 2);


                            export.WriteProperties(props);
                        }
                        break;
                    case "BioPlanet":
                        {
                            var props = export.GetProperties();
                            var starColor = props.GetProp<StructProperty>("SunColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(starColor, false);
                            }

                            starColor = props.GetProp<StructProperty>("FlareTint");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(starColor, false);
                            }


                            starColor = props.GetProp<StructProperty>("CloudColor");
                            if (starColor != null)
                            {
                                RStructs.RandomizeTint(starColor, false);
                            }

                            var resourceRichness = props.GetProp<FloatProperty>("ResourceRichness");
                            if (resourceRichness != null)
                            {
                                resourceRichness.Value = ThreadSafeRandom.NextFloat(0, 1.2);
                            }
                            else
                            {
                                props.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.NextFloat(0, .6), "ResourceRichness"));
                            }

                            props.GetProp<IntProperty>("PosX").Value = ThreadSafeRandom.Next(1000);
                            props.GetProp<IntProperty>("PosY").Value = ThreadSafeRandom.Next(1000);


                            var scale = props.GetProp<FloatProperty>("Scale");
                            if (scale != null) scale.Value = ThreadSafeRandom.NextFloat(.1, 6);


                            export.WriteProperties(props);
                        }
                        break;
                    case "MaterialInstanceConstant":
                        RMaterialInstance.RandomizeExport(export, null);
                        break;
                }
            }
            MERFileSystem.SavePackage(package);
            return true;
        }

    }
}
