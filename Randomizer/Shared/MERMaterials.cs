using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Shared
{
    internal class MERMaterials
    {
        /// <summary>
        /// Makes a material instance constant from a material based on the listed expressions it has
        /// </summary>
        /// <param name="matExp">Material to make constant for</param>
        public static ExportEntry GenerateMaterialInstanceConstantFromMaterial(ExportEntry matExp)
        {
            if (matExp.ClassName == "Material" || matExp.ClassName == "MaterialInstanceConstant")
            {
                var matExpProps = matExp.GetProperties();

                // Create the export
                var matInstConst = ExportCreator.CreateExport(matExp.FileRef, matExp.ObjectName.Name + "_matInst", "MaterialInstanceConstant", matExp.Parent);
                matInstConst.indexValue--; // Decrement it by one so it starts at 0

                var matInstConstProps = matInstConst.GetProperties();
                var lightingParent = matExpProps.GetProp<StructProperty>("LightingGuid");
                if (lightingParent != null)
                {
                    lightingParent.Name = "ParentLightingGuid"; // we aren't writing to parent so this is fine
                    matInstConstProps.AddOrReplaceProp(lightingParent);
                }

                matInstConstProps.AddOrReplaceProp(new ObjectProperty(matExp.UIndex, "Parent"));
                matInstConstProps.AddOrReplaceProp(CommonStructs.GuidProp(Guid.NewGuid(), "m_Guid")); // IDK if this is used but we're gonna do it anyways

                ArrayProperty<StructProperty> vectorParameters = new ArrayProperty<StructProperty>("VectorParameterValues");
                ArrayProperty<StructProperty> scalarParameters = new ArrayProperty<StructProperty>("ScalarParameterValues");
                ArrayProperty<StructProperty> textureParameters = new ArrayProperty<StructProperty>("TextureParameterValues");

                var expressions = matExpProps.GetProp<ArrayProperty<ObjectProperty>>("Expressions");
                if (expressions != null)
                {
                    foreach (var expressionOP in expressions)
                    {
                        if (expressionOP.Value <= 0)
                            continue; // It's null
                        var expression = expressionOP.ResolveToEntry(matExp.FileRef) as ExportEntry;
                        switch (expression.ClassName)
                        {
                            case "MaterialExpressionScalarParameter":
                                {
                                    var spvP = expression.GetProperties();
                                    var paramValue = spvP.GetProp<FloatProperty>("DefaultValue");
                                    if (paramValue == null)
                                    {
                                        spvP.Add(new FloatProperty(0, "ParameterValue"));

                                    }
                                    else
                                    {
                                        paramValue.Name = "ParameterValue";
                                        spvP.RemoveAt(0);
                                        spvP.AddOrReplaceProp(paramValue); // This value goes on the end
                                    }
                                    scalarParameters.Add(new StructProperty("ScalarParameterValue", spvP));
                                }
                                break;
                            case "MaterialExpressionVectorParameter":
                                {
                                    var vpvP = expression.GetProperties();
                                    var paramValue = vpvP.GetProp<StructProperty>("DefaultValue");
                                    if (paramValue == null)
                                    {
                                        vectorParameters.Add(CommonStructs.Vector3Prop(0, 0, 0, "DefaultValue"));

                                    }
                                    else
                                    {
                                        paramValue.Name = "ParameterValue";
                                        vectorParameters.Add(new StructProperty("VectorParameterValue", vpvP));
                                    }
                                }
                                break;
                            case "MaterialExpressionTextureSampleParameter2D":
                                {
                                    var tpvP = expression.GetProperties();
                                    var paramValue = tpvP.GetProp<ObjectProperty>("Texture");
                                    paramValue.Name = "ParameterValue";
                                    textureParameters.Add(new StructProperty("TextureParameterValue", tpvP));
                                }
                                break;
                        }
                    }
                }

                if (vectorParameters.Any()) matInstConstProps.AddOrReplaceProp(vectorParameters);
                if (scalarParameters.Any()) matInstConstProps.AddOrReplaceProp(scalarParameters);
                if (textureParameters.Any()) matInstConstProps.AddOrReplaceProp(textureParameters);

                matInstConst.WriteProperties(matInstConstProps);
                return matInstConst;
            }

            return null;
        }

        public static void SetMatConstVectorParam(ExportEntry matConst, string paramName, float X, float Y, float Z)
        {
            var vpv = matConst.GetProperty<ArrayProperty<StructProperty>>("VectorParameterValues");
            if (vpv == null)
                return; // Cannot add new stuff here. TOO LAZY

            foreach (var sp in vpv)
            {
                var parmName = sp.GetProp<NameProperty>("ParameterName");
                if (parmName == null)
                    continue; // this shouldn't occur

                if (parmName.Value.Instanced != paramName)
                    continue;

                // This is the parameter we want
                // Why did I do it this way...
                var cfv4 = new CFVector4()
                {
                    W = X, // R
                    X = Y, // G
                    Y = Z, // B
                    Z = 1 // Alpha
                };

                sp.Properties.AddOrReplaceProp(cfv4.ToLinearColorStructProperty("ParameterValue"));
                matConst.WriteProperty(vpv); // Commit back to the export
                break;
            }
        }
    }
}
