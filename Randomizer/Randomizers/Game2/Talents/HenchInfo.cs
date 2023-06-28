using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;

namespace Randomizer.Randomizers.Game2.Talents
{
    /// <summary>
    /// Contains information about a henchman and their talents, as well as info about their passive strings
    /// </summary>
    internal class HenchInfo
    {
        /// <summary>
        /// The internal name for this henchman
        /// </summary>
        public string HenchInternalName { get; set; }
        /// <summary>
        /// The list of packages that contain this henchmen
        /// </summary>
        public List<IMEPackage> Packages { get; } = new List<IMEPackage>();

        /// <summary>
        /// Loadout InstancedFullPaths. Samara has multiple loadouts
        /// </summary>
        public List<HenchLoadoutInfo> PackageLoadouts { get; } = new List<HenchLoadoutInfo>();
        /// <summary>
        /// Hint for loadout inventory to only match on object named this. If null it'll pull in all loadouts in the package file that start with hench_.
        /// </summary>
        public string LoadoutHint { get; set; }

        public HenchInfo(string internalName = null)
        {
            HenchInternalName = internalName;
        }

        public void ResetTalents()
        {
            foreach (var l in PackageLoadouts)
            {
                l.ResetTalents();
            }
        }

        public void ResetEvolutions()
        {
            foreach (var l in PackageLoadouts)
            {
                l.ResetEvolutions();
            }
        }
    }
}
