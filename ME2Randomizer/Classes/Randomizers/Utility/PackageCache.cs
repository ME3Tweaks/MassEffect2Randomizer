using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    /// <summary>
    /// Basic implementation of a memory package cache. Take care not to put too many items into it! 
    /// </summary>
    class PackageCache
    {
        private Dictionary<string, IMEPackage> CacheMap = new Dictionary<string, IMEPackage>();
        public IMEPackage GetPackage(string packageName)
        {
            var packageFile = MERFileSystem.GetPackageFile(packageName);
            if (packageFile != null && File.Exists(packageFile))
            {
                if (CacheMap.TryGetValue(packageFile, out var package))
                {
                    return package;
                }

                package = MEPackageHandler.OpenMEPackage(packageFile);
                CacheMap[packageName] = package;
                return package;
            }

            return null;
        }
    }
}
