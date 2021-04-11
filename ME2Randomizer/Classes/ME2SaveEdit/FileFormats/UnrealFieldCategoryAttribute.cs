using System;

namespace ME2Randomizer.Classes.ME2SaveEdit.FileFormats
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
