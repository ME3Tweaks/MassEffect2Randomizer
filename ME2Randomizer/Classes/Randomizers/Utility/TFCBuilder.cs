using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using ME3ExplorerCore.Unreal.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    class TFCBuilder
    {
        public static TFCBuilder ActiveBuilder;
        public List<RTexture2D> TextureRandomizations { get; private set; }
        public StructProperty guidProp { get; private set; }
        public NameProperty TFCName { get; private set; }
        /// <summary>
        /// Opens a new TFC file for writing
        /// </summary>
        /// <param name="randomizations"></param>
        /// <param name="tfcName"></param>
        public static void StartNewTFC(List<RTexture2D> randomizations, string tfcName)
        {
            ActiveBuilder = new TFCBuilder();
            ActiveBuilder.TFCName = new NameProperty(tfcName, "TextureFileCacheName");
            ActiveBuilder.TextureRandomizations = randomizations;

            Guid tfcGuid = new Guid();
            ActiveBuilder.guidProp = StructProperty.FromGuid(tfcGuid);

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
