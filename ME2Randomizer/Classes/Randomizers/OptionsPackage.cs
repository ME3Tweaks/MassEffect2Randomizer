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
        /// If we should use the MER FileSystem, aka a DLC mod when possible
        /// </summary>
        public bool UseMERFS { get; set; }
        /// <summary>
        /// The list of randomization options that were selected
        /// </summary>
        public List<RandomizationOption> SelectedOptions { get; set; }
    }
}
