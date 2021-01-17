using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class PackageTools
    {
        private static Regex isLevelPersistentPackage = new Regex("Bio[ADPS]_[^_]+.pcc", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsLocalizationPackage(string pName)
        {
            return pName.Contains("_LOC_");
        }

        public static bool IsPersistentPackage(string pName)
        {
            return isLevelPersistentPackage.IsMatch(pName);
        }
    }
}
