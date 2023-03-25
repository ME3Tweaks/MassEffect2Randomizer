using System.IO;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Sound.Wwise;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using Randomizer.MER;
using Randomizer.Shared.DataTypes;

namespace Randomizer.Randomizers.Utility
{
    public static class WwiseTools
    {
        /// <summary>
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
            WwiseStream w = ObjectBinary.From<WwiseStream>(targetAudioStream);
            WwiseHelper.UpdateReferencedWwiseEventLengths(targetAudioStream, (float)w.GetAudioInfo().GetLength().TotalMilliseconds);
        }

        /// <summary>
        /// Repoint a WwiseStream to play the data from another precomputed stream.
        /// </summary>
        /// <param name="originalExport">The audio you want to play (e.g. this is the audio that will be 'installed')</param>
        /// <param name="targetAudioStream">The audio stream that you want to replace.</param>
        public static void RepointWwiseStreamToInfo(MusicStreamInfo streamInfo, ExportEntry targetAudioStream)
        {
            WwiseStream stream = new WwiseStream();
            PropertyCollection props = new PropertyCollection();

#if __GAME2__

            stream.Filename = ""; // Just make sure it's not set to null so internal code thinks this is not Pcc stored.
            stream.DataSize = streamInfo.DataSize;
            stream.Unk1 = stream.Unk2 = stream.Unk3 = stream.Unk4 = stream.Unk5 = 1;
            stream.UnkGuid = streamInfo.UnkGuid;
            stream.DataOffset = streamInfo.DataOffset;

            props.Add(targetAudioStream.GetProperty<IntProperty>("Id")); // Use the existing Id
            props.Add(new NameProperty(streamInfo.Filename, "Filename")); // AFC file
            props.Add(new NameProperty(streamInfo.BankName, "BankName")); // ?
#endif
#if __GAME3__
            stream.Filename = ""; // Just make sure it's not set to null so internal code thinks this is not Pcc stored.
            stream.DataSize = streamInfo.DataSize;
            stream.Unk1 = stream.Unk2 = stream.Unk3 = stream.Unk4 = 1;
            stream.UnkGuid = streamInfo.UnkGuid;
            stream.DataOffset = streamInfo.DataOffset;

            props.Add(targetAudioStream.GetProperty<IntProperty>("Id")); // Use the existing Id
            props.Add(new NameProperty(streamInfo.Filename, "Filename")); // AFC file
            props.Add(new NameProperty(streamInfo.BankName, "BankName")); // ?
#endif
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

        /// <summary>
        /// Repoints audio to a SINGLE AFC that is new via MER, located in the DLC mod folder.
        /// </summary>
        /// <param name="originalExport"></param>
        /// <param name="afcName"></param>
        public static void RepointWwiseStreamToSingleAFC(ExportEntry originalExport, string afcName)
        {
            var props = originalExport.GetProperties();
            var bin = ObjectBinary.From<WwiseStream>(originalExport);
            props.AddOrReplaceProp(new NameProperty(afcName, "Filename"));
            bin.DataOffset = 0;
            bin.DataSize = (int) new FileInfo(Path.Combine(MERFileSystem.DLCModCookedPath, $"{afcName}.afc")).Length;
            originalExport.WritePropertiesAndBinary(props, bin);

            WwiseStream w = ObjectBinary.From<WwiseStream>(originalExport);
            WwiseHelper.UpdateReferencedWwiseEventLengths(originalExport, (float)w.GetAudioInfo().GetLength().TotalMilliseconds);
        }
    }
}
