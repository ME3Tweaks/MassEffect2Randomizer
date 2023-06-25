using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace Randomizer.Randomizers.Shared.Classes
{
    // Old code maybe?


    public class Gesture
    {
        public string GestureSet { get; set; }
        public NameReference GestureAnim { get; set; }
        /// <summary>
        /// Entry that was used to generate the info for this Gesture if it was loaded from an export
        /// </summary>
        public IEntry Entry { get; set; }
        public Gesture(StructProperty structP)
        {
            GestureSet = structP.GetProp<NameProperty>("nmGestureSet").Value;
            GestureAnim = structP.GetProp<NameProperty>("nmGestureAnim").Value;
        }

        public Gesture(ExportEntry export)
        {
            GestureAnim = export.GetProperty<NameProperty>("SequenceName").Value;
            GestureSet = export.ObjectName.Name.Substring(0, export.ObjectName.Instanced.Length - GestureAnim.Instanced.Length - 1); // -1 for _
            Entry = export;

        }

        public Gesture() { }

        /// <summary>
        /// Fetches the IEntry that this gesture uses by looking up the animsequence and it's listed bioanimset. Can return null if it can't be found!
        /// </summary>
        /// <param name="exportFileRef"></param>
        /// <returns></returns>
        public IEntry GetBioAnimSet(IMEPackage exportFileRef, Dictionary<string, string> GestureSetNameToPackageExportName)
        {
            if (PopulateEntry(exportFileRef, GestureSetNameToPackageExportName) && Entry is ExportEntry exp)
            {
                return exp.GetProperty<ObjectProperty>("m_pBioAnimSetData")?.ResolveToEntry(exportFileRef);
            }

            return null;
        }

        /// <summary>
        /// Populates the Entry variable, locating the animsequence in the specified package
        /// </summary>
        /// <param name="exportFileRef"></param>
        /// <returns></returns>
        private bool PopulateEntry(IMEPackage exportFileRef, Dictionary<string, string> GestureSetNameToPackageExportName)
        {
            if (Entry != null) return true;
            if (GestureSetNameToPackageExportName.TryGetValue(GestureSet, out var pName))
            {
                Entry = exportFileRef.FindEntry($"{pName}.{GestureSet}_{GestureAnim}");
                return Entry != null;
            }

            return false;
        }
    }
}
