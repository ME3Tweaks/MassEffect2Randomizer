using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.SharpDX;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    public class RMaterialInstance
    {
        private static bool CanRandomize(ExportEntry export) => export.ClassName == @"MaterialInstanceConstant" || export.ClassName == "BioMaterialInstanceConstant";

        public static bool RandomizeExport(ExportEntry material, RandomizationOption option)
        {
            if (!CanRandomize(material)) return false;
            var props = material.GetProperties();

            {
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                if (vectors != null)
                {
                    foreach (var vector in vectors)
                    {
                        var pc = vector.GetProp<StructProperty>("ParameterValue");
                        if (pc != null)
                        {
                            RStructs.RandomizeTint(pc, false);
                        }
                    }
                }

                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
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
                            //Debug.WriteLine("Randomizing parameter " + scalar.GetProp<NameProperty>("ParameterName"));
                            scalar.GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, 1);
                        }
                    }

                    //foreach (var scalar in vectors)
                    //{
                    //    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                    //    RandomizeTint( paramValue, false);
                    //}
                }
            }
            material.WriteProperties(props);
            return true;
        }

        public static bool RandomizeExportSkin(ExportEntry material, RandomizationOption option, ref StructProperty skinColorPV)
        {
            if (!CanRandomize(material)) return false;
            var props = material.GetProperties();

            {
                var vectors = props.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues");
                if (vectors != null)
                {
                    foreach (var vector in vectors)
                    {
                        var pn = vector.GetProp<NameProperty>("ParameterName");
                        var pv = vector.GetProp<StructProperty>("ParameterValue");

                        if (pn != null && pn.Value == "SkinTone" && skinColorPV != null && pv != null)
                        {
                            vector.Properties.AddOrReplaceProp(skinColorPV);
                        }
                        else if (pv != null)
                        {
                            RStructs.RandomizeTint(pv, false);
                            if (pn.Value == "SkinTone")
                            {
                                skinColorPV = pv; // Below method will assign data to this structproperty.
                            }
                        }
                    }
                }

                var scalars = props.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues");
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
                            //Debug.WriteLine("Randomizing parameter " + scalar.GetProp<NameProperty>("ParameterName"));
                            scalar.GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(0, 1);
                        }
                    }

                    //foreach (var scalar in vectors)
                    //{
                    //    var paramValue = vector.GetProp<StructProperty>("ParameterValue");
                    //    RandomizeTint( paramValue, false);
                    //}
                }
            }
            material.WriteProperties(props);
            return true;
        }


        // Jacob is not listed as an iconic appearance.
        // If we run skin randomizer it will look weird
        private static bool CanRandomizeNPCExport(ExportEntry export) => !export.IsDefaultObject && export.IsA("BioPawn") && !export.ObjectName.Name.Contains("Jacob");

        public static bool RandomizeNPCExport(ExportEntry export, RandomizationOption arg3)
        {
            if (!CanRandomizeNPCExport(export)) return false;
            //Don't know if this works
            //if (export.UIndex != 118) return false;
            var props = export.GetProperties();

            var isIconic = props.GetProp<BoolProperty>("bIconicAppearance");

            // debuggin
            if (isIconic != null && isIconic)
            {
                return false; // Don't modify an iconic look as it has a bunch fo stuff in it that can totally break it like scalp seams.
                Debug.WriteLine($"ICONIC PAWN: {export.UIndex} {export.FullPath} in {export.FileRef.FilePath}");
            }

            // 1. Get list of all SMC
            List<ExportEntry> allSkelMeshes = new List<ExportEntry>();
            foreach (var prop in props.OfType<ObjectProperty>())
            {
                if (prop.ResolveToEntry(export.FileRef) is ExportEntry re && re.ClassName == "SkeletalMeshComponent")
                {
                    allSkelMeshes.Add(re);
                }
            }

            allSkelMeshes = allSkelMeshes.Distinct().ToList();
            StructProperty sc = null;
            foreach (var sm in allSkelMeshes)
            {
                var materials = sm.GetProperty<ArrayProperty<ObjectProperty>>("Materials");
                if (materials != null)
                {
                    foreach (var materialObj in materials.Where(x => x.Value > 0))
                    {
                        //MaterialInstanceConstant
                        ExportEntry material = export.FileRef.GetUExport(materialObj.Value);
                        RMaterialInstance.RandomizeExportSkin(material, null, ref sc);
                    }
                }
            }


            //var hairMeshObj = props.GetProp<ObjectProperty>("m_oHairMesh");
            //if (hairMeshObj != null && export.FileRef.IsUExport(hairMeshObj.Value))
            //{
            //    Log.Information(@"Randomizing BioPawn m_oHairMesh")
            //    return RandomizeSMComponent(hairMeshObj.ResolveToEntry(export.FileRef) as ExportEntry, props);
            //}
            return true;
        }
    }
}
