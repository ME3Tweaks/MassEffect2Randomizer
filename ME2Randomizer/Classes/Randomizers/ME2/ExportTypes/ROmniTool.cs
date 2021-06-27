using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.SharpDX;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    /// <summary>
    /// Randomizes particle systems for the omni tool
    /// </summary>
    class ROmniTool
    {
        private static bool CanRandomize(ExportEntry export)
        {
            // DOES NOT WORK
            return false;
            if (export.IsDefaultObject 
                || export.ClassName != "ParticleSystem" 
                || !IsOmniToolParticleSys(export))
                return false;
            return true;
        }

        private static bool IsOmniToolParticleSys(ExportEntry export)
        {
            var objName = export.ObjectName.Name;
            if (objName == "OmniArm_2sec") 
                return true;
            if (objName == "OmniHand_2sec") 
                return true;
            if (objName.Contains("Omni", StringComparison.InvariantCultureIgnoreCase)) 
                return true;

            return false;
        }


        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            //TestR(export);
            //return true;

            if (!CanRandomize(export)) return false;
            var ps = BioParticleSystem.Parse(export);

            // Select the color attributes
            var bpsc = new List<BioParticleModuleColorOverLife>();
            foreach (var em in ps.Emitters)
            {
                foreach (var lod in em.LODLevels)
                {
                    foreach (var module in lod.Modules)
                    {
                        if (module is BioParticleModuleColorOverLife colmod)
                        {
                            bpsc.Add(colmod);
                        }
                    }
                }
            }

            // Test: Just randomize that shit
            foreach (var v in bpsc)
            {
                for (int i = 0; i < v.ColorOverLife.Vectors.Count(); i++)
                {
                    Vector3 newC = OverlordDLC.MakeRandomColor(v.ColorOverLife.MaxValue);
                    v.ColorOverLife.Vectors[i] = newC;
                }

                v.ColorOverLife.WriteIntoStruct();
                v.Export.WriteProperty(v.ColorOverLife.Property);
            }



            return true;
        }

        private static void TestR(ExportEntry export)
        {
            if (/*export.UIndex == 36224 && */export.ClassName == "Material")
            {
                var obj = ObjectBinary.From<Material>(export);
                foreach(var v in obj.SM3MaterialResource.UniformPixelVectorExpressions)
                {
                    if (v is MaterialUniformExpressionVectorParameter vp)
                    {
                        vp.DefaultA = ThreadSafeRandom.NextFloat(0,1);
                        vp.DefaultR = ThreadSafeRandom.NextFloat();
                        vp.DefaultG = ThreadSafeRandom.NextFloat();
                        vp.DefaultB = ThreadSafeRandom.NextFloat();
                    } else if (v is MaterialUniformExpressionAppendVector av)
                    {
                        
                    }

                }

                foreach (var v in obj.SM3MaterialResource.UniformPixelVectorExpressions)
                {

                }
                export.WriteBinary(obj);
            }

            if (export.UIndex == 37354 || export.UIndex == 37355)
            {
                var vParms = VectorParameter.GetVectorParameters(export);
                if (vParms != null)
                {
                    foreach (var vParm in vParms)
                    {
                        CFVector4 nv = new CFVector4();
                        nv.W = ThreadSafeRandom.NextFloat(2000);
                        nv.X = ThreadSafeRandom.NextFloat(2000);
                        nv.Y = ThreadSafeRandom.NextFloat(2000);
                        nv.Z = ThreadSafeRandom.NextFloat(2000);
                        vParm.ParameterValue = nv;
                    }
                }
            }
        }
    }
}
