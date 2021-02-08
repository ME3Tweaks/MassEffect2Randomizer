using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes
{
    public class RandomizationOption : INotifyPropertyChanged
    {

        public enum EOptionDangerousness
        {
            /// <summary>
            /// Normal safety. Can be used fairly OK
            /// </summary>
            Danger_Normal,
            /// <summary>
            /// No danger
            /// </summary>
            Danger_Safe,
            /// <summary>
            /// Slight danger that can be avoided for the most part
            /// </summary>
            Danger_Warning,
            /// <summary>
            /// Somewhat dangerous, may break game
            /// </summary>
            Danger_Unsafe,
            /// <summary>
            /// The game is definitely not gonna work how you want
            /// </summary>
            Danger_RIP
        }

        public EOptionDangerousness Dangerousness { get; set; } = EOptionDangerousness.Danger_Normal;

        /// <summary>
        /// An key that can be used to uniquely identify the option
        /// </summary>
        public string SubOptionKey { get; set; }
        /// <summary>
        /// The UI displayed text for this option
        /// </summary>
        public string HumanName { get; set; }
        /// <summary>
        /// Description of the option.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// If the option is checked when the UI is set to recommended mode
        /// </summary>
        public bool IsRecommended { get; set; }
        /// <summary>
        /// If this option is selected for operation
        /// </summary>
        public bool OptionIsSelected { get; set; }

        public void OnOptionIsSelectedChanged()
        {
            StateChangingDelegate?.Invoke(this);
        }

        /// <summary>
        /// If the slider should be shown
        /// </summary>
        public bool HasSliderOption { get; set; }
        /// <summary>
        /// If option is mutually exclusive, all values must have same value here set, and then only one item can be picked (or none).
        /// </summary>
        public string MutualExclusiveSet { get; set; }

        /// <summary>
        /// The ticks to use, if this supports a slider value
        /// </summary>
        public string Ticks { get; set; } = "1"; // Default to single tick to prevent binding problems.

        private void OnTicksChanged()
        {
            var tickValues = Ticks.Split(',');
            TickMin = double.Parse(tickValues.First());
            TickMax = double.Parse(tickValues.Last());
        }

        public double TickMax { get; private set; }
        public double TickMin { get; private set; }

        /// <summary>
        /// The UI text to show for the selected tick option
        /// </summary>
        public string TickText { get; private set; }
        /// <summary>
        /// The select slider value. This can be fed through a converter for the UI.
        /// </summary>
        public double SliderValue { get; set; }

        public void OnSliderValueChanged()
        {
            TickText = SliderToTextConverter?.Invoke(SliderValue) ?? SliderValue.ToString();
        }

        public Func<double, string> SliderToTextConverter { get; set; }

        /// <summary>
        /// The export-specific randomization method pointer. It takes an export entry, random, and returns a bool if the operation was run on the export.
        /// </summary>
        public Func<ExportEntry, RandomizationOption, bool> PerformRandomizationOnExportDelegate { get; set; }
        /// <summary>
        /// The callback to perform is this is not an export randomizer
        /// </summary>
        public Func<RandomizationOption, bool> PerformSpecificRandomizationDelegate { get; set; }
        /// <summary>
        /// Randomization method that is invoked before a file's export delegate randomization occurs
        /// </summary>
        public Func<IMEPackage, RandomizationOption, bool> PerformFileSpecificRandomization { get; set; }

        /// <summary>
        /// List of suboptions this option may have
        /// </summary>
        public ObservableCollectionExtended<RandomizationOption> SubOptions { get; init; }

        /// <summary>
        /// Specifies if this is an export randomizer, or if it's a specialized randomizer that operates in a specific context.
        /// </summary>
        public bool IsExportRandomizer => PerformRandomizationOnExportDelegate != null;
        /// <summary>
        /// If this randomization option requires loading TLKs. This can speed up randomization if a TLK option is not chosen.
        /// </summary>
        public bool RequiresTLK { get; set; }
        /// <summary>
        /// If this option is selectable/unselectable. Essentially changes the behavior of other randomizers only (parent) and does not have it's own algorithm
        /// </summary>
        public bool IsOptionOnly { get; set; }
        public Action<RandomizationOption> StateChangingDelegate { get; internal set; }

#pragma warning disable
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore
        /// <summary>
        /// Check if this option has a suboption that is selected with the specified key
        /// </summary>
        /// <param name="optionName"></param>
        /// <returns></returns>
        public bool HasSubOptionSelected(string optionName)
        {
            return SubOptions != null && SubOptions.Any(x => x.SubOptionKey == optionName && x.OptionIsSelected);
        }
    }
}
