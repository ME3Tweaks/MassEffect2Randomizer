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
        public static void ReplaceTexture(ExportEntry export, Stream incomingTextureImageFileData, bool packageStored)
        {
            //Check aspect ratios
            var props = export.GetProperties();
            var listedWidth = props.GetProp<IntProperty>("SizeX")?.Value ?? 0;
            var listedHeight = props.GetProp<IntProperty>("SizeY")?.Value ?? 0;

            byte[] incomingData;
            if (incomingTextureImageFileData is MemoryStream ms)
                incomingData = ms.GetBuffer();
            else
            {
                MemoryStream ms2 = new MemoryStream();
                incomingTextureImageFileData.CopyTo(ms2);
                incomingData = ms2.GetBuffer();
            }

            Image image;
            try
            {
                image = Image.LoadFromFileMemory(incomingData, 2, PixelFormat.ARGB);
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

            if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight != listedWidth / listedHeight)
            {
                Debug.WriteLine($@"Error: aspect ratios have changed");
                return;
            }

            Texture2D t2d = new Texture2D(export);
            var t = t2d.Replace(image, props);
            return;
        }
    }
}
