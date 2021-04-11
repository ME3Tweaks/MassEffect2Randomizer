using System;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats
{
    public class UnrealFieldDisplayNameAttribute : Attribute
    {
        public string Name;

        public UnrealFieldDisplayNameAttribute(string name)
        {
            this.Name = name;
        }
    }
}
