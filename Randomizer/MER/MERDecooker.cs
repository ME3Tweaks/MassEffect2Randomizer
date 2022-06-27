using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.Randomizers;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.MER
{

    /// <summary>
    /// Class that contains information about how to decook an object out of a file.
    /// </summary>
    public class ObjectDecookInfo
    {
        /// <summary>
        /// Which file to open to find the data in
        /// </summary>
        public string SourceFileName { get; set; }

        /// <summary>
        /// Contains the asset path and the destination package name
        /// </summary>
        public SeekFreeInfo SeekFreeInfo { get; set; }
    }

    /// <summary>
    /// Information about how to seek free reference an object.
    /// </summary>
    public class SeekFreeInfo
    {
        /// <summary>
        /// The entry path to map to a package
        /// </summary>
        public string EntryPath { get; set; }
        /// <summary>
        /// The package that contains the specified EntryPath for loading if not in memory
        /// </summary>
        public string SeekFreePackage { get; set; }
        /// <summary>
        /// Generates the struct text used in Coalesced files
        /// </summary>
        public string GetSeekFreeStructText() => $"(ObjectName=\"{EntryPath}\",SeekFreePackageName=\"{SeekFreePackage}\", bReplicate=true)";
    }


    /// <summary>
    /// Class for decooking objects to individual package files for optimized loading.
    /// </summary>
    public static class MERDecooker
    {
        /// <summary>
        /// Decooks objects out of cooked package files and stores them individually into package files. Does a memory safe resolution to help ensure all assets required will be available on load.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="option"></param>
        /// <param name="objectsToDecook"></param>
        /// <param name="operationTextBase"></param>
        /// <param name="addSeekFree">If a seek free reference should be added to sfxengine</param>
        public static void DecookObjectsToPackages(GameTarget target, RandomizationOption option, List<ObjectDecookInfo> objectsToDecook, string operationTextBase, bool addSeekFree)
        {
            MERPackageCache gc = new MERPackageCache(target);
            MERPackageCache c = new MERPackageCache(target);
            option.ProgressMax = objectsToDecook.Count;
            option.ProgressValue = 0;
            option.ProgressIndeterminate = false;
            option.CurrentOperation = operationTextBase;

            ConcurrentDictionary<int, CoalesceValue> mappings = new ConcurrentDictionary<int, CoalesceValue>();

#if DEBUG
            Parallel.ForEach(objectsToDecook, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, di =>
#else
            Parallel.ForEach(objectsToDecook, di =>

#endif
            {
                //foreach (var di in decooksRequired)
                //{
                // If there is a source filename, we need to decook it
                if (di.SourceFileName != null)
                {
                    var cachedPacakge = c.GetCachedPackage(di.SourceFileName);
                    var objRef = PackageTools.CreateObjectReferencer(cachedPacakge);
                    objRef.WriteProperty(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(cachedPacakge.FindExport(di.SeekFreeInfo.EntryPath).UIndex) }, "ReferencedObjects")); // Write the reference - overwrite if same cached package

                    var outPath = Path.Combine(MERFileSystem.DLCModCookedPath, $"{di.SeekFreeInfo.SeekFreePackage}.pcc");
                    var results = EntryExporter.ExportExportToFile(objRef, outPath, out _, globalCache: gc, pc: c);
                    if (results != null)
                    {
                        foreach (var v in results)
                        {
                            Debug.WriteLine(v.Message);
                        }
                    }
                }

                mappings[option.IncrementProgressValue()] = new CoalesceValue(di.SeekFreeInfo.GetSeekFreeStructText(), CoalesceParseAction.AddUnique);
                //}
            });

            // Add seek free info

            if (addSeekFree)
            {
                CoalescedHandler.AddDynamicLoadMappingEntries(mappings.Select(x => x.Value));
            }
        }
    }
}
