using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK.ME2ME3;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class RTexts
    {
        public const string SUBOPTIONKEY_VOWELS_HARDMODE = "VOWELS_HARDMODE";
        public const string SUBOPTIONKEY_UWU_KEEPCASING = "UWU_KEEPCASING";
        public const string SUBOPTIONKEY_REACTIONS_ENABLED = "UWU_ADDFACES";

        private static List<Reaction> ReactionList;
        private static Regex regexEndOfSentence;
        private static Regex regexAllLetters;
        private static Regex regexPunctuationRemover;
        private static Regex regexBorkedElipsesFixer;

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
            bool addReactions = option.HasSubOptionSelected(RTexts.SUBOPTIONKEY_REACTIONS_ENABLED);

            var existingTLK = TLKHandler.GetBuildingTLK();
            var skipIDs = existingTLK.StringRefs.Select(x => x.StringID).ToList();
            var MERTlks = TLKHandler.GetMERTLKs();

            var nonMerTLKs = TLKHandler.GetAllTLKs().Where(x => !MERTlks.Contains(x));

            option.ProgressValue = 0;
            option.ProgressMax = nonMerTLKs.Where(x=>x.LangCode == "INT").Sum(x => x.StringRefs.Count(y => y.StringID > 0 && !string.IsNullOrWhiteSpace(y.Data)));
            option.ProgressMax += MERTlks.Where(x => x.LangCode == "INT").Sum(x => x.StringRefs.Count(y => y.StringID > 0 && !string.IsNullOrWhiteSpace(y.Data)));
            option.ProgressIndeterminate = false;


            // UwUify MER TLK first
            foreach (TalkFile tf in MERTlks)
            {
                UwuifyTalkFile(tf, keepCasing, addReactions, skipIDs, true, option);
            }

            // UwUify non MER TLK
            foreach (TalkFile tf in nonMerTLKs)
            {
                UwuifyTalkFile(tf, keepCasing, addReactions, skipIDs, false, option);
            }
            return true;
        }

        private static void UwuifyTalkFile(TalkFile tf, bool keepCasing, bool addReactions, List<int> skipIDs, bool isMERTlk, RandomizationOption option)
        {
            var tfName = Path.GetFileNameWithoutExtension(tf.path);
            var langCode = tfName.Substring(tfName.LastIndexOf("_", StringComparison.InvariantCultureIgnoreCase) + 1);
            if (langCode != "INT")
                return;
            foreach (var sref in tf.StringRefs.Where(x => x.StringID > 0 && !string.IsNullOrWhiteSpace(x.Data)))
            {
                option.IncrementProgressValue();

                var strData = sref.Data;

                //strData = "New Game";
                if (strData.Contains("DLC_")) continue; // Don't modify
                if (!isMERTlk && skipIDs.Contains(sref.StringID))
                {
                    continue; // Do not randomize this version of the string as it's in the DLC version specifically
                }

                //if (sref.StringID != 325648)
                //    continue;
                // See if strref has CUSTOMTOKEN or a control symbol
                List<int> skipRanges = new List<int>();
                FindSkipRanges(sref, skipRanges);
                // uwuify it
                StringBuilder sb = new StringBuilder();
                char previousChar = (char)0x00;
                char currentChar;
                for (int i = 0; i < strData.Length; i++)
                {
                    if (skipRanges.Any() && skipRanges[0] == i)
                    {
                        sb.Append(strData.Substring(skipRanges[0], skipRanges[1] - skipRanges[0]));
                        previousChar = (char)0x00;
                        i = skipRanges[1] - 1; // We subtract one as the next iteration of the loop will +1 it again, which then will make it read the 'next' character
                        skipRanges.RemoveAt(0); // remove first 2
                        skipRanges.RemoveAt(0); // remove first 2

                        if (i >= strData.Length - 1)
                            break;
                        continue;
                    }

                    currentChar = strData[i];
                    if (currentChar == 'L' || currentChar == 'R')
                    {
                        sb.Append(keepCasing ? 'W' : 'w');
                    }
                    else if (currentChar == 'l' || currentChar == 'r')
                    {
                        sb.Append('w');
                        if (ThreadSafeRandom.Next(5) == 0)
                        {
                            sb.Append('w'); // append another w 20% of the time
                            if (ThreadSafeRandom.Next(8) == 0)
                            {
                                sb.Append('w'); // append another w 20% of the time
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
                            sb.Append(keepCasing ? strData[i] : char.ToLower(strData[i]));
                        }
                    }
                    else if (currentChar == '!' && !addReactions)
                    {
                        sb.Append(currentChar);
                        if (ThreadSafeRandom.Next(2) == 0)
                        {
                            sb.Append(currentChar); // append another ! 50% of the time
                        }
                    }
                    else
                    {
                        sb.Append(keepCasing ? strData[i] : char.ToLower(strData[i]));
                    }

                    previousChar = currentChar;
                }

                var str = sb.ToString();

                if (addReactions)
                {
                    str = AddReactionToLine(strData, str, keepCasing);
                }
                else
                {
                    str = str.Replace("fuck", keepCasing ? "UwU" : "uwu", StringComparison.InvariantCultureIgnoreCase);
                }

                TLKHandler.ReplaceString(sref.StringID, str, langCode);
            }
        }

        private static char[] uwuPunctuationDuplicateChars = { '?', '!' };


        private static string AddReactionToLine(string vanillaLine, string modifiedLine, bool keepCasing)
        {
            string finalString = "";
            bool dangerousLine = false;

            //initialize reactions/regex if this is first run
            if (ReactionList == null)
            {
                string rawReactionDefinitions = MERUtilities.GetEmbeddedStaticFilesTextFile("reactiondefinitions.xml");
                var reactionXml = new StringReader(rawReactionDefinitions);
                XmlSerializer serializer = new XmlSerializer(typeof(List<Reaction>), new XmlRootAttribute("ReactionDefinitions"));
                ReactionList = (List<Reaction>)serializer.Deserialize(reactionXml);

                regexEndOfSentence = new Regex(@"(?<![M| |n][M|D|r][s|r]\.)(?<!(,""))(?<=[.!?""])(?= [A-Z])", RegexOptions.Compiled);
                regexAllLetters = new Regex("[a-zA-Z]", RegexOptions.Compiled);
                regexPunctuationRemover = new Regex("(?<![D|M|r][w|r|s])[.!?](?!.)", RegexOptions.Compiled);
                regexBorkedElipsesFixer = new Regex("(?<!\\.)\\.\\.(?=\\s|$)", RegexOptions.Compiled);
            }

            if (modifiedLine.Length < 2 || regexAllLetters.Matches(modifiedLine).Count == 0 || vanillaLine.Contains('{'))
            {
                //I should go.
                return modifiedLine;
            }

            char[] dangerousCharacters = { '<', '\n' };
            if (modifiedLine.IndexOfAny(dangerousCharacters) >= 0 || vanillaLine.Length > 200)
            {
                dangerousLine = true;
            }

            //split strings into sentences for processing
            List<string> splitVanilla = new List<string>();
            List<string> splitModified = new List<string>();

            MatchCollection regexMatches = regexEndOfSentence.Matches(vanillaLine);
            int modOffset = 0;

            //for each regex match in the vanilla line:
            for (int matchIndex = 0; matchIndex <= regexMatches.Count; matchIndex++)
            {
                int start;
                int stop;

                //vanilla sentence splitting
                //find indexes for start and stop from surrounding regexMatches
                if (regexMatches.Count == 0)
                {
                    start = 0;
                    stop = vanillaLine.Length;
                }
                else if (matchIndex == 0)
                {
                    start = 0;
                    stop = regexMatches[matchIndex].Index;
                }
                else if (matchIndex == regexMatches.Count)
                {
                    start = regexMatches[matchIndex - 1].Index;
                    stop = vanillaLine.Length;
                }
                else
                {
                    start = regexMatches[matchIndex - 1].Index;
                    stop = regexMatches[matchIndex].Index;
                }

                splitVanilla.Add(vanillaLine.Substring(start, stop - start));

                //modified sentence splitting
                int modStart = start + modOffset;
                int modStop = stop + modOffset;

                //step through sentence looking for punctuation or EOL
                while (!((".!?\"").Contains(modifiedLine[modStop - 1])) && ((modStop - 1) < (modifiedLine.Length - 1)))
                {
                    modOffset++;
                    modStop++;
                }

                //step through sentence looking for next space character or EOL
                while (!(modifiedLine[modStop - 1].Equals(' ')) && ((modStop - 1) < (modifiedLine.Length - 1)))
                {
                    modOffset++;
                    modStop++;
                }

                //if we found a space, step back
                if (modStop < modifiedLine.Length)
                {
                    modOffset--;
                    modStop--;
                }

                splitModified.Add(modifiedLine.Substring(modStart, modStop - modStart));
            }

            //reaction handling loop
            for (int i = 0; i < splitVanilla.Count; i++)
            {
                string sv = splitVanilla[i];
                string sm = splitModified[i];

                //calculate scores
                foreach (Reaction r in ReactionList)
                {
                    r.keywordScore = 0;

                    string s = (r.properties.Contains("comparetomodified") ? sm : sv);
                    foreach (string keyword in r.keywords)
                    {
                        //if the keyword contains a capital letter, it's case sensitive
                        if (s.Contains(keyword, (keyword.Any(char.IsUpper) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)))
                        {
                            r.EarnPoint();
                        }
                    }

                    //question check, done here to make it a semi-random point
                    if (s.Contains('?') && r.properties.Contains("question") && ThreadSafeRandom.Next(10) == 0)
                    {
                        r.EarnPoint();
                    }

                    //exclamation check
                    if (s.Contains('!') && r.properties.Contains("exclamation") && ThreadSafeRandom.Next(10) == 0)
                    {
                        r.EarnPoint();
                    }
                }

                //determine winner
                ReactionList.Shuffle(); //in case of a tie
                Reaction winningReaction = new Reaction();
                foreach (Reaction r in ReactionList)
                {
                    if (r.properties.Contains("nolower") && !keepCasing)
                    {
                        //reaction is not lowercase safe and lowercase is enabled
                        continue;
                    }

                    if (r.properties.Contains("dangerous") && dangerousLine)
                    {
                        //reaction is dangerous and this is a dangerous string
                        continue;
                    }

                    if (!r.properties.Contains("lowpriority"))
                    {
                        if ((r.keywordScore >= winningReaction.keywordScore))
                        {
                            winningReaction = r;
                        }
                    }
                    else
                    {
                        if ((r.keywordScore > winningReaction.keywordScore))
                        {
                            winningReaction = r;
                        }
                    }
                }

                //winner processing if one exists
                if (winningReaction.keywordScore > 0)
                {
                    //we have a winner!! congwatuwations ^_^
                    if (!winningReaction.properties.Contains("easteregg"))
                    {
                        //standard winner processing. remove punctuation, apply face to line
                        sm = regexPunctuationRemover.Replace(sm, "");
                        sm += " " + winningReaction.GetFace();

                        if (!keepCasing)
                        {
                            sm = sm.ToLower();
                        }
                    }
                    else
                    {
                        //easter egg processing
                        switch (winningReaction.name)
                        {
                            case "reee":
                                //SAREN REEEEE
                                //technically should be WEEEEEE. Still funny, but WEEEEE isn't the meme.
                                string reee = "Sawen REEEEE";

                                for (int e = 0; e < ThreadSafeRandom.Next(8); e++)
                                {
                                    reee += 'E';
                                }

                                if (!keepCasing)
                                {
                                    reee = reee.ToLower();
                                }

                                sm = Regex.Replace(sm, "(s|S)aw*en", reee);
                                break;

                            case "finger":
                                //Sovereign t('.'t)
                                string finger = "Soveweign t('.'t)";

                                if (!keepCasing)
                                {
                                    finger = finger.ToLower();
                                }

                                sm = Regex.Replace(sm, "(S|s)ovew*eign", finger);
                                break;

                            case "jason":
                                //Jacob? Who?
                                string jason = winningReaction.GetFace();

                                if (!keepCasing)
                                {
                                    jason = jason.ToLower();
                                }

                                sm = Regex.Replace(sm, "(J|j)acob", jason);
                                break;

                            case "shitpost":
                                //We ArE hArBiNgEr
                                bool flipflop = true;
                                string sHiTpOsT = "";
                                foreach (char c in sm.ToCharArray())
                                {
                                    if (flipflop)
                                    {
                                        sHiTpOsT += char.ToUpper(c);
                                    }
                                    else
                                    {
                                        sHiTpOsT += char.ToLower(c);
                                    }
                                    flipflop = !flipflop;
                                }
                                sm = sHiTpOsT;
                                break;

                            case "kitty":
                                //nyaaaa =^.^=
                                string nyaSentence = "";
                                string[] words = sm.Split(" ");
                                for (int w = 0; w < words.Length; w++) //each word in sentence
                                {
                                    foreach (string k in winningReaction.keywords) //each keyword in reaction
                                    {
                                        //semi-random otherwise it's EVERYWHERE
                                        if (words[w].Contains(k, StringComparison.OrdinalIgnoreCase) && ThreadSafeRandom.Next(5) == 0)
                                        {
                                            words[w] = Regex.Replace(words[w], "[.!?]", "");
                                            words[w] += " " + winningReaction.GetFace();
                                        }
                                    }
                                    nyaSentence += words[w] + " ";
                                }
                                nyaSentence = nyaSentence.Remove(nyaSentence.Length - 1, 1); //string will always have a trailing space
                                sm = nyaSentence;
                                break;

                            default:
                                Debug.WriteLine("Easter egg reaction happened, but it was left unhandled!");
                                break;
                        }
                    }
                }
                finalString += sm;
            }

            //borked elipses removal
            finalString = regexBorkedElipsesFixer.Replace(finalString, "");

            //do punctuation duplication thing because it's funny
            foreach (char c in uwuPunctuationDuplicateChars)
            {
                if (finalString.Contains(c))
                {
                    int rnd = ThreadSafeRandom.Next(4);
                    switch (rnd)
                    {
                        case 0:
                            finalString = finalString.Replace(c.ToString(), String.Concat(c, c));
                            break;
                        case 1:
                            finalString = finalString.Replace(c.ToString(), String.Concat(c, c, c));
                            break;
                        default:
                            break;
                    }
                }
            }

            //Debug.WriteLine("----------\nreaction input:  " + vanillaLine);
            //Debug.WriteLine("reaction output: " + finalString);
            return finalString;
        }

        private static void FindSkipRanges(ME1TalkFile.TLKStringRef sref, List<int> skipRanges)
        {
            var str = sref.Data;
            int startPos = -1;
            char openingChar = (char)0x00;
            for (int i = 0; i < sref.Data.Length; i++)
            {
                if (startPos < 0 && (sref.Data[i] == '[' || sref.Data[i] == '<' || sref.Data[i] == '{'))
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
                else if (startPos >= 0 && openingChar == '{' && sref.Data[i] == '}') // token for powers (?)
                {
                    //var insideStr = sref.Data.Substring(startPos + 1, i - startPos - 1);
                    //Debug.WriteLine(insideStr);
                    // { } brackets are for ui tokens in powers, saves, I think.
                    skipRanges.Add(startPos);
                    skipRanges.Add(i + 1);

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
            MERLog.Information("Randomizing vowels in words");
            var hardMode = option.HasSubOptionSelected(RTexts.SUBOPTIONKEY_VOWELS_HARDMODE);


            var existingTLK = TLKHandler.GetBuildingTLK();
            var skipIDs = existingTLK.StringRefs.Select(x => x.StringID).ToList();
            var MERTLKs = TLKHandler.GetMERTLKs();
            Dictionary<char, char> vowels = null;
            List<char> vowelValues = null;

            bool retryMapping = true;
            while (retryMapping)
            {
                bool failed = false;
                vowels = GetVowelMap();
                vowelValues = vowels.Values.ToList();

                int numAttemptsRemaining = 10;

                foreach (var sourceVowel in vowels.Keys)
                {
                    var value = vowelValues.RandomElement();
                    while (hardMode && value == sourceVowel && numAttemptsRemaining > 0)
                    {
                        value = vowelValues.RandomElement();
                        numAttemptsRemaining--;
                    }

                    if (numAttemptsRemaining == 0 && hardMode && value == sourceVowel)
                    {
                        // This attempt has failed
                        MERLog.Warning(@"Hard mode vowel randomization failed, retrying");
                        failed = true;
                        break;
                    }

                    vowelValues.Remove(value); // Do not allow reassignment of same vowel
                    Debug.WriteLine($"Vowel Randomizer: {sourceVowel} -> {value}");
                    vowels[sourceVowel] = value;
                }

                if (!failed)
                    retryMapping = false;
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

            var nonMERTLKs = TLKHandler.GetAllTLKs().Where(x => !MERTLKs.Contains(x)).ToList();
            // MER

            option.ProgressValue = 0;
            option.ProgressMax = nonMERTLKs.Sum(x => x.StringRefs.Count(y => y.StringID > 0 && !string.IsNullOrWhiteSpace(y.Data)));
            option.ProgressMax += MERTLKs.Sum(x => x.StringRefs.Count(y => y.StringID > 0 && !string.IsNullOrWhiteSpace(y.Data)));
            option.ProgressIndeterminate = false;

            foreach (var merTLK in MERTLKs)
            {
                RandomizeVowelsInternal(merTLK, skipIDs, translationMap, true, option);
            }

            // Non MER
            Parallel.ForEach(nonMERTLKs, tf =>
              {
                  RandomizeVowelsInternal(tf, skipIDs, translationMap, false, option);
              });
            return true;
        }

        private static void RandomizeVowelsInternal(TalkFile tf, List<int> skipIDs, Dictionary<char, char> translationMap, bool isMERTlk, RandomizationOption option)
        {
            var tfName = Path.GetFileNameWithoutExtension(tf.path);
            var langCode = tfName.Substring(tfName.LastIndexOf("_", StringComparison.InvariantCultureIgnoreCase) + 1);

            foreach (var sref in tf.StringRefs.Where(x => x.StringID > 0 && !string.IsNullOrWhiteSpace(x.Data)).ToList())
            {
                option.IncrementProgressValue();

                if (sref.Data.Contains("DLC_")) continue; // Don't modify
                if (!isMERTlk && skipIDs.Contains(sref.StringID))
                {
                    continue; // Do not randomize this version of the string as it's in the DLC version specifically
                }
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
        }
    }

    [XmlType("reaction")]
    public class Reaction
    {
        [XmlAttribute("name")]
        public string name;

        [XmlElement("property")]
        public List<string> properties;

        [XmlElement("face")]
        public List<string> faces;

        [XmlElement("keyword")]
        public List<string> keywords;

        public int keywordScore = 0;

        public void EarnPoint()
        {
            keywordScore += (properties.Contains("doublescore") ? 2 : 1);
        }

        public string GetFace()
        {
            return faces[ThreadSafeRandom.Next(faces.Count)];
        }
    }
}
