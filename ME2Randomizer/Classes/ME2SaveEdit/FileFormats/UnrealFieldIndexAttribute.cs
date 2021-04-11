﻿using System;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats
{
    public class UnrealFieldIndexAttribute : Attribute
    {
        public uint Index;

        public UnrealFieldIndexAttribute(uint index)
        {
            this.Index = index;
        }
    }
}
