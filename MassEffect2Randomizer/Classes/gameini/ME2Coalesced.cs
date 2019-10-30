﻿using MassEffectRandomizer.Classes;
using ME3Explorer;
using System;
using System.Collections.Generic;
using System.IO;

namespace MassEffectModManagerCore.modmanager.gameini
{
    public class ME2Coalesced
    {
        public SortedDictionary<string, DuplicatingIni> Inis = new SortedDictionary<string, DuplicatingIni>();
        public string Inputfile { get; }
        public ME2Coalesced(string file)
        {
            Inputfile = file;
            using FileStream fs = new FileStream(file, FileMode.Open);
            int unknownInt = fs.ReadInt32();
            if (unknownInt != 0x1E)
            {
                Console.WriteLine();
                throw new Exception("First 4 bytes were not 0x1E.This does not appear to be a Coalesced file.");
            }

            while (fs.Position < fs.Length)
            {
                long pos = fs.Position;
                string filename = fs.ReadUnrealString();
                string contents = fs.ReadUnrealString();
                Inis[filename] =  DuplicatingIni.ParseIni(contents);
            }
        }

        public bool Serialize(string outputfile = null)
        {
            string outfile = outputfile ?? Inputfile;
            MemoryStream outStream = new MemoryStream();
            outStream.WriteInt32(0x1E); //Unknown header but seems to just be 1E. Can't find any documentation on what this is.
            foreach (var file in Inis)
            {
                //Console.WriteLine("Coalescing " + Path.GetFileName(file));
                outStream.WriteUnrealStringASCII(file.Key);
                outStream.WriteUnrealStringASCII(file.Value.ToString());
            }
            File.WriteAllBytes(outfile, outStream.ToArray());
            return true;
        }
    }
}
