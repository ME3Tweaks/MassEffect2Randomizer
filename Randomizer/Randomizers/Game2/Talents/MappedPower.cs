using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Game2.Talents
{
    /// <summary>
    /// Defines a full power - the starting rank, the base power, and ranks 4a and 4b
    /// </summary>
    public class MappedPower
    {
        public int StartingRank { get; set; }
        public HTalent BaseTalent { get; set; }
        public HTalent EvolvedTalent1 { get; set; }
        public HTalent EvolvedTalent2 { get; set; }
    }
}
