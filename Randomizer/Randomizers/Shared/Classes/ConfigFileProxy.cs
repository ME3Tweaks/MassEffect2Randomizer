using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Coalesced.Xml;
using LegendaryExplorerCore.Misc;

namespace Randomizer.Randomizers.Shared.Classes
{
    //public class ConfigFileProxy
    //{
    //    public enum ConfigPropertyType
    //    {
    //        /// <summary>
    //        /// Assigns a value to a property.
    //        /// </summary>
    //        ASSIGN = 0,
    //        /// <summary>
    //        /// Clears an array property.
    //        /// </summary>
    //        CLEAR = 1,
    //        /// <summary>
    //        /// Adds a new item to an array property.
    //        /// </summary>
    //        ADD = 2,
    //        /// <summary>
    //        /// Adds a new item to an array property, if it is unique.
    //        /// </summary>
    //        ADDUNIQUE = 3,
    //        /// <summary>
    //        /// Removes an item from an array property, if it exactly matches.
    //        /// </summary>
    //        SUBTRACT = 4
    //    }

    //    public ConfigSection this[string sectionName]
    //    {
    //        get
    //        {
    //            var existingSection = Sections.FirstOrDefault(x => x.Header == sectionName);
    //            if (existingSection != null) return existingSection;
    //            var ns = new ConfigSection()
    //            {
    //                Header = sectionName
    //            };
    //            Sections.Add(ns);
    //            return ns;
    //        }
    //        set
    //        {
    //            var sectionToReplace = Sections.FirstOrDefault(x => x.Header == sectionName);
    //            if (sectionToReplace != null)
    //            {
    //                Sections.Remove(sectionToReplace);
    //            }
    //            Sections.Add(value);
    //        }
    //    }

    //    public List<ConfigSection> Sections = new List<ConfigSection>();

    //    public ConfigIniEntry GetValue(string sectionname, string key, ConfigPropertyType? type = null)
    //    {
    //        var section = GetSection(sectionname);
    //        return section?.GetValue(key, type);
    //    }

    //    public ConfigSection GetSection(string sectionname)
    //    {
    //        return Sections.FirstOrDefault(x => x.Header.Equals(sectionname, StringComparison.InvariantCultureIgnoreCase));
    //    }

    //    public ConfigSection GetOrAddSection(string sectionname)
    //    {
    //        var s = GetSection(sectionname);
    //        if (s != null) return s;
    //        s = new ConfigSection() { Header = sectionname };
    //        Sections.Add(s);
    //        return s;
    //    }

    //    public ConfigSection GetSection(ConfigSection configSection)
    //    {
    //        return Sections.FirstOrDefault(x => x.Header.Equals(configSection.Header, StringComparison.InvariantCultureIgnoreCase));
    //    }

   

    //    /// <summary>
    //    /// Parses coalesce asset xml to ConfigFileProxy object
    //    /// </summary>
    //    /// <param name="xmlText"></param>
    //    /// <returns></returns>
    //    public static ConfigFileProxy ParseCoalesceAsset(string xmlText)
    //    {
    //        ConfigFileProxy cfp = new ConfigFileProxy();
    //        var coalAsset = XmlCoalesceAsset.LoadFromMemory(xmlText);
    //        ConfigSection currentConfigSection = null;
    //        foreach (var line in coalAsset.Sections)
    //        {
    //            var configSection = new ConfigSection() { Header = line.Key };
    //            foreach (var entry in line.Value.Keys)
    //            {
    //                configSection.Entries.Add(new ConfigIniEntry(line.Value[line.Key].,));
    //            }

    //            string trimmed = line.Trim();
    //            if (string.IsNullOrWhiteSpace(trimmed)) continue; //blank line
    //            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
    //            {
    //                //New section
    //                currentConfigSection = new ConfigSection()
    //                {
    //                    Header = trimmed.Trim('[', ']')
    //                };
    //                cfp.Sections.Add(currentConfigSection);
    //            }
    //            else if (currentConfigSection == null)
    //            {
    //                continue; //this parser only supports section items
    //            }
    //            else
    //            {
    //                currentConfigSection.Entries.Add(new ConfigIniEntry(trimmed, null));
    //            }
    //        }
    //        return cfp;
    //    }


    //    /// <summary>
    //    /// Converts this ConfigIni object into an ME2-style ini file as a string.
    //    /// </summary>
    //    /// <returns></returns>
    //    public string ToIniString()
    //    {
    //        StringBuilder sb = new StringBuilder();
    //        bool isFirst = true;
    //        foreach (var section in Sections)
    //        {
    //            if (!section.Entries.Any())
    //            {
    //                continue; //Do not write out empty sections.
    //            }
    //            if (isFirst)
    //            {
    //                isFirst = false;
    //            }
    //            else
    //            {
    //                sb.Append("\n");
    //            }
    //            sb.Append($"[{section.Header}]");
    //            sb.Append("\n"); //AppendLine does \r\n which we don't want.
    //            foreach (var line in section.Entries)
    //            {
    //                line.PrintIniPrefix(sb);
    //                if (line.HasValue)
    //                {
    //                    sb.Append($"{line.Key}={line.Value}");
    //                    sb.Append("\n"); //AppendLine does \r\n which we don't want.
    //                }
    //                else
    //                {
    //                    sb.Append(line.RawText);
    //                    sb.Append("\n"); //AppendLine does \r\n which we don't want.
    //                }
    //            }
    //        }

    //        return sb.ToString();
    //    }

    //    /// <summary>
    //    /// Converts this ConfigIni object into an ME3-style xml file as a string.
    //    /// </summary>
    //    /// <returns></returns>
    //    public string ToXmlString()
    //    {
    //        XDocument doc = new XDocument();
    //        var coalesceAsset = new XElement("CoalesceAsset");
    //        // Todo: Somehow add attributes, maybe?

    //        doc.Root.Add(coalesceAsset);
    //        var sections = new XElement("Sections");
    //        coalesceAsset.Add(sections);

    //        foreach (var section in Sections)
    //        {
    //            if (!section.Entries.Any())
    //            {
    //                continue; //Do not write out empty sections.
    //            }

    //            var xsection = new XElement("Section");
    //            xsection.SetAttributeValue("name", section.Header);
    //            sections.Add(xsection);

    //            foreach (var entry in section.Entries)
    //            {
    //                var xproperty = new XElement("Property");
    //                xproperty.SetAttributeValue("name", entry.Value);
    //                xproperty.SetAttributeValue("type", entry.EntryType);
    //                xsection.Add(xproperty);
    //            }
    //        }

    //        return doc.ToString();
    //    }

    //    /// <summary>
    //    /// Writes this ini file out to a file using the ToString() method
    //    /// </summary>
    //    /// <param name="filePath"></param>
    //    /// <param name="encoding"></param>
    //    public void WriteToFile(string filePath, Encoding encoding = null)
    //    {
    //        WriteToFile(filePath, ToString(), encoding);
    //    }

    //    /// <summary>
    //    /// Writes a specified ini string out to a file
    //    /// </summary>
    //    /// <param name="filePath"></param>
    //    /// <param name="iniString"></param>
    //    /// <param name="encoding"></param>
    //    public void WriteToFile(string filePath, string iniString, Encoding encoding = null)
    //    {
    //        encoding ??= Encoding.UTF8;

    //        using FileStream fs = File.Open(filePath, FileMode.Create, FileAccess.Write);
    //        using StreamWriter sr = new(fs, encoding);
    //        sr.Write(iniString);
    //    }

    //    [DebuggerDisplay("ConfigIni Section [{Header}] with {Entries.Count} entries")]
    //    public class ConfigSection
    //    {
    //        /// <summary>
    //        /// The name of the section.
    //        /// </summary>
    //        public string Header;

    //        /// <summary>
    //        /// The entries in the section.
    //        /// </summary>
    //        public List<ConfigIniEntry> Entries = new List<ConfigIniEntry>();

    //        public ConfigIniEntry GetValue(string key, ConfigPropertyType? type = null)
    //        {
    //            return Entries.FirstOrDefault(x => x.Key != null && x.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase) && (type == null || x.EntryType == type));
    //        }

    //        public ConfigIniEntry this[string keyname]
    //        {
    //            get
    //            {
    //                var firstExistingEntry = Entries.FirstOrDefault(x => x.Key == keyname);
    //                if (firstExistingEntry != null) return firstExistingEntry;

    //                var ne = new ConfigIniEntry(keyname, ConfigPropertyType.ADD);
    //                Entries.Add(ne);
    //                return ne;
    //            }
    //            set
    //            {
    //                var keyToReplace = Entries.FirstOrDefault(x => x.Key == keyname);
    //                if (keyToReplace != null)
    //                {
    //                    Entries.Remove(keyToReplace);
    //                }
    //                Entries.Add(value);
    //            }
    //        }

    //        public void SetSingleEntry(string key, string value, ConfigPropertyType type)
    //        {
    //            RemoveAllNamedEntries(key);
    //            Entries.Add(new ConfigIniEntry(key, value, type));
    //        }

    //        public void SetSingleEntry(string key, int value, ConfigPropertyType type)
    //        {
    //            RemoveAllNamedEntries(key);
    //            Entries.Add(new ConfigIniEntry(key, value.ToString(), type));
    //        }

    //        public void SetSingleEntry(string key, float value, ConfigPropertyType type)
    //        {
    //            RemoveAllNamedEntries(key);
    //            Entries.Add(new ConfigIniEntry(key, value.ToString(CultureInfo.InvariantCulture), type));
    //        }

    //        /// <summary>
    //        /// Removes all entries from this section with the specified name. If the name is not specified, all entries are removed.
    //        /// </summary>
    //        /// <param name="name"></param>
    //        public void RemoveAllNamedEntries(string name = null)
    //        {
    //            if (name != null)
    //            {
    //                Entries.RemoveAll(x => x.Key == name);
    //            }
    //            else
    //            {
    //                Entries.Clear();
    //            }
    //        }
    //    }

    //    /*[DebuggerDisplay("IniEntry {Key} = {Value}")]

    //    public class ConfigIniEntry
    //    {
    //        public string RawText;

    //        public bool HasValue => Key != null && Value != null;

    //        public ConfigIniEntry(string line, ConfigPropertyType? type)
    //        {
    //            RawText = line;
    //            Key = KeyPair.Key;
    //            Value = KeyPair.Value;

    //            if (TryGetEntryType(Key, out var foundType) && foundType != null) // != null is redundant but makes VS happy
    //            {
    //                EntryType = foundType.Value;
    //                Key = KeyPair.Key.Substring(1);
    //            }
    //            else
    //            {
    //                if (type != null)
    //                {
    //                    EntryType = type.Value;
    //                }
    //                else
    //                {
    //                    EntryType = ConfigPropertyType.ADD;
    //                }
    //            }
    //        }
    //        public ConfigIniEntry(string key, string value, ConfigPropertyType? type)
    //        {
    //            RawText = $"{key}={value}";

    //            if (TryGetEntryType(key, out var foundType))
    //            {
    //                EntryType = foundType.Value;
    //                Key = KeyPair.Key.Substring(1);
    //            }
    //            else
    //            {
    //                Key = key;
    //                if (type != null)
    //                {
    //                    EntryType = type.Value;
    //                }
    //                else
    //                {
    //                    EntryType = ConfigPropertyType.ADD;
    //                }
    //            }

    //            Value = KeyPair.Value;
    //        }

    //        private bool TryGetEntryType(string key, out ConfigPropertyType? foundType)
    //        {
    //            var firstChar = key[0];
    //            switch (firstChar)
    //            {

    //                // This doesn't appear to be used
    //                //case '.':
    //                //    foundType = ConfigPropertyType.ADD;
    //                //    return true;
    //                case '!':
    //                    foundType = ConfigPropertyType.CLEAR;
    //                    return true;
    //                case '+':
    //                    foundType = ConfigPropertyType.ADDUNIQUE;
    //                    return true;
    //                case '-':
    //                    foundType = ConfigPropertyType.SUBTRACT;
    //                    return true;
    //            }

    //            foundType = null;
    //            return false;
    //        }

    //        public string Key { get; set; }

    //        public string Value { get; set; }
    //        public ConfigPropertyType EntryType { get; set; }

    //        public KeyValuePair<string, string> KeyPair
    //        {
    //            get
    //            {
    //                var separator = RawText.IndexOf('=');
    //                if (separator > 0)
    //                {
    //                    string key = RawText.Substring(0, separator).Trim();
    //                    string value = RawText.Substring(separator + 1).Trim();
    //                    return new KeyValuePair<string, string>(key, value);
    //                }
    //                return new KeyValuePair<string, string>(null, null);
    //            }
    //        }

    //        public void PrintIniPrefix(StringBuilder sb)
    //        {
    //            if (EntryType == ConfigPropertyType.ADDUNIQUE) sb.Append("+");
    //            if (EntryType == ConfigPropertyType.SUBTRACT) sb.Append("-");
    //            if (EntryType == ConfigPropertyType.CLEAR) sb.Append("!");
    //        }
    //    }*/
    //}
}
