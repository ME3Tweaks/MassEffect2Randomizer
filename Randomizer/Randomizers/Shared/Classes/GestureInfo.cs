using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;

namespace Randomizer.Randomizers.Shared.Classes
{
    /// <summary>
    /// Class for passing around Gesture-related data
    /// </summary>
    class GestureInfo
    {
        public ExportEntry GestureAnimSequence { get; set; }
        public ExportEntry GestureAnimSetData { get; set; }
        public string GestureGroup { get; set; }
        /// <summary>
        /// Name of the gesture sequence (like WI_SittingIdle)
        /// </summary>
        public string GestureName => GestureAnimSequence.ObjectName.Name.Substring(GestureGroup.Length + 1);
    }
}
