using ME3ExplorerCore.Packages;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RTextureMovie
    {
        //private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"TextureMovie" && ExportNameToAssetMapping.ContainsKey(export.ObjectName.Name);
        //public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        //{
        //    if (!CanRandomize(export)) return false;
        //    var assets = ExportNameToAssetMapping[export.ObjectName.Name];
        //    byte[] tmAsset = GetTextureMovieAssetBinary(assets[ThreadSafeRandom.Next(assets.Count)]);
        //    var tm = ObjectBinary.From<TextureMovie>(export);
        //    tm.EmbeddedData = tmAsset;
        //    tm.DataSize = tmAsset.Length;
        //    export.WriteBinary(tm);
        //    return true;
        //}

        // ME2 only has few texture movies so these are used
        public static bool RandomizeExportDirect(ExportEntry export, RandomizationOption option, byte[] tmAsset)
        {
            var tm = ObjectBinary.From<TextureMovie>(export);
            tm.EmbeddedData = tmAsset;
            tm.DataSize = tmAsset.Length;
            export.WriteBinary(tm);
            return true;
        }

        //        private static Dictionary<string, List<string>> ExportNameToAssetMapping;

        //        public static void SetupOptions()
        //        {
        //#if __ME2__
        //            ExportNameToAssetMapping = new Dictionary<string, List<string>>()
        //            {
        //                { "ProFre_501_VeetorFootage", new List<string>()
        //                    {
        //                        "Veetor.size_mer.bik"
        //                    }
        //                }
        //            };
        //#elif __ME3__

        //#endif


        public static byte[] GetTextureMovieAssetBinary(string assetName)
        {
            var items = typeof(MainWindow).Assembly.GetManifestResourceNames();
            var fullname = $"ME2Randomizer.Classes.Randomizers.{MERFileSystem.Game}.TextureMovieAssets.{assetName}";
            using (Stream stream = typeof(MainWindow).Assembly.GetManifestResourceStream(fullname))
            {
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }
        }
    }
}