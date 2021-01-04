using System;
using System.Collections.Generic;
using System.Text;
using ME3ExplorerCore.Misc;

namespace ME2Randomizer.Classes
{
    public class RandomizationGroup
    {
        /// <summary>
        /// The header of the group.
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// The list of randomization options in this group.
        /// </summary>
        public ObservableCollectionExtended<RandomizationOption> Options { get; init; }
    }
}
