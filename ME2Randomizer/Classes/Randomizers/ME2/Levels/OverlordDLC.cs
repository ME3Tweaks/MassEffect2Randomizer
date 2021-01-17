using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.SharpDX;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public static class OverlordDLC
    {
        private static string[] vfxCrustPackages = new[]
            {
                @"BioD_Unc1Base2_00Narrative.pcc",
                @"BioD_Unc1Base2_01Narrative.pcc",
                @"BioD_Unc1Explore.pcc",
            };

        private static void RandomizeCorruptionVFX(IMEPackage package)
        {
            // Relay at the end of the DLC
            if (!vfxCrustPackages.Contains(Path.GetFileName(package.FilePath))) return;

            foreach (var v in package.Exports)
            {
                if (v.ClassName == "BioVFXTemplate" && v.FullPath.StartsWith("BioVFX_Env_UNC_Pack01"))
                {
                    var vfxTemplateProps = v.GetProperty<ArrayProperty<ObjectProperty>>("aInstanceParameters");
                    if (vfxTemplateProps != null)
                    {
                        foreach (var vfx in vfxTemplateProps)
                        {
                            var vfxe = vfx.ResolveToEntry(package) as ExportEntry;
                            var vfxeprops = vfxe.GetProperties();
                            var vfxParm = vfxeprops.GetProp<StrProperty>("sParameterName");
                            if (ContainsColorStr(vfxParm.Value))
                            {
                                var curveTable = vfxeprops.GetProp<StructProperty>("m_curve").GetProp<ArrayProperty<FloatProperty>>("LookupTable");
                                if (curveTable != null)
                                {
                                    var dominantAddin = ThreadSafeRandom.Next(3) == 0 ? 5 : 0;
                                    foreach (var fp in curveTable)
                                    {
                                        fp.Value = ThreadSafeRandom.NextFloat(0, 2) + dominantAddin; //ThreadSafeRandom.NextFloat(fp / 2, fp * 1.5);
                                    }

                                    vfxe.WriteProperties(vfxeprops);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool ContainsColorStr(string vfxParmValue)
        {
            if (vfxParmValue.Contains("Red", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (vfxParmValue.Contains("Blue", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (vfxParmValue.Contains("Green", StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            var uncFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.Contains("_Unc1", StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (uncFiles.Any())
            {
                // DLC is installed
                foreach (var uncF in uncFiles)
                {
                    var package = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(Path.GetFileName(uncF)));
                    RandomizeArcherFaceColor(package);
                    RandomizeCorruptionVFX(package);
                    MakeGethCannonScary(package);
                    ChangeUNC4BaseColors(package);
                    MERFileSystem.SavePackage(package);
                }
            }

            return true;
        }

        private static void MakeGethCannonScary(IMEPackage package)
        {
            if (Path.GetFileName(package.FilePath) != @"BioD_Unc1Base3_100Entrance.pcc") return;
            package.GetUExport(20446).WriteProperty(new FloatProperty(15000, "FloatValue")); // Damage
            package.GetUExport(19749).WriteProperty(new FloatProperty(10, "PlayRate")); // Make cannon turn faster (not sure this does anything?)
            package.GetUExport(19638).WriteProperty(new FloatProperty(1, "Duration")); // Min time between shots
            package.GetUExport(19635).WriteProperty(new FloatProperty(2, "Duration")); // Wind up time for the cannon
        }

        private static void ChangeUNC4BaseColors(IMEPackage package)
        {
            if (Path.GetFileName(package.FilePath) != @"BioA_Unc1Base4.pcc") return;
            var yellowToGreen = package.GetUExport(93);
            var color1 = MakeRandomColor(1.21f);
            var color2 = MakeRandomColor(1.21f);
            SetBaseColorInterpGroup(yellowToGreen, color1, color2);
        }

        private static void SetBaseColorInterpGroup(ExportEntry yellowToGreen, Vector3 color1, Vector3 color2)
        {
            var tracks = yellowToGreen.GetProperty<ArrayProperty<ObjectProperty>>("InterpTracks");
            foreach (var t in tracks)
            {
                var itvmp = t.ResolveToEntry(yellowToGreen.FileRef) as ExportEntry;
                var ittProps = itvmp.GetProperties();

                var parmName = ittProps.GetProp<NameProperty>("ParamName").Value.Name;
                var vStartPoint = ittProps.GetProp<StructProperty>("VectorTrack").GetProp<ArrayProperty<StructProperty>>("Points")[0];
                var vEndPoint = ittProps.GetProp<StructProperty>("VectorTrack").GetProp<ArrayProperty<StructProperty>>("Points")[1];

                Vector3 color1Used = color1;
                Vector3 color2Used = color2;

                switch (parmName)
                {
                    case "Reflection_Color":
                        color1Used = BoostColor(color1, .1f);
                        color2Used = BoostColor(color2, 0.1f);
                        break;
                    case "Wire_Color":
                        color1Used = BoostColor(color1, .2f);
                        color2Used = BoostColor(color2, .2f);
                        break;
                    case "FloorTileColor":
                        color1Used = BoostColor(color1, -.1f);
                        color2Used = BoostColor(color2, -.1f);
                        break;
                    case "WireColor":
                        color1Used = BoostColor(color1, .15f);
                        color2Used = BoostColor(color2, .15f);
                        break;
                    case "MainColor":
                        color1Used = BoostColor(color1, 0);
                        color2Used = BoostColor(color2, 0);
                        break;
                    case "GlowColor":
                        color1Used = BoostColor(color1, -.2f);
                        color2Used = BoostColor(color2, -.2f);
                        break;
                }
                vStartPoint.Properties.AddOrReplaceProp(color1Used.ToVectorStructProperty("OutVal"));
                vEndPoint.Properties.AddOrReplaceProp(color2Used.ToVectorStructProperty("OutVal"));

                itvmp.WriteProperties(ittProps);
            }
        }

        private static Vector3 BoostColor(Vector3 source, float boostAmount)
        {
            Vector3 newCol = source;
            newCol.X += boostAmount;
            newCol.Y += boostAmount;
            newCol.Z += boostAmount;

            newCol.X = Math.Clamp(newCol.X, 0f, 1f);
            newCol.Y = Math.Clamp(newCol.Y, 0f, 1f);
            newCol.Z = Math.Clamp(newCol.Z, 0f, 1f);
            return newCol;
        }

        private static void RandomizeArcherFaceColor(IMEPackage package)
        {
            var uncMaterials = package.FindExport("BioVFX_Env_UNC_Pack01.Materials");
            if (uncMaterials != null)
            {
                // Probably should check a bit more than this, such as VI
                var viMats = package.Exports.Where(x => x.idxLink == uncMaterials.UIndex && x.ClassName == "MaterialInstanceConstant" &&
                    (
                        x.ObjectName.Name.StartsWith("VI_")
                     || x.ObjectName.Name.StartsWith("Line_Sweep")
                    )).ToList();
                if (viMats.Any())
                {
                    // Generate the color
                    var fullColor = MakeRandomColor(11.2f);

                    foreach (var m in viMats)
                    {
                        var props = m.GetProperties();
                        var linearColor = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues")?[0].GetProp<StructProperty>("ParameterValue");
                        if (linearColor != null)
                        {
                            if (m.ObjectName.Name.StartsWith("Line"))
                            {
                                // One Tenth color
                                linearColor.Properties.GetProp<FloatProperty>("R").Value = fullColor.X / 10;
                                linearColor.Properties.GetProp<FloatProperty>("G").Value = fullColor.Y / 10;
                                linearColor.Properties.GetProp<FloatProperty>("B").Value = fullColor.Z / 10;
                            }
                            else
                            {
                                // Full color
                                linearColor.Properties.GetProp<FloatProperty>("R").Value = fullColor.X;
                                linearColor.Properties.GetProp<FloatProperty>("G").Value = fullColor.Y;
                                linearColor.Properties.GetProp<FloatProperty>("B").Value = fullColor.Z;
                            }

                            m.WriteProperties(props);
                        }
                    }
                }
            }
        }

        private static Vector3 MakeRandomColor(float totalColor)
        {
            List<float> colorComponents = new List<float>();

            // 1
            float val = ThreadSafeRandom.NextFloat(0, totalColor);
            totalColor -= val;
            colorComponents.Add(val);

            // 2
            val = ThreadSafeRandom.NextFloat(0, totalColor);
            totalColor -= val;
            colorComponents.Add(val);

            // 3
            colorComponents.Add(totalColor); //what's left

            // Build color with no respect to a single channel (pick random channels)
            Vector3 color = new Vector3();
            var componentIdx = colorComponents.RandomIndex();
            color.X = colorComponents[componentIdx];
            colorComponents.RemoveAt(componentIdx);

            componentIdx = colorComponents.RandomIndex();
            color.Y = colorComponents[componentIdx];
            colorComponents.RemoveAt(componentIdx);

            componentIdx = colorComponents.RandomIndex();
            color.Z = colorComponents[componentIdx];
            colorComponents.RemoveAt(componentIdx);
            return color;
        }
    }
}
