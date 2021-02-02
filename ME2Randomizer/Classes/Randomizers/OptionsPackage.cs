using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers
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
    }
}
