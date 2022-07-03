using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.UnrealScript;
using LegendaryExplorerCore.UnrealScript.Compiling.Errors;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Utility
{
    internal class ScriptTools
    {
        /// <summary>
        /// Installs the specified resource script into the specified package and target name
        /// </summary>
        /// <param name="target"></param>
        /// <param name="packageFile"></param>
        /// <param name="instancedFullPath"></param>
        /// <param name="scriptFilename"></param>
        /// <param name="shared"></param>
        public static IMEPackage InstallScriptToPackage(IMEPackage pf, string instancedFullPath, string scriptFilename, bool shared, bool saveOnFinish = false, PackageCache cache = null)
        {
            var targetExp = pf.FindExport(instancedFullPath);
            InstallScriptToExport(targetExp, scriptFilename, shared, cache);
            if (saveOnFinish)
            {
                MERFileSystem.SavePackage(pf);
            }

            return pf;
        }

        /// <summary>
        /// Installs the specified resource script into the specified package and target name
        /// </summary>
        /// <param name="target"></param>
        /// <param name="packageFile"></param>
        /// <param name="instancedFullPath"></param>
        /// <param name="scriptFilename"></param>
        /// <param name="shared"></param>
        public static IMEPackage InstallScriptToPackage(GameTarget target, string packageFile, string instancedFullPath, string scriptFilename, bool shared, bool saveOnFinish = false, PackageCache cache = null)
        {
            var pf = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, packageFile));
            return InstallScriptToPackage(pf, instancedFullPath, scriptFilename, shared, saveOnFinish, cache);
        }

        public static void InstallScriptToExport(ExportEntry targetExport, string scriptFilename, bool shared = false, PackageCache cache = null)
        {
            MERLog.Information($@"Installing script {scriptFilename} to export {targetExport.InstancedFullPath}");
            string scriptText = MEREmbedded.GetEmbeddedTextAsset($"Scripts.{scriptFilename}", shared);
            InstallScriptTextToExport(targetExport, scriptText, scriptFilename, cache);
        }

        public static void InstallScriptTextToExport(ExportEntry targetExport, string scriptText, string scriptFileNameForLogging, PackageCache cache)
        {
            var fl = new FileLib(targetExport.FileRef);
            bool initialized = fl.Initialize(cache);
            if (!initialized)
            {
                MERLog.Error($@"FileLib loading failed for package {targetExport.InstancedFullPath} ({targetExport.FileRef.FilePath}):");
                foreach (var v in fl.InitializationLog.AllErrors)
                {
                    MERLog.Error(v.Message);
                }

                throw new Exception($"Failed to initialize FileLib for package {targetExport.FileRef.FilePath}");
            }

            MessageLog log;
            switch (targetExport.ClassName)
            {
                case "Function":
                    (_, log) = UnrealScriptCompiler.CompileFunction(targetExport, scriptText, fl);
                    break;
                case "State":
                    (_, log) =UnrealScriptCompiler.CompileState(targetExport, scriptText, fl);
                    break;
                case "Class":
                    (_, log) = UnrealScriptCompiler.CompileClass(targetExport.FileRef, scriptText, fl, export: targetExport);
                    break;
                default:
                    throw new Exception("Can't compile to this type yet!");
            }

            if (log.AllErrors.Any())
            {
                MERLog.Error($@"Error compiling {targetExport.ClassName} {targetExport.InstancedFullPath} from filename {scriptFileNameForLogging}:");
                foreach (var l in log.AllErrors)
                {
                    MERLog.Error(l.Message);
                }

                throw new Exception($"Error compiling {targetExport.ClassName} {targetExport.InstancedFullPath} from file {scriptFileNameForLogging}: {string.Join(Environment.NewLine, log.AllErrors)}");
            }

        }
    }
}
