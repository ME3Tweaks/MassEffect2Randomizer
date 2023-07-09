using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Shared
{
    /// <summary>
    /// Eye (non Illusive man) randomizer
    /// </summary>
    class RSharedEyes
    {

        // ME2R code
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ClassName == "MaterialInstanceConstant" && exp.ObjectName != "HMM_HED_EYEillusiveman_MAT_1a" && exp.ObjectName.Name.Contains("_EYE");
        public static bool RandomizeExport(GameTarget target, ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            //Log.Information("Randomizing eye color");
            RSharedMaterialInstance.RandomizeExport(exp, null);
            return true;
        }



        // LE2 specific!!
        abstract class ExpressionDefault
        {
            public string ParameterName { get; set; }
            public FGuid ExpressionGuid { get; set; }

            protected void ReadSharedProperties(ExportEntry export, out PropertyCollection propertyCollection)
            {
                propertyCollection = export.GetProperties();
                ExpressionGuid = new FGuid(propertyCollection.GetProp<StructProperty>("ExpressionGuid"));
                ParameterName = propertyCollection.GetProp<NameProperty>("ParameterName").Value.Instanced;
            }
        }

        class ScalarExpressionDefault : ExpressionDefault
        {
            public ScalarExpressionDefault(ExportEntry expr)
            {
                ReadSharedProperties(expr, out PropertyCollection props);
                ExpressionGuid = new FGuid(props.GetProp<StructProperty>("ExpressionGuid"));
                DefaultValue = props.GetProp<FloatProperty>("DefaultValue") ?? 0.0f;
            }

            public float DefaultValue { get; set; }
        }

        class VectorExpressionDefault : ExpressionDefault
        {
            public VectorExpressionDefault(ExportEntry expr)
            {
                ReadSharedProperties(expr, out PropertyCollection props);
                var sv = props.GetProp<StructProperty>("DefaultValue");
                if (sv != null)
                {
                    DefaultValue = CFVector4.FromStructProperty(sv, "R", "G", "B", "A");
                }
                else
                {
                    DefaultValue = new CFVector4();
                }
            }

            public CFVector4 DefaultValue { get; set; }
        }


        private static Dictionary<string, ScalarExpressionDefault> MasterEyeScalarExpresions;
        private static Dictionary<string, VectorExpressionDefault> MasterEyeVectorExpresions;

        /// <summary>
        /// Inventories the expressions for the eye material
        /// </summary>
        /// <param name="target"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool Init(GameTarget target, RandomizationOption option)
        {
            MasterEyeVectorExpresions = new();
            MasterEyeScalarExpresions = new();

            var startupPatch = MERFileSystem.GetPackageFile(target, "Startup_METR_Patch01_INT.pcc");
            if (startupPatch == null)
                return false; // We got issues

            var startupPatchP = MERFileSystem.OpenMEPackage(startupPatch);
            var masterMat = startupPatchP.FindExport("BIOG_Humanoid_EYE_OVRD.HMM_EYE_MASTER_OVRD_MAT");
            var expressions = masterMat.GetProperty<ArrayProperty<ObjectProperty>>("Expressions");
            foreach (var expr in expressions.Select(x => x.ResolveToExport(startupPatchP)))
            {
                if (expr == null) continue;

                if (expr.ClassName == "MaterialExpressionScalarParameter")
                {
                    var s = new ScalarExpressionDefault(expr);
                    MasterEyeScalarExpresions[expr.GetProperty<NameProperty>("ParameterName").Value.Instanced] = s;
                }
                else if (expr.ClassName == "MaterialExpressionVectorParameter")
                {
                    var v = new VectorExpressionDefault(expr);
                    MasterEyeVectorExpresions[expr.GetProperty<NameProperty>("ParameterName").Value.Instanced] = v;
                }
            }

            return true;
        }

        private static bool CanRandomize2(ExportEntry exp) => !exp.IsDefaultObject && !exp.IsArchetype() && exp.ClassName == "SFXSkeletalMeshActorMAT";
        public static bool RandomizeExport2(GameTarget target, ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize2(exp)) return false;
            //Log.Information("Randomizing eye color");

            var skm = exp.GetProperty<ObjectProperty>("HeadMesh").ResolveToExport(exp.FileRef);
            if (skm == null)
                return false; // idk what happened

            var materials = skm.GetProperty<ArrayProperty<ObjectProperty>>("Materials");
            if (materials == null)
            {
                return false; // Maybe code later, copy from archetype, not sure how this will work with archetype randomizers
            }

            foreach (var mat in materials)
            {
                var matExp = mat.ResolveToExport(exp.FileRef);
                var properties = matExp.GetProperties();
                var parent = properties.GetProp<ObjectProperty>(@"Parent").ResolveToEntry(matExp.FileRef);
                if (parent == null)
                    continue; // Another IDK

                if (!parent.ObjectName.Name.Contains(@"_EYE_"))
                {
                    continue; // Not this one
                }

                // Add missing vectors
                var vectors = properties.GetProp<ArrayProperty<StructProperty>>("VectorParameterValues") ?? new ArrayProperty<StructProperty>("VectorParameterValues");
                foreach (var ve in MasterEyeVectorExpresions)
                {
                    var expr = GetOrAddDefaultVectorExpression(vectors, ve.Value);
                    if (expr.GetProp<FloatProperty>("R").Value == 0
                        && expr.GetProp<FloatProperty>("G").Value == 0
                        && expr.GetProp<FloatProperty>("B").Value == 0
                        && expr.GetProp<FloatProperty>("A").Value == 0)
                    {
                        // It's a zero vector
                        // Same code for now but may change later
                        expr.GetProp<FloatProperty>("R").Value = ThreadSafeRandom.NextFloat(-.9, .9);
                        expr.GetProp<FloatProperty>("G").Value = ThreadSafeRandom.NextFloat(-.9, .9);
                        expr.GetProp<FloatProperty>("B").Value = ThreadSafeRandom.NextFloat(-.9, .9);
                        expr.GetProp<FloatProperty>("A").Value = ThreadSafeRandom.NextFloat(-.9, .9);

                    }
                    else
                    {
                        // Fuzz it
                        expr.GetProp<FloatProperty>("R").Value *= ThreadSafeRandom.NextFloat(-.9, .9);
                        expr.GetProp<FloatProperty>("G").Value *= ThreadSafeRandom.NextFloat(-.9, .9);
                        expr.GetProp<FloatProperty>("B").Value *= ThreadSafeRandom.NextFloat(-.9, .9);
                        expr.GetProp<FloatProperty>("A").Value *= ThreadSafeRandom.NextFloat(-.9, .9);
                    }
                }


                // Add missing scalars
                var scalars = properties.GetProp<ArrayProperty<StructProperty>>("ScalarParameterValues") ?? new ArrayProperty<StructProperty>("ScalarParameterValues");
                foreach (var se in MasterEyeScalarExpresions)
                {
                    var exprVal = GetOrAddDefaultScalarExpressionValue(scalars, se.Value);
                    if (exprVal == 0)
                    {
                        // It's a zero scalar
                        // Same code for now but may change later
                        exprVal.Value = ThreadSafeRandom.NextFloat(-.9, .9);

                    }
                    else
                    {
                        // Fuzz it
                        exprVal.Value *= ThreadSafeRandom.NextFloat(-.9, .9);
                    }
                }

                // Add lists if they didn't exist
                properties.AddOrReplaceProp(vectors);
                properties.AddOrReplaceProp(scalars);

                matExp.WriteProperties(properties);
            }

            return true;
        }

        private static FloatProperty GetOrAddDefaultScalarExpressionValue(ArrayProperty<StructProperty> scalars, ScalarExpressionDefault sed)
        {
            foreach (var v in scalars)
            {
                if (v.GetProp<NameProperty>("ParameterName").Value.Instanced == sed.ParameterName)
                    return v.GetProp<FloatProperty>("ParameterValue");
            }

            // We have to add the new expression
            PropertyCollection pc = new PropertyCollection();
            pc.Add(CommonStructs.GuidProp(new Guid(), "ExpressionGuid")); // We want it to be 000000 so we use new Guid()
            pc.Add(new NameProperty(sed.ParameterName, "ParameterName"));
            var fv = new FloatProperty(sed.DefaultValue, "ParameterValue");
            pc.Add(fv);
            scalars.Add(new StructProperty("ScalarParameterValue", pc));
            return fv;
        }

        private static StructProperty GetOrAddDefaultVectorExpression(ArrayProperty<StructProperty> vectors, VectorExpressionDefault ved)
        {
            foreach (var v in vectors)
            {
                if (v.GetProp<NameProperty>("ParameterName").Value.Instanced == ved.ParameterName)
                    return v.GetProp<StructProperty>("ParameterValue");
            }

            // We have to add the new expression
            PropertyCollection pc = new PropertyCollection();
            var sv = ved.DefaultValue.ToLinearColorStructProperty("ParameterValue");
            pc.Add(sv);
            pc.Add(CommonStructs.GuidProp(new Guid(), "ExpressionGuid")); // We want it to be 000000 so we use new Guid()
            pc.Add(new NameProperty(ved.ParameterName, "ParameterName"));
            vectors.Add(new StructProperty("VectorParameterValue", pc));
            return sv;
        }
    }
}
