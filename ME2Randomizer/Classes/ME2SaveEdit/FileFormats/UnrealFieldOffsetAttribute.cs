using System;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats
{
    public class UnrealFieldOffsetAttribute : Attribute
    {
        public uint Offset;

        public UnrealFieldOffsetAttribute(uint offset)
        {
            this.Offset = offset;
        }
    }
}
