using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using ME2Randomizer.Classes.Randomizers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes
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
            var parent = Directory.GetParent(origName);
            if (parent == null)
            {
                return $"{fname}_INT.{Path.GetExtension(origName)}";
            }
            return Path.Combine(parent.FullName, $"{fname}_INT.{Path.GetExtension(origName)}");



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
}
