using System;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats
{
    public class UnrealFieldDescriptionAttribute : Attribute
    {
        public string Description;

        public UnrealFieldDescriptionAttribute(string description)
        {
            this.Description = description;
        }
    }
}
