using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Game2.Talents
{
    [DebuggerDisplay("HenchLoadoutInfo for {HenchUIName}")]
    [AddINotifyPropertyChangedInterface]
    class HenchLoadoutInfo
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
            var lastIndex = LoadoutIFP.LastIndexOf("_"); // get miranda off hench_Miranda
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
                default:
                    HenchGender = Gender.Male;
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
            // it/its him/his don't line up with female's only having 'her'

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

        public void ResetSourcePowerNames()
        {
            foreach (var pow in HenchTalentSet.Powers)
            {
                pow.ResetSourcePowerName();
            }
        }
    }
}
