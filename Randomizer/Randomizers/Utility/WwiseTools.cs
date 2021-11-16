using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using Randomizer.Randomizers.Game2.ExportTypes;

namespace RandomizerUI.Classes.Randomizers.Utility
{
    public static class WwiseTools
    {
        /// <summary>
        /// ME2 SPECIFIC<para/>
        /// Repoint a WwiseStream to play the data from another, typically across banks.
        /// </summary>
        /// <param name="originalExport">The audio you want to play (e.g. this is the audio that will be 'installed')</param>
        /// <param name="targetAudioStream">The audio stream that you want to replace.</param>
        public static void RepointWwiseStream(ExportEntry originalExport, ExportEntry targetAudioStream)
        {
            var props = originalExport.GetProperties();
            var bin = ObjectBinary.From<WwiseStream>(originalExport);
            var targetId = targetAudioStream.GetProperty<IntProperty>("Id");
            props.AddOrReplaceProp(targetId);
            targetAudioStream.WritePropertiesAndBinary(props, bin);
        }

        /// <summary>
        /// ME2 SPECIFIC<para/>
        /// Repoint a WwiseStream to play the data from another precomputed stream.
        /// </summary>
        /// <param name="originalExport">The audio you want to play (e.g. this is the audio that will be 'installed')</param>
        /// <param name="targetAudioStream">The audio stream that you want to replace.</param>
        public static void RepointWwiseStreamToInfo(RMusic.MusicStreamInfo streamInfo, ExportEntry targetAudioStream)
        {
            WwiseStream stream = new WwiseStream();
            stream.Filename = ""; // Just make sure it's not set to null so internal code thinks this is not Pcc stored.
            stream.DataSize = streamInfo.DataSize;
            stream.Unk1 = stream.Unk2 = stream.Unk3 = stream.Unk4 = stream.Unk5 = 1;
            stream.UnkGuid = streamInfo.UnkGuid;
            stream.DataOffset = streamInfo.DataOffset;

            PropertyCollection props = new PropertyCollection();
            props.Add(targetAudioStream.GetProperty<IntProperty>("Id")); // Use the existing Id
            props.Add(new NameProperty(streamInfo.Filename, "Filename")); // AFC file
            props.Add(new NameProperty(streamInfo.BankName, "BankName")); // ?
            targetAudioStream.WritePropertiesAndBinary(props, stream);
        }

        /// <summary>
        /// Extract a TLK ID from a WwiseStream. Returns -1 if a value could not be extracted
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        public static int ExtractTLKIdFromExportName(ExportEntry export)
        {
            //parse out tlk id?
            var splits = export.ObjectName.Name.Split('_', ',');
            for (int i = splits.Length - 1; i > 0; i--)
            {
                //backwards is faster
                if (int.TryParse(splits[i], out var parsed))
                {
                    return parsed;
                }
            }

            return -1;
        }
    }
}
