using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class SizeSixteensChatHandler
    {
        /// <summary>
        /// People who survived ME1's onslaught of SizeSixteens' stream
        /// </summary>
        private static string[] SizeSixteenChatMembers = new[]
        {
            "Nalie Walie",
            "Jed Ted",
            "Mok",
            "Shamrock Snipes",
            "Steeler Wayne",
            "Castle Arrrgh",
            "Bev",
            "Lurxx",
            "Chirra Kitteh",
            "Daynan",
            "Peeress Sabine", //new 
            "dnc510", // new

            "Criken", // NEW
            "LittleSiha" // NEW
        };

        private static List<string> AvailableMembers;

        public static void ResetClass()
        {
            AvailableMembers = SizeSixteenChatMembers.ToList();
            AvailableMembers.Shuffle();
        }

        public static string GetMember()
        {
            return AvailableMembers.PullFirstItem();
        }
    }
}
