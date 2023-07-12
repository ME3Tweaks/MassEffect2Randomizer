using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Textures;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using Microsoft.IO;

namespace Randomizer.Randomizers.Utility
{
    internal class TextureTools
    {
        public static void ReplaceTexture(ExportEntry export, Stream incomingTextureImageFileData, bool packageStored, out Image loadedImage, Image preloadedImage = null)
        {
            if (incomingTextureImageFileData == null && preloadedImage == null)
            {
                loadedImage = null;
                Debug.WriteLine(@"Cannot replace texture without input data!");
                return;
            }
            //Check aspect ratios
            var props = export.GetProperties();
            var listedWidth = props.GetProp<IntProperty>("SizeX")?.Value ?? 0;
            var listedHeight = props.GetProp<IntProperty>("SizeY")?.Value ?? 0;

            byte[] incomingData = null;
            if (preloadedImage == null)
            {
                if (incomingTextureImageFileData is MemoryStream ms)
                    incomingData = ms.GetBuffer();
                else
                {
                    MemoryStream ms2 = new MemoryStream();
                    incomingTextureImageFileData.CopyTo(ms2);
                    incomingData = ms2.GetBuffer();
                }
            }

            loadedImage = null;
            try
            {
                loadedImage = preloadedImage ?? Image.LoadFromFileMemory(incomingData, 2, PixelFormat.ARGB);
            }
            catch (TextureSizeNotPowerOf2Exception)
            {
                Debug.WriteLine(@"Image must be a power of 2!");
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine($@"Error: {e.Message}");
                return;
            }

            if (loadedImage.mipMaps[0].origWidth / loadedImage.mipMaps[0].origHeight != listedWidth / listedHeight)
            {
                Debug.WriteLine($@"Error: aspect ratios have changed");
                loadedImage = null;
                return;
            }

            //if (loadedImage.mipMaps[0].width != 1024 || loadedImage.mipMaps[0].height != 512)
            //{
            //    Debug.WriteLine($@"Error: aspect ratio is not 2:1 1024x512");
            //    loadedImage = null;
            //    return;
            //}

            Texture2D t2d = new Texture2D(export);
#if __GAME2__
            t2d.Replace(loadedImage, props, forcedTFCName: Randomizer.Randomizers.Game2.TextureAssets.LE2.LE2Textures.PremadeTFCName);
#else
            throw new NotImplementedException();
#endif
        }
    }
}
