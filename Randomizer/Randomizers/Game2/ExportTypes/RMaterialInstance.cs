using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using Randomizer.Randomizers.Game2.Misc;

namespace Randomizer.Randomizers.Game2.ExportTypes
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
                            scalar.GetProp<FloatProperty>("ParameterValue").Value = ThreadSafeRandom.NextFloat(.1, currentValue * 1.3);
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
        // private static bool CanRandomizeNPCExport(ExportEntry export) => !export.IsDefaultObject && (export.IsA("BioPawn") || (export.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject) && export.IsA("SFXSkeletalMeshActorMAT")));
        private static bool CanRandomizeNPCExport2(ExportEntry export) => !export.IsDefaultObject && (export.IsA("BioPawn") || export.IsA("SFXSkeletalMeshActorMAT"));

        public static bool RandomizeNPCExport2(ExportEntry export, RandomizationOption randOption)
        {
            if (!CanRandomizeNPCExport2(export)) return false;

            var props = export.GetProperties();
            var isIconic = props.GetProp<BoolProperty>("bIconicAppearance");
            if (isIconic != null && isIconic)
            {
                return false; // Don't modify an iconic look as it has a bunch fo stuff in it that can totally break it like scalp seams.
            }

            Dictionary<string, CFVector4> vectorValues = new();
            Dictionary<string, float> scalarValues = new();
            if (export.IsA("BioPawn"))
            {
                ChangeColorsInSubObjects(export, vectorValues, scalarValues, props);
            }
            else
            {
                // It's a SFXSkeletalMeshActorMAT, a basic type of NPC.
                var parms = VectorParameter.GetVectorParameters(export);
                if (parms != null)
                {
                    foreach (var parm in parms)
                    {
                        vectorValues[parm.ParameterName] = parm.ParameterValue;
                        RStructs.RandomizeTint(parm.ParameterValue, false);
                    }
                    VectorParameter.WriteVectorParameters(export, parms, "VectorParameters");

                    // Get submaterials and write out their properties too
                    ChangeColorsInSubObjects(export, vectorValues, scalarValues, props);
                }
                // Should we try to randomize things that don't have a skin tone...?

                if (export.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject) && PackageTools.IsLevelSubfile(Path.GetFileName(export.FileRef.FilePath)))
                {
                    export.indexValue = ThreadSafeRandom.Next();
                }
            }

            return true;
        }

        private static void ChangeColorsInSubObjects(ExportEntry export, Dictionary<string, CFVector4> vectorValues, Dictionary<string, float> scalarValues, PropertyCollection props = null)
        {
            props ??= export.GetProperties();
            foreach (var childObjProp in props.OfType<ObjectProperty>())
            {
                var childObj = childObjProp.ResolveToEntry(export.FileRef) as ExportEntry;
                if (childObj != null && childObj.ClassName == "SkeletalMeshComponent")
                {
                    var childMaterials = childObj.GetProperty<ArrayProperty<ObjectProperty>>("Materials");
                    if (childMaterials != null)
                    {
                        foreach (var childMatObj in childMaterials)
                        {
                            if (childMatObj.ResolveToEntry(export.FileRef) is ExportEntry childMat)
                            {
                                RandomizeSubMatInst(childMat, vectorValues, scalarValues);
                            }
                        }
                    }
                }
            }
        }

        public static void RandomizeSubMatInst(ExportEntry childMat, Dictionary<string, CFVector4> vectorValues, Dictionary<string, float> scalarValues)
        {
            // VECTOR PARAMETERS
            //Debug.WriteLine($"Randomizing matinst {childMat.InstancedFullPath}");
            var vectorParameterValues = VectorParameter.GetVectorParameters(childMat);
            if (vectorParameterValues != null)
            {
                foreach (var vpv in vectorParameterValues)
                {
                    CFVector4 color;
                    if (!vectorValues.TryGetValue(vpv.ParameterName, out color))
                    {
                        color = vpv.ParameterValue;
                        RStructs.RandomizeTint(color, false);
                        vectorValues[vpv.ParameterName] = color;
                    }
                    else
                    {
                        vpv.ParameterValue = color;
                    }
                }
                //Debug.WriteLine(childMat.InstancedFullPath);
                VectorParameter.WriteVectorParameters(childMat, vectorParameterValues);
            }

            // SCALAR PARAMETERS
            var scalarParameterValues = ScalarParameter.GetScalarParameters(childMat);
            if (scalarParameterValues != null)
            {
                foreach (var vpv in scalarParameterValues)
                {
                    if (!scalarValues.TryGetValue(vpv.ParameterName, out float scalarVal))
                    {
                        // Write new
                        vpv.ParameterValue *= ThreadSafeRandom.NextFloat(0.75, 1.25);
                        scalarValues[vpv.ParameterName] = vpv.ParameterValue;
                    }
                    else
                    {
                        // Write existing
                        vpv.ParameterValue = scalarVal;
                    }
                }
                //Debug.WriteLine(childMat.InstancedFullPath);
                ScalarParameter.WriteScalarParameters(childMat, scalarParameterValues);
            }
        }
    }
}
