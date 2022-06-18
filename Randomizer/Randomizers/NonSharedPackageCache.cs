using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers
{
    class NonSharedPackageCache
    {
        public static MERPackageCache Cache { get; private set; }
        public static void InitNonSharedPackageCache(GameTarget target) => Cache = new MERPackageCache(target);
    }
}