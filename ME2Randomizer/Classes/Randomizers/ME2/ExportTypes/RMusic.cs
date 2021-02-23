using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Enemy;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Audio;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Gammtek.IO;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using Newtonsoft.Json;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    public class RMusic
    {
        public class MusicStreamInfo
        {
            // Build only
            [JsonIgnore] public bool IsUsable { get; set; } = true;

            [JsonIgnore] public string StreamFullPath { get; set; }


            // For the WwiseEvent that will have to have the correct length set, I think
            [JsonProperty("duration")] public float DurationMs { get; set; }

            // Properties
            [JsonProperty("filename")] public string Filename { get; set; }
            [JsonProperty("bankname")] public string BankName { get; set; }


            // Binary
            [JsonProperty("unkguid")] public Guid UnkGuid { get; set; }
            [JsonProperty("dataoffset")] public int DataOffset { get; set; }
            [JsonProperty("datasize")] public int DataSize { get; set; }

            // Randomizer info
            /// <summary>
            /// How many times this info is used in BioS files across game.
            /// </summary>
            [JsonProperty("instancecount")]
            public int InstanceCount { get; set; }

            /// <summary>
            /// Constructs musicstreaminfo from a WwiseStream export
            /// </summary>
            /// <param name="export"></param>
            /// <param name="files"></param>
            public MusicStreamInfo(ExportEntry export, List<string> loadedFiles)
            {
                StreamFullPath = export.InstancedFullPath;
                if (export.ObjectName.Name.Contains("silence")
                    || !export.ObjectName.Name.StartsWith("mus_")
                    || export.ObjectName.Name.Contains("gui"))
                {
                    IsUsable = false;
                    return;
                }

                var bin = ObjectBinary.From<WwiseStream>(export);
                if (bin.IsPCCStored)
                {
                    IsUsable = false;
                    return; // We aren't going to support this
                }

                // Props
                var props = export.GetProperties();
                Filename = props.GetProp<NameProperty>("Filename").Value;
                BankName = props.GetProp<NameProperty>("BankName").Value;

                // Bin
                DataOffset = bin.DataOffset;
                DataSize = bin.DataSize;
                UnkGuid = bin.UnkGuid;

                // Get the duration
                var fname = Filename + ".afc";
                var afcFile = loadedFiles.FirstOrDefault(x => Path.GetFileName(x).Equals(fname, StringComparison.InvariantCultureIgnoreCase));
                if (afcFile != null)
                {
                    var ai = GetAudioInfo(afcFile);
                    var len = ai.GetLength();
                    DurationMs = (float)len.TotalMilliseconds;
                }
                else
                {
                    Debug.WriteLine($"Could not find AFC file {Filename}!");
                    IsUsable = false;
                }
            }


            public MusicStreamInfo()
            {

            }
            private AudioInfo GetAudioInfo(string afc)
            {
                try
                {
                    AudioInfo ai = new AudioInfo();
                    var dataStream = ExternalFileHelper.ReadExternalData(afc, DataOffset, DataSize);

                    using EndianReader er = new EndianReader(dataStream);
                    var header = er.ReadStringASCII(4);
                    if (header == "RIFX") er.Endian = Endian.Big;
                    if (header == "RIFF") er.Endian = Endian.Little;
                    // Position 4

                    er.Seek(0xC, SeekOrigin.Current); // Post 'fmt ', get fmt size
                    var fmtSize = er.ReadInt32();
                    var postFormatPosition = er.Position;
                    ai.CodecID = er.ReadUInt16();

                    switch (ai.CodecID)
                    {
                        case 0xFFFF:
                            ai.CodecName = "Vorbis";
                            break;
                        case 0x0166:
                            ai.CodecName = "XMA2";
                            break;
                        default:
                            ai.CodecName = $"Unknown codec ID {ai.CodecID}";
                            break;
                    }

                    ai.Channels = er.ReadUInt16();
                    ai.SampleRate = er.ReadUInt32();
                    er.ReadInt32(); //Average bits per second
                    er.ReadUInt16(); //Alignment. VGMStream shows this is 16bit but that doesn't seem right
                    ai.BitsPerSample = er.ReadUInt16(); //Bytes per sample. For vorbis this is always 0!
                    var extraSize = er.ReadUInt16();
                    if (extraSize == 0x30)
                    {
                        // Newer Wwise
                        er.Seek(postFormatPosition + 0x18, SeekOrigin.Begin);
                        ai.SampleCount = er.ReadUInt32();
                    }
                    else
                    {
                        if (ai.CodecID == 0xFFFF)
                        {
                            // Vorbis
                            er.Seek(0x14 + fmtSize, SeekOrigin.Begin);
                            var chunkName = er.ReadStringASCII(4);
                            while (!chunkName.Equals("vorb", StringComparison.InvariantCultureIgnoreCase))
                            {
                                er.Seek(er.ReadInt32(), SeekOrigin.Current);
                                chunkName = er.ReadStringASCII(4);
                            }

                            er.SkipInt32(); //Skip vorb size
                            ai.SampleCount = er.ReadUInt32();
                        }
                        else if (ai.CodecID == 0x0166)
                        {
                            // XMA2 (360)

                            // This calculation is wrong.
                            // See https://github.com/losnoco/vgmstream/blob/master/src/meta/wwise.c#L484
                            // and 
                            // https://github.com/losnoco/vgmstream/blob/b61908f3af892714dda09c143a52fe0d65228985/src/coding/coding_utils.c#L767
                            // Seems correct, but will need to investigate why it's wrong. Example file is 543 in BioSnd_OmgPrA.xxx Xenon ME2
                            er.Seek(0x14 + 0x18, SeekOrigin.Begin); //Start of fmt + 0x18
                            ai.SampleCount = er.ReadUInt32();
                        }
                        else
                        {
                            // UNKNOWN!!
                            Debug.WriteLine("Unknown codec ID!");
                        }
                    }

                    // We don't care about the rest.
                    return ai;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private static object syncObj = new object();

        public static bool RandomizeMusic(ExportEntry export, RandomizationOption arg2)
        {
            if (!CanRandomize(export, out bool isSfxGame)) return false;

            var wwiseInfo = ObjectBinary.From<WwiseEvent>(export);

            if (wwiseInfo.Links.Count == 1 && wwiseInfo.Links[0].WwiseStreams.Any())
            {
                float newMaxDuration = 0;
                for (int i = 0; i < wwiseInfo.Links[0].WwiseStreams.Count; i++)
                {
                    var wwiseStreamExp = export.FileRef.GetUExport(wwiseInfo.Links[0].WwiseStreams[i].value);
                    if (wwiseStreamExp.DataSize > 0x98)
                        continue; // Very likely PCC stored

                    MusicStreamInfo newMusInfo = null;
                    lock (syncObj)
                    {
                        newMusInfo = AvailableMusicPool.Any() ? AvailableMusicPool.PullFirstItem() : StaticMusicPool.RandomElement();
                        while (newMusInfo.Filename.Contains("DLC") && isSfxGame)
                        {
                            // SFXGame cannot use DLC audio
                            AvailableMusicPool.Add(newMusInfo);
                            newMusInfo = AvailableMusicPool.Any() ? AvailableMusicPool.PullFirstItem() : StaticMusicPool.RandomElement();
                        }
                    }

                    newMaxDuration = Math.Max(newMaxDuration, newMusInfo.DurationMs);
                    WwiseTools.RepointWwiseStreamToInfo(newMusInfo, wwiseStreamExp);
                }

                // Update length of WwiseEvent
                export.WriteProperty(new FloatProperty(newMaxDuration, "DurationMilliseconds"));

                return true;
            }

            return false;
        }

        public static bool Init(RandomizationOption randomizationOption)
        {
            LoadMusicList();
            return true;
        }


        private static List<MusicStreamInfo> AvailableMusicPool;

        private static void LoadMusicList()
        {
            var loadedFiles = MELoadedFiles.GetFilesLoadedInGame(MERFileSystem.Game, true, includeAFCs: true);
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("musiclistme2.json");
            var musicList = JsonConvert.DeserializeObject<List<MusicStreamInfo>>(fileContents);

            AvailableMusicPool = new();
            foreach (var mus in musicList)
            {
                // Check it has AFC available
                if (loadedFiles.TryGetValue(mus.Filename + ".afc", out _))
                {
                    AvailableMusicPool.Add(mus);
                }
                else
                {
                    Debug.WriteLine($"Music not available, not adding to pool: {mus.Filename}");
                }
            }

            AvailableMusicPool.Shuffle();
            StaticMusicPool = AvailableMusicPool.ToList();
        }

        public static List<MusicStreamInfo> StaticMusicPool { get; set; }

        private static bool CanRandomize(ExportEntry exportEntry, out bool isSFXGame)
        {
            // WwiseEvent's that contain 'music' within BioS files.
            isSFXGame = false;
            bool canRandomize = !exportEntry.IsDefaultObject && exportEntry.ClassName == "WwiseEvent" && (exportEntry.ObjectName.Name.Contains("Music") || exportEntry.ObjectName.Name.Contains("_mus_"));
            if (!canRandomize)
                return false;
            if (Path.GetFileName(exportEntry.FileRef.FilePath).StartsWith("BioS"))
                return true;
            if (Path.GetFileName(exportEntry.FileRef.FilePath) == "SFXGame.pcc")
            {
                isSFXGame = true;
                return true;
            }
            return false;
        }
    }
}