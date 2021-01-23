using System;
using System.Collections.Concurrent;
using System.IO;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers
{
    class NonSharedPackageCache
    {
        public static PackageCache Cache = new PackageCache();
    }
}
