using System.Collections.Generic;
using System.Diagnostics;

namespace Randomizer.Randomizers.Shared.Classes
{
    /// <summary>
    /// Contains information about a randomized planet object that will be installed into the game.
    /// </summary>
    [DebuggerDisplay("RandomPlanetInfo ({PlanetName}) - Playable: {Playable}")]
    public class RandomizedPlanetInfo
    {
        /// <summary>
        /// What 0-based row this planet information is for in the Bio2DA - GAME1 ONLY
        /// </summary>
        public int RowID;

        /// <summary>
        /// Prevents shuffling this item outside of it's row ID
        /// </summary>
        public bool PreventShuffle;

        /// <summary>
        /// Indicator that this is an MSV planet
        /// </summary>
        public bool IsMSV;

        /// <summary>
        /// Indicator that this is an Asteroid Belt
        /// </summary>
        public bool IsAsteroidBelt;

        /// <summary>
        /// Indicator that this is an Asteroid
        /// </summary>
        public bool IsAsteroid;

        /// <summary>
        /// Name to assign for randomization. If this is a plot planet, this value is the original planet name
        /// </summary>
        public string PlanetName;

        /// <summary>
        /// Name used for randomizing if it is a plot planet and the plot planet option is on
        /// </summary>
        public string PlanetName2;

        /// <summary>
        /// Description of the planet in the Galaxy Map
        /// </summary>
        public string PlanetDescription;

        /// <summary>
        /// WHen updating 2DA_AreaMap, labels that begin with these prefixes will be analyzed and updated accordingly by full (if no :) or anything before :.
        /// NOTE: THIS IS UNUSED... I THINK
        /// </summary>
        public List<string> MapBaseNames { get; internal set; }

        /// <summary>
        /// Category of image to use. Ensure there are enough images in the imagegroup folder.
        /// </summary>
        public string ImageGroup { get; internal set; }
        /// <summary>
        /// DLC folder this RPI belongs to. Can be UNC, Vegas, or null. Used with PreventShuffle as some Row ID's will be the same.
        /// </summary>
        public string DLC { get; internal set; }

        /// <summary>
        /// Text to assign the action button if the row has an action button (like Land or Survey)
        /// </summary>
        public string ButtonLabel { get; set; }
        public bool Playable { get; internal set; }
    }
}
