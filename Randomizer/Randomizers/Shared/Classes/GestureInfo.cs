using LegendaryExplorerCore.Packages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Shared.Classes
{
    /// <summary>
    /// Class for passing around Gesture-related data
    /// </summary>
    class GestureInfo
    {
        /// <summary>
        /// The AnimSequence (animation) data
        /// </summary>
        public ExportEntry GestureAnimSequence { get; set; }

        /// <summary>
        /// The animset data (controls the bones)
        /// </summary>
        public ExportEntry GestureAnimSetData { get; set; }

        /// <summary>
        /// The group of the gesture (e.g. HMF_Towny)
        /// </summary>
        public string GestureGroup { get; set; }

        /// <summary>
        /// Name of the gesture sequence (like WI_SittingIdle)
        /// </summary>
        public string GestureName => GestureAnimSequence.ObjectName.Name.Substring(GestureGroup.Length + 1);
    }
}
