using System.Linq;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Misc;
using Randomizer.Randomizers.Handlers;

namespace Randomizer.Randomizers.Utility
{
    /// <summary>
    /// TODO: NEEDS FIXED TO WORK ON OTHER GAMES BESIDES ME2
    /// </summary>
    public static class ThreadSafeDLCStartupPackage
    {
        private static object syncObj = new object();

        /// <summary>
        /// Adds a startup package to always be loaded in memory. Be extremely careful using this.
        /// </summary>
        /// <param name="packagename"></param>
        /// <returns></returns>
        public static bool AddStartupPackage(string packagename)
        {
            lock (syncObj)
            {
#if __GAME2__
                var engine = CoalescedHandler.GetIniFile("BIOEngine.ini");
                var sp = engine.GetOrAddSection("Engine.StartupPackages");
                sp.AddEntryIfUnique(new CoalesceProperty("DLCStartupPackage", new CoalesceValue(packagename, CoalesceParseAction.AddUnique)));
                return true;
#elif __GAME3__
                var engine = CoalescedHandler.GetIniFile("BioEngine.xml");
                var sp = engine.GetOrAddSection("engine.startuppackages");
                sp.AddEntryIfUnique(new CoalesceProperty("dlcstartuppackage", new CoalesceValue(packagename, CoalesceParseAction.AddUnique)));
                return true;
#endif
            }
            return false;
        }
    }
}
