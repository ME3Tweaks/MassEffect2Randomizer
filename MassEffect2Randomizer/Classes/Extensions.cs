using Gibbed.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MassEffectRandomizer.Classes
{
    public static class EnumerableExtensions
    {
        public static T[] TypedClone<T>(this T[] src)
        {
            return (T[])src.Clone();
        }

        /// <summary>
        /// Overwrites a portion of an array starting at offset with the contents of another array.
        /// Accepts negative indexes
        /// </summary>
        /// <typeparam name="T">Content of array.</typeparam>
        /// <param name="dest">Array to write to</param>
        /// <param name="offset">Start index in dest. Can be negative (eg. last element is -1)</param>
        /// <param name="source">data to write to dest</param>
        public static void OverwriteRange<T>(this IList<T> dest, int offset, IList<T> source)
        {
            if (offset < 0)
            {
                offset = dest.Count + offset;
                if (offset < 0)
                {
                    throw new IndexOutOfRangeException("Attempt to write before the beginning of the array.");
                }
            }
            if (offset + source.Count > dest.Count)
            {
                throw new IndexOutOfRangeException("Attempt to write past the end of the array.");
            }
            for (int i = 0; i < source.Count; i++)
            {
                dest[offset + i] = source[i];
            }
        }
    }

    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds <paramref name="value"/> to List&lt;<typeparamref name="TValue"/>&gt; associated with <paramref name="key"/>. Creates List&lt;<typeparamref name="TValue"/>&gt; if neccesary.
        /// </summary>
        public static void AddToListAt<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out List<TValue> list))
            {
                list = new List<TValue>();
                dict[key] = list;
            }
            list.Add(value);
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        public static bool ContainsKey<Tkey, TValue>(this List<KeyValuePair<Tkey, TValue>> list, Tkey key)
        {
            foreach (var kvp in list)
            {
                if (EqualityComparer<Tkey>.Default.Equals(kvp.Key, key))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetValue<Tkey, TValue>(this List<KeyValuePair<Tkey, TValue>> list, Tkey key, out TValue value)
        {
            foreach (var kvp in list)
            {
                if (EqualityComparer<Tkey>.Default.Equals(kvp.Key, key))
                {
                    value = kvp.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static void Add<Tkey, TValue>(this List<KeyValuePair<Tkey, TValue>> list, Tkey key, TValue value)
        {
            list.Add(new KeyValuePair<Tkey, TValue>(key, value));
        }

        public static IEnumerable<TValue> Values<Tkey, TValue>(this List<KeyValuePair<Tkey, TValue>> list)
        {
            foreach (var kvp in list)
            {
                yield return kvp.Value;
            }
        }
    }


    public static class RandomExtensions
    {
        public static float NextFloat(
            this Random random,
            double minValue,
            double maxValue)
        {
            return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
        }
    }

    public static class ListExtensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list, Random random = null)
        {
            if (random == null && rng == null) rng = new Random();
            Random randomToUse = random ?? rng;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = randomToUse.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public static class IOExtensions
    {
        public static void WriteStringASCII(this Stream stream, string value)
        {
            stream.WriteValueS32(value.Length + 1);
            stream.WriteStringZ(value, Encoding.ASCII);
        }

        public static void WriteStringUnicode(this Stream stream, string value)
        {
            if (value.Length > 0)
            {
                stream.WriteValueS32(-(value.Length + 1));
                stream.WriteStringZ(value, Encoding.Unicode);
            }
            else
            {
                stream.WriteValueS32(0);
            }
        }

        public static void WriteStream(this Stream stream, MemoryStream value)
        {
            value.WriteTo(stream);
        }

        /// <summary>
        /// Copies the inputstream to the outputstream, for the specified amount of bytes
        /// </summary>
        /// <param name="input">Stream to copy from</param>
        /// <param name="output">Stream to copy to</param>
        /// <param name="bytes">The number of bytes to copy</param>
        public static void CopyToEx(this Stream input, Stream output, int bytes)
        {
            var buffer = new byte[32768];
            int read;
            while (bytes > 0 &&
                   (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Returns if extension is pcc/sfm/u/upk
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool RepresentsPackageFilePath(this string path)
        {
            string extension = Path.GetExtension(path);
            if (extension.Equals(".pcc", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (extension.Equals(".sfm", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (extension.Equals(".u", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (extension.Equals(".upk", StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Truncates string so that it is no longer than the specified number of characters.
        /// </summary>
        /// <param name="str">String to truncate.</param>
        /// <param name="length">Maximum string length.</param>
        /// <returns>Original string or a truncated one if the original was too long.</returns>
        public static string Truncate(this string str, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");
            }

            if (str == null)
            {
                return null;
            }

            int maxLength = Math.Min(str.Length, length);
            return str.Substring(0, maxLength);
        }

        public static bool StartsWithAny(this string input, params char[] beginnings)
        {
            foreach (var x in beginnings)
                if (input.StartsWith(x.ToString()))
                    return true;

            return false;
        }

        public static bool ContainsWord(this string s, string word)
        {
            string[] ar = s.Split(' ', '.', ':'); //Split on space and periods

            foreach (string str in ar)
            {
                if (str.ToLower() == word.ToLower())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Splits string on space, period and :.  Returns the index of the word or -1 if not found
        /// </summary>
        /// <param name="s"></param>
        /// <param name="word">Word we are searching for</param>
        /// <returns></returns>
        public static int IndexOfWord(this string s, string word)
        {
            string[] ar = s.Split(' ', '.', ':'); //Split on space and periods

            int i = 0;
            foreach (string str in ar)
            {
                if (str.ToLower() == word.ToLower())
                    return i;
                i++;
            }
            return -1;
        }

        /// <summary>
        /// Trims a string by lines - the beginning and end of the string will have all leading whitespace (multiple lines included) and trailing whitespace too
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string TrimLines(this string s)
        {

            List<string> lines = s.Split('\n').ToList();

            //Trim first empty lines
            while (lines.Count() > 0 && lines[0].Trim() == "")
            {
                lines.RemoveAt(0);
            }

            //Trim trailing newines
            while (lines.Count() > 0 && lines[lines.Count - 1].Trim() == "")
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines.Select(x => x.Trim()));
        }

        public static string ReplaceInsensitive(this string str, string from, string to)
        {
            str = Regex.Replace(str, from, to, RegexOptions.IgnoreCase);
            return str;
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
