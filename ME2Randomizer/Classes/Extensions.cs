using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ME2Randomizer.Classes.Randomizers;

namespace ME2Randomizer.Classes
{
    public static class StringExtensions
    {
        public static SolidColorBrush ToBrush(this string hexColorString)
        {
            return (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColorString));
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
