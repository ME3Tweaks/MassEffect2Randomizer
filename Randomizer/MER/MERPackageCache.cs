using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Serilog;
using WinCopies.Util;

namespace Randomizer.MER
{
    /// <summary>
    /// PackageCache for Mass Effect Randomizer that enables parent cache lookups, read only opening, and opening from specific targets
    /// </summary>
    public class MERPackageCache : PackageCache
    {
        /// <summary>
        /// Target to fetch packages out of for MER. This must be set or an exception will be thrown.
        /// </summary>
        private readonly GameTarget Target;

        /// <summary>
        /// Cache to also look in for packages
        /// </summary>
        public MERPackageCache ParentCache;

        /// <summary>
        /// If packages opened from this cache can be saved (through MERFS)
        /// </summary>
        private bool PreventSaves;

        /// <summary>
        /// Creates a tiered cache for the specified target with the parent cache to look into if necessary
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parent"></param>
        /// <param name="preventSaves"></param>
        public MERPackageCache(GameTarget target, MERPackageCache parent, bool preventSaves)
        {
            Target = target;
            ParentCache = parent;
            PreventSaves = preventSaves;
        }

        /// <summary>
        /// Returns a cached package. Ensure this cache is synchronized if across threads or you may end up saving two different instances of files to the same location
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public override IMEPackage GetCachedPackage(string packageName, bool openIfNotInCache = true, Func<string, IMEPackage> openPackageMethod = null)
        {
            var parentP = ParentCache?.GetCachedPackage(packageName, false);
            if (parentP != null)
                return parentP;

            // May need way to set maximum size of dictionary so we don't hold onto too much memory.
            packageName = Path.GetFileName(packageName); // Ensure we only use filename

            parentP = ParentCache?.GetCachedPackage(packageName, false);
            if (parentP != null)
                return parentP;

            if (Cache.TryGetValue(packageName, out var package))
            {
                return package;
            }

            if (openIfNotInCache)
            {
                var file = MERFileSystem.GetPackageFile(Target, packageName, false);
                if (file != null && File.Exists(file))
                {
                    int i = 3;
                    while (i > 0)
                    {
                        try
                        {
                            i--;
                            package = MERFileSystem.OpenMEPackage(file);
                            MERFileSystem.SetReadOnly(package, PreventSaves);
                        }
                        catch (IOException e)
                        {
                            // This is a cheap hack around potential multithreading issues
                            MERLog.Warning($@"I/O Exception opening {file}: {e.Message}. We have {i} attempts remaining to open this package");
                            Thread.Sleep(1000);
                        }
                    }

                    if (package == null)
                        return null;

                    InsertIntoCache(package);
                    return package;
                }
            }
            return null; //Package could not be found
        }

        /// <summary>
        /// Returns a cached package that references an internally embedded package. Ensure this cache is synchronized across threads or you may end up saving two different instances of files to the same location
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public IMEPackage GetCachedPackageEmbedded(MEGame game, string embeddedPath, bool openIfNotInCache = true)
        {
            // May need way to set maximum size of dictionary so we don't hold onto too much memory.
            if (Cache.TryGetValue(embeddedPath, out var package))
            {
                return package;
            }

            if (openIfNotInCache)
            {
                var embeddedData = MEREmbedded.GetEmbeddedPackage(game, embeddedPath);
                if (embeddedData != null)
                {
                    package = MEPackageHandler.OpenMEPackageFromStream(embeddedData);
                    MERFileSystem.SetReadOnly(package, PreventSaves);
                    Cache[embeddedPath] = package;
                    return package;
                }
            }
            return null; //Package could not be found
        }

        public override void InsertIntoCache(IMEPackage package)
        {
            Cache[Path.GetFileName(package.FilePath)] = package;
            LastAccessMap[Path.GetFileName(package.FilePath)] = DateTime.Now;
            CheckCacheFullness();
        }

        public override void CheckCacheFullness()

        {
            if (CacheMaxSize > 1 && Cache.Count > CacheMaxSize)
            {
                var accessOrder = LastAccessMap.OrderBy(x => x.Value).ToList();
                while (CacheMaxSize > 1 && Cache.Count > CacheMaxSize)
                {
                    // Find the oldest package
                    if (!ResidentPackages.Contains(accessOrder[0].Key))
                    {
                        ReleasePackage(accessOrder[0].Key);
                    }
                    accessOrder.RemoveAt(0);
                }
            }

            if (CacheMaxSize == 0)
            {
                //Debug.WriteLine(guid);
                //Debugger.Break();
            }
        }

        public IReadOnlyCollection<IMEPackage> GetPackages()
        {
            return new ReadOnlyCollection<IMEPackage>(Cache.Values.ToList());
        }
    }
}
