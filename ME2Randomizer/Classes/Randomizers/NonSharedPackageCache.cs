using System;
using System.Collections.Concurrent;
using System.IO;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers
{
    class NonSharedPackageCache
    {
        private static ConcurrentDictionary<string, IMEPackage> Cache = new ConcurrentDictionary<string, IMEPackage>();

        /// <summary>
        /// Returns a cached package. Do not modify packages returned by the cache, they are for asset fetching only!
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public static IMEPackage GetCachedPackage(string packageName)
        {
            // May need way to set maximum size of dictionary so we don't hold onto too much memory.
            if (Cache.TryGetValue(packageName, out var package))
            {
                return package;
            }

            var file = MERFileSystem.GetPackageFile(packageName);
            if (file != null && File.Exists(file))
            {
                package = MEPackageHandler.OpenMEPackage(file);
                Cache[packageName] = package;
                return package;
            }

            return null; //Package could not be found
        }

        public static void ReleasePackages()
        {
            Cache.Clear();
            GC.Collect(); // This may drop a large amount of memory so we should just force a GC
        }
    }
}
