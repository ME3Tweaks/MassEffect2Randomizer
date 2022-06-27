using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3TweaksCore.Targets;

namespace Randomizer.MER
{
    internal class MERCaches
    {
        public static void Init(GameTarget target)
        {
            LookupCache = new MERPackageCache(target);
        }

        public static void Cleanup()
        {
            LookupCache?.Dispose();
            LookupCache = null;
        }

        public static MERPackageCache LookupCache;
    }
}
