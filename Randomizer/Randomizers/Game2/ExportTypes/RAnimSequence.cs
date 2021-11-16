using System;
using System.Diagnostics;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    class RAnimSequence
    {
        public static string UIConverter(double setting)
        {
            if (setting == BASIC_RANDOM) return "Basic bones only";
            if (setting == MODERATE_RANDOM) return "Advanced bones";
            return "Unknown setting!";
        }


        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"AnimSequence";
        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var game = export.FileRef.Game;
            byte[] data = export.Data;
            try
            {
                var TrackOffsets = export.GetProperty<ArrayProperty<IntProperty>>("CompressedTrackOffsets");
                var animsetData = export.GetProperty<ObjectProperty>("m_pBioAnimSetData");
                if (animsetData.Value <= 0)
                {
                    //Debug.WriteLine("trackdata is an import skipping");
                    return false;
                } // don't randomize;

                var boneList = export.FileRef.GetUExport(animsetData.Value).GetProperty<ArrayProperty<NameProperty>>("TrackBoneNames");
                Enum.TryParse(export.GetProperty<EnumProperty>("RotationCompressionFormat").Value.Name, out AnimationCompressionFormat rotCompression);
                int offset = export.propsEnd();
                //ME2 SPECIFIC
                offset += 16; //3 0's, 1 offset of data point
                int binLength = BitConverter.ToInt32(data, offset);
                //var LengthNode = new BinInterpNode
                //{
                //    Header = $"0x{offset:X4} AnimBinary length: {binLength}",
                //    Name = "_" + offset,
                //    Tag = NodeType.StructLeafInt
                //};
                //offset += 4;
                //subnodes.Add(LengthNode);
                var animBinStart = offset;

                int bone = 0;

                for (int i = 0; i < TrackOffsets.Count; i++)
                {
                    var bonePosOffset = TrackOffsets[i].Value;
                    i++;
                    var bonePosCount = TrackOffsets[i].Value;
                    var boneName = boneList[bone].Value;
                    bool doSomething = shouldRandomizeBone(boneName, option);
                    //POSKEYS
                    for (int j = 0; j < bonePosCount; j++)
                    {
                        offset = animBinStart + bonePosOffset + j * 12;
                        //Key #
                        //var PosKeys = new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} PosKey {j}",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.Unknown
                        //};
                        //BoneID.Items.Add(PosKeys);


                        var posX = BitConverter.ToSingle(data, offset);
                        if (doSomething)
                            data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(posX - (posX * .3f), posX + (posX * .3f))));

                        //PosKeys.Items.Add(new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} X: {posX} ",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.StructLeafFloat
                        //});
                        offset += 4;

                        var posY = BitConverter.ToSingle(data, offset);
                        if (doSomething)
                            data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(posY - (posY * .3f), posY + (posY * .3f))));
                        //PosKeys.Items.Add(new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} Y: {posY} ",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.StructLeafFloat
                        //});
                        offset += 4;

                        var posZ = BitConverter.ToSingle(data, offset);
                        if (doSomething)
                            data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(posZ - (posZ * .3f), posZ + (posZ * .3f))));

                        //PosKeys.Items.Add(new BinInterpNode
                        //{
                        //    Header = $"0x{offset:X5} Z: {posZ} ",
                        //    Name = "_" + offset,
                        //    Tag = NodeType.StructLeafFloat
                        //});
                        offset += 4;
                    }

                    var lookat = boneName.Name.Contains("lookat");

                    i++;
                    var boneRotOffset = TrackOffsets[i].Value;
                    i++;
                    var boneRotCount = TrackOffsets[i].Value;
                    int l = 12; // 12 length of rotation by default
                    var offsetRotX = boneRotOffset;
                    var offsetRotY = boneRotOffset;
                    var offsetRotZ = boneRotOffset;
                    var offsetRotW = boneRotOffset;
                    for (int j = 0; j < boneRotCount; j++)
                    {
                        float rotX = 0;
                        float rotY = 0;
                        float rotZ = 0;
                        float rotW = 0;

                        switch (rotCompression)
                        {
                            case AnimationCompressionFormat.ACF_None:
                                l = 16;
                                offset = animBinStart + boneRotOffset + j * l;
                                offsetRotX = offset;
                                rotX = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotX - (rotX * .1f), rotX + (rotX * .1f))));
                                offset += 4;
                                offsetRotY = offset;
                                rotY = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotY - (rotY * .1f), rotY + (rotY * .1f))));
                                offset += 4;
                                offsetRotZ = offset;
                                rotZ = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotZ - (rotZ * .1f), rotZ + (rotZ * .1f))));
                                offset += 4;
                                offsetRotW = offset;
                                rotW = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotW - (rotW * .1f), rotW + (rotW * .1f))));
                                offset += 4;
                                break;
                            case AnimationCompressionFormat.ACF_Float96NoW:
                                offset = animBinStart + boneRotOffset + j * l;
                                offsetRotX = offset;
                                rotX = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotX - (rotX * .1f), rotX + (rotX * .1f))));

                                offset += 4;
                                offsetRotY = offset;
                                rotY = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotY - (rotY * .1f), rotY + (rotY * .1f))));

                                offset += 4;
                                offsetRotZ = offset;
                                rotZ = BitConverter.ToSingle(data, offset);
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotZ - (rotZ * .1f), rotZ + (rotZ * .1f))));

                                offset += 4;
                                break;
                            case AnimationCompressionFormat.ACF_Fixed48NoW: // normalized quaternion with 3 16-bit fixed point fields
                                                                            //FQuat r;
                                                                            //r.X = (X - 32767) / 32767.0f;
                                                                            //r.Y = (Y - 32767) / 32767.0f;
                                                                            //r.Z = (Z - 32767) / 32767.0f;
                                                                            //RESTORE_QUAT_W(r);
                                                                            //break;
                            case AnimationCompressionFormat.ACF_Fixed32NoW:// normalized quaternion with 11/11/10-bit fixed point fields
                                                                           //FQuat r;
                                                                           //r.X = X / 1023.0f - 1.0f;
                                                                           //r.Y = Y / 1023.0f - 1.0f;
                                                                           //r.Z = Z / 511.0f - 1.0f;
                                                                           //RESTORE_QUAT_W(r);
                                                                           //break;
                            case AnimationCompressionFormat.ACF_IntervalFixed32NoW:
                            //FQuat r;
                            //r.X = (X / 1023.0f - 1.0f) * Ranges.X + Mins.X;
                            //r.Y = (Y / 1023.0f - 1.0f) * Ranges.Y + Mins.Y;
                            //r.Z = (Z / 511.0f - 1.0f) * Ranges.Z + Mins.Z;
                            //RESTORE_QUAT_W(r);
                            //break;
                            case AnimationCompressionFormat.ACF_Float32NoW:
                                //FQuat r;

                                //int _X = data >> 21;            // 11 bits
                                //int _Y = (data >> 10) & 0x7FF;  // 11 bits
                                //int _Z = data & 0x3FF;          // 10 bits

                                //*(unsigned*)&r.X = ((((_X >> 7) & 7) + 123) << 23) | ((_X & 0x7F | 32 * (_X & 0xFFFFFC00)) << 16);
                                //*(unsigned*)&r.Y = ((((_Y >> 7) & 7) + 123) << 23) | ((_Y & 0x7F | 32 * (_Y & 0xFFFFFC00)) << 16);
                                //*(unsigned*)&r.Z = ((((_Z >> 6) & 7) + 123) << 23) | ((_Z & 0x3F | 32 * (_Z & 0xFFFFFE00)) << 17);

                                //RESTORE_QUAT_W(r);


                                break;
                            case AnimationCompressionFormat.ACF_BioFixed48:
                                offset = animBinStart + boneRotOffset + j * l;
                                const float shift = 0.70710678118f;
                                const float scale = 1.41421356237f;
                                offsetRotX = offset;
                                rotX = (data[0] & 0x7FFF) / 32767.0f * scale - shift;
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotX - (rotX * .1f), rotX + (rotX * .1f))));

                                offset += 4;
                                offsetRotY = offset;
                                rotY = (data[1] & 0x7FFF) / 32767.0f * scale - shift;
                                if (lookat)

                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotY - (rotY * .1f), rotY + (rotY * .1f))));

                                offset += 4;
                                offsetRotZ = offset;
                                rotZ = (data[2] & 0x7FFF) / 32767.0f * scale - shift;
                                if (lookat)
                                    data.OverwriteRange(offset, BitConverter.GetBytes(ThreadSafeRandom.NextFloat(rotZ - (rotZ * .1f), rotZ + (rotZ * .1f))));



                                //float w = 1.0f - (rotX * rotX + rotY * rotY + rotZ * rotZ);
                                //w = w >= 0.0f ? (float)Math.Sqrt(w) : 0.0f;
                                //int s = ((data[0] >> 14) & 2) | ((data[1] >> 15) & 1);
                                break;
                        }

                        if (rotCompression == AnimationCompressionFormat.ACF_BioFixed48 || rotCompression == AnimationCompressionFormat.ACF_Float96NoW || rotCompression == AnimationCompressionFormat.ACF_None)
                        {
                            //randomize here?
                            //var RotKeys = new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotX:X5} RotKey {j}",
                            //    Name = "_" + offsetRotX,
                            //    Tag = NodeType.Unknown
                            //};
                            //BoneID.Items.Add(RotKeys);
                            //RotKeys.Items.Add(new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotX:X5} RotX: {rotX} ",
                            //    Name = "_" + offsetRotX,
                            //    Tag = NodeType.StructLeafFloat
                            //});
                            //RotKeys.Items.Add(new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotY:X5} RotY: {rotY} ",
                            //    Name = "_" + offsetRotY,
                            //    Tag = NodeType.StructLeafFloat
                            //});
                            //RotKeys.Items.Add(new BinInterpNode
                            //{
                            //    Header = $"0x{offsetRotZ:X5} RotZ: {rotZ} ",
                            //    Name = "_" + offsetRotZ,
                            //    Tag = NodeType.StructLeafFloat
                            //});
                            if (rotCompression == AnimationCompressionFormat.ACF_None)
                            {
                                //RotKeys.Items.Add(new BinInterpNode
                                //{
                                //    Header = $"0x{offsetRotW:X5} RotW: {rotW} ",
                                //    Name = "_" + offsetRotW,
                                //    Tag = NodeType.StructLeafFloat
                                //});
                            }
                        }
                    }
                    bone++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading animsequence: " + ex.Message + ". Skipping");
            }

            export.Data = data; //write back
            return true;
        }

        private static double BASIC_RANDOM = 1;
        private static double MODERATE_RANDOM = 2;

        private static bool shouldRandomizeBone(string boneName, RandomizationOption option)
        {
            if (boneName.Contains("base", StringComparison.InvariantCultureIgnoreCase)) 
                return false;

            if (boneName.Contains("finger", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("eye", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("mouth", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("sneer", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (boneName.Contains("brow", StringComparison.InvariantCultureIgnoreCase)) return true;

            if (option.SliderValue >= MODERATE_RANDOM)
            {
                if (ThreadSafeRandom.Next(4) != 0) return false;

                if (boneName.Contains("spine", StringComparison.InvariantCultureIgnoreCase)) return true;
                if (boneName.Contains("chest", StringComparison.InvariantCultureIgnoreCase)) return true;
                if (boneName.Contains("butt", StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }
    }
}
