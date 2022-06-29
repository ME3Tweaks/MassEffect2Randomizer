using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.MER
{
    public class MERDebug
    {
        public static void InstallDebugScript(GameTarget target, string packagename, string scriptName)
        {
#if DEBUG
            Debug.WriteLine($"Installing debug script {scriptName}");
            ScriptTools.InstallScriptToPackage(target, packagename, scriptName, "Debug." + scriptName + ".uc", false,
                true);
#endif
        }

        public static void InstallDebugScript(IMEPackage package, string scriptName, bool saveOnFinish)
        {
#if DEBUG
            Debug.WriteLine($"Installing debug script {scriptName}");
            ScriptTools.InstallScriptToPackage(package, scriptName, "Debug." + scriptName + ".uc", false, saveOnFinish);
#endif
        }

        public static void DebugPrintActorNames(object sender, RunWorkerCompletedEventArgs e)
        {
            var game = MEGame.LE3;
            var files = MELoadedFiles.GetFilesLoadedInGame(game, true, false).Values
                //.Where(x =>
                //                    !x.Contains("_LOC_")
                //&& x.Contains(@"CitHub", StringComparison.InvariantCultureIgnoreCase)
                //)
                //.OrderBy(x => x.Contains("_LOC_"))
                .ToList();

            // PackageName -> GesturePackage
            int i = 0;
            SortedSet<string> actorTypeNames = new SortedSet<string>();
            TLKBuilder.StartHandler(new GameTarget(game, MEDirectories.GetDefaultGamePath(game), false));
            foreach (var f in files)
            {
                i++;
                var p = MEPackageHandler.UnsafePartialLoad(f,
                    x => !x.IsDefaultObject &&
                         (x.ClassName == "SFXSimpleUseModule" || x.ClassName == "SFXModule_AimAssistTarget"));
                foreach (var exp in p.Exports.Where(x =>
                             !x.IsDefaultObject && (x.ClassName == "SFXSimpleUseModule" ||
                                                    x.ClassName == "SFXModule_AimAssistTarget")))
                {
                    if (exp.Parent.ClassName == "SFXPointOfInterest")
                        continue; // 
                    var displayNameVal = exp.GetProperty<StringRefProperty>("m_SrGameName");
                    if (displayNameVal != null)
                    {
                        var displayName = TLKBuilder.TLKLookupByLang(displayNameVal.Value, MELocalization.INT);
                        actorTypeNames.Add($"{displayNameVal.Value}: {displayName}");
                    }
                    else
                    {
                        // actorTypeNames.Add(exp.ObjectName.Instanced);
                    }
                }

            }

            foreach (var atn in actorTypeNames)
            {
                Debug.WriteLine(atn);
            }
        }
    }
}