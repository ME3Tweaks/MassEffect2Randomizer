﻿using System;
using System.ComponentModel;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats.Save
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public partial class Vector : IUnrealSerializable
    {
        [UnrealFieldDisplayName("X")]
        public float X;

        [UnrealFieldDisplayName("Y")]
        public float Y;

        [UnrealFieldDisplayName("Z")]
        public float Z;

        public void Serialize(IUnrealStream stream)
        {
            stream.Serialize(ref this.X);
            stream.Serialize(ref this.Y);
            stream.Serialize(ref this.Z);
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}",
                this.X,
                this.Y,
                this.Z);
        }
    }
}
