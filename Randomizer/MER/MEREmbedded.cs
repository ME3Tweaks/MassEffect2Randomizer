using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;

namespace Randomizer.MER
{
    /// <summary>
    /// Handles embedded asset files in MER
    /// </summary>
    public static class MEREmbedded
    {
        /// <summary>
        /// Fetches a text file's contents from the Assets.Text directory.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetEmbeddedTextAsset(string filename, bool shared = false)
        {
            using var stream = GetEmbeddedAsset("Text", filename, shared);
            using StreamReader sr = new StreamReader(stream);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// Fetches an embedded asset
        /// </summary>
        /// <param name="assettype">The type - e.g. Binary, Text. Can be null if using full path</param>
        /// <param name="assetpath">The relative or full path of the asset</param>
        /// <param name="shared">If using shared assets (relative only)</param>
        /// <param name="fullPath">If we should directly use assetpath</param>
        /// <returns></returns>
        public static Stream GetEmbeddedAsset(string assettype, string assetpath, bool shared = false, bool fullPath = false)
        {
            if (fullPath)
            {
#if DEBUG
                var items = Assembly.GetExecutingAssembly().GetManifestResourceNames();
#endif
                return Assembly.GetExecutingAssembly().GetManifestResourceStream(assetpath);
            }
            else
            {
                var fullAssetPath = GetEmbeddedAssetBasePath(assettype, assetpath, shared);
#if DEBUG
                var items = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                var result = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullAssetPath);
                if (result == null)
                    Debugger.Break();
                return result;
#else
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(assetBase + assetpath);
#endif
            }
        }

        /// <summary>
        /// Gets a list of asset paths that match the given type and folder path.
        /// </summary>
        /// <param name="assettype"></param>
        /// <param name="assetFolderPath"></param>
        /// <param name="shared"></param>
        /// <returns></returns>
        public static List<string> ListEmbeddedAssets(string assettype, string assetFolderPath, bool shared = false)
        {
            var assetBase = GetEmbeddedAssetBasePath(assettype, assetFolderPath, shared);
            var items = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            return items.Where(x => x.StartsWith(assetBase)).ToList();
        }


        //public static string ExtractInternalFile(string internalResourceName, bool fullname, string destination, bool overwrite)
        //{
        //    return ExtractInternalFile(internalResourceName, fullname, destination, overwrite, null);
        //}

        //public static void ExtractInternalFileToMemory(string internalResourceName, bool fullname, MemoryStream stream)
        //{
        //    ExtractInternalFile(internalResourceName, fullname, destStream: stream);
        //}

        private static string ExtractInternalFile(string fullResourcePath, string destination = null, bool overwrite = false, Stream destStream = null)
        {
            MERLog.Information($"Extracting file {fullResourcePath}");
            if (destStream != null || (destination != null && (!File.Exists(destination) || overwrite)))
            {
                using Stream stream = GetResourceStream(fullResourcePath);
                bool close = destStream == null;
                if (destStream == null)
                {
                    destStream = new FileStream(destination, FileMode.Create, FileAccess.Write);
                }

                stream.CopyTo(destStream);
                if (close) destStream.Close();
            }
            else if (destination != null && !overwrite)
            {
                MERLog.Warning("File already exists. Not overwriting.");
            }
            else
            {
                MERLog.Warning("Invalid extraction parameters!");
            }

            return destination;
        }

        public static string GetFilenameFromAssetName(string assetName)
        {
            var parts = assetName.Split('.');
            return string.Join('.', parts[^2], parts[^1]);
        }

        /// <summary>
        /// Gets the stream data for a game-specific package.
        /// </summary>
        /// <param name="targetGame"></param>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public static Stream GetEmbeddedPackage(MEGame targetGame, string packageName)
        {
            return GetEmbeddedAsset("Binary", $"Packages.{targetGame}.{packageName}");
        }

        /// <summary>
        /// Extracts the contents of an embedded binary folder to the DLC directory
        /// </summary>
        /// <param name="subBinaryPath">The subpath under the Binary folder to copy</param>
        public static List<string> ExtractEmbeddedBinaryFolder(string subBinaryPath)
        {
            var extractedItems = new List<string>();
            var basePath = GetEmbeddedAssetBasePath("Binary", subBinaryPath);
            var items = ListEmbeddedAssets("Binary", subBinaryPath);

            foreach (var v in items)
            {
                var dest = Path.Combine(MERFileSystem.DLCModCookedPath, GetFilenameFromAssetName(v));
                ExtractInternalFile(v, dest, true);
                extractedItems.Add(dest);
            }

            return extractedItems;
        }

        /// <summary>
        /// Returns the base path of the embedded assets, of the given type, e.g. Randomizer.Randomizers.Game3.Assets.Binary
        /// </summary>
        /// <param name="assettype"></param>
        /// <param name="subPath"></param>
        /// <param name="shared"></param>
        /// <returns></returns>
        private static string GetEmbeddedAssetBasePath(string assettype, string subPath, bool shared = false)
        {
#if __GAME1__
            var assetBase = $"Randomizer.Randomizers.Game1.Assets.{assettype}.";
#elif __GAME2__
            var assetBase = $"Randomizer.Randomizers.Game2.Assets.{assettype}.";
#elif __GAME3__
            var assetBase = $"Randomizer.Randomizers.Game3.Assets.{assettype}.";
#endif
            if (shared)
                assetBase = $"Randomizer.Randomizers.SharedAssets.{assettype}.";

            return $"{assetBase}{subPath}";
        }

        private static Stream GetResourceStream(string assemblyResource)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
#if DEBUG
            var resources = assembly.GetManifestResourceNames();
#endif
            return assembly.GetManifestResourceStream(assemblyResource);
        }

    }
}
