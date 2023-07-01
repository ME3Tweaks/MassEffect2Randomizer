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

        /// <summary>
        /// Opens a package and installs a script to the specified path, then saves the package.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="packageFile"></param>
        /// <param name="instancedFullPath"></param>
        /// <param name="scriptText"></param>
        /// <param name="scriptFileNameForLogging"></param>
        /// <param name="cache"></param>
        public static IMEPackage InstallScriptTextToPackage(GameTarget target, string packageFile, string instancedFullPath, string scriptText, string scriptFileNameForLogging, bool saveOnFinish = false, PackageCache cache = null)
        {
            var pf = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, packageFile));
            var export = pf.FindExport(instancedFullPath);
            InstallScriptTextToExport(export, scriptText, scriptFileNameForLogging, cache);
            if (saveOnFinish)
            {
                MERFileSystem.SavePackage(pf);
            }

            return pf;
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
                    (_, log) = UnrealScriptCompiler.CompileState(targetExport, scriptText, fl);
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

        /// <summary>
        /// Installs a class into the listed package, under the named package, or at the root if null. The package is not saved
        /// </summary>
        /// <param name="target">The game target that this script can compile against</param>
        /// <param name="packageToInstallTo">The package to install the class into</param>
        /// <param name="embeddedClassName">The name of the embedded class file - the class name must match the filename. DO NOT PUT .uc here, it will be added automatically</param>
        /// <param name="rootPackageName">The IFP of the root package, null if you want the class created at the root of the file</param>
        public static ExportEntry InstallClassToPackage(GameTarget target, IMEPackage packageToInstallTo, string embeddedClassName, string rootPackageName = null)
        {
            var fl = new FileLib(packageToInstallTo);
            bool initialized = fl.Initialize(gameRootPath: target.TargetPath);
            if (!initialized)
            {
                MERLog.Error($@"FileLib loading failed for package {packageToInstallTo.FileNameNoExtension}:");
                foreach (var v in fl.InitializationLog.AllErrors)
                {
                    MERLog.Error(v.Message);
                }

                throw new Exception($"Failed to initialize FileLib for package {packageToInstallTo.FilePath}");
            }

            ExportEntry parentExport = null;
            if (rootPackageName != null)
            {
                parentExport = packageToInstallTo.FindExport(rootPackageName);
                if (parentExport == null)
                {
                    // Create the root package we will instal the class under
                    parentExport = ExportCreator.CreatePackageExport(packageToInstallTo, rootPackageName);
                }
            }

            // Todo: See if the class already exists

            var scriptText = MEREmbedded.GetEmbeddedTextAsset($"Classes.{embeddedClassName}.uc");
            MessageLog log;
            (_, log) = UnrealScriptCompiler.CompileClass(packageToInstallTo, scriptText, fl, parent: parentExport);

            if (log.AllErrors.Any())
            {
                MERLog.Error($@"Error compiling class {embeddedClassName}:");
                foreach (var l in log.AllErrors)
                {
                    MERLog.Error(l.Message);
                }

                throw new Exception($"Error compiling {embeddedClassName}: {string.Join(Environment.NewLine, log.AllErrors)}");
            }

            return parentExport != null
                ? packageToInstallTo.FindExport($"{parentExport.InstancedFullPath}.{embeddedClassName}")
                : packageToInstallTo.FindExport(embeddedClassName);
        }
    }
}
