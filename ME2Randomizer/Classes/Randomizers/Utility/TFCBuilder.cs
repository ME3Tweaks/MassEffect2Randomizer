using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using ME3ExplorerCore.Unreal.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class TFCBuilder
    {
        public static TFCBuilder ActiveBuilder;
        public List<RTexture2D> TextureRandomizations { get; private set; }
        public StructProperty guidProp { get; private set; }
        public NameProperty TFCNameProp { get; private set; }
        private FileStream tfcStream { get; set; }
        /// <summary>
        /// [assetfilename+texturefullpath] => already installed binary 
        /// </summary>
        private Dictionary<string, UTexture2D> alreadyInstalledTextureBinary { get; set; }
        /// <summary>
        /// Opens a new TFC file for writing
        /// </summary>
        /// <param name="randomizations"></param>
        /// <param name="tfcName"></param>
        public static void StartNewTFC(List<RTexture2D> randomizations, string tfcName)
        {
            ActiveBuilder = new TFCBuilder();
            ActiveBuilder.TFCNameProp = new NameProperty(tfcName, "TextureFileCacheName"); //Written into texture properties
            ActiveBuilder.TextureRandomizations = randomizations;

            Guid tfcGuid = new Guid();
            ActiveBuilder.guidProp = StructProperty.FromGuid(tfcGuid, "TFCFileGuid"); //Written into texture properties
            ActiveBuilder.tfcStream = new FileStream(MERFileSystem.GetTFCPath(), FileMode.Create, FileAccess.ReadWrite);
            ActiveBuilder.tfcStream.WriteGuid(tfcGuid);
        }

        /// <summary>
        /// Can this texture be randomized?
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        private static bool CanRandomize(ExportEntry export, out string fullpath)
        {
            fullpath = null;
            if (export.IsDefaultObject || !export.IsTexture()) return false;
            var fpath = fullpath = export.FullPath;
            return ActiveBuilder != null && ActiveBuilder.TextureRandomizations.Any(x => x.TextureFullPath == fpath);
        }

        public static bool RandomizeExport(ExportEntry export)
        {
            if (!CanRandomize(export, out var fullpath)) return false;

            // This texture can be randomized.
            // Process:
            // 1. Fetch a random asset that matches the export's fullpath from the TextureAssets path
            //    - Asset binary is as if the texture was stored in the package.
            //    - This allows us to use the binary data without having to do more work on the data
            //    - Mips > 6 are stored externally directly into the TFC stream
            //    - Mip <= 6 are stored locally and are decompressed
            // 2. Is the asset already written somewhere? If it is, we should just write that cached binary out as the binary is not file specific. 
            // 3. If asset is not cached yet, we replace the texture binary with the asset's data
            // 4. Read the export in as a UTexture2D
            // 5. Decompress the lower mips, write the higher mips to TFC
            // 6. Update the local exportentry binary and CACHE the written-out UTexture2D object. We will need this later!
            // 7. Update the properties according to the UTexture2D object.

            // Get the assets to use.
            // 1. Fetch asset
            var r2d = ActiveBuilder.TextureRandomizations.FirstOrDefault(x => x.TextureFullPath == fullpath);
            var asset = r2d.FetchRandomTextureAsset();


            // 1. Update properties
            var props = export.GetProperties();
            props.AddOrReplaceProp(ActiveBuilder.guidProp); //Write the GUID
            props.AddOrReplaceProp(ActiveBuilder.TFCNameProp); //Write the TFC name



            // 2. Update binary

            return true;
        }

        public static void EndTFC()
        {
            ActiveBuilder.tfcStream?.Close();
            ActiveBuilder = null;
        }

    }

    class RTexture2D
    {
        /// <summary>
        /// The full path of the memory instance. Textures that have this match will have their texture reference updated to one of the random allowed asset names.
        /// </summary>
        public string TextureFullPath { get; set; }
        public List<string> AllowedTextureAssetNames { get; set; }
        public Dictionary<string, (PropertyCollection props, UTexture2D texData)> InstantiatedItems = new Dictionary<string, (PropertyCollection props, UTexture2D texData)>();
    }
}
