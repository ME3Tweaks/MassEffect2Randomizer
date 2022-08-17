using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game1.GalaxyMap;
using Randomizer.Randomizers.Utility;
using Serilog;

namespace Randomizer.Randomizers.Levels
{
    class RSplash
    {
        private void RandomizeBioLookAtDefinition(ExportEntry export, Random random)
        {
            MERLog.Information("Randomizing BioLookAtDefinition " + export.UIndex);
            var boneDefinitions = export.GetProperty<ArrayProperty<StructProperty>>("BoneDefinitions");
            if (boneDefinitions != null)
            {
                foreach (var item in boneDefinitions)
                {
                    if (item.GetProp<NameProperty>("m_nBoneName").Value.Name.StartsWith("Eye"))
                    {
                        item.GetProp<FloatProperty>("m_fLimit").Value = ThreadSafeRandom.Next(1, 5);
                        item.GetProp<FloatProperty>("m_fUpDownLimit").Value = ThreadSafeRandom.Next(1, 5);
                    }
                    else
                    {
                        item.GetProp<FloatProperty>("m_fLimit").Value = ThreadSafeRandom.Next(1, 170);
                        item.GetProp<FloatProperty>("m_fUpDownLimit").Value = ThreadSafeRandom.Next(70, 170);
                    }

                }
            }
            export.WriteProperty(boneDefinitions);
        }

        private void RandomizeHeightFogComponent(ExportEntry exp, Random random)
        {
            var properties = exp.GetProperties();
            var lightColor = properties.GetProp<StructProperty>("LightColor");
            if (lightColor != null)
            {
                lightColor.GetProp<ByteProperty>("R").Value = (byte)ThreadSafeRandom.Next(256);
                lightColor.GetProp<ByteProperty>("G").Value = (byte)ThreadSafeRandom.Next(256);
                lightColor.GetProp<ByteProperty>("B").Value = (byte)ThreadSafeRandom.Next(256);

                var density = properties.GetProp<FloatProperty>("Density");
                if (density != null)
                {
                    var twentyPercent = ThreadSafeRandom.NextFloat(-density * .05, density * 0.75);
                    density.Value = density + twentyPercent;
                }
                exp.WriteProperties(properties);
            }
        }

        private void RandomizePawnMaterialInstances(ExportEntry exp, Random random)
        {
            //Don't know if this works
            var hairMeshObj = exp.GetProperty<ObjectProperty>("m_oHairMesh");
            if (hairMeshObj != null)
            {
                var headMesh = exp.FileRef.GetUExport(hairMeshObj.Value);
                var materials = headMesh.GetProperty<ArrayProperty<ObjectProperty>>("Materials");
                if (materials != null)
                {
                    foreach (var materialObj in materials)
                    {
                        //MaterialInstanceConstant
                        ExportEntry material = exp.FileRef.GetUExport(materialObj.Value);
                        var props = material.GetProperties();

                        {
                            var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                            var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                            if (scalars != null)
                            {
                                for (int i = 0; i < scalars.Count; i++)
                                {
                                    var scalar = scalars[i];
                                    var parameter = scalar.GetProp<NameProperty>("ParameterName");
                                    var currentValue = scalar.GetProp<FloatProperty>("ParameterValue");
                                    if (currentValue > 1)
                                    {
                                        scalar.GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, currentValue * 1.3);
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Randomizing parameter " + scalar.GetProp<NameProperty>("ParameterName"));
                                        scalar.GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, 1);
                                    }
                                }

                                foreach (var vector in vectors)
                                {
                                    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                                    StructTools.RandomizeTint(paramValue, false);
                                }
                            }
                        }
                        material.WriteProperties(props);
                    }
                }
            }
        }


        private void RandomizeSplash(GameTarget target, RandomizationOption option, IMEPackage entrymenu)
        {
            ExportEntry planetMaterial = entrymenu.GetUExport(1316);
            PlanetMIC.RandomizePlanetMaterialInstanceConstant(target, planetMaterial);

            //Corona
            ExportEntry coronaMaterial = entrymenu.GetUExport(1317);
            var props = coronaMaterial.GetProperties();
            {
                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                scalars[0].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0.01, 0.05); //Bloom
                scalars[1].GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(1, 10); //Opacity
                StructTools.RandomizeTint(vectors[0].GetProp<StructProperty>("ParameterValue"), false);
            }
            coronaMaterial.WriteProperties(props);

            //CameraPan
            ExportEntry cameraInterpData = entrymenu.GetUExport(946);
            var interpLength = cameraInterpData.GetProperty<FloatProperty>("InterpLength");
            float animationLength = ThreadSafeRandom.NextFloat(60, 120);
            ;
            interpLength.Value = animationLength;
            cameraInterpData.WriteProperty(interpLength);

            ExportEntry cameraInterpTrackMove = entrymenu.GetUExport(967);
            cameraInterpTrackMove.Data = MEREmbedded.GetEmbeddedAsset("Binary", "exportreplacements.InterpTrackMove967_EntryMenu_CameraPan.bin").ToBytes();
            props = cameraInterpTrackMove.GetProperties(forceReload: true);
            var posTrack = props.GetProp<StructProperty>("PosTrack");
            bool ZUp = false;
            if (posTrack != null)
            {
                var points = posTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                float startx = ThreadSafeRandom.NextFloat(-5100, -4800);
                float starty = ThreadSafeRandom.NextFloat(13100, 13300);
                float startz = ThreadSafeRandom.NextFloat(-39950, -39400);

                startx = -4930;
                starty = 13212;
                startz = -39964;

                float peakx = ThreadSafeRandom.NextFloat(-5100, -4800);
                float peaky = ThreadSafeRandom.NextFloat(13100, 13300);
                float peakz = ThreadSafeRandom.NextFloat(-39990, -39920); //crazy small Z values here for some reason.
                ZUp = peakz > startz;

                if (points != null)
                {
                    int i = 0;
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            if (i != 1) x.Value = startx;
                            y.Value = i == 1 ? peaky : starty;
                            z.Value = i == 1 ? peakz : startz;
                        }

                        if (i > 0)
                        {
                            s.GetProp<FloatProperty>("InVal").Value = i == 1 ? (animationLength / 2) : animationLength;
                        }

                        i++;
                    }
                }
            }

            var eulerTrack = props.GetProp<StructProperty>("EulerTrack");
            if (eulerTrack != null)
            {
                var points = eulerTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                //float startx = ThreadSafeRandom.NextFloat(, -4800);
                float startPitch = ThreadSafeRandom.NextFloat(25, 35);
                float startYaw = ThreadSafeRandom.NextFloat(-195, -160);

                //startx = 1.736f;
                //startPitch = 31.333f;
                //startYaw = -162.356f;

                float peakx = 1.736f; //Roll
                float peakPitch = ZUp ? ThreadSafeRandom.NextFloat(0, 30) : ThreadSafeRandom.NextFloat(-15, 10); //Pitch
                float peakYaw = ThreadSafeRandom.NextFloat(-315, -150);
                if (points != null)
                {
                    int i = 0;
                    foreach (StructProperty s in points)
                    {
                        var outVal = s.GetProp<StructProperty>("OutVal");
                        if (outVal != null)
                        {
                            FloatProperty x = outVal.GetProp<FloatProperty>("X");
                            FloatProperty y = outVal.GetProp<FloatProperty>("Y");
                            FloatProperty z = outVal.GetProp<FloatProperty>("Z");
                            //x.Value = i == 1 ? peakx : startx;
                            y.Value = i == 1 ? peakPitch : startPitch;
                            z.Value = i == 1 ? peakYaw : startYaw;
                        }

                        if (i > 0)
                        {
                            s.GetProp<FloatProperty>("InVal").Value = i == 1 ? (animationLength / 2) : animationLength;
                        }

                        i++;
                    }

                }
            }

            cameraInterpTrackMove.WriteProperties(props);

            var fovCurve = entrymenu.GetUExport(964);
            fovCurve.Data = MEREmbedded.GetEmbeddedAsset("Binary", "exportreplacements.InterpTrackMove964_EntryMenu_CameraFOV.bin").ToBytes();
            props = fovCurve.GetProperties(forceReload: true);
            //var pi = props.GetProp<ArrayProperty<StructProperty>>("Points");
            //var pi2 = props.GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<FloatProperty>("OutVal");
            props.GetProp<StructProperty>("FloatTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<FloatProperty>("OutVal").Value = ThreadSafeRandom.NextFloat(65, 90); //FOV
            props.GetProp<StructProperty>("FloatTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<FloatProperty>("InVal").Value = ThreadSafeRandom.NextFloat(1, animationLength - 1);
            props.GetProp<StructProperty>("FloatTrack").GetProp<ArrayProperty<StructProperty>>("Points")[2].GetProp<FloatProperty>("InVal").Value = animationLength;
            fovCurve.WriteProperties(props);

            var menuTransitionAnimation = entrymenu.GetUExport(968);
            props = menuTransitionAnimation.GetProperties();
            props.AddOrReplaceProp(new EnumProperty("IMF_RelativeToInitial", "EInterpTrackMoveFrame", MEGame.ME1, "MoveFrame"));
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[0].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("X").Value = 0;
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[0].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Y").Value = 0;
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[0].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Z").Value = 0;

            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("X").Value = ThreadSafeRandom.NextFloat(-180, 180);
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Y").Value = ThreadSafeRandom.NextFloat(-180, 180);
            props.GetProp<StructProperty>("EulerTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Z").Value = ThreadSafeRandom.NextFloat(-180, 180);

            menuTransitionAnimation.WriteProperties(props);

            var dbStandard = entrymenu.GetUExport(730);
            props = dbStandard.GetProperties();
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[1].GetProp<ArrayProperty<StructProperty>>("Links")[1].GetProp<ObjectProperty>("LinkedOp").Value = 2926; //Bioware MERLogo
            dbStandard.WriteProperties(props);
        }
    }
}
