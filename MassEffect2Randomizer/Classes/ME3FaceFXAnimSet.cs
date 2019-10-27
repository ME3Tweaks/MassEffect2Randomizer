using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ME3Explorer.Packages;
using ME3Explorer.Unreal;
using MassEffectRandomizer.Classes;

namespace ME3Explorer.FaceFX
{
    public class ME3FaceFXAnimSet : IFaceFXAnimSet
    {
        IMEPackage pcc;
        public ExportEntry export;
        public ExportEntry Export => export;
        ME3HeaderStruct header;
        public HeaderStruct Header => header;
        public ME3DataAnimSetStruct Data { get; private set; }

        public ME3FaceFXAnimSet()
        {
        }
        public ME3FaceFXAnimSet(IMEPackage Pcc, ExportEntry Entry)
        {
            
            pcc = Pcc;
            export = Entry;
            int start = export.propsEnd() + 4;
            SerializingContainer Container = new SerializingContainer(new MemoryStream(export.Data.Skip(start).ToArray()));
            Container.isLoading = true;
            Serialize(Container);
        }

        void Serialize(SerializingContainer Container)
        {
            SerializeHeader(Container);
            SerializeData(Container);
        }

        void SerializeHeader(SerializingContainer Container)
        {
            if (Container.isLoading)
                header = new ME3HeaderStruct();
            header.Magic = Container + header.Magic;
            header.unk1 = Container + header.unk1;
            header.unk2 = Container + header.unk2;
            int count = 0;
            if (!Container.isLoading)
                count = header.Licensee.Length;
            else
                header.Licensee = "";
            header.Licensee = SerializeString(Container, header.Licensee);
            count = 0;
            if (!Container.isLoading)
                count = header.Project.Length;
            else
                header.Project = "";
            header.Project = SerializeString(Container, header.Project);
            header.unk3 = Container + header.unk3;
            header.unk4 = Container + header.unk4;
            count = 0;
            if (!Container.isLoading)
                count = header.Nodes.Length;
            count = Container + count;
            if (Container.isLoading)
                header.Nodes = new HNodeStruct[count];
            for (int i = 0; i < count; i++)
            {
                if (Container.isLoading)
                    header.Nodes[i] = new HNodeStruct();
                HNodeStruct t = header.Nodes[i];
                t.unk1 = Container + t.unk1;
                t.unk2 = Container + t.unk2;
                t.Name = SerializeString(Container, t.Name);
                t.unk3 = Container + t.unk3;
                header.Nodes[i] = t;
            }
            count = 0;
            if (!Container.isLoading)
                count = header.Names.Length;
            count = Container + count;
            if (Container.isLoading)
                header.Names = new string[count];
            for (int i = 0; i < count; i++)
                header.Names[i] = SerializeString(Container, header.Names[i]);
        }

        void SerializeData(SerializingContainer Container)
        {
            if (Container.isLoading)
                Data = new ME3DataAnimSetStruct();
            Data.unk1 = Container + Data.unk1;
            Data.unk2 = Container + Data.unk2;
            Data.unk3 = Container + Data.unk3;
            Data.unk4 = Container + Data.unk4;
            int count = 0;
            if (!Container.isLoading)
                count = Data.Data.Length;
            count = Container + count;
            if (Container.isLoading)
                Data.Data = new ME3FaceFXLine[count];
            for (int i = 0; i < count; i++)
            {
                if (Container.isLoading)
                    Data.Data[i] = new ME3FaceFXLine();
                ME3FaceFXLine d = Data.Data[i];
                d.Name = Container + d.Name;
                if (Container.isLoading)
                {
                    d.NameAsString = header.Names[d.Name];
                }
                int count2 = 0;
                if (!Container.isLoading)
                    count2 = d.animations.Length;
                count2 = Container + count2;
                if (Container.isLoading)
                    d.animations = new ME3NameRef[count2];
                for (int j = 0; j < count2; j++)
                {
                    if (Container.isLoading)
                        d.animations[j] = new ME3NameRef();
                    ME3NameRef u = d.animations[j];
                    u.index = Container + u.index;
                    u.unk2 = Container + u.unk2;
                    d.animations[j] = u;
                }
                count2 = 0;
                if (!Container.isLoading)
                    count2 = d.points.Length;
                count2 = Container + count2;
                if (Container.isLoading)
                    d.points = new ControlPoint[count2];
                for (int j = 0; j < count2; j++)
                {
                    if (Container.isLoading)
                        d.points[j] = new ControlPoint();
                    ControlPoint u = d.points[j];
                    u.time = Container + u.time;
                    u.weight = Container + u.weight;
                    u.inTangent = Container + u.inTangent;
                    u.leaveTangent = Container + u.leaveTangent;
                    d.points[j] = u;
                }
                if (d.points.Length > 0)
                {
                    count2 = 0;
                    if (!Container.isLoading)
                        count2 = d.numKeys.Length;
                    count2 = Container + count2;
                    if (Container.isLoading)
                        d.numKeys = new int[count2];
                    for (int j = 0; j < count2; j++)
                        d.numKeys[j] = Container + d.numKeys[j]; 
                }
                else if (Container.isLoading)
                {
                    d.numKeys = new int[d.animations.Length];
                }
                d.FadeInTime = Container + d.FadeInTime;
                d.FadeOutTime = Container + d.FadeOutTime;
                d.unk2 = Container + d.unk2;
                d.path = SerializeString(Container, d.path);
                d.ID = SerializeString(Container, d.ID);
                d.index = Container + d.index;
                Data.Data[i] = d;
            }
        }

        string SerializeString(SerializingContainer Container, string s)
        {
            int len = 0;
            byte t = 0;
            if (Container.isLoading)
            {
                s = "";
                len = Container + len;
                for (int i = 0; i < len; i++)
                    s += (char)(Container + (byte)0);
            }
            else
            {
                len = s.Length;
                len = Container + len;
                foreach (char c in s)
                    t = Container + (byte)c;
            }
            return s;
        }

        public void DumpToFile(string path)
        {
            
            MemoryStream m = new MemoryStream();
            SerializingContainer Container = new SerializingContainer(m);
            Container.isLoading = false;
            Serialize(Container);
            m = Container.Memory;
            File.WriteAllBytes(path, m.ToArray());
        }

        public void Save()
        {
            
            MemoryStream m = new MemoryStream();
            SerializingContainer Container = new SerializingContainer(m)
            {
                isLoading = false
            };
            Serialize(Container);
            m = Container.Memory;
            MemoryStream res = new MemoryStream();
            int start = export.propsEnd();
            res.Write(export.Data, 0, start);
            res.WriteInt32((int)m.Length);
            res.WriteStream(m);
            res.WriteInt32(0);
            export.Data = res.ToArray();
        }

        public void CloneEntry(int n)
        {
            if (n < 0 || n >= Data.Data.Length)
                return;
            var list = new List<ME3FaceFXLine>();
            list.AddRange(Data.Data);
            list.Add(Data.Data[n]);
            Data.Data = list.ToArray();
        }
        public void RemoveEntry(int n)
        {
            if (n < 0 || n >= Data.Data.Length)
                return;
            var list = new List<ME3FaceFXLine>();
            list.AddRange(Data.Data);
            list.RemoveAt(n);
            Data.Data = list.ToArray();
        }

        public void MoveEntry(int n, int m)
        {
            if (n < 0 || n >= Data.Data.Length || m < 0 || m >= Data.Data.Length)
                return;
            List<ME3FaceFXLine> list = Data.Data.Where((_, i) => i != n).ToList();
            list.Insert(m, Data.Data[n]);
            Data.Data = list.ToArray();
        }

        public void AddName(string s)
        {
            var list = new List<string>(header.Names);
            list.Add(s);
            header.Names = list.ToArray();
        }

        public void FixNodeTable()
        {
            header.Nodes = ME3HeaderStruct.fullNodeTable;
        }
    }
}
