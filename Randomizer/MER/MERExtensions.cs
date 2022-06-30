using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using Randomizer.Randomizers;

namespace Randomizer.MER
{
    public static class UnrealExtensions
    {
        /// <summary>
        /// Gets the defaults for this export - the export must be a class. Returns null if the defaults is an import.
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        public static ExportEntry GetDefaults(this ExportEntry export)
        {
            return export.FileRef.GetUExport(ObjectBinary.From<UClass>(export).Defaults);
        }

        public static ExportEntry GetLevel(this IMEPackage package)
        {
            return package.FindExport("TheWorld.PersistentLevel");
        }

        public static Level GetLevelBinary(this IMEPackage package)
        {
            var level = GetLevel(package);
            if (level != null)
                return ObjectBinary.From<Level>(level);
            return null;
        }
    }

    public static class StringExtensions
    {
        public static SolidColorBrush ToBrush(this string hexColorString)
        {
            return (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColorString));
        }

        /// <summary>
        /// Returns a localized version of this filename, such as Startup.pcc to Startup_INT.pcc. Works on full paths too.
        /// </summary>
        /// <param name="origName"></param>
        /// <returns></returns>
        public static string ToLocalizedFilename(this string origName)
        {
            var fname = Path.GetFileNameWithoutExtension(origName);
            if (!fname.Contains(Path.DirectorySeparatorChar))
            {
                // localized filename
                return $"{fname}_INT{Path.GetExtension(origName)}";
            }

            // Full path
            var parent = Directory.GetParent(origName);
            return Path.Combine(parent.FullName, $"{fname}_INT{Path.GetExtension(origName)}");
        }

        /// <summary>
        /// Memory efficient line splitter.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        // From https://stackoverflow.com/questions/1547476/easiest-way-to-split-a-string-on-newlines-in-net
        public static IEnumerable<string> SplitToLines(this string input)
        {
            if (input == null)
            {
                yield break;
            }

            using (System.IO.StringReader reader = new System.IO.StringReader(input))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        ///     A string extension method that query if '@this' contains any values.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="values">A variable-length parameters list containing values.</param>
        /// <returns>true if it contains any values, otherwise false.</returns>
        public static bool ContainsAny(this string @this, params string[] values)
        {
            foreach (string value in values)
            {
                if (@this.IndexOf(value, StringComparison.Ordinal) != -1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     A string extension method that query if '@this' contains any values.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="comparisonType">Type of the comparison.</param>
        /// <param name="values">A variable-length parameters list containing values.</param>
        /// <returns>true if it contains any values, otherwise false.</returns>
        public static bool ContainsAny(this string @this, StringComparison comparisonType, params string[] values)
        {
            foreach (string value in values)
            {
                if (@this.IndexOf(value, comparisonType) != -1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Trims each line of text and then reassembles the string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string TrimLines(this string s)
        {
            List<string> lines = s.Split('\n').ToList();

            //Trim first empty lines
            while (lines.Count > 0 && lines[0].Trim() == "")
            {
                lines.RemoveAt(0);
            }

            //Trim trailing newines
            while (lines.Count > 0 && lines[lines.Count - 1].Trim() == "")
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines.Select(x => x.Trim()));
        }

        /// <summary>
        /// Replaces a string within another string, case insensitive.
        /// </summary>
        /// <param name="str">The input string</param>
        /// <param name="from">What is being replaced</param>
        /// <param name="to">The new string to replace with</param>
        /// <returns></returns>
        public static string ReplaceInsensitive(this string str, string from, string to)
        {
            str = Regex.Replace(str, from, to, RegexOptions.IgnoreCase);
            return str;
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
    }

    public static class ListExtensions
    {
        public static int RandomIndex<T>(this IList<T> list)
        {
            return ThreadSafeRandom.Next(list.Count);
        }

        public static T RandomElement<T>(this IList<T> list)
        {
            return list[RandomIndex(list)];
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Removes the first item from the list and removes it. Ensure this method is not called if the list is being iterated on in foreach()!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T PullFirstItem<T>(this IList<T> list)
        {
            if (list.Count == 0)
                throw new Exception("Cannot pull item from list, as list is empty!");
            var retVal = list[0];
            list.RemoveAt(0);
            return retVal;
        }
    }

    public static class DictionaryExtensions
    {
        // Kind of inefficient...
        public static KeyValuePair<TKey, TValue> RandomKeyPair<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            var keys = dict.Keys.ToList();
            var values = dict.Values.ToList();

            var index = keys.RandomIndex();
            return new KeyValuePair<TKey, TValue>(keys[index], values[index]);

        }
    }

    public static class Vector3Extensions
    {
        public static float Min(this Vector3 vector)
        {
            float min = vector.X;
            if (vector.Y < min) min = vector.Y;
            if (vector.Z < min) min = vector.Z;
            return min;
        }

        public static float Max(this Vector3 vector)
        {
            float max = vector.X;
            if (vector.Y > max) max = vector.Y;
            if (vector.Z > max) max = vector.Z;
            return max;
        }
    }
}
