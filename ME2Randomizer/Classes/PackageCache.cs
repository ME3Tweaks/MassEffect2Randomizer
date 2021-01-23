using ME3ExplorerCore.Packages;
using System;
using System.IO;
using ME2Randomizer.Classes.gameini;

namespace ME2Randomizer.Classes
{
    /// <summary>
    /// Class that allows you to cache packages in memory for fast accessing, without having to use a global package cache like ME3Explorer's system
    /// </summary>
    class PackageCache
    {
        private CaseInsensitiveConcurrentDictionary<IMEPackage> Cache = new CaseInsensitiveConcurrentDictionary<IMEPackage>();

        /// <summary>
        /// Returns a cached package. Ensure this cache is synchronized if across threads or you may end up saving two different instances of files to the same location
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public IMEPackage GetCachedPackage(string packageName, bool openIfNotInCache = true)
        {
            // May need way to set maximum size of dictionary so we don't hold onto too much memory.
            if (Cache.TryGetValue(packageName, out var package))
            {
                return package;
            }

            if (openIfNotInCache)
            {
                var file = MERFileSystem.GetPackageFile(packageName);
                if (file != null && File.Exists(file))
                {
                    package = MEPackageHandler.OpenMEPackage(file);
                    Cache[packageName] = package;
                    return package;
                }
            }
            return null; //Package could not be found
        }

        public void ReleasePackages()
        {
            Cache.Clear();
            GC.Collect(); // This may drop a large amount of memory so we should just force a GC
        }
    }
}
