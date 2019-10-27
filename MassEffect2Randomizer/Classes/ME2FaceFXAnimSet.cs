using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME3Explorer.Packages;
using ME3Explorer.Unreal;

namespace ME3Explorer.FaceFX
{
    public class ME2FaceFXAnimSet : IFaceFXAnimSet
    {
        IMEPackage pcc;
        public ExportEntry Export { get; }
        public ME2HeaderStruct header;
        public HeaderStruct Header => header;

        public ME2DataAnimSetStruct data;
        public ME3DataAnimSetStruct Data => data;

        public ME2FaceFXAnimSet()
        {
        }

        public ME2FaceFXAnimSet( ExportEntry Entry)
        {
            pcc = Entry.FileRef;
            Export = Entry;
            int start = Export.propsEnd() + 4;
            SerializingContainer Container = new SerializingContainer(new MemoryStream(Export.Data.Skip(start).ToArray()));
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
                header = new ME2HeaderStruct();
            header.Magic = Container + header.Magic;
            header.unk1 = Container + header.unk1;
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
                count = header.Names.Length;
            count = Container + count;
            if (Container.isLoading)
                header.Names = new string[count];
            ushort unk = 0;
            for (int i = 0; i < count; i++)
            {
                unk = Container + unk;
                header.Names[i] = SerializeString(Container, header.Names[i]);
            }
        }

        void SerializeData(SerializingContainer Container)
        {
            if (Container.isLoading)
                data = new ME2DataAnimSetStruct();
            data.unk1 = Container + data.unk1;
            data.unk2 = Container + data.unk2;
            data.unk3 = Container + data.unk3;
            data.unk4 = Container + data.unk4;
            data.unk5 = Container + data.unk5;
            data.unk6 = Container + data.unk6;
            data.unk7 = Container + data.unk7;
            data.unk8 = Container + data.unk8;
            data.unk9 = Container + data.unk9;
            int count = 0;
            if (!Container.isLoading)
                count = data.Data.Length;
            count = Container + count;
            if (Container.isLoading)
                data.Data = new ME2FaceFXLine[count];
            for (int i = 0; i < count; i++)
            {
                if (Container.isLoading)
                    data.Data[i] = new ME2FaceFXLine();
                ME2FaceFXLine d = (ME2FaceFXLine)data.Data[i];
                d.unk0 = Container + d.unk0;
                d.unk1 = Container + d.unk1;
                d.Name = Container + d.Name;
                if (Container.isLoading)
                {
                    d.NameAsString = header.Names[d.Name];
                }
                d.unk6 = Container + d.unk6;
                int animationCount = 0;
                if (!Container.isLoading)
                    animationCount = d.animations.Length;
                animationCount = Container + animationCount;
                if (Container.isLoading)
                    d.animations = new ME2NameRef[animationCount];
                for (int j = 0; j < animationCount; j++)
                {
                    if (Container.isLoading)
                        d.animations[j] = new ME2NameRef();
                    ME2NameRef u = d.animations[j] as ME2NameRef;
                    u.unk0 = Container + u.unk0;
                    u.unk1 = Container + u.unk1;
                    u.index = Container + u.index;
                    u.unk2 = Container + u.unk2;
                    u.unk3 = Container + u.unk3;
                    d.animations[j] = u;
                }
                int pointCount = 0;
                if (!Container.isLoading)
                    pointCount = d.points.Length;
                pointCount = Container + pointCount;
                if (Container.isLoading)
                    d.points = new ControlPoint[pointCount];
                for (int j = 0; j < pointCount; j++)
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

                if (pointCount > 0)
                {
                    d.unk4 = Container + d.unk4;
                    int numKeysCount = 0;
                    if (!Container.isLoading)
                        numKeysCount = d.numKeys.Length;
                    numKeysCount = Container + numKeysCount;
                    if (Container.isLoading)
                        d.numKeys = new int[numKeysCount];
                    for (int j = 0; j < numKeysCount; j++)
                        d.numKeys[j] = Container + d.numKeys[j];
                }
                else if (Container.isLoading)
                {
                    d.numKeys = new int[d.animations.Length];
                }
                d.FadeInTime = Container + d.FadeInTime;
                d.FadeOutTime = Container + d.FadeOutTime;
                d.unk2 = Container + d.unk2;
                d.unk5 = Container + d.unk5;
                d.path = SerializeString(Container, d.path);
                d.ID = SerializeString(Container, d.ID);
                d.index = Container + d.index;
                data.Data[i] = d;
            }
        }

        string SerializeString(SerializingContainer Container, string s)
        {
            int len = 0;
            byte t = 0;
            ushort unk1 = 1;
            unk1 = Container + unk1;
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
            SerializingContainer Container = new SerializingContainer(m);
            Container.isLoading = false;
            Serialize(Container);
            m = Container.Memory;
            MemoryStream res = new MemoryStream();
            int start = Export.propsEnd();
            res.Write(Export.Data, 0, start);
            res.WriteInt32((int)m.Length);
            res.WriteStream(m);
            Export.Data = res.ToArray();
        }

        public void CloneEntry(int n)
        {
            if (n < 0 || n >= data.Data.Length)
                return;
            List<ME2FaceFXLine> list = new List<ME2FaceFXLine>();
            list.AddRange((ME2FaceFXLine[])data.Data);
            list.Add((ME2FaceFXLine)data.Data[n]);
            data.Data = list.ToArray();
        }
        public void RemoveEntry(int n)
        {
            if (n < 0 || n >= data.Data.Length)
                return;
            List<ME2FaceFXLine> list = new List<ME2FaceFXLine>();
            list.AddRange((ME2FaceFXLine[])data.Data);
            list.RemoveAt(n);
            data.Data = list.ToArray();
        }

        public void MoveEntry(int n, int m)
        {
            if (n < 0 || n >= data.Data.Length || m < 0 || m >= data.Data.Length)
                return;
            List<ME2FaceFXLine> list = new List<ME2FaceFXLine>();
            for (int i = 0; i < data.Data.Length; i++)
                if (i != n)
                    list.Add((ME2FaceFXLine)data.Data[i]);
            list.Insert(m, (ME2FaceFXLine)data.Data[n]);
            data.Data = list.ToArray();
        }

        public void AddName(string s)
        {
            List<string> list = new List<string>(header.Names);
            list.Add(s);
            header.Names = list.ToArray();
        }
    }
}
