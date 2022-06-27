using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.Randomizers.Utility;

namespace Randomizer.MER
{
    internal class MERDebug
    {
        public static void InstallDebugScript(GameTarget target, string packagename, string scriptName)
        {
#if DEBUG
            Debug.WriteLine($"Installing debug script {scriptName}");
            ScriptTools.InstallScriptToPackage(target, packagename, scriptName, "Debug." + scriptName + ".uc", false, true);
#endif
        }

        public static void InstallDebugScript(IMEPackage package, string scriptName, bool saveOnFinish)
        {
#if DEBUG
            Debug.WriteLine($"Installing debug script {scriptName}");
            ScriptTools.InstallScriptToPackage(package, scriptName, "Debug." + scriptName +".uc", false, saveOnFinish);
#endif
        }
    }
}
