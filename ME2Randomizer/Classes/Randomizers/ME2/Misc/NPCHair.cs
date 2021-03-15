using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class NPCHair
    {
        public static bool Init(RandomizationOption option)
        {
            var hmmHir = NonSharedPackageCache.Cache.GetCachedPackage("BIOG_HMM_HIR_PRO_R.pcc");
            var hmfHir = NonSharedPackageCache.Cache.GetCachedPackage("BIOG_HMF_HIR_PRO.pcc");
            //var jenyaHairP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.JenyaHair.pcc")));
            
            // Prepare items for porting in by forcing all items to use the correct idxLink for relinker
            EntryExporter.PrepareGlobalFileForPorting(hmmHir, "BIOG_HMM_HIR_PRO_R");
            EntryExporter.PrepareGlobalFileForPorting(hmfHir, "BIOG_HMF_HIR_PRO");

            // Get a list of all hairs we can use
            HairListMale.AddRange(hmmHir.Exports.Where(x => x.ClassName == "SkeletalMesh" && x.ObjectName.Name.StartsWith("HMM_HIR_"))); // Filter out the bad ones
            HairListFemale.AddRange(hmfHir.Exports.Where(x => x.ClassName == "SkeletalMesh" && x.ObjectName.Name.StartsWith("HMF_HIR_"))); // Filter out the bad ones (?)
            //HairListFemale.AddRange(jenyaHairP.Exports.Where(x => x.ClassName == "SkeletalMesh" && x.ObjectName.Name.StartsWith("HMF_HIR_"));

            //&&
            //(x.ObjectName.Name.Contains("Ptl")
            //|| x.ObjectName.Name.Contains("Lbn")
            //|| x.ObjectName.Name.Contains("IBun"))));
            return true;
        }

        private static List<ExportEntry> HairListMale = new List<ExportEntry>();
        private static List<ExportEntry> HairListFemale = new List<ExportEntry>();

        public static void ResetClass()
        {
            HairListMale.Clear();
            HairListFemale.Clear();
        }

        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export, false, out var hairMeshExp)) return false;
            return ForcedRun(hairMeshExp);
        }

        private static bool ForcedRun(ExportEntry hairMeshExport)
        {
            if (hairMeshExport.GetProperty<ObjectProperty>("SkeletalMesh") is ObjectProperty obj && obj.Value != 0 && obj.ResolveToEntry(hairMeshExport.FileRef) is IEntry entry)
            {
                var isfemaleHair = entry.ObjectName.Name.StartsWith("HMF_HIR_");
                var newHair = isfemaleHair ? HairListFemale.RandomElement() : HairListMale.RandomElement();
                if (newHair.ObjectName.Name == entry.ObjectName.Name)
                    return false; // We are not changing this
                Debug.WriteLine($"Changing hair mesh: {entry.ObjectName} -> {newHair.ObjectName}, object {hairMeshExport.FullPath}, class {entry.ClassName}");
                var newHairMdl = PackageTools.PortExportIntoPackage(hairMeshExport.FileRef, newHair);
                var mdlBin = ObjectBinary.From<SkeletalMesh>(newHairMdl);
                obj.Value = newHairMdl.UIndex;
                hairMeshExport.WriteProperty(obj);

                // Update the materials
                var materials = hairMeshExport.GetProperty<ArrayProperty<ObjectProperty>>("Materials");
                if (materials != null && materials.Any())
                {
                    //if (materials.Count() != 1)
                    //    Debugger.Break();
                    var mat = materials[0].ResolveToEntry(hairMeshExport.FileRef) as ExportEntry;
                    if (mat != null)
                    {
                        mat.WriteProperty(new ObjectProperty(mdlBin.Materials[0], "Parent"));
                        var parentMat = mdlBin.Materials[0] > 0 ? hairMeshExport.FileRef.GetUExport(mdlBin.Materials[0]) : EntryImporter.ResolveImport(hairMeshExport.FileRef.GetImport(mdlBin.Materials[0]));
                        // Need to match child to parent params that start with HAIR
                        var parentMatTextureParms = parentMat.GetProperty<ArrayProperty<StructProperty>>("TextureParameterValues");
                        if (parentMatTextureParms != null)
                        {
                            var parentMatHairParms = parentMatTextureParms.Where(x => x.Properties.GetProp<NameProperty>("ParameterName").Value.Name.StartsWith("HAIR_")).ToList();

                            // Need to match child to parent params that start with HAIR
                            var matTextureParms = mat.GetProperty<ArrayProperty<StructProperty>>("TextureParameterValues");
                            if (matTextureParms != null)
                            {
                                var matHairParms = matTextureParms.Where(x => x.Properties.GetProp<NameProperty>("ParameterName").Value.Name.StartsWith("HAIR_")).ToList();

                                // Map them
                                foreach (var matHairParm in matHairParms)
                                {
                                    var locName = matHairParm.Properties.GetProp<NameProperty>("ParameterName");
                                    var matchingParent = parentMatHairParms.FirstOrDefault(x => x.Properties.GetProp<NameProperty>("ParameterName").Value == locName.Value);

                                    // Assign it
                                    if (matchingParent != null)
                                    {
                                        matHairParm.Properties.AddOrReplaceProp(matchingParent.GetProp<ObjectProperty>("ParameterValue"));
                                    }
                                }
                                mat.WriteProperty(matTextureParms);
                            }
                        }
                        else
                        {

                        }
                    }

                    //foreach (var mat in materials)
                    //{

                    //    mdlBin.
                    //}
                }
                return true;
            }
            return false;
        }

        private static bool CanRandomize(ExportEntry export, bool isArchetypeCheck, out ExportEntry hairMeshExp)
        {
            hairMeshExp = null;
            if (export.IsDefaultObject) return false;
            if (export.ClassName == "BioPawn"
                && export.GetProperty<ObjectProperty>("m_oHairMesh") is ObjectProperty op
                && op.ResolveToEntry(export.FileRef) is ExportEntry hairExp)
            {
                hairMeshExp = hairExp;
                return true;
            }

            if (export.ClassName == "SFXSkeletalMeshActorMAT")
            {
                if (export.GetProperty<ObjectProperty>("HairMesh") is ObjectProperty opSKM
                    && opSKM.ResolveToEntry(export.FileRef) is ExportEntry hairExpSKM)
                {
                    // Check if skeletal mesh is set locally or if it's done in the archetype
                    // if set to zero we should not add hair cause it will look bad

                    var skeletalMesh = hairExpSKM.GetProperty<ObjectProperty>("SkeletalMesh");
                    if (skeletalMesh == null)
                    {
                        // Look in archetype
                        if (export.Archetype != null)
                        {
                            ExportEntry arch = export.Archetype as ExportEntry;
                            if (export.Archetype is ImportEntry imp)
                            {
                                // oof
                                arch = EntryImporter.ResolveImport(imp, MERFileSystem.GetGlobalCache());
                            }
                            hairMeshExp = hairExpSKM;
                            var result = export.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject) == isArchetypeCheck && CanRandomize(arch, true, out _); // look at archetype
                            if (result && !isArchetypeCheck)
                            {
                                Debug.WriteLine($"Running on {export.ObjectName.Instanced}");
                            }
                            return result;
                        }
                    }
                    else
                    {
                        hairMeshExp = hairExpSKM;
                        var result = export.ObjectFlags.Has(UnrealFlags.EObjectFlags.ArchetypeObject) == isArchetypeCheck && skeletalMesh.Value != 0;
                        if (result && !isArchetypeCheck)
                        {
                            Debug.WriteLine($"Running on {export.ObjectName.Instanced}");
                        }
                        return result;
                    }
                }
            }
            return false;
        }
    }
}
