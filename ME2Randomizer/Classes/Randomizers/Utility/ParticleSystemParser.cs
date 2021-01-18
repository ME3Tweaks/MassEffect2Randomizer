using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.SharpDX;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.Utility
{

    /// <summary>
    /// Contains information about the sprites to emit for the particle system
    /// </summary>
    class BioParticleSpriteEmitter
    {
        public static BioParticleSpriteEmitter Parse(ExportEntry export)
        {
            BioParticleSpriteEmitter bpse = new BioParticleSpriteEmitter()
            {
                Export = export
            };

            var props = export.GetProperties();
            bpse.SpawnRate = DistributionFloat.FromStruct(props.GetProp<StructProperty>("SpawnRate"));
            bpse.EmitterName = props.GetProp<NameProperty>("EmitterName")?.Value;
            var lods = props.GetProp<ArrayProperty<ObjectProperty>>("LODLevels").Select(x => BioParticleLODLevel.Parse(x.ResolveToEntry(export.FileRef) as ExportEntry));
            bpse.LODLevels = new ObservableCollectionExtended<BioParticleLODLevel>(lods);
            return bpse;
        }

        public ExportEntry Export { get; set; }
        public ObservableCollectionExtended<BioParticleLODLevel> LODLevels { get; set; }
        public string EmitterName { get; set; }
        public DistributionFloat SpawnRate { get; set; }
    }

    /// <summary>
    /// Contains information about the sprites to emit for the particle system
    /// </summary>
    class BioParticleLODLevel
    {
        public int LODLevel { get; set; }
        public int PeakActiveParticles { get; set; }

        public static BioParticleLODLevel Parse(ExportEntry export)
        {
            BioParticleLODLevel bpll = new BioParticleLODLevel()
            {
                Export = export
            };

            var props = export.GetProperties();

            // We have to drill into the LODs to get what we want
            bpll.LODLevel = props.GetProp<IntProperty>("Level")?.Value ?? 0;
            bpll.PeakActiveParticles = props.GetProp<IntProperty>("PeakActiveParticles")?.Value ?? 0;
            var modules = props.GetProp<ArrayProperty<ObjectProperty>>("Modules");
            if (modules != null)
            {
                bpll.Modules = modules.Select(x => ParseModule(x.ResolveToEntry(export.FileRef) as ExportEntry)).ToList();
            }


            return bpll;
        }

        private static BioParticleModule ParseModule(ExportEntry moduleExp)
        {
            if (moduleExp.ClassName == "BioParticleModuleSound")
                return BioParticleModuleSound.Parse(moduleExp);
            if (moduleExp.ClassName == "ParticleModuleColorOverLife")
                return BioParticleModuleColorOverLife.Parse(moduleExp);
            /*if (moduleExp.ClassName == "ParticleModuleColorOverLife")
                return BioParticleModuleColorOverLife.Parse(moduleExp);
            if (moduleExp.ClassName == "ParticleModuleRequired")
                return BioParticleModuleColorOverLife.Parse(moduleExp);
            if (moduleExp.ClassName == "ParticleModuleSize")
                return BioParticleModuleColorOverLife.Parse(moduleExp);
            if (moduleExp.ClassName == "ParticleModuleTypeDataMesh")
                return BioParticleModuleColorOverLife.Parse(moduleExp); */

            return null;
        }

        public ExportEntry Export { get; set; }
        public List<BioParticleModule> Modules { get; set; }
    }

    abstract class BioParticleModule
    {
        public ExportEntry Export { get; set; }
    }

    class BioParticleModuleLifetime : BioParticleModule
    {

    }

    class BioParticleModuleSound : BioParticleModule
    {
        private IEntry WwiseEventEntry { get; set; }
        public static BioParticleModuleSound Parse(ExportEntry export)
        {
            var wwiseEvent = export.GetProperty<ObjectProperty>("oWwiseEvent")?.Value;
            var wwiseEventObj = (IEntry)null;
            export.FileRef.TryGetEntry(wwiseEvent ?? 0, out wwiseEventObj);
            return new BioParticleModuleSound()
            {
                Export = export,
                WwiseEventEntry = wwiseEventObj
            };
        }
    }

    class BioParticleModuleColorOverLife : BioParticleModule
    {
        public DistributionVector ColorOverLife { get; set; }
        public DistributionFloat AlphaOverLife { get; set; }
        public static BioParticleModuleColorOverLife Parse(ExportEntry export)
        {
            var props = export.GetProperties();
            var bpmcol = new BioParticleModuleColorOverLife();
            bpmcol.Export = export;
            bpmcol.ColorOverLife = DistributionVector.FromStruct(props.GetProp<StructProperty>("ColorOverLife"));
            bpmcol.AlphaOverLife = DistributionFloat.FromStruct(props.GetProp<StructProperty>("AlphaOverLife"));
            return bpmcol;
        }
    }

    class DistributionVector
    {
        public string PropertyName { get; set; }
        public bool HasLookupTable { get; set; }
        public StructProperty Property { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public ObservableCollectionExtended<Vector3> Vectors { get; set; }

        /// <summary>
        /// Generates a DistributionVector 
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        public static DistributionVector FromStruct(StructProperty sp)
        {
            var lookupTable = sp.GetProp<ArrayProperty<FloatProperty>>("LookupTable");
            if (lookupTable != null && lookupTable.Count > 1)
            {
                float min = lookupTable[0];
                float max = lookupTable[1];

                int index = 2;
                List<Vector3> vectors = new List<Vector3>();
                while (index < lookupTable.Count)
                {
                    Vector3 v = new Vector3(lookupTable[index], lookupTable[index + 1], lookupTable[index + 2]);
                    vectors.Add(v);
                    index += 3;
                }

                DistributionVector dv = new DistributionVector
                {
                    HasLookupTable = true,
                    MinValue = min,
                    MaxValue = max,
                    Property = sp,
                    PropertyName = sp.Name.Name,
                    Vectors = new ObservableCollectionExtended<Vector3>(vectors)
                };
                return dv;
            }

            // ERROR
            return null;
        }

        public void WriteIntoStruct(StructProperty existingStruct = null)
        {
            // We assume it already exists
            MinValue = Vectors.Min(x => x.MinValue());
            MaxValue = Vectors.Max(x => x.MaxValue());
            var tbl = new List<float> {MinValue, MaxValue};
            foreach (var v in Vectors)
            {
                tbl.Add(v.X);
                tbl.Add(v.Y);
                tbl.Add(v.Z);
            }

            if (existingStruct == null)
            {
                existingStruct = Property;
            }
            existingStruct.GetProp<ArrayProperty<FloatProperty>>("LookupTable").ReplaceAll(tbl.Select(x => new FloatProperty(x)));
        }

        //public void SetupUIProps()
        //{
        //    if (PropertyName.Contains("Color"))
        //    {
        //        foreach (var v in Vectors)
        //        {
        //            v.IsColor = true;

        //            var colorR = v.Vector.X;
        //            var colorG = v.Vector.Y;
        //            var colorB = v.Vector.Z;
        //            if (MaxValue > 1)
        //            {
        //                colorR = colorR * 1 / MaxValue;
        //                colorG = colorG * 1 / MaxValue;
        //                colorB = colorB * 1 / MaxValue;
        //            }
        //            colorR = Math.Min(colorR * 255, 255);
        //            colorG = Math.Min(colorG * 255, 255);
        //            colorB = Math.Min(colorB * 255, 255);
        //        }
        //    }
        //}
    }

    public class DistributionFloat
    {
        public string PropertyName { get; set; }
        public StructProperty Property { get; set; }
        public bool HasLookupTable { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public ObservableCollectionExtended<float> Floats { get; init; }

        /// <summary>
        /// Generates a DistributionVector 
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        public static DistributionFloat FromStruct(StructProperty sp)
        {
            var lookupTable = sp.GetProp<ArrayProperty<FloatProperty>>("LookupTable");
            if (lookupTable != null && lookupTable.Count > 1)
            {

                float min = lookupTable[0];
                float max = lookupTable[1];

                return new DistributionFloat
                {
                    HasLookupTable = true,
                    MinValue = min,
                    MaxValue = max,
                    Property = sp,
                    PropertyName = sp.Name.Name,
                    Floats = new ObservableCollectionExtended<float>(lookupTable.Skip(2).Take(lookupTable.Count - 2).Select(x => x.Value))
                };
            }

            return null;
        }

        public void WriteIntoStruct(StructProperty existingStruct = null)
        {
            // We assume it already exists
            MinValue = Floats.Min();
            MaxValue = Floats.Max();
            var tbl = new List<float>();
            tbl.Add(MinValue);
            tbl.Add(MaxValue);
            tbl.AddRange(Floats);
            if (existingStruct == null)
            {
                existingStruct = Property;
            }
            existingStruct.GetProp<ArrayProperty<FloatProperty>>("LookupTable").ReplaceAll(tbl.Select(x => new FloatProperty(x)));
        }
    }

    /// <summary>
    /// Top level Particle System
    /// </summary>
    class BioParticleSystem
    {
        public List<BioParticleSpriteEmitter> Emitters { get; set; }
        public static BioParticleSystem Parse(ExportEntry export)
        {
            BioParticleSystem bps = new BioParticleSystem()
            {
                Export = export
            };

            // We have to drill into the PS to get what we want
            var emitters = export.GetProperty<ArrayProperty<ObjectProperty>>("Emitters");
            if (emitters != null)
            {
                bps.Emitters = emitters.Select(x => BioParticleSpriteEmitter.Parse(x.ResolveToEntry(export.FileRef) as ExportEntry)).ToList();
            }

            return bps;
        }

        public ExportEntry Export { get; set; }
    }
}
