using System;
using System.Collections.Concurrent;
using System.IO;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers
{
    class NonSharedPackageCache
    {
        public static MERPackageCache Cache = new MERPackageCache();
    }
}
