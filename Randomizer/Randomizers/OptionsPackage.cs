using System.Collections.Generic;

namespace Randomizer.Randomizers
{
    public class OptionsPackage
    {
        /// <summary>
        /// The seed used for this run
        /// </summary>
        public int Seed { get; set; }
        /// <summary>
        /// If randomization should be multithreaded. Multithreaded randomizations cannot use a seed as the thread that picks up a file cannot be guaranteed (at least, not without a lot of extra work).
        /// </summary>
        public bool UseMultiThread { get; set; }
        /// <summary>
        /// The list of randomization options that were selected
        /// </summary>
        public List<RandomizationOption> SelectedOptions { get; set; }
        /// <summary>
        /// If true, remove the DLC mod component before install, which prevents stacking for most randomization. If false, the DLC component will stay, which stacks changes
        /// </summary>
        public bool Reroll { get; set; }
    }
}
