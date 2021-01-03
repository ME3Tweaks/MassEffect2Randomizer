using System;
using System.Collections.Generic;

namespace ME2Randomizer.Classes.gameini
{
    public class CaseInsensitiveDictionary<V> : Dictionary<string, V>
    {
        public CaseInsensitiveDictionary() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
