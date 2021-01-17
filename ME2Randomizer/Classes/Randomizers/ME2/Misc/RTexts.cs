using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.TLK.ME2ME3;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class RTexts
    {
        public static bool RandomizeIntroText(RandomizationOption arg)
        {
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("openingcrawls.xml");
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
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("gameovertexts.xml");
            XElement rootElement = XElement.Parse(fileContents);
            var gameoverTexts = rootElement.Elements("gameovertext").Select(x => x.Value).ToList();
            var gameOverText = gameoverTexts[ThreadSafeRandom.Next(gameoverTexts.Count)];
            TLKHandler.ReplaceString(157152, gameOverText);
            return true;
        }


        private static readonly List<char> englishVowels = new List<char>(new[] { 'a', 'e', 'i', 'o', 'u' });
        private static readonly List<char> upperCaseVowels = new List<char>(new[] { 'A', 'E', 'I', 'O', 'U' });


        /// <summary>
        /// Swap the vowels around
        /// </summary>
        /// <param name="Tlks"></param>
        public static bool RandomizeVowels(RandomizationOption option)
        {
            List<char> scottishVowelOrdering;
            List<char> upperScottishVowelOrdering;
            Log.Information("Making text possibly scottish");
            scottishVowelOrdering = new List<char>(new char[] { 'a', 'e', 'i', 'o', 'u' });
            scottishVowelOrdering.Shuffle();
            upperScottishVowelOrdering = new List<char>();
            foreach (var c in scottishVowelOrdering)
            {
                upperScottishVowelOrdering.Add(char.ToUpper(c, CultureInfo.InvariantCulture));
            }

            int currentTlkIndex = 0;
            foreach (TalkFile tf in TLKHandler.GetOfficialTLKs())
            {
                var tfName = Path.GetFileNameWithoutExtension(tf.path);
                var langCode = tfName.Substring(tfName.LastIndexOf("_", StringComparison.InvariantCultureIgnoreCase) + 1);
                currentTlkIndex++;
                int max = tf.StringRefs.Count();
                int current = 0;

                foreach (var sref in tf.StringRefs)
                {
                    current++;
                    if (string.IsNullOrWhiteSpace(sref.Data) || sref.Data.Contains("DLC_")) continue; //This string has already been updated and should not be modified.

                    if (!string.IsNullOrWhiteSpace(sref.Data))
                    {
                        string originalString = sref.Data;
                        if (originalString.Length == 1)
                        {
                            continue; //Don't modify I, A
                        }

                        string[] words = originalString.Split(' ');
                        for (int j = 0; j < words.Length; j++)
                        {
                            string word = words[j];
                            if (word.Length == 1 || IsBlacklistedWord(word))
                            {
                                continue; //Don't modify I, A
                            }

                            char[] newStringAsChars = word.ToArray();
                            for (int i = 0; i < word.Length; i++)
                            {
                                //Undercase
                                var vowelIndex = englishVowels.IndexOf(word[i]);
                                if (vowelIndex >= 0)
                                {
                                    if (i + 1 < word.Length && englishVowels.Contains(word[i + 1]))
                                    {
                                        continue; //don't modify dual vowel first letters.
                                    }
                                    else
                                    {
                                        newStringAsChars[i] = scottishVowelOrdering[vowelIndex];
                                    }
                                }
                                else
                                {
                                    var upperVowelIndex = upperCaseVowels.IndexOf(word[i]);
                                    if (upperVowelIndex >= 0)
                                    {
                                        if (i + 1 < word.Length && upperCaseVowels.Contains(word[i + 1]))
                                        {
                                            continue; //don't modify dual vowel first letters.
                                        }
                                        else
                                        {
                                            newStringAsChars[i] = upperScottishVowelOrdering[upperVowelIndex];
                                        }
                                    }
                                }
                            }

                            words[j] = new string(newStringAsChars);
                        }

                        string rebuiltStr = string.Join(" ", words);
                        TLKHandler.ReplaceString(sref.StringID, rebuiltStr, langCode);
                    }
                }
            }

            return true;
        }

        private static bool IsBlacklistedWord(string word)
        {
            if (word.Contains("<CUSTOM")) return true;
            if (word.StartsWith("[") && word.EndsWith("]")) return true; //control token
            return false;
        }
    }
}
