using System;

namespace RandomizerUI.Classes.ME2SaveEdit.FileFormats
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
