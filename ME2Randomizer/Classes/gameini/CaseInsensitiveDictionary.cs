using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RandomizerUI.Classes.gameini
{
    public class CaseInsensitiveDictionary<V> : Dictionary<string, V>
    {
        public CaseInsensitiveDictionary() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }

    public class CaseInsensitiveConcurrentDictionary<V> : ConcurrentDictionary<string, V>
    {
        public CaseInsensitiveConcurrentDictionary() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
