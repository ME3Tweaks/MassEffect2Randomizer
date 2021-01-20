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

        /// <summary>
        /// Ports an export into a package.
        /// </summary>
        /// <param name="targetPackage">The target package to port into.</param>
        /// <param name="sourceExport">The source export to port over, including all dependencies and references.</param>
        /// <param name="targetLink">The target link UIndex. Only used if createParentPackages is false.</param>
        /// <param name="createParentPackages">If the export should be ported in the same way as it was cooked into the package natively, e.g. create the parent package paths. The export must directly sit under a Package or an exception will be thrown.</param>
        /// <returns></returns>
        public static ExportEntry PortExportIntoPackage(IMEPackage targetPackage, ExportEntry sourceExport, int targetLink = 0, bool createParentPackages = true)
        {
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
                    var sourceFullPath = p.FullPath;
                    var matchingParent = targetPackage.Exports.FirstOrDefault(x => x.FullPath == sourceFullPath) as IEntry;
                    if (matchingParent == null)
                    {
                        matchingParent = targetPackage.Imports.FirstOrDefault(x => x.FullPath == sourceFullPath) as IEntry;
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

            Dictionary<IEntry, IEntry> crossPCCObjectMap = new Dictionary<IEntry, IEntry>(); // Not sure what this is used for these days. SHould probably just be part of the method
            var relinkResults = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, sourceExport, targetPackage,
                newParent, true, out IEntry newEntry, crossPCCObjectMap);

            if (relinkResults.Any())
            {
                Debugger.Break();
            }

            return newEntry as ExportEntry;
        }
    }
}
