using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.Misc
{
    public class HenchTalents
    {
        public const string SUBOPTION_HENCHPOWERS_REMOVEGATING = "SUBOPTION_HENCHPOWERS_REMOVEGATING";


        [DebuggerDisplay("HTalent - {PowerExport.ObjectName}, Base {BasePower.ObjectName}, IsPassive: {IsPassive}")]
        class HTalent
        {
            protected bool Equals(HTalent other)
            {
                return Equals(PowerExport.InstancedFullPath, other.PowerExport.InstancedFullPath);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return PowerExport.InstancedFullPath.Equals(((HTalent)obj).PowerExport.InstancedFullPath);
            }

            public override int GetHashCode()
            {
                return (PowerExport != null ? PowerExport.GetHashCode() : 0);
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

                IsAmmoPower = PowerName.Contains("Ammo");
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
                HTalent evo1 = new HTalent(baseProps.GetProp<ObjectProperty>("EvolvedPowerClass1").ResolveToEntry(BasePower.FileRef) as ExportEntry, true);
                HTalent evo2 = new HTalent(baseProps.GetProp<ObjectProperty>("EvolvedPowerClass2").ResolveToEntry(BasePower.FileRef) as ExportEntry, true);
                return new[] { evo1, evo2 };
            }
        }

        class TalentSet
        {
            /// <summary>
            /// If we were able to find a solution of compatible powers for this set
            /// </summary>
            public bool IsBaseValid { get; }
            /// <summary>
            /// Creates a talent set from the list of all powers that are available for use. The list WILL be modified so ensure it is a clone
            /// </summary>
            /// <param name="allPowers"></param>
            public TalentSet(HenchLoadoutInfo hpi, List<HTalent> allPowers)
            {
                int numPassives = 0;

                int numPowersToAssign = hpi.NumPowersToAssign;
                for (int i = 0; i < numPowersToAssign; i++)
                {
                    var talent = allPowers.PullFirstItem();
                    int retry = allPowers.Count;
                    while (retry > 0 && Powers.Any(x => x.BaseName == talent.BaseName))
                    {
                        allPowers.Add(talent);
                        talent = allPowers.PullFirstItem();
                        retry--;
                        if (retry <= 0)
                        {
                            IsBaseValid = false;
                            return;
                        }
                    }

                    if (talent.BasePower.ObjectName.Name.Contains("Passive"))
                    {
                        numPassives++;
                        if (numPassives > 3)
                        {
                            // We must ensure there is not a class full of passives
                            // or the evolutions will not have a solution
                            // only doing half allows us to give the evolution solution finder
                            // a better chance at a quick solution
                            IsBaseValid = false;
                            return;
                        }
                    }
                    Powers.Add(talent);
                }

                // Add in the fixed powers
                if (hpi.FixedPowers.Any())
                {
                    Powers.AddRange(hpi.FixedPowers);
                    Powers.Shuffle();
                }

                IsBaseValid = true;
            }

            public bool SetEvolutions(HenchLoadoutInfo hpi, List<HTalent> availableEvolutions)
            {
                // 1. Calculate the number of required bonus evolutions
                var numToPick = (Powers.Count(x => x.HasEvolution()) - hpi.FixedPowers.Count) * 2;
                while (numToPick > 0)
                {
                    var numAttempts = availableEvolutions.Count;
                    var evolutionToCheck = availableEvolutions.PullFirstItem();
                    while (numAttempts > 0 && EvolvedPowers.Any(x => x.PowerExport.InstancedFullPath == evolutionToCheck.PowerExport.InstancedFullPath)) // Ensure there are no duplicate power exports
                    {
                        // Repick
                        availableEvolutions.Add(evolutionToCheck);
                        evolutionToCheck = availableEvolutions.PullFirstItem();
                        numAttempts--;
                        if (numAttempts == 0)
                        {
                            Debug.WriteLine("Could not find suitable evolution for talentset!");
                            return false; // There is no viable solution
                        }
                    }

                    EvolvedPowers.Add(evolutionToCheck);
                    numToPick--;
                }

                return true;
            }

            /// <summary>
            /// The base power set of the kit
            /// </summary>
            public List<HTalent> Powers { get; } = new();

            /// <summary>
            /// The evolution power pool. Should not contain any items that are the same as an item in the Powers list
            /// </summary>
            public List<HTalent> EvolvedPowers { get; } = new();
        }

        class MappedPower
        {
            public int StartingRank { get; set; }
            public HTalent BaseTalent { get; set; }
            public HTalent EvolvedTalent1 { get; set; }
            public HTalent EvolvedTalent2 { get; set; }
        }

        [DebuggerDisplay("HenchLoadoutInfo for {HenchUIName}")]
        class HenchLoadoutInfo : INotifyPropertyChanged
        {
            internal enum Gender
            {
                Male,
                Female,
                Robot
            }

            /// <summary>
            /// The list of talents that will be installed to this henchman
            /// </summary>
            public TalentSet HenchTalentSet { get; private set; }
            /// <summary>
            /// Powers that cannot be removed from this squadmate
            /// </summary>
            public List<HTalent> FixedPowers { get; } = new List<HTalent>(3);

            /// <summary>
            /// Number of powers to try to assign when building a base talent set
            /// </summary>
            public int NumPowersToAssign { get; set; } = 4;
            /// <summary>
            /// Instanced full path for this loadout object
            /// </summary>
            public string LoadoutIFP { get; set; }

            /// <summary>
            /// Called when LoadoutIFP changes.
            /// </summary>
            public void OnLoadoutIFPChanged()
            {
                var lastIndex = LoadoutIFP.LastIndexOf("_");
                //HenchUIName = LoadoutIFP.Substring(lastIndex + 1).UpperFirst();
                var henchName = LoadoutIFP.Substring(lastIndex + 1);
                HenchUIName = char.ToUpper(henchName[0]) + henchName.Substring(1);

                switch (HenchUIName)
                {
                    case "Miranda":
                    case "Samara":
                    case "Morinth":
                    case "Kasumi":
                    case "Liara":
                    case "Kenson":
                    case "Jack":
                    case "Tali":
                        HenchGender = Gender.Female;
                        break;
                    case "Legion":
                        HenchGender = Gender.Robot;
                        break;
                }
            }

            /// <summary>
            /// The UI name of the henchman that this loadout is for
            /// </summary>
            public string HenchUIName { get; private set; }

            public Gender HenchGender { get; private set; } = Gender.Male;

            private static string[] squadmateNames = new[]
            {
                "Kasumi",
                "Grunt",
                "Thane",
                "Jack",
                "Miranda",
                "Legion",
                "Zaeed",
                "Tali",
                "Samara",
                "Morinth",
                "Mordin",
                "Jacob",
                "Garrus",
                "Liara",
                "Kenson",
                "Wilson",
            };

            private static string[] maleKeywords = new[] { "him", "his" };
            private static string[] femaleKeywords = new[] { "her" };
            private static string[] robotKeywords = new[] { "its" };
            private static string[] allKeywords = new[] { " him ", " his ", " her ", " its " };


            /// <summary>
            /// Converts the input string to the gender of this loadout, including the name.
            /// </summary>
            /// <param name="str"></param>
            /// <returns></returns>
            public string GenderizeString(string str)
            {
                // SPECIAL CASES
                // it/its him/his dont' line up with female's only having 'her'

                var targetGenderWord = HenchGender == Gender.Male ? " his" : HenchGender == Gender.Female ? " her" : " its";

                // GENERAL GENDER CASES - WILL RESULT IN SOME WEIRD HIM/HIS ISSUES

                var keywords = HenchGender == Gender.Male ? maleKeywords : HenchGender == Gender.Female ? femaleKeywords : robotKeywords;
                var sourceGenderWords = allKeywords.Except(keywords).ToList();
                for (int i = 0; i < sourceGenderWords.Count; i++)
                {
                    str = str.Replace(sourceGenderWords[i], targetGenderWord);
                }

                // Change squadmate name
                var otherSquadmateNames = squadmateNames.Where(x => x != HenchUIName).ToList();
                foreach (var osn in otherSquadmateNames)
                {
                    str = str.Replace($"{osn}' ", $"{osn}'s "); // Converts "Garrus' " to "Garrus's ", which we will properly adapt below
                    str = str.Replace(osn, HenchUIName);
                }

                str = str.Replace("Garrus's", "Garrus'"); // Fix plural possessive for s

                // Correct weird him/his
                // KROGAN BERSERKER ON MALE NEEDS TO STAY GIVES HIM KROGAN HEALTH REGEN, RANKS
                if (HenchGender != Gender.Female)
                {
                    // MORINTH FIX
                    var targetStr = $"{(HenchGender == Gender.Male ? "him" : "it")} unnatural";
                    str = str.Replace($"{(HenchGender == Gender.Male ? "his" : "its")} unnatural", targetStr);

                    // SUBJECT ZERO 'will to live make her harder to kill'
                    targetStr = $"{(HenchGender == Gender.Male ? "him" : "it")} even harder to kill";
                    str = str.Replace($"{(HenchGender == Gender.Male ? "his" : "its")} even harder to kill", targetStr);
                }
                else if (HenchGender != Gender.Male)
                {
                    // DRELL ASSASSIN FIX (uses 'his')
                    var targetStr = $"wounds increases {(HenchGender == Gender.Female ? "her" : "its")} effective health.";
                    str = str.Replace("wounds increases his effective health.", targetStr);
                }


                Debug.WriteLine(str);
                return str;
            }

            /// <summary>
            /// Builds a valid base talentset for this henchman from the list of available base talents. If none can be built this method returns false
            /// </summary>
            /// <param name="baseTalents"></param>
            /// <returns></returns>
            public bool BuildTalentSet(List<HTalent> baseTalentPool)
            {
                HenchTalentSet = new TalentSet(this, baseTalentPool);
                return HenchTalentSet.IsBaseValid;
            }

            /// <summary>
            /// Builds a valid evolved talentset for this henchmen from the list of available evolved talents. If none can be built, this method returns false
            /// </summary>
            /// <param name="evolvedTalentPool"></param>
            /// <returns></returns>
            public bool BuildEvolvedTalentSet(List<HTalent> evolvedTalentPool)
            {
                return HenchTalentSet.SetEvolutions(this, evolvedTalentPool);
            }

            /// <summary>
            /// Puts base powers in order, with HUD powers first and non-HUD powers at the bottom. This ensure the unlock requirements are done properly
            /// </summary>
            public void OrderBasePowers()
            {
                var powers = HenchTalentSet.Powers.ToList();
                HenchTalentSet.Powers.Clear();

                HenchTalentSet.Powers.AddRange(powers.Where(x => x.ShowInCR));
                HenchTalentSet.Powers.AddRange(powers.Where(x => !x.ShowInCR));
            }

            /// <summary>
            /// Completely resets the talentset object
            /// </summary>
            public void ResetTalents()
            {
                HenchTalentSet = null;
            }

            /// <summary>
            /// Clears the existing talentset's evolutions
            /// </summary>
            public void ResetEvolutions()
            {
                HenchTalentSet?.EvolvedPowers.Clear();
            }
#pragma warning disable
            public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore
        }

        class HenchInfo
        {
            /// <summary>
            /// The internal name for this henchman
            /// </summary>
            public string HenchInternalName { get; set; }
            /// <summary>
            /// The list of packages that contain this henchmen
            /// </summary>
            public List<IMEPackage> Packages { get; } = new List<IMEPackage>();

            /// <summary>
            /// Loadout InstancedFullPaths. Samara has multiple loadouts
            /// </summary>
            public List<HenchLoadoutInfo> PackageLoadouts { get; } = new List<HenchLoadoutInfo>();
            /// <summary>
            /// Hint for loadout inventory to only match on object named this. If null it'll pull in all loadouts in the package file that start with hench_.
            /// </summary>
            public string LoadoutHint { get; set; }

            public HenchInfo(string internalName = null)
            {
                HenchInternalName = internalName;
            }

            public void ResetTalents()
            {
                foreach (var l in PackageLoadouts)
                {
                    l.ResetTalents();
                }
            }

            public void ResetEvolutions()
            {
                foreach (var l in PackageLoadouts)
                {
                    l.ResetEvolutions();
                }
            }
        }

        private static string[] NonBioHHenchmenFiles = new[]
        {
            "BioD_ArvLvl1.pcc" // Kenson
        };

        public static bool ShuffleSquadmateAbilities(GameTarget target, RandomizationOption option)
        {
            bool removeRank2Gating = option.HasSubOptionSelected(SUBOPTION_HENCHPOWERS_REMOVEGATING);

            PatchOutTutorials(target);

            var henchCache = new MERPackageCache();

            // We can have up to 5 UI powers assigned. However, kits will only have 4 powers total assigned as each pawn only has 4 in vanilla gameplay.
            var squadmatePackageMap = new CaseInsensitiveConcurrentDictionary<HenchInfo>();

            #region Build squadmate sets
            option.CurrentOperation = "Inventorying henchmen powers";
            var henchFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioH_") && !x.Contains("_LOC_") && !x.Contains("END") && x != "BioH_SelectGUI" && (x.Contains("00")) || x.Contains("BioH_Wilson") || NonBioHHenchmenFiles.Contains(x)).ToList();


            int numDone = 0;
            option.ProgressMax = henchFiles.Count;

#if DEBUG
            ThreadSafeRandom.SetSeed(132512);
#endif
            Parallel.ForEach(henchFiles, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, h =>
                {
                    //foreach (var h in henchFiles)
                    //{

                    // Debug only
#if DEBUG
                    if (true
                        && !h.Contains("mystic", StringComparison.InvariantCultureIgnoreCase)
                        && !h.Contains("assassin", StringComparison.InvariantCultureIgnoreCase)
                        && !h.Contains("liara", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return;
                    }
#endif

                    HenchInfo hpi = new HenchInfo();

                    string internalName;
                    if (h == "BioH_Wilson.pcc")
                    {
                        hpi.HenchInternalName = "wilson";
                    }
                    else if (h == "BioD_ArvLvl1.pcc")
                    {
                        hpi.LoadoutHint = "hench_Kenson";
                        hpi.HenchInternalName = "kenson";
                    }
                    else if (h.StartsWith("BioH_"))
                    {
                        internalName = h.Substring(5); // Remove BioH_
                        internalName = internalName.Substring(0, internalName.IndexOf("_", StringComparison.InvariantCultureIgnoreCase)); // "Assassin"
                        hpi.HenchInternalName = internalName;

                        // These pawns appear embedded in existing files
                        if (h.Contains("leading", StringComparison.InvariantCultureIgnoreCase))
                        {
                            hpi.LoadoutHint = "hench_Jacob";
                        }
                        else if (h.Contains("leading", StringComparison.InvariantCultureIgnoreCase))
                        {
                            hpi.LoadoutHint = "hench_Miranda";
                        }
                    }
                    else
                    {
                        // Custom other
                    }

                    LoadSquadmatePackages(hpi, h, squadmatePackageMap, henchCache);

                    Interlocked.Increment(ref numDone);
                    option.ProgressValue = numDone;
                    option.ProgressIndeterminate = false;

                    //}
                });
            #endregion

            #region Build power pool
            List<HTalent> talentPoolMaster = new List<HTalent>();
            List<HTalent> evolvedTalentPoolMaster = new List<HTalent>();
            numDone = 0;
            option.ProgressValue = 0;
            option.CurrentOperation = "Building henchmen power sets";
            ConcurrentBag<string> passiveStrs = new ConcurrentBag<string>();
            Parallel.ForEach(squadmatePackageMap, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, henchInfo =>
                {
                    var henchP = henchInfo.Value.Packages[0];
                    var loadouts = henchP.Exports.Where(x => x.ClassName == "SFXPlayerSquadLoadoutData").ToList();
                    foreach (var loadout in loadouts)
                    {
                        if (henchInfo.Value.LoadoutHint != null)
                        {
                            // HenchInfo has hint to indicate only select this named loadout
                            if (loadout.ObjectName != henchInfo.Value.LoadoutHint)
                                return; // Don't operate on this one
                        }


                        HenchLoadoutInfo hli = new HenchLoadoutInfo() { LoadoutIFP = loadout.InstancedFullPath };
                        henchInfo.Value.PackageLoadouts.Add(hli);
                        if (henchInfo.Key == "wilson")
                        {
                            // Wilson only has 1 power.
                            hli.NumPowersToAssign = 1;
                        }

                        // Inventory the powers
                        var powers = loadout.GetProperty<ArrayProperty<ObjectProperty>>("Powers").Select(x => x.ResolveToEntry(loadout.FileRef) as ExportEntry).ToList();
                        int numContributed = 0;
                        foreach (var p in powers)
                        {
                            if (CanBeShuffled(p, out var usi))
                            {
                                if (hli.LoadoutIFP == "BioChar_Loadouts.Henchmen.hench_Morinth")
                                {
                                    p.CondenseArchetypes(); // makes porting work better by removing Samara's parent power which can be used
                                }

                                var htalent = new HTalent(p);
                                if (htalent.IsPassive)
                                {
                                    passiveStrs.Add(htalent.PassiveDescriptionString);
                                    passiveStrs.Add(htalent.PassiveTalentDescriptionString);
                                    passiveStrs.Add(htalent.PassiveRankDescriptionString);
                                }

                                talentPoolMaster.Add(htalent);

                                var evolutions = htalent.GetEvolutions();
                                evolvedTalentPoolMaster.AddRange(evolutions);
                                numContributed++;
                            }
                            else if (usi.Pawn != null && usi.Pawn.Equals(henchInfo.Key, StringComparison.InvariantCultureIgnoreCase))
                            {
                                hli.FixedPowers.Add(new HTalent(p, isFixedPower: true));

                                if (usi.CountsTowardsTalentCount)
                                {
                                    hli.NumPowersToAssign--;
                                }
                            }
                        }

                        Debug.WriteLine($"{hli.LoadoutIFP} contributed {numContributed} powers");
                        Interlocked.Increment(ref numDone);
                        option.ProgressValue = numDone;
                        //}
                    }
                });
            #endregion

            var allPS = passiveStrs.Distinct().ToList();
            foreach (var s in allPS)
            {
                Debug.WriteLine(s);
            }

            // Step 3. Precalculate talent sets that will be assigned
            #region Find compatible power sets
            int baseAttempt = 0;
            bool powersConfigured = false; //if a solution was found
            while (!powersConfigured)
            {
                // Reset
                Debug.WriteLine($"Assigning hench base talent sets, attempt #{baseAttempt++}");
                var basePowerPool = talentPoolMaster.ToList();
                basePowerPool.Shuffle();

                foreach (var hpi in squadmatePackageMap.Values)
                {
                    hpi.ResetTalents();
                }

                // Attempt to build a list of compatible base powers
                bool powerSetsValid = true;
                foreach (var hpi in squadmatePackageMap.Values)
                {
                    foreach (var loadout in hpi.PackageLoadouts)
                    {
                        if (!loadout.BuildTalentSet(basePowerPool) || loadout.HenchTalentSet == null || !loadout.HenchTalentSet.IsBaseValid)
                        {
                            powerSetsValid = false;
                            break; // Try again, this is not a valid solution to the problem
                        }
                    }

                    if (!powerSetsValid)
                    {
                        break;
                    }
                    // Valid set found
                    //Debug.WriteLine($@"Assigned powers for {hpi.HenchInternalName}. Powers remaining: {basePowerPool.Count}");
                }

                if (!powerSetsValid)
                {
                    continue; // try again
                }


                // We have configured a set of compatible base powers
                // There should be a configuration of evolutions that works,
                // but we will only attempt 5 ways
                bool configuredEvolutions = false;
                int evolutionAttempt = 0;
                while (!configuredEvolutions)
                {
                    // Reset
                    Debug.WriteLine($"Assigning evolution talent sets, attempt #{baseAttempt - 1}-{evolutionAttempt++}");
                    var assignmentEvolutions = evolvedTalentPoolMaster.ToList();
                    foreach (var hpi in squadmatePackageMap)
                    {
                        hpi.Value.ResetEvolutions();
                    }

                    // Attempt solution
                    assignmentEvolutions.Shuffle(); // reshuffle for new attempt
                    bool foundSolution = true;
                    foreach (var hpi in squadmatePackageMap.Values)
                    {
                        foreach (var loadout in hpi.PackageLoadouts)
                        {
                            foundSolution &= loadout.BuildEvolvedTalentSet(assignmentEvolutions);
                            if (!foundSolution)
                                break; //short circuit search for solution
                        }
                        if (!foundSolution)
                            break; //short circuit search for solution
                    }

                    if (!foundSolution)
                    {
                        if (evolutionAttempt > 4)
                            break; // Force full retry
                        continue; // Try again, a solution was not found
                    }

                    configuredEvolutions = true;
                }

                if (!configuredEvolutions)
                    continue; // Do a full retry
                powersConfigured = true;

                // Print results
                MERLog.Information($@"Found a power solution for basegame powers on henchmen in {baseAttempt} attempts:");
                foreach (var hpi in squadmatePackageMap.Values)
                {
                    foreach (var loadout in hpi.PackageLoadouts)
                    {
                        MERLog.Information($"{loadout.LoadoutIFP}-----");
                        foreach (var pow in loadout.HenchTalentSet.Powers)
                        {
                            MERLog.Information($" - BP {pow.PowerName} ({pow.BaseName})");
                        }

                        foreach (var pow in loadout.HenchTalentSet.EvolvedPowers)
                        {
                            MERLog.Information($" - EP {pow.PowerName} ({pow.BaseName})");
                        }
                    }
                }
            }
            #endregion

            // Step 4. Assign and map powers
            foreach (var pow in squadmatePackageMap.Values)
            {
                foreach (var loadout in pow.PackageLoadouts)
                {
                    loadout.OrderBasePowers();
                }
            }

            #region Install powers and evolutions
            option.CurrentOperation = "Installing randomized henchmen powers";
            option.ProgressValue = 0;
            option.ProgressMax = squadmatePackageMap.Values.Sum(x => x.Packages.Count);
            numDone = 0;
            foreach (var hpi in squadmatePackageMap.Values)
            {
                object tlkSync = new object();
                MERLog.Information($"Installing talent set for {hpi.HenchInternalName}");
                option.CurrentOperation = $"Installing randomized powers for {hpi.PackageLoadouts[0].HenchUIName}";
                // We force a large GC here cause this loop can make like 7GB, I'll take the timing hit 
                // to reduce memory usage
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();

                Parallel.ForEach(hpi.Packages, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, package =>
                  {
                      //foreach (var loadout in assets)
                      //{
                      // We must load a new copy of the package file into memory, one for reading, one for modifying,
                      // otherwise it will be concurrent modification
                      //if (!package.FilePath.Contains("Thief"))
                      //      return;
                      if (package == null)
                          Debugger.Break();
                      foreach (var loadoutInfo in hpi.PackageLoadouts)
                      {
                          var loadout = package.FindExport(loadoutInfo.LoadoutIFP);

                          if (loadout != null)
                          {
                              List<MappedPower> configuredPowers = new List<MappedPower>();

                              var loadoutProps = loadout.GetProperties();
                              var powersList = loadoutProps.GetProp<ArrayProperty<ObjectProperty>>("Powers");

                              var originalCount = powersList.Count;
                              var powersToKeep = powersList.Where(x => !CanBeShuffled(x.ResolveToEntry(package) as ExportEntry, out _)).ToList();
                              powersList.Clear();

                              var talentSet = loadoutInfo.HenchTalentSet;

                              // Assign base powers
                              int powIndex = 0;
                              // Lists for updating the class description in the character creator
                              foreach (var talentSetBasePower in talentSet.Powers)
                              {
                                  if (loadoutInfo.FixedPowers.Contains(talentSetBasePower))
                                      continue; // Do not modify this
                                  var portedPower = PackageTools.PortExportIntoPackage(loadout.FileRef, talentSetBasePower.PowerExport);
                                  powersList.Add(new ObjectProperty(portedPower.UIndex));

                                  // For each power, change the evolutions
                                  var defaults = portedPower.GetDefaults();
                                  var props = defaults.GetProperties();
                                  var evolution1 = talentSet.EvolvedPowers[powIndex * 2];
                                  var evolution2 = talentSet.EvolvedPowers[(powIndex * 2) + 1];
                                  configuredPowers.Add(new MappedPower() { BaseTalent = talentSetBasePower, EvolvedTalent1 = evolution1, EvolvedTalent2 = evolution2 });
                                  var evo1 = PackageTools.PortExportIntoPackage(portedPower.FileRef, evolution1.PowerExport);
                                  var evo2 = PackageTools.PortExportIntoPackage(portedPower.FileRef, evolution2.PowerExport);
                                  evo1.WriteProperty(new ArrayProperty<StructProperty>("UnlockRequirements"));
                                  evo2.WriteProperty(new ArrayProperty<StructProperty>("UnlockRequirements"));
                                  props.AddOrReplaceProp(new ObjectProperty(evo1, "EvolvedPowerClass1"));
                                  props.AddOrReplaceProp(new ObjectProperty(evo2, "EvolvedPowerClass2"));

                                  // Update the evolution text via override
                                  var ranksSource = talentSetBasePower.CondensedProperties.GetProp<ArrayProperty<StructProperty>>("Ranks");
                                  if (ranksSource == null)
                                  {
                                      Debugger.Break();
                                      // Projectile powers are subclassed for player
                                      //ranksSource = (basePower.BasePower.SuperClass as ExportEntry).GetDefaults().GetProperty<ArrayProperty<StructProperty>>("Ranks");
                                  }

                                  #region Ranks text change
                                  lock (tlkSync)
                                  {
                                      // Update passive strings for ranks 1/2/3
                                      if (talentSetBasePower.IsPassive)
                                      {
                                          int i = 0;
                                          var newStringID = TLKBuilder.GetNewTLKID();
                                          string rankDescription = null;
                                          while (i <= 2)
                                          {
                                              var rankDescriptionProp = ranksSource[i].Properties.GetProp<StringRefProperty>("Description");
                                              if (rankDescription == null)
                                              {
                                                  rankDescription = loadoutInfo.GenderizeString(talentSetBasePower.PassiveRankDescriptionString);
                                                  TLKBuilder.ReplaceString(newStringID, rankDescription);
                                              }
                                              rankDescriptionProp.Value = newStringID; // written below by addreplaceRanksSource
                                              i++;
                                          }
                                      }

                                      // Update Rank 4 unevolved text
                                      {
                                          var evolveRank = ranksSource[3];
                                          var descriptionProp = evolveRank.Properties.GetProp<StringRefProperty>("Description");
                                          if (!TLKBuilder.IsAssignedMERString(descriptionProp.Value))
                                          {
                                              var description = TLKBuilder.TLKLookupByLang(descriptionProp.Value, MELocalization.INT);
                                              var descriptionLines = description.Split('\n');
                                              descriptionLines[2] = $"1) {loadoutInfo.GenderizeString(evolution1.EvolvedBlurb)}";
                                              descriptionLines[4] = $"2) {loadoutInfo.GenderizeString(evolution2.EvolvedBlurb)}";
                                              var newStringID = TLKBuilder.GetNewTLKID();
                                              TLKBuilder.ReplaceString(newStringID, string.Join('\n', descriptionLines));
                                              descriptionProp.Value = newStringID; // written below by addreplaceRanksSource
                                          }
                                      }
                                  }
                                  props.AddOrReplaceProp(ranksSource); // copy the source rank info into our power with the modification
                                  #endregion

                                  #region Passives text changes (non-ranks)
                                  if (talentSetBasePower.IsPassive)
                                  {
                                      lock (tlkSync)
                                      {
                                          // Talent Description
                                          var talentDescriptionProp = talentSetBasePower.CondensedProperties.GetProp<StringRefProperty>("TalentDescription");
                                          if (!TLKBuilder.IsAssignedMERString(talentDescriptionProp.Value))
                                          {
                                              var newStringID = TLKBuilder.GetNewTLKID();
                                              TLKBuilder.ReplaceString(newStringID, loadoutInfo.GenderizeString(talentSetBasePower.PassiveTalentDescriptionString));
                                              talentDescriptionProp.Value = newStringID;
                                          }
                                          props.AddOrReplaceProp(talentDescriptionProp);

                                          var descriptionProp = talentSetBasePower.CondensedProperties.GetProp<StringRefProperty>("Description");
                                          if (!TLKBuilder.IsAssignedMERString(descriptionProp.Value))
                                          {
                                              var newStringID = TLKBuilder.GetNewTLKID();
                                              TLKBuilder.ReplaceString(newStringID, loadoutInfo.GenderizeString(talentSetBasePower.PassiveDescriptionString));
                                              descriptionProp.Value = newStringID;
                                          }
                                          props.AddOrReplaceProp(descriptionProp);
                                      }
                                  }
                                  #endregion

                                  defaults.WriteProperties(props);
                                  powIndex++;
                              }

                              // Add passives back
                              List<ObjectProperty> appendAtEndItems = new List<ObjectProperty>();
                              foreach (var ptk in powersToKeep)
                              {
                                  var export = ptk.ResolveToEntry(package) as ExportEntry;
                                  var talent = new HTalent(export, false, isFixedPower: true);
                                  if (talent.ShowInCR)
                                  {
                                      powersList.Add(ptk);
                                  }
                                  else
                                  {
                                      appendAtEndItems.Add(ptk);
                                  }
                              }

                              powersList.AddRange(appendAtEndItems);

                              // Order the powers
                              // Write loadout data
                              if (powersList.Count != originalCount)
                              {
                                  Debugger.Break();
                              }

                              loadoutProps.AddOrReplaceProp(powersList);

                              // Build the autoranks
                              foreach (var fixedPower in loadoutInfo.FixedPowers.Where(x => x.ShowInCR))
                              {
                                  // Add fixed powers evo information
                                  var mp = new MappedPower() { BaseTalent = fixedPower };
                                  var evo1c = fixedPower.CondensedProperties.GetProp<ObjectProperty>("EvolvedPowerClass1").ResolveToEntry(fixedPower.PowerExport.FileRef) as ExportEntry;
                                  var evo2c = fixedPower.CondensedProperties.GetProp<ObjectProperty>("EvolvedPowerClass2").ResolveToEntry(fixedPower.PowerExport.FileRef) as ExportEntry;
                                  mp.EvolvedTalent1 = new HTalent(evo1c, true);
                                  mp.EvolvedTalent2 = new HTalent(evo2c, true);
                                  configuredPowers.Add(mp);
                              }

                              loadoutProps.AddOrReplaceProp(BuildAutoRankList(loadout, configuredPowers));

                              // Finalize loadout export
                              loadout.WriteProperties(loadoutProps);

                              // Correct the unlock criteria to ensure every power can be unlocked. Special stuff for Kenson and Liara. Don't care about Wilson, sorry dude.
                              for (int i = 0; i < talentSet.Powers.Count; i++)
                              {
                                  // i = Power Index (0 indexed)

                                  // Power dependency map:
                                  // Power 1: Rank 1
                                  // Power 2: Depends on Power 1 Rank 1 UNLESS Miranda or Jacob who have this power at rank 1 already
                                  // Power 3: Rank 0
                                  // Power 4: Locked by Loyalty

                                  var powerExport = loadout.FileRef.FindExport(talentSet.Powers[i].PowerExport.InstancedFullPath);
                                  var defaults = powerExport.GetDefaults();

                                  var properties = defaults.GetProperties();
                                  properties.RemoveNamedProperty("Rank");
                                  if (properties.RemoveNamedProperty("UnlockRequirements"))
                                  {
                                      Debug.WriteLine("T");
                                  }

                                  // All squadmates have a piont in Slot 0 by default.
                                  // Miranda and Jacob have a point in slot 1
                                  if (hpi.HenchInternalName == "kenson")
                                  {
                                      // Since you can't power up kenson we'll just give her points in all her powers
                                      properties.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.Next(2) + 1, "Rank"));
                                  }
                                  // VANILLA HENCH CODE
                                  else if (i == 0 || (i == 1 && hpi.HenchInternalName == "vixen" || hpi.HenchInternalName == "leading"))
                                  {
                                      properties.Add(new FloatProperty(1, "Rank"));
                                  }
                                  else if (i == 1)
                                  {
                                      if (removeRank2Gating)
                                      {
                                          // Rank 0
                                          properties.AddOrReplaceProp(new FloatProperty(0, "Rank"));
                                      }
                                      else
                                      {
                                          // Has unlock dependency on the prior slotted item
                                          var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                                          dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport(talentSet.Powers[i - 1].PowerExport.InstancedFullPath), false));
                                          properties.AddOrReplaceProp(dependencies);
                                      }
                                  }
                                  else if (i == 3 && hpi.HenchInternalName != "liara")
                                  {
                                      // Has unlock dependency on loyalty power
                                      // Has unlock dependency on the prior slotted item
                                      var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                                      dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport("SFXGameContent_Powers.SFXPower_LoyaltyRequirement"), true));
                                      properties.AddOrReplaceProp(dependencies);
                                  }

                                  defaults.WriteProperties(properties);
                              }
                          }
                      }

                      Interlocked.Increment(ref numDone);
                      option.ProgressValue = numDone;
                  });
            }

            #endregion

            #region Finalize packages
            numDone = 0;
            option.CurrentOperation = "Saving squadmate packages";
            option.ProgressValue = 0;
            GC.Collect();
            foreach (var hpi in squadmatePackageMap.Values)
            {
                foreach (var package in hpi.Packages)
                {
                    foreach (var loadoutInfo in hpi.PackageLoadouts)
                    {
                        var loadout = package.FindExport(loadoutInfo.LoadoutIFP);
                        if (loadout != null)
                        {
                            var powersList = loadout.GetProperty<ArrayProperty<ObjectProperty>>("Powers");
                            foreach (var powO in powersList)
                            {
                                // Memory-unique powers so squadmates with same powers don't conflict. This has to be done after all the other changes are done or it will not be able to use FindExport()
                                var entry = powO.ResolveToEntry(package);
                                if (CanBeShuffled(entry as ExportEntry, out _))
                                {
                                    entry = package.FindExport(entry.InstancedFullPath);
                                    entry.ObjectName = entry.ObjectName.Name + $"_MER{hpi.HenchInternalName}";
                                    var defaults = (entry as ExportEntry).GetDefaults();
                                    defaults.ObjectName = defaults.ObjectName.Name + $"_MER{hpi.HenchInternalName}";
                                }
                            }
                        }
                    }

                    MERFileSystem.SavePackage(package);
                    Interlocked.Increment(ref numDone);
                    option.ProgressValue = numDone;
                }
            }
            #endregion

            GC.Collect(); // Dump lots of memory out
            return true;
        }

        private static void PatchOutTutorials(GameTarget target)
        {
            // Patch out Warp tutorial
            var catwalkF = MERFileSystem.GetPackageFile(target,"BioD_ProCer_200Catwalk.pcc");
            if (catwalkF != null)
            {
                var catwalkP = MEPackageHandler.OpenMEPackage(catwalkF);

                // They're falling back -> Stop respawns
                KismetHelper.CreateOutputLink(catwalkP.GetUExport(386), "Done", catwalkP.GetUExport(376), 2);

                // Remove outputs from Delay 1s
                KismetHelper.RemoveOutputLinks(catwalkP.GetUExport(2231));

                // Set Delay 1s to 'They're falling back'
                KismetHelper.CreateOutputLink(catwalkP.GetUExport(2231), "Finished", catwalkP.GetUExport(2247), 0);

                catwalkP.GetUExport(2231).WriteProperty(new FloatProperty(5, "Duration"));

                MERFileSystem.SavePackage(catwalkP);
            }

            // Patch out Warp tutorial
            var controlRoomF = MERFileSystem.GetPackageFile(target, "BioD_ProCer_250ControlRoom.pcc");
            if (controlRoomF != null)
            {
                var controlRoomP = MEPackageHandler.OpenMEPackage(controlRoomF);

                // Fastest way to the shuttle ->ForceCrateExplode
                KismetHelper.CreateOutputLink(controlRoomP.GetUExport(765), "Done", controlRoomP.GetUExport(2932), 0);

                // The fastest way to the shuttle -> Destory crate object
                KismetHelper.CreateOutputLink(controlRoomP.GetUExport(765), "Done", controlRoomP.GetUExport(2963), 0);

                var outboundToHint = SeqTools.GetOutboundLinksOfNode(controlRoomP.GetUExport(766));
                if (outboundToHint.Count == 4)
                {
                    // Total hack, but it works, maybe
                    if (outboundToHint[0].Count == 3)
                    {
                        outboundToHint[0].RemoveAt(0);
                    }
                    if (outboundToHint[1].Count == 2)
                    {
                        outboundToHint[1].RemoveAt(0);
                    }

                    SeqTools.WriteOutboundLinksToNode(controlRoomP.GetUExport(766), outboundToHint);
                }

                controlRoomP.GetUExport(3323).WriteProperty(new FloatProperty(3, "FloatValue"));

                // Remove outputs from Delay 1s
                //KismetHelper.RemoveOutputLinks(controlRoomP.GetUExport(2231));

                //// Set Delay 1s to 'They're falling back'
                //KismetHelper.CreateOutputLink(controlRoomP.GetUExport(2231), "Finished", catwalkP.GetUExport(2247), 0);

                //controlRoomP.GetUExport(2231).WriteProperty(new FloatProperty(5, "Duration"));

                MERFileSystem.SavePackage(controlRoomP);
            }
        }

        private static void LoadSquadmatePackages(HenchInfo hpi, string vanillaPackagePath, CaseInsensitiveConcurrentDictionary<HenchInfo> squadmatePackageMap, MERPackageCache henchCache)
        {

            var sqm = henchCache.GetCachedPackage(vanillaPackagePath);
            hpi.Packages.Add(sqm);
            var casualHubsVer = henchCache.GetCachedPackage($"BioH_{hpi.HenchInternalName}_00_NC.pcc", true);
            if (casualHubsVer != null)
            {
                // Liara doesn't have endgm file
                hpi.Packages.Add(casualHubsVer);
            }

            var endGmFile = henchCache.GetCachedPackage($"BioH_END_{hpi.HenchInternalName}_00.pcc", true);
            if (endGmFile != null)
            {
                // Liara doesn't have endgm file
                hpi.Packages.Add(endGmFile);
            }

            squadmatePackageMap[hpi.HenchInternalName] = hpi;

            // Build out the rest of the list for this pawn
            int sqmIndex = 0;
            while (true)
            {
                sqmIndex++;
                var fName = $"BioH_{hpi.HenchInternalName}_0{sqmIndex}.pcc";
                var newPackageF = henchCache.GetCachedPackage(fName, true);
                if (newPackageF != null)
                {
                    hpi.Packages.Add(newPackageF);
                    hpi.Packages.Add(henchCache.GetCachedPackage($"BioH_END_{hpi.HenchInternalName}_0{sqmIndex}.pcc", true));
                    var casHubV = henchCache.GetCachedPackage($"BioH_{hpi.HenchInternalName}_0{sqmIndex}_NC.pcc", true);
                    if (casHubV != null)
                        hpi.Packages.Add(casHubV);
                }
                else
                {
                    // No more squadmates in this set
                    break;
                }
            }

            // Add specific files for specific squadmates
            switch (hpi.HenchInternalName.ToLower())
            {
                case "leading": // Jacob
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioP_ProCer.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_350BriefRoom.pcc", true));
                    break;
                case "vixen": // Miranda
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioP_ProCer.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_300ShuttleBay.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_350BriefRoom.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioA_ZyaVtl_100.pcc", true));
                    break;
                case "kenson":
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ArvLvl1_710Shuttle.pcc", true));
                    break;
            }
        }

        class UnshuffleablePower
        {
            public string PowerName { get; init; }
            public string Pawn { get; init; }
            public bool CountsTowardsTalentCount { get; init; } = true;
        }

        private static UnshuffleablePower[] UnshuffleablePowers = new[]
        {
            new UnshuffleablePower() {PowerName = "SFXPower_LoyaltyRequirement"},
            new UnshuffleablePower() {PowerName = "SFXPower_StasisNew_Liara", Pawn = "liara"},

            new UnshuffleablePower() {PowerName = "SFXPower_KasumiCloakTeleport", Pawn = "thief"},
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiUnique", Pawn = "thief"},
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiAssassinate", Pawn = "thief", CountsTowardsTalentCount = false},
            new UnshuffleablePower() {PowerName = "SFXPower_ZaeedUnique", Pawn = "veteran"},
        };

        /// <summary>
        /// If the power can be shuffled/reassigned
        /// </summary>
        /// <param name="exportEntry"></param>
        /// <returns></returns>
        private static bool CanBeShuffled(ExportEntry exportEntry, out UnshuffleablePower unshuffablePower)
        {
            if (exportEntry == null)
            {
                unshuffablePower = new UnshuffleablePower() { PowerName = "UNKNOWN POWER!!!" };
                return false;
            }

            unshuffablePower = UnshuffleablePowers.FirstOrDefault(x => x.PowerName.Equals(exportEntry.ObjectName.Name, StringComparison.InvariantCultureIgnoreCase));
            return unshuffablePower == null;
        }

        /// <summary>
        /// Generates the structs for
        /// <param name="powerClass">The kit-power to depend on.</param>
        /// <returns>< UnlockRequirements that are used to setup a dependency on this power
        /// </summary>/returns>
        private static List<StructProperty> GetUnlockRequirementsForPower(ExportEntry powerClass, bool isLoyaltyPower, bool blankUnlock = false)
        {
            void PopulateProps(PropertyCollection props, ExportEntry export, float rank)
            {
                props.Add(new ObjectProperty(export.UIndex, "PowerClass")); // The power that will be checked
                props.Add(new FloatProperty(rank, "Rank")); // Required rank. 1 means at least one point in it and is used for evolved powers which area always at 4.
                props.Add(new StringRefProperty(isLoyaltyPower ? 339163 : 0, "CustomUnlockText")); // "Locked: squad member is not loyal"
            }

            List<StructProperty> powerRequirements = new();
            if (blankUnlock)
                return powerRequirements;

            // Unevolved power
            PropertyCollection props = new PropertyCollection();
            PopulateProps(props, powerClass, isLoyaltyPower ? 1 : 2); // Loyalty is rank 1. Others are rank 2
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            // EVOLVED POWER
            if (!isLoyaltyPower)
            {
                // Find base power
                var baseClass = powerClass;
                ExportEntry baseDefaults = baseClass.GetDefaults();
                var evolvedPower1 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass1");
                while (evolvedPower1 == null || evolvedPower1.Value == 0)
                {
                    baseClass = (ExportEntry)baseClass.SuperClass;
                    baseDefaults = baseClass.GetDefaults();
                    evolvedPower1 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass1");
                }


                // Loyalty power has no unlock requirement beyond SFXPower_Loyalty

                // Evolved power 1
                props = new PropertyCollection();
                PopulateProps(props, evolvedPower1.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
                powerRequirements.Add(new StructProperty("UnlockRequirement", props));

                // Evolved power 2
                var evolvedPower2 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass2");
                props = new PropertyCollection();
                PopulateProps(props, evolvedPower2.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
                powerRequirements.Add(new StructProperty("UnlockRequirement", props));
            }

            return powerRequirements;
        }

        private static ArrayProperty<StructProperty> BuildAutoRankList(ExportEntry loadout, List<MappedPower> mappedPowers)
        {
            ArrayProperty<StructProperty> alui = new ArrayProperty<StructProperty>("PowerLevelUpInfo");
            Dictionary<MappedPower, int> rankMap = mappedPowers.ToDictionary(x => x, x => 1);
            while (rankMap.Any(x => x.Value != 4))
            {
                // Add ranks
                var mp = mappedPowers.RandomElement();
                while (rankMap[mp] >= 4)
                {
                    mp = mappedPowers.RandomElement(); // Repick
                }

                var newRank = rankMap[mp] + 1;

                PropertyCollection props = new PropertyCollection();
                props.Add(new ObjectProperty(loadout.FileRef.FindExport(mp.BaseTalent.PowerExport.InstancedFullPath).UIndex, "PowerClass"));
                props.Add(new FloatProperty(newRank, "Rank"));
                if (newRank < 4)
                {
                    props.Add(new ObjectProperty(0, "EvolvedPowerClass"));
                }
                else
                {
                    var evo = ThreadSafeRandom.Next(2) == 0 ? mp.EvolvedTalent1 : mp.EvolvedTalent2;
                    props.Add(new ObjectProperty(loadout.FileRef.FindExport(evo.PowerExport.InstancedFullPath).UIndex, "EvolvedPowerClass"));
                }
                props.Add(new NoneProperty());
                alui.Add(new StructProperty("PowerLevelUp", props));
                rankMap[mp] = newRank;
            }

            return alui;
        }

        public static void ResetTalents(RandomizationOption obj)
        {
            //todo: fix this
            //TalentReset.ResetTalents(true);
        }
    }
}
