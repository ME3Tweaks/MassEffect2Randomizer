﻿using ME3Explorer.Packages;
using ME3Explorer.Unreal.Classes;

namespace ME3Explorer.FaceFX
{
    public interface IFaceFXAnimSet
    {
        ME3DataAnimSetStruct Data { get; }
        HeaderStruct Header { get; }

        ExportEntry Export { get; }

        void AddName(string s);
        void CloneEntry(int n);
        void DumpToFile(string path);
        void MoveEntry(int n, int m);
        void RemoveEntry(int n);
        void Save();
    }

    public class HeaderStruct
    {
        public string[] Names;
    }

    public class ME3HeaderStruct : HeaderStruct
    {
        public uint Magic;
        public int unk1;
        public int unk2;
        public string Licensee;
        public string Project;
        public int unk3;
        public ushort unk4;
        public HNodeStruct[] Nodes;

        public static readonly HNodeStruct[] fullNodeTable =
        {
            new HNodeStruct {unk1 = 0x1A, unk2 = 1, Name = "FxObject", unk3 = 0},
            new HNodeStruct {unk1 = 0x48, unk2 = 1, Name = "FxAnim", unk3 = 6},
            new HNodeStruct {unk1 = 0x54, unk2 = 1, Name = "FxAnimSet", unk3 = 0},
            new HNodeStruct {unk1 = 0x5F, unk2 = 1, Name = "FxNamedObject", unk3 = 0},
            new HNodeStruct {unk1 = 0x64, unk2 = 1, Name = "FxName", unk3 = 1},
            new HNodeStruct {unk1 = 0x6D, unk2 = 1, Name = "FxAnimCurve", unk3 = 1},
            new HNodeStruct {unk1 = 0x75, unk2 = 1, Name = "FxAnimGroup", unk3 = 0}
        };
    }
    public class HNodeStruct
    {
        public int unk1;
        public int unk2;
        public string Name;
        public ushort unk3;
    }

    public class ME2HeaderStruct : HeaderStruct
    {
        public uint Magic;
        public int unk1;
        public string Licensee;
        public string Project;
        public int unk3;
        public int unk4;
    }

    public class ME1HeaderStruct : HeaderStruct
    {
        public uint Magic;
        public int unk1;
        public string Licensee;
        public string Project;
        public int unk3;
        public ushort unk4;
    }

    public class ME3DataAnimSetStruct
    {
        public int unk1;
        public int unk2;
        public int unk3;
        public int unk4;
        public ME3FaceFXLine[] Data;
    }

    public class ME2DataAnimSetStruct : ME3DataAnimSetStruct
    {
        public int unk5;
        public int unk6;
        public int unk7;
        public int unk8;
        public int unk9;
    }

    public class ME3FaceFXLine
    {
        public int Name;
        public string NameAsString { get; set; }
        public ME3NameRef[] animations;
        public ControlPoint[] points;
        public int[] numKeys;
        public float FadeInTime;
        public float FadeOutTime;
        public int unk2;
        public string path { get; set; }
        public string ID;
        public int index;

        public ME3FaceFXLine Clone()
        {
            ME3FaceFXLine line = (ME3FaceFXLine)MemberwiseClone();
            line.animations = line.animations.TypedClone();
            line.points = line.points.TypedClone();
            line.numKeys = line.numKeys.TypedClone();
            return line;
        }
    }

    public class ME2FaceFXLine : ME3FaceFXLine
    {
        public int unk0;
        public ushort unk1;
        //public int Name;
        public int unk6;
        //public ME2NameRef[] animations;
        //public ControlPoint[] points;
        public ushort unk4;
        //public int[] numKeys;
        //public float FadeInTime;
        //public float FadeOutTime;
        //public int unk2;
        public ushort unk5;
        //public string path { get; set; }
        //public string ID;
        //public int index;
    }

    public class ME3NameRef
    {
        public int index;
        public int unk2;
    }

    public class ME2NameRef : ME3NameRef
    {
        public int unk0;
        public ushort unk1;
        //public int index;
        //public ushort unk2;
        public ushort unk3;
    }

    public struct ControlPoint
    {
        public float time;
        public float weight;
        public float inTangent;
        public float leaveTangent;
    }
}