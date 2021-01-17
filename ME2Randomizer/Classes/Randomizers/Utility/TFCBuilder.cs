using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using ME3ExplorerCore.Unreal.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class TFCBuilder
    {
        public static TFCBuilder ActiveBuilder;
        public List<RTexture2D> TextureRandomizations { get; private set; }

        // DLC (and basegame, if not using DLC mod) TFC ----
        public StructProperty DLCTFCGuidProp { get; private set; }
        public Guid DLCTFCGuid { get; private set; }
        public NameProperty DLCTFCNameProp { get; private set; }
        private FileStream DLCTfcStream { get; set; }

        // BASEGAME FORCED TFC (for textures that MUST be stored in basegame like SFXGame.pcc) ----

        public StructProperty BGTFCGuidProp { get; private set; }
        public Guid BGTFCGuid { get; private set; }
        public NameProperty BGTFCNameProp { get; private set; }
        private FileStream BGTfcStream { get; set; }

        /// <summary>
        /// Opens a new TFC file for writing
        /// </summary>
        /// <param name="randomizations"></param>
        /// <param name="dlcTfcName"></param>
        public static void StartNewTFCs(List<RTexture2D> randomizations)
        {
            ActiveBuilder = new TFCBuilder();
            ActiveBuilder.TextureRandomizations = randomizations;

            var dlcTFCPath = MERFileSystem.GetTFCPath(true);
            // DLC TFC
            ActiveBuilder.DLCTFCNameProp = new NameProperty(Path.GetFileNameWithoutExtension(dlcTFCPath), "TextureFileCacheName"); //Written into texture properties
            ActiveBuilder.DLCTFCGuid = Guid.NewGuid();
            ActiveBuilder.DLCTFCGuidProp = StructProperty.FromGuid(ActiveBuilder.DLCTFCGuid, "TFCFileGuid"); //Written into texture properties
            ActiveBuilder.DLCTfcStream = new FileStream(MERFileSystem.GetTFCPath(true), FileMode.Create, FileAccess.ReadWrite);
            ActiveBuilder.DLCTfcStream.WriteGuid(ActiveBuilder.DLCTFCGuid);

            // PRELOAD TFC
            var bgTfcPath = MERFileSystem.GetTFCPath(false);
            ActiveBuilder.BGTFCNameProp = new NameProperty(Path.GetFileNameWithoutExtension(bgTfcPath), "TextureFileCacheName"); //Written into texture properties
            ActiveBuilder.BGTFCGuid = Guid.NewGuid();
            ActiveBuilder.BGTFCGuidProp = StructProperty.FromGuid(ActiveBuilder.BGTFCGuid, "TFCFileGuid"); //Written into texture properties
            ActiveBuilder.BGTfcStream = new FileStream(bgTfcPath, FileMode.Create, FileAccess.ReadWrite);
            ActiveBuilder.BGTfcStream.WriteGuid(ActiveBuilder.BGTFCGuid);
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
            return ActiveBuilder != null && ActiveBuilder.TextureRandomizations.Any(x => x.TextureInstancedFullPath == fpath);
        }

        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export, out var instancedFullPath)) return false;

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
            // 5. Write the higher compressed mips to TFC
            // 6. Setup the properties for texture
            // 7. Commit the props and binary
            // 8. Cache the work that's been done

            // 1. Fetch asset
            var r2d = ActiveBuilder.TextureRandomizations.First(x => x.TextureInstancedFullPath == instancedFullPath);
            var asset = r2d.FetchRandomTextureAsset();

            // 2. Is this asset already parsed?
            if (r2d.InstantiatedItems.TryGetValue(asset, out var instantiated))
            {
                // It's already been instantiated. Just use this data instead
                Log.Information($@"Writing out cached asset {asset} for {instancedFullPath}");
                export.WritePropertiesAndBinary(instantiated.props, instantiated.texData);
            }
            else
            {
                Log.Information($@"Installing texture asset {asset} for {instancedFullPath}");

                // 3. Asset has not been setup yet. Write out the precomputed data.
                export.WriteBinary(GetTextureAssetBinary(asset));

                // 4. Read in the data so it's in the correct context.
                var ut2d = ObjectBinary.From<UTexture2D>(export);

                // 5. Move compressed mips to TFC
                foreach (var mip in ut2d.Mips)
                {
                    if (mip.IsCompressed)
                    {
                        mip.DataOffset = TFCBuilder.WriteMip(r2d, mip.Mip); // If it's compressed it needs to be stored externally.
                        if (mip.StorageType == StorageTypes.pccLZO)
                            mip.StorageType = StorageTypes.extLZO;
                        if (mip.StorageType == StorageTypes.pccZlib)
                            mip.StorageType = StorageTypes.extZlib;
                    }
                }

                // 6. Setup the properties
                var props = export.GetProperties();
                props.AddOrReplaceProp(new IntProperty(ut2d.Mips.Count - 1, @"MipTailBaseIdx"));
                props.AddOrReplaceProp(new IntProperty(ut2d.Mips[0].SizeX, @"SizeX"));
                props.AddOrReplaceProp(new IntProperty(ut2d.Mips[0].SizeY, @"SizeY"));
                props.AddOrReplaceProp(r2d.PreMountTexture ? ActiveBuilder.BGTFCNameProp : ActiveBuilder.DLCTFCNameProp);
                props.AddOrReplaceProp(r2d.PreMountTexture ? ActiveBuilder.BGTFCGuidProp : ActiveBuilder.DLCTFCGuidProp);
                if (r2d.LODGroup != null)
                {
                    // Write a new LOD property
                    props.AddOrReplaceProp(r2d.LODGroup);
                }


                // 7. Commit the export
                export.WritePropertiesAndBinary(props, ut2d);

                // 8. Cache the work that's been done so we don't need to it again.
                r2d.InstantiatedItems[asset] = (props, ut2d);

            }
            return true;
        }

        private static object syncLock = new object();

        private static int WriteMip(RTexture2D tex, byte[] mipBytes)
        {
            // Only one thread is allowed to write a mip at a time.
            lock (syncLock)
            {
                if (tex.PreMountTexture)
                {
                    var startOffset = (int)ActiveBuilder.BGTfcStream.Position;
                    ActiveBuilder.BGTfcStream.Write(mipBytes);
                    return startOffset;
                }
                else
                {
                    var startOffset = (int)ActiveBuilder.DLCTfcStream.Position;
                    ActiveBuilder.DLCTfcStream.Write(mipBytes);
                    return startOffset;
                }
            }
        }

        private static byte[] GetTextureAssetBinary(string asset)
        {
            var items = typeof(MainWindow).Assembly.GetManifestResourceNames();
            var fullname = $"ME2Randomizer.Classes.Randomizers.{MERFileSystem.Game}.TextureAssets.{asset}";
            using (Stream stream = typeof(MainWindow).Assembly.GetManifestResourceStream(fullname))
            {
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }
        }


        public static void EndTFCs()
        {
            var usedBGTFC = ActiveBuilder.BGTfcStream.Position > 16;
            var usedDLCTFC = ActiveBuilder.DLCTfcStream.Position > 16;
            ActiveBuilder.BGTfcStream?.Close();
            ActiveBuilder.DLCTfcStream?.Close();

            if (!usedBGTFC)
                File.Delete(MERFileSystem.GetTFCPath(false));
            if (!usedDLCTFC)
                File.Delete(MERFileSystem.GetTFCPath(true));
            ActiveBuilder = null;
        }
    }

    class RTexture2D
    {
        /// <summary>
        /// The full path of the memory instance. Textures that have this match will have their texture reference updated to one of the random allowed asset names.
        /// </summary>
        public string TextureInstancedFullPath { get; set; }
        /// <summary>
        /// List of assets that can be used for this texture (path in exe)
        /// </summary>
        public List<string> AllowedTextureAssetNames { get; set; }
        /// <summary>
        /// The LODGroup property to write to the texture. If this is null, the group is not updated
        /// </summary>
        public EnumProperty LODGroup { get; set; }
        /// <summary>
        /// Indicates that this texture must be stored in the basegame, as it will be cached into memory before DLC mount. Use this for textures in things like Startup and SFXGame
        /// </summary>
        public bool PreMountTexture { get; set; }
        /// <summary>
        /// Mapping of an asset to it's instantiated/used data that should be placed into a usage of the texture
        /// </summary>
        public ConcurrentDictionary<string, (PropertyCollection props, UTexture2D texData)> InstantiatedItems = new ConcurrentDictionary<string, (PropertyCollection props, UTexture2D texData)>();

        public string FetchRandomTextureAsset()
        {
            return AllowedTextureAssetNames[ThreadSafeRandom.Next(AllowedTextureAssetNames.Count)];
        }
    }
}
