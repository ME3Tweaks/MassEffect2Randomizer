using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class PackageTools
    {
        private static Regex isLevelPersistentPackage = new Regex("Bio([ADPS]|Snd)_[A-Za-z0-9]+.pcc", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex isSublevelPackage = new Regex("Bio([ADPS]|Snd)_[A-Za-z0-9]+_[A-Za-z0-9]+.pcc", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Is this a localization file?
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        public static bool IsLocalizationPackage(string pName)
        {
            return pName.Contains("_LOC_");
        }

        /// <summary>
        /// Is this a top level master file (BioP/S/D/A ?)
        /// </summary>
        /// <param name="pName"></param>
        /// <returns></returns>
        public static bool IsPersistentPackage(string pName)
        {
            return isLevelPersistentPackage.IsMatch(pName);
        }

        public static bool IsLevelSubfile(string pName)
        {
            return isSublevelPackage.IsMatch(pName);
        }

        /// <summary>
        /// Ports an export into a package. Checks if the export already exists, and if it does, returns that instead.
        /// </summary>
        /// <param name="targetPackage">The target package to port into.</param>
        /// <param name="sourceExport">The source export to port over, including all dependencies and references.</param>
        /// <param name="targetLink">The target link UIndex. Only used if createParentPackages is false.</param>
        /// <param name="createParentPackages">If the export should be ported in the same way as it was cooked into the package natively, e.g. create the parent package paths. The export must directly sit under a Package or an exception will be thrown.</param>
        /// <param name="ensureMemoryUniqueness">If this object is an instance, such as a sequence object, and should be made memory-unique so it is properly used</param>
        /// <returns></returns>
        public static ExportEntry PortExportIntoPackage(IMEPackage targetPackage, ExportEntry sourceExport, int targetLink = 0, bool createParentPackages = true, bool ensureMemoryUniqueness = false, bool useMemorySafeImport = false, PackageCache cache = null)
        {
            var existing = targetPackage.FindExport(sourceExport.InstancedFullPath);
            if (existing != null)
                return existing;

            // Create parent heirarchy
            IEntry newParent = null;
            if (createParentPackages)
            {
                List<IEntry> parents = new List<IEntry>();
                var parent = sourceExport.Parent;
                while (parent != null)
                {
                    if (parent.ClassName != "Package")
                        throw new Exception("Parent is not package!");
                    parents.Add(parent);
                    parent = parent.Parent;
                }

                // Create the parents
                parents.Reverse();
                foreach (var p in parents)
                {
                    var sourceFullPath = p.InstancedFullPath;
                    var matchingParent = targetPackage.FindExport(sourceFullPath) as IEntry;
                    if (matchingParent == null)
                    {
                        matchingParent = targetPackage.FindImport(sourceFullPath);
                    }

                    if (matchingParent != null)
                    {
                        newParent = matchingParent;
                        continue;
                    }

                    newParent = ExportCreator.CreatePackageExport(targetPackage, p.ObjectName, newParent);
                }
            }
            else
            {
                newParent = targetPackage.GetEntry(targetLink);
            }


            IEntry newEntry;
            if (!useMemorySafeImport)
            {
                Dictionary<IEntry, IEntry> crossPCCObjectMap = new Dictionary<IEntry, IEntry>(); // Not sure what this is used for these days. Should probably just be part of the method
                var relinkResults = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceExport, targetPackage,
                    newParent, true, out newEntry, crossPCCObjectMap);
                if (relinkResults.Any())
                {
                    Debugger.Break();
                }
            }
            else
            {
                // Memory safe, fixes upstream
                var relinkedResults = EntryExporter.ExportExportToPackage(sourceExport, targetPackage, out newEntry, MERFileSystem.GetGlobalCache(), cache);
                if (relinkedResults.Any())
                {
                    Debugger.Break();
                }
            }

            // Helps ensure we don't have memory duplicates
            if (ensureMemoryUniqueness)
            {
                newEntry.ObjectName = targetPackage.GetNextIndexedName(newEntry.ObjectName);
            }

            return newEntry as ExportEntry;
        }

        /// <summary>
        /// Creates an ImportEntry that references the listed ExportEntry. Ensure the item will be in memory or this will crash the game!
        /// </summary>
        /// <param name="sourceExport"></param>
        /// <param name="targetPackage"></param>
        /// <returns></returns>
        public static ImportEntry CreateImportForClass(ExportEntry sourceExport, IMEPackage targetPackage, IEntry parentObject = null)
        {
            if (sourceExport.ClassName != "Class")
            {
                throw new Exception("Cannot reliably create import for non-class object!");
            }

            var existingImport = targetPackage.FindImport(sourceExport.InstancedFullPath);
            if (existingImport != null)
            {
                return existingImport;
            }

            ImportEntry imp = new ImportEntry(targetPackage)
            {
                ObjectName = sourceExport.ObjectName,
                PackageFile = "Core", //Risky...
                ClassName = sourceExport.ClassName,
                idxLink = parentObject?.UIndex ?? 0,
            };
            targetPackage.AddImport(imp);
            return imp;
        }

        public static void AddReferencesToWorld(IMEPackage package, IEnumerable<ExportEntry> newRefs)
        {
            var theWorld = package.FindExport("TheWorld");
            var world = ObjectBinary.From<World>(theWorld);
            var extarRefs = world.ExtraReferencedObjects.ToList();
            extarRefs.AddRange(newRefs.Select(x => new UIndex(x.UIndex)));
            world.ExtraReferencedObjects = extarRefs.Distinct().ToArray();
            theWorld.WriteBinary(world);
        }
    }
}
