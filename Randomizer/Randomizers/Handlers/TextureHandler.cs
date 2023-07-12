using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.TextureAssets.LE2;

namespace Randomizer.Randomizers.Handlers
{
    class TextureHandler
    {
        //public static TextureHandler ActiveBuilder;
        public static List<RTexture2D> TextureRandomizations { get; private set; }

        /// <summary>
        /// Contains data to copy into packages
        /// </summary>
        private static IMEPackage PremadeTexturePackage { get; set; }

        //// DLC (and basegame, if not using DLC mod) TFC ----
        //public StructProperty DLCTFCGuidProp { get; private set; }
        //public Guid DLCTFCGuid { get; private set; }
        //public NameProperty DLCTFCNameProp { get; private set; }
        //private FileStream DLCTfcStream { get; set; }

        // BASEGAME FORCED TFC (for textures that MUST be stored in basegame like SFXGame.pcc) ----

        //public StructProperty BGTFCGuidProp { get; private set; }
        //public Guid BGTFCGuid { get; private set; }
        //public NameProperty BGTFCNameProp { get; private set; }
        //private FileStream BGTfcStream { get; set; }

        /// <summary>
        /// Opens a new TFC file for writing
        /// </summary>
        /// <param name="randomizations"></param>
        /// <param name="dlcTfcName"></param>
        public static void StartHandler(GameTarget target, List<RTexture2D> randomizations)
        {
            TextureRandomizations = randomizations;
            // PremadeTFCName: CHANGE FOR OTHER GAMES
#if __GAME2__
            var tfcStream = MEREmbedded.GetEmbeddedAsset("Binary", $"Textures.{LE2Textures.PremadeTFCName}.tfc");
            tfcStream.WriteToFile(Path.Combine(MERFileSystem.DLCModCookedPath, $"{LE2Textures.PremadeTFCName}.tfc")); // Write the embedded TFC out to the DLC folder
#endif
            // PremadeTexturePackage = MEPackageHandler.OpenMEPackageFromStream(MEREmbedded.GetEmbeddedPackage(MERFileSystem.Game, @"Textures.PremadeImages.pcc"), @"PremadeImages.pcc");
        }

        /// <summary>
        /// Can this texture be randomized?
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        private static bool CanRandomize(ExportEntry export, out string instancedFullPath)
        {
            instancedFullPath = null;
            if (export.IsDefaultObject || !export.IsTexture()) return false;
            var fpath = instancedFullPath = export.InstancedFullPath;
            return TextureRandomizations.Any(x => x.TextureInstancedFullPath == fpath);
        }

        public static bool RandomizeExport(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export, out var instancedFullPath)) return false;

            var r2d = TextureRandomizations.First(x => x.TextureInstancedFullPath == instancedFullPath);
            InstallTexture(target, r2d, export);
            return true;
        }

        /// <summary>
        /// Installs the specified r2d to the specified export. Specify an id name if you wish to use a specific id in the RTexture2D object.
        /// </summary>
        /// <param name="r2d"></param>
        /// <param name="export"></param>
        /// <param name="id"></param>
        public static void InstallTexture(GameTarget target, RTexture2D r2d, ExportEntry export, string id = null)
        {
            // If no id was specified pick a random id
            id ??= r2d.FetchRandomTextureId();

            var sourceTexToCopy = PremadeTexturePackage.FindExport(id);
#if DEBUG
            if (sourceTexToCopy == null)
            {
                Debugger.Break();
            }
#endif
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingularWithRelink, sourceTexToCopy, export.FileRef, export, true, new RelinkerOptionsPackage(), out _);
        }


        public static void EndHandler(GameTarget target)
        {
            PremadeTexturePackage = null; // Lose reference
        }
    }

    public class RTexture2D
    {
        /// <summary>
        /// The full path of the memory instance. Textures that have this match will have their texture reference updated to one of the random allowed id names.
        /// </summary>
        public string TextureInstancedFullPath { get; set; }

        /// <summary>
        /// List of assets that can be used for this texture (path in exe)
        /// </summary>
        public List<string> AllowedTextureIds { get; set; }

        /// <summary>
        /// Indicates that this texture must be stored in the basegame, as it will be cached into memory before DLC mount. Use this for textures in things like Startup and SFXGame
        /// </summary>
        public bool PreMountTexture { get; set; }

        /// <summary>
        /// Cached mip storage data - only populated on first install a texture
        /// </summary>
        // public ConcurrentDictionary<string, List<MipStorage>> StoredMips { get; set; }

        ///// <summary>
        ///// Mapping of an id to it's instantiated/used data that should be placed into a usage of the texture
        ///// </summary>
        //public ConcurrentDictionary<string, (PropertyCollection props, UTexture2D texData)> InstantiatedItems = new ConcurrentDictionary<string, (PropertyCollection props, UTexture2D texData)>();

        public string FetchRandomTextureId()
        {
            return AllowedTextureIds[ThreadSafeRandom.Next(AllowedTextureIds.Count)];
        }

        /// <summary>
        /// Resets this RTexture2D, dropping the instantiatetd list
        /// </summary>
        public void Reset()
        {
        }
    }
}
