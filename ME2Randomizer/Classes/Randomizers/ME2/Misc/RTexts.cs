using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.TLK.ME1;
using ME3ExplorerCore.TLK.ME2ME3;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class RTexts
    {
        public const string SUBOPTIONKEY_VOWELS_HARDMODE = "VOWELS_HARDMODE";
        public const string SUBOPTIONKEY_UWU_KEEPCASING = "UWU_KEEPCASING";

        public static bool RandomizeIntroText(RandomizationOption arg)
        {
            string fileContents = MERUtilities.GetEmbeddedStaticFilesTextFile("openingcrawls.xml");
            XElement rootElement = XElement.Parse(fileContents);
            var gameoverTexts = rootElement.Elements("CrawlText").Select(x => x.Value).ToList();
            // The trim calls here will remove first and last lines that are blank. The TrimForIntro() will remove whitespace per line.
            TLKHandler.ReplaceString(331765, TrimForIntro(gameoverTexts.RandomElement().Trim()));
            TLKHandler.ReplaceString(263408, TrimForIntro(gameoverTexts.RandomElement().Trim()));
            TLKHandler.ReplaceString(348756, TrimForIntro(gameoverTexts.RandomElement().Trim()));
            TLKHandler.ReplaceString(391285, TrimForIntro(gameoverTexts.RandomElement().Trim())); // Genesis DLC uses this extra string for some reason
            return true;
        }

        private static string TrimForIntro(string randomElement)
        {
            var lines = randomElement.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }
            return string.Join('\n', lines);
        }

        public static bool RandomizeGameOverText(RandomizationOption arg)
        {
            string fileContents = MERUtilities.GetEmbeddedStaticFilesTextFile("gameovertexts.xml");
            XElement rootElement = XElement.Parse(fileContents);
            var gameoverTexts = rootElement.Elements("gameovertext").Select(x => x.Value).ToList();
            var gameOverText = gameoverTexts[ThreadSafeRandom.Next(gameoverTexts.Count)];
            TLKHandler.ReplaceString(157152, gameOverText);
            return true;
        }

        public static bool UwuifyText(RandomizationOption option)
        {
            bool keepCasing = option.HasSubOptionSelected(RTexts.SUBOPTIONKEY_UWU_KEEPCASING);
            foreach (TalkFile tf in TLKHandler.GetOfficialTLKs())
            {
                var tfName = Path.GetFileNameWithoutExtension(tf.path);
                var langCode = tfName.Substring(tfName.LastIndexOf("_", StringComparison.InvariantCultureIgnoreCase) + 1);
                int max = tf.StringRefs.Count();
                if (langCode != "INT")
                    continue;
                foreach (var sref in tf.StringRefs.Where(x => x.StringID > 0 && !string.IsNullOrWhiteSpace(x.Data)))
                {
                    if (sref.Data.Contains("DLC_")) continue; // Don't modify

                    //if (sref.StringID != 325648)
                    //    continue;
                    // See if strref has CUSTOMTOKEN or a control symbol
                    List<int> skipRanges = new List<int>();
                    FindSkipRanges(sref, skipRanges);
                    // uwuify it
                    StringBuilder sb = new StringBuilder();
                    char previousChar = (char)0x00;
                    char currentChar;
                    for (int i = 0; i < sref.Data.Length; i++)
                    {
                        if (skipRanges.Any() && skipRanges[0] == i)
                        {
                            sb.Append(sref.Data.Substring(skipRanges[0], skipRanges[1] - skipRanges[0]));
                            previousChar = (char)0x00;
                            i = skipRanges[1] - 1; // We subtract one as the next iteration of the loop will +1 it again, which then will make it read the 'next' character
                            skipRanges.RemoveAt(0); // remove first 2
                            skipRanges.RemoveAt(0); // remove first 2

                            if (i >= sref.Data.Length - 1)
                                break;
                            continue;
                        }

                        currentChar = sref.Data[i];
                        if (currentChar == 'L' || currentChar == 'R')
                        {
                            sb.Append(keepCasing ? 'W' : 'w');
                        }
                        else if (currentChar == 'l' || currentChar == 'r')
                        {
                            sb.Append('w');
                            if (ThreadSafeRandom.Next(5) == 0)
                            {
                                sb.Append('w'); // append another ! 50% of the time
                                if (ThreadSafeRandom.Next(5) == 0)
                                {
                                    sb.Append('w'); // append another ! 50% of the time
                                }
                            }
                        }
                        else if (currentChar == 'N' && (previousChar == 0x00 || previousChar == ' '))
                        {
                            sb.Append(keepCasing ? "Nyaa" : "nyaa");
                        }
                        else if (currentChar == 'O' || currentChar == 'o')
                        {
                            if (previousChar == 'N' || previousChar == 'n' ||
                                previousChar == 'M' || previousChar == 'm')
                            {
                                sb.Append("yo");
                            }
                            else
                            {
                                sb.Append(keepCasing ? sref.Data[i] : char.ToLower(sref.Data[i]));
                            }
                        }
                        else if (currentChar == '!')
                        {
                            sb.Append(currentChar);
                            if (ThreadSafeRandom.Next(2) == 0)
                            {
                                sb.Append(currentChar); // append another ! 50% of the time
                            }
                        }
                        else
                        {
                            sb.Append(keepCasing ? sref.Data[i] : char.ToLower(sref.Data[i]));
                        }

                        previousChar = currentChar;
                    }

                    var str = sb.ToString();
                    str = str.Replace("fuck", keepCasing ? "UwU" : "uwu", StringComparison.InvariantCultureIgnoreCase);
                    TLKHandler.ReplaceString(sref.StringID, str, langCode);
                }
            }
            return true;
        }

        private static void FindSkipRanges(ME1TalkFile.TLKStringRef sref, List<int> skipRanges)
        {
            var str = sref.Data;
            int startPos = -1;
            char openingChar = (char)0x00;
            for (int i = 0; i < sref.Data.Length; i++)
            {
                if (startPos < 0 && (sref.Data[i] == '[' || sref.Data[i] == '<'))
                {
                    startPos = i;
                    openingChar = sref.Data[i];
                }
                else if (startPos >= 0 && openingChar == '[' && sref.Data[i] == ']') // ui control token
                {
                    var insideStr = sref.Data.Substring(startPos + 1, i - startPos - 1);
                    if (insideStr.StartsWith("Xbox", StringComparison.InvariantCultureIgnoreCase))
                    {
                        skipRanges.Add(startPos);
                        skipRanges.Add(i + 1);
                    }
                    //else
                    //    Debug.WriteLine(insideStr);

                    startPos = -1;
                    openingChar = (char)0x00;

                }
                else if (startPos >= 0 && openingChar == '<' && sref.Data[i] == '>') //cust token
                {
                    var insideStr = sref.Data.Substring(startPos + 1, i - startPos - 1);
                    if (insideStr.StartsWith("CUSTOM") || insideStr.StartsWith("font") || insideStr.StartsWith("/font") || insideStr.Equals("br", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // custom token. Do not modify it
                        skipRanges.Add(startPos);
                        skipRanges.Add(i + 1);
                    }
                    //else
                    //Debug.WriteLine(insideStr);

                    startPos = -1;
                    openingChar = (char)0x00;
                }

                // it's nothing.
            }
        }

        /// <summary>
        /// Gets map of uppercase consonants mapped to the same letter. The values can then be changed to build a translation map
        /// </summary>
        /// <returns></returns>
        private static Dictionary<char, char> GetConsonantMap()
        {
            var consonants = "BCDFGHJKLMNPQRSTVWXYZ"; // Remaining consonants
            return consonants.ToDictionary(x => x, x => x);
        }

        /// <summary>
        /// Gets map of uppercase vowels mapped to the same letter. The values can then be changed to build a translation map
        /// </summary>
        /// <returns></returns>
        private static Dictionary<char, char> GetVowelMap()
        {
            var vowels = "AEIOU".ToList(); // Remaining consonants
            return vowels.ToDictionary(x => x, x => x);
        }

        /// <summary>
        /// Swap the vowels around. Optional hard mode allows swapping 2 consonants to make it extra difficult to read
        /// </summary>
        /// <param name="Tlks"></param>
        public static bool RandomizeVowels(RandomizationOption option)
        {
            // Map of what letter maps to what other letter
            Log.Information("Randomizing vowels in words");
            var hardMode = option.HasSubOptionSelected(RTexts.SUBOPTIONKEY_VOWELS_HARDMODE);

            var vowels = GetVowelMap();
            var vowelValues = vowels.Values.ToList();
            foreach (var sourceVowel in vowels.Keys)
            {
                var value = vowelValues.RandomElement();
                while (hardMode && value == sourceVowel)
                {
                    value = vowelValues.RandomElement();
                }

                vowelValues.Remove(value); // Do not allow reassignment of same vowel
                Debug.WriteLine($"Vowel Randomizer: {sourceVowel} -> {value}");
                vowels[sourceVowel] = value;
            }

            var consonants = GetConsonantMap();
            if (hardMode)
            {
                // Swap some consontants around
                var numConsonantsToRandomize = 2;
                var consonantValues = consonants.Values.ToList();
                while (numConsonantsToRandomize > 0)
                {
                    var sourceValue = consonantValues.RandomElement();
                    var value = consonantValues.RandomElement();
                    while (sourceValue == value)
                        value = consonantValues.RandomElement();
                    consonantValues.Remove(value); // Do not allow reassignment of same vowel
                    consonantValues.Remove(sourceValue); // Do not allow reassignment of same vowel

                    Debug.WriteLine($"Vowel Randomizer Hard Mode: {sourceValue} -> {value}");

                    consonants[sourceValue] = value;
                    consonants[value] = sourceValue;
                    numConsonantsToRandomize--;
                }
            }

            // Build full translation map (uppercase)
            var translationMapUC = new[] { vowels, consonants }.SelectMany(dict => dict)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            // Add lowercase translation
            var lowerCaseMap = translationMapUC.ToDictionary(x => char.ToLowerInvariant(x.Key), x => char.ToLowerInvariant(x.Value));

            // Build a full translation
            var translationMap = new[] { translationMapUC, lowerCaseMap }.SelectMany(dict => dict)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            Parallel.ForEach(TLKHandler.GetOfficialTLKs(), tf =>
            {
                var tfName = Path.GetFileNameWithoutExtension(tf.path);
                var langCode = tfName.Substring(tfName.LastIndexOf("_", StringComparison.InvariantCultureIgnoreCase) + 1);

                foreach (var sref in tf.StringRefs.Where(x => x.StringID > 0 && !string.IsNullOrWhiteSpace(x.Data)))
                {
                    if (sref.Data.Contains("DLC_")) continue; // Don't modify

                    // See if strref has CUSTOMTOKEN or a control symbol
                    List<int> skipRanges = new List<int>();
                    FindSkipRanges(sref, skipRanges);

                    var newStr = sref.Data.ToArray();
                    for (int i = 0; i < sref.Data.Length; i++) // For every letter
                    {
                        // Skip any items we should not skip.
                        if (skipRanges.Any() && skipRanges[0] == i)
                        {
                            i = skipRanges[1] - 1; // We subtract one as the next iteration of the loop will +1 it again, which then will make it read the 'next' character
                            skipRanges.RemoveAt(0); // remove first 2
                            skipRanges.RemoveAt(0); // remove first 2

                            if (i >= sref.Data.Length - 1)
                                break;
                            continue;
                        }

                        if (translationMap.ContainsKey(newStr[i]))
                        {
                            // Remap the letter
                            newStr[i] = translationMap[newStr[i]];
                        }
                        else
                        {
                            // Do not change the letter. It might be something like <.
                        }

                        TLKHandler.ReplaceString(sref.StringID, new string(newStr), langCode);
                    }
                }
            });
            return true;
        }
    }
}
