using Randomizer.Randomizers.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game2.Talents
{

    /// <summary>
    /// Talent handling class for ME2/LE2, for base and evolution of powers
    /// </summary>
    [DebuggerDisplay("HTalent - {PowerExport.ObjectName}, Base {BasePower.ObjectName}, IsPassive: {IsPassive}")]
    public class HTalent
    {
        protected bool Equals(HTalent other)
        {
            return Equals(PowerExport.InstancedFullPath, other.PowerExport.InstancedFullPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return PowerExport.InstancedFullPath.Equals(((HTalent)obj).PowerExport.InstancedFullPath);
        }

        public override int GetHashCode()
        {
            return PowerExport != null ? PowerExport.GetHashCode() : 0;
        }
        /// <summary>
        /// If power is shown in the Character Record (squad) screen
        /// </summary>
        public bool ShowInCR { get; set; } = true;

        public PropertyCollection CondensedProperties { get; private set; }

        /// <summary>
        /// Builds a property collection going up the export tree, keeping bottom properties and populating missing ones available in higher classes
        /// </summary>
        /// <param name="export"></param>
        private void CondenseTalentProperties(ExportEntry export)
        {
            CondensedProperties = export.GetDefaults().GetProperties();
            IEntry superEntry = export.SuperClass;
            while (superEntry is ExportEntry superC)
            {
                var archProps = superC.GetDefaults().GetProperties();
                foreach (Property prop in archProps)
                {
                    if (!CondensedProperties.ContainsNamedProp(prop.Name))
                    {
                        CondensedProperties.AddOrReplaceProp(prop);
                    }
                }

                superEntry = superC.SuperClass;
            }
        }

        #region Passive Strings
        public string PassiveDescriptionString { get; set; }
        public string PassiveRankDescriptionString { get; set; }
        public string PassiveTalentDescriptionString { get; set; }

        #endregion
        public HTalent(ExportEntry powerClass, bool isEvolution = false, bool isFixedPower = false)
        {
            PowerExport = powerClass;
            OriginalPowerName = powerClass.ObjectName;
            IsEvolution = isEvolution;

            CondenseTalentProperties(powerClass);
            var displayName = CondensedProperties.GetProp<StringRefProperty>("DisplayName");
            if (displayName != null)
            {
                PowerName = TLKBuilder.TLKLookupByLang(displayName.Value, MELocalization.INT);
            }

            ShowInCR = CondensedProperties.GetProp<BoolProperty>("DisplayInCharacterRecord")?.Value ?? true;

            if (isFixedPower)
            {
                BasePower = PowerExport;
                if (PowerName == null)
                {
                    PowerName = powerClass.ObjectName.Name;
                }
                return;
            }

            var baseClass = powerClass;
            var baseClassObj = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
            while (baseClass.SuperClass is ExportEntry bcExp && (baseClassObj == null || baseClassObj.Value == 0))
            {
                baseClass = bcExp;
                baseClassObj = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
            }

            BasePower = baseClass; // BasePower is used to prevent duplicates... I think

            if (BasePower.ObjectName.Name.Contains("Passive"))
            {
                IsPassive = true;
            }
            else
            {
                var baseName = baseClass.GetDefaults().GetProperty<NameProperty>("BaseName");
                while (baseName == null)
                {
                    baseClass = (ExportEntry)baseClass.SuperClass;
                    baseName = baseClass.GetDefaults().GetProperty<NameProperty>("BaseName");
                }

                BaseName = baseName.Value.Name;
            }

            // Setup name
            //var superDefaults = PowerExport.GetDefaults();
            //var displayNameProps = superDefaults.GetProperties();
            //var superProps = displayNameProps;

            //TalentDescriptionProp = superProps.GetProp<StringRefProperty>("TalentDescription");
            //while (displayName == null)
            //{
            //    superDefaults = ((superDefaults.Class as ExportEntry).SuperClass as ExportEntry).GetDefaults();
            //    superProps = superDefaults.GetProperties();
            //    superProps.GetProp<StringRefProperty>("DisplayName");
            //    displayName = superProps.GetProp<StringRefProperty>("DisplayName");
            //    TalentDescriptionProp ??= superProps.GetProp<StringRefProperty>("TalentDescription");
            //}

            if (IsEvolution)
            {
                // Setup the blurb
                var blurbDesc = TLKBuilder.TLKLookupByLang(CondensedProperties.GetProp<StringRefProperty>("TalentDescription").Value, MELocalization.INT).Split('\n')[0];
                EvolvedBlurb = $"{PowerName}: {blurbDesc}";
            }

            IsAmmoPower = PowerName != null && PowerName.Contains("Ammo");
            IsCombatPower = !IsAmmoPower && !IsPassive;

            if (IsPassive)
            {
                // We have to pull in strings so when we change genders on who we assign this power to, it is accurate.
                var talentStrId = CondensedProperties.GetProp<StringRefProperty>("TalentDescription").Value;
                if (talentStrId == 389424)
                {
                    // This string is not defined in vanilla but we need a value for this to work
                    PassiveTalentDescriptionString = "Kenson's technological prowess refines her combat skills, boosting her health, weapon damage, and shields.";
                    PassiveDescriptionString = "Kenson's technological prowess refines her combat skills, boosting her health, weapon damage, and shields.";
                }
                else
                {
                    PassiveTalentDescriptionString = TLKBuilder.TLKLookupByLang(talentStrId, MELocalization.INT);
                }
                PassiveDescriptionString ??= TLKBuilder.TLKLookupByLang(CondensedProperties.GetProp<StringRefProperty>("Description").Value, MELocalization.INT);
                PassiveRankDescriptionString = TLKBuilder.TLKLookupByLang(CondensedProperties.GetProp<ArrayProperty<StructProperty>>("Ranks")[0].GetProp<StringRefProperty>("Description").Value, MELocalization.INT);
            }
        }


        /// <summary>
        /// Is this a combat power? (Like Warp, Throw, Tech Armor)
        /// </summary>
        public bool IsCombatPower { get; set; }
        /// <summary>
        /// Is this an ammo power? (Like Disruptor, Cryo)
        /// </summary>
        public bool IsAmmoPower { get; set; }

        /// <summary>
        /// Blurb text to use when changing the evolution
        /// </summary>
        public string EvolvedBlurb { get; set; }

        /// <summary>
        /// If this is a passive power
        /// </summary>
        public bool IsPassive { get; set; }

        /// <summary>
        /// If this is an evolved power
        /// </summary>
        public bool IsEvolution { get; set; }

        /// <summary>
        /// The base class of the power - player version
        /// </summary>
        public ExportEntry BasePower { get; }

        /// <summary>
        /// The usable power export
        /// </summary>
        public ExportEntry PowerExport { get; }

        /// <summary>
        /// The base name of the power that is used for mapping in config
        /// </summary>
        public string BaseName { get; }

        /// <summary>
        /// The name of the power (localized)
        /// </summary>
        public string PowerName { get; }

        public bool HasEvolution()
        {
            return !IsEvolution;
        }

        public IEnumerable<HTalent> GetEvolutions()
        {
            var baseProps = BasePower.GetDefaults().GetProperties();
            var evos = new List<HTalent>();

            var evo1prop = baseProps.GetProp<ObjectProperty>("EvolvedPowerClass1")?.ResolveToEntry(BasePower.FileRef) as ExportEntry;
            if (evo1prop != null)
            {
                evos.Add(new HTalent(evo1prop, true));
            }

            var evo2prop = baseProps.GetProp<ObjectProperty>("EvolvedPowerClass2")?.ResolveToEntry(BasePower.FileRef) as ExportEntry;
            if (evo2prop != null)
            {
                evos.Add(new HTalent(evo2prop, true));
            }
            return evos;
        }

        /// <summary>
        /// The name of the power when this object was constructed
        /// </summary>
        private NameReference OriginalPowerName;

        /// <summary>
        /// Sets the source name of the power export back to the original, used when porting to differentiate duplicate 
        /// </summary>
        public void ResetSourcePowerName()
        {
            PowerExport.ObjectName = OriginalPowerName;
            PowerExport.GetDefaults().ObjectName = $"Defaults__{OriginalPowerName}";
        }

        public void SetUniqueName(string henchInfoHenchUiName)
        {
            PowerExport.ObjectName = new NameReference($"{PowerExport.ObjectName.Name}_{henchInfoHenchUiName.UpperFirst()}_MER");
            PowerExport.GetDefaults().ObjectName = new NameReference($"Defaults__{PowerExport.ObjectName.Name}_{henchInfoHenchUiName.UpperFirst()}_MER");

        }
    }
}
