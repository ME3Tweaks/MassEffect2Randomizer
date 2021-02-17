﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.Utility
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