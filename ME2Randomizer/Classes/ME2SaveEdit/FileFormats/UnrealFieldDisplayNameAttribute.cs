using System;

namespace RandomizerUI.Classes.ME2SaveEdit.FileFormats
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
