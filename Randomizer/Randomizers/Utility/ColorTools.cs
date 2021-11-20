using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Utility
{
    class ColorTools
    {

        /// <summary>
        /// Gets a random color RGB string (typically used in 2DAs) in form of 'RGB(x,y,z)'
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        internal static string GetRandomColorRBGStr()
        {
            return $"RGB({ThreadSafeRandom.Next(255)},{ThreadSafeRandom.Next(255)},{ThreadSafeRandom.Next(255)})";
        }
    }
}
