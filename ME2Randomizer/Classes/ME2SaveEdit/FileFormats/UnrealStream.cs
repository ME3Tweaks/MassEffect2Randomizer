using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats
{
    public class UnrealStream : IUnrealStream
    {
        private Stream Stream;
        private bool Loading;
        public uint Version { get; private set; }

        public UnrealStream(Stream stream, bool loading, uint version)
        {
            this.Stream = stream;
            this.Loading = loading;
            this.Version = version;
        }

        public void Serialize(ref bool value)
        {
            if (this.Loading == true)
            {
                value = this.Stream.ReadInt32() != 0;
            }
            else
            {
                this.Stream.WriteUInt32(value ? 1u : 0u);
            }
        }

        public void Serialize(ref byte value)
        {
            if (this.Loading == true)
            {
                value = (byte) this.Stream.ReadByte(); //does not catch end of stream condition!
            }
            else
            {
                this.Stream.WriteByte(value);
            }
        }

        public void Serialize(ref int value)
        {
            if (this.Loading == true)
            {
                value = this.Stream.ReadInt32();
            }
            else
            {
                this.Stream.WriteInt32(value);
            }
        }

        public void Serialize(ref uint value)
        {
            if (this.Loading == true)
            {
                value = this.Stream.ReadUInt32();
            }
            else
            {
                this.Stream.WriteUInt32(value);
            }
        }

        public void Serialize(ref float value)
        {
            if (this.Loading == true)
            {
                value = this.Stream.ReadFloat();
            }
            else
            {
                this.Stream.WriteFloat(value);
            }
        }

        public void Serialize(ref string value)
        {
            if (this.Loading == true)
            {
                value = this.Stream.ReadUnrealString();
            }
            else
            {
                this.Stream.WriteUnrealString(value, MEGame.ME2);
            }
        }

        public void Serialize(ref Guid value)
        {
            if (this.Loading == true)
            {
                value = this.Stream.ReadGuid();
            }
            else
            {
                this.Stream.WriteGuid(value);
            }
        }

        public void SerializeEnum<TEnum>(ref TEnum value)
        {
            if (this.Loading == true)
            {
                value = this.Stream.ReadValueEnum<TEnum>();
            }
            else
            {
                this.Stream.WriteValueEnum<TEnum>(value);
            }
        }

        public void Serialize(ref List<bool> values)
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                List<bool> list = new List<bool>();

                for (uint i = 0; i < count; i++)
                {
                    list.Add(this.Stream.ReadInt32() != 0);
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                this.Stream.WriteInt32(values.Count);
                foreach (bool value in values)
                {
                    this.Stream.WriteUInt32(value ? 1u : 0u);
                }
            }
        }

        public void Serialize(ref List<int> values)
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                List<int> list = new List<int>();

                for (uint i = 0; i < count; i++)
                {
                    list.Add(this.Stream.ReadInt32());
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                this.Stream.WriteInt32(values.Count);
                foreach (int value in values)
                {
                    this.Stream.WriteInt32(value);
                }
            }
        }

        public void Serialize(ref List<uint> values)
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                List<uint> list = new List<uint>();

                for (uint i = 0; i < count; i++)
                {
                    list.Add(this.Stream.ReadUInt32());
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                this.Stream.WriteInt32(values.Count);
                foreach (uint value in values)
                {
                    this.Stream.WriteUInt32(value);
                }
            }
        }

        public void Serialize(ref List<float> values)
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                List<float> list = new List<float>();

                for (uint i = 0; i < count; i++)
                {
                    list.Add(this.Stream.ReadFloat());
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                this.Stream.WriteInt32(values.Count);
                foreach (float value in values)
                {
                    this.Stream.WriteFloat(value);
                }
            }
        }

        public void Serialize(ref List<string> values)
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                List<string> list = new List<string>();

                for (uint i = 0; i < count; i++)
                {
                    list.Add(this.Stream.ReadUnrealString());
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                this.Stream.WriteInt32(values.Count);
                foreach (string value in values)
                {
                    this.Stream.WriteUnrealString(value, MEGame.ME2); // ME2 SPECIFIC
                }
            }
        }

        public void Serialize(ref List<Guid> values)
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                List<Guid> list = new List<Guid>();

                for (uint i = 0; i < count; i++)
                {
                    list.Add(this.Stream.ReadGuid());
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                this.Stream.WriteInt32(values.Count);
                foreach (Guid value in values)
                {
                    this.Stream.WriteGuid(value);
                }
            }
        }

        public void Serialize(ref BitArray values)
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                BitArray list = new BitArray((int)(count * 32));

                for (uint i = 0; i < count; i++)
                {
                    uint offset = i * 32;
                    int value = this.Stream.ReadInt32();

                    for (int bit = 0; bit < 32; bit++)
                    {
                        list.Set((int)(offset + bit), (value & (1 << bit)) != 0);
                    }
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                uint count = ((uint)values.Count + 31) / 32;
                this.Stream.WriteUInt32(count);

                for (uint i = 0; i < count; i++)
                {
                    uint offset = i * 32;
                    int value = 0;
                    
                    for (int bit = 0; bit < 32 && offset + bit < values.Count; bit++)
                    {
                        value |= (values.Get((int)(offset + bit)) ? 1 : 0) << bit;
                    }

                    this.Stream.WriteInt32(value);
                }
            }
        }

        public void Serialize<TFormat>(ref TFormat value)
            where TFormat : IUnrealSerializable, new()
        {
            if (this.Loading == false && value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (this.Loading == true)
            {
                value = new TFormat();
            }

            value.Serialize(this);
        }

        public void Serialize<TFormat>(ref List<TFormat> values)
            where TFormat : IUnrealSerializable, new()
        {
            if (this.Loading == true)
            {
                uint count = this.Stream.ReadUInt32();

                if (count >= 0x7FFFFF)
                {
                    throw new Exception("sanity check");
                }

                List<TFormat> list = new List<TFormat>();

                for (uint i = 0; i < count; i++)
                {
                    TFormat value = new TFormat();
                    value.Serialize(this);
                    list.Add(value);
                }

                values = list;
            }
            else
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }

                this.Stream.WriteInt32(values.Count);
                foreach (TFormat value in values)
                {
                    value.Serialize(this);
                }
            }
        }
    }
}
