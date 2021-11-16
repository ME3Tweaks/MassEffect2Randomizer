using System;

namespace RandomizerUI.Classes.ME2SaveEdit.FileFormats
{
    public class UnrealFieldCategoryAttribute : Attribute
    {
        public string Category;

        public UnrealFieldCategoryAttribute(string category)
        {
            this.Category = category;
        }
    }
}
