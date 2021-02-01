using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    /// <summary>
    /// Class version of Vector4. Easier to manipulate than a struct.
    /// </summary>
    class CVector4
    {
        public float W { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public static CVector4 FromVector4(Vector4 vector)
        {
            return new CVector4()
            {
                W = vector.W,
                X = vector.X,
                Y = vector.Y,
                Z = vector.Z
            };
        }

        public static CVector4 FromStructProperty(StructProperty sp, string wKey, string xKey, string yKey, string zKey)
        {
            return new CVector4()
            {
                W = sp.GetProp<FloatProperty>(wKey),
                X = sp.GetProp<FloatProperty>(xKey),
                Y = sp.GetProp<FloatProperty>(yKey),
                Z = sp.GetProp<FloatProperty>(zKey)
            };
        }

        public Vector4 ToVector4()
        {
            return new Vector4()
            {
                W = W,
                X = X,
                Y = Y,
                Z = Z
            };
        }
    }

    /// <summary>
    /// Class version of Integer Vector3. Easier to manipulate than a struct.
    /// </summary>
    public class CIVector3
    {
        public CIVector3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public CIVector3() { }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public static CIVector3 FromRotator(StructProperty sp)
        {
            return FromStructProperty(sp, "Pitch", "Yaw", "Roll");
        }
        public static CIVector3 FromStructProperty(StructProperty sp, string xKey, string yKey, string zKey)
        {
            return new CIVector3()
            {
                X = sp.GetProp<IntProperty>(xKey),
                Y = sp.GetProp<IntProperty>(yKey),
                Z = sp.GetProp<IntProperty>(zKey)
            };
        }
        //public static CIVector3 FromVector3(Vector3 vector)
        //{
        //    return new CIVector3()
        //    {
        //        X = vector.X,
        //        Y = vector.Y,
        //        Z = vector.Z
        //    };
        //}
        //public Vector3 ToVector3()
        //{
        //    return new Vector3()
        //    {
        //        X = X,
        //        Y = Y,
        //        Z = Z
        //    };
        //}

        internal StructProperty ToRotatorStructProperty(string propName = null)
        {
            return ToStructProperty("Pitch", "Yaw", "Roll", propName, true);
        }

        internal StructProperty ToStructProperty(string xName, string yName, string zName, string propName = null, bool isImmutable = true)
        {
            PropertyCollection props = new PropertyCollection();
            props.Add(new IntProperty(X, xName));
            props.Add(new IntProperty(Y, yName));
            props.Add(new IntProperty(Z, zName));

            return new StructProperty("Rotator", props, propName, isImmutable);
        }
    }

    /// <summary>
    /// Class version of a Float Vector3. Easier to manipulate than a struct.
    /// </summary>
    class CFVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public static CFVector3 FromStructProperty(StructProperty sp, string xKey, string yKey, string zKey)
        {
            return new CFVector3()
            {
                X = sp.GetProp<FloatProperty>(xKey),
                Y = sp.GetProp<FloatProperty>(yKey),
                Z = sp.GetProp<FloatProperty>(zKey)
            };
        }
        public static CFVector3 FromVector3(Vector3 vector)
        {
            return new CFVector3()
            {
                X = vector.X,
                Y = vector.Y,
                Z = vector.Z
            };
        }
        public Vector3 ToVector3()
        {
            return new Vector3()
            {
                X = X,
                Y = Y,
                Z = Z
            };
        }

        internal StructProperty ToStructProperty(string xName, string yName, string zName, string propName = null, bool isImmutable = true)
        {
            PropertyCollection props = new PropertyCollection();
            props.Add(new FloatProperty(X, xName));
            props.Add(new FloatProperty(Y, yName));
            props.Add(new FloatProperty(Z, zName));

            return new StructProperty("Vector", props, propName, isImmutable);
        }

        public StructProperty ToLocationStructProperty(string propName = null)
        {
            return ToStructProperty("X", "Y", "Z", propName, true);
        }
    }

    class VectorTrackPoint
    {
        public float InVal { get; set; }
        public CFVector3 OutVal { get; set; }
        public CFVector3 ArriveTangent { get; set; }
        public CFVector3 LeaveTangent { get; set; }
        public EInterpCurveMode InterpMode { get; set; }

        public static VectorTrackPoint FromStruct(StructProperty vtsp)
        {
            return new VectorTrackPoint()
            {
                InVal = vtsp.GetProp<FloatProperty>("InVal"),
                OutVal = CFVector3.FromStructProperty(vtsp.GetProp<StructProperty>("OutVal"), "X", "Y", "Z"),
                ArriveTangent = CFVector3.FromStructProperty(vtsp.GetProp<StructProperty>("ArriveTangent"), "X", "Y", "Z"),
                LeaveTangent = CFVector3.FromStructProperty(vtsp.GetProp<StructProperty>("LeaveTangent"), "X", "Y", "Z"),
                InterpMode = Enum.Parse<EInterpCurveMode>(vtsp.GetProp<EnumProperty>("InterpMode").Value)
            };
        }

        internal StructProperty ToStructProperty()
        {
            PropertyCollection props = new PropertyCollection();
            props.Add(new FloatProperty(InVal, "InVal"));
            props.Add(OutVal.ToStructProperty("X", "Y", "Z", "OutVal"));
            props.Add(ArriveTangent.ToStructProperty("X", "Y", "Z", "ArriveTangent"));
            props.Add(LeaveTangent.ToStructProperty("X", "Y", "Z", "LeaveTangent"));
            props.Add(new EnumProperty(InterpMode.ToString(), "EInterpCurveMode", MERFileSystem.Game, "InterpMode"));
            return new StructProperty("nterpCurvePointVector", props);
        }
    }

    class InterpTools
    {
        public static List<VectorTrackPoint> GetVectorTrackPoints(ExportEntry export)
        {
            List<VectorTrackPoint> list = new List<VectorTrackPoint>();

            var vt = export.GetProperty<StructProperty>("VectorTrack");
            if (vt != null)
            {
                var points = vt.GetProp<ArrayProperty<StructProperty>>("Points");
                foreach (var p in points)
                {
                    list.Add(VectorTrackPoint.FromStruct(p));
                }
                return list;
            }
            return null;
        }

        public static void WriteVectorTrackPoints(ExportEntry export, List<VectorTrackPoint> points)
        {

            var vt = export.GetProperty<StructProperty>("VectorTrack");
            if (vt != null)
            {
                var pointsA = vt.GetProp<ArrayProperty<StructProperty>>("Points");
                pointsA.Clear();
                foreach (var p in points)
                {
                    pointsA.Add(p.ToStructProperty());
                }
                export.WriteProperty(vt);
            }
        }

        [DebuggerDisplay("InterpData {Export.InstancedFullPath} {Export.UIndex} in {Path.GetFileName(Export.FileRef.FilePath)}")]
        public class InterpData
        {
            public float InterpLength { get; set; }
            public List<InterpGroup> InterpGroups { get; } = new List<InterpGroup>();
            public ExportEntry Export { get; set; }

            public InterpData(ExportEntry export)
            {
                Export = export;
                var props = export.GetProperties();
                InterpLength = props.GetProp<FloatProperty>("InterpLength");
                var groups = props.GetProp<ArrayProperty<ObjectProperty>>("InterpGroups");
                if (groups != null)
                {
                    InterpGroups.AddRange(groups.Select(x => new InterpGroup(x.ResolveToEntry(export.FileRef) as ExportEntry)));
                }
            }
        }

        [DebuggerDisplay("InterpTrack {TrackTitle} {Export.InstancedFullPath} {Export.UIndex} in {Path.GetFileName(Export.FileRef.FilePath)}")]
        internal class InterpTrack
        {
            public string TrackTitle { get; set; }
            public ExportEntry Export { get; set; }
            public InterpTrack(ExportEntry export)
            {
                Export = export;
                var props = export.GetProperties();
                TrackTitle = props.GetProp<StrProperty>("TrackTitle")?.Value;
            }
        }

        [DebuggerDisplay("InterpGroup {GroupName} {Export.InstancedFullPath} {Export.UIndex} in {Path.GetFileName(Export.FileRef.FilePath)}")]
        internal class InterpGroup
        {
            public ExportEntry Export { get; set; }
            public string GroupName { get; set; } // technically is namereference
            public List<InterpTrack> Tracks { get; } = new List<InterpTrack>();
            public InterpGroup(ExportEntry export)
            {
                Export = export;
                var props = export.GetProperties();
                GroupName = props.GetProp<NameProperty>("GroupName")?.Value;
                var tracks = props.GetProp<ArrayProperty<ObjectProperty>>("InterpTracks");
                if (tracks != null)
                {
                    Tracks.AddRange(tracks.Select(x => new InterpTrack(x.ResolveToEntry(export.FileRef) as ExportEntry)));
                }
            }
        }
    }
}
