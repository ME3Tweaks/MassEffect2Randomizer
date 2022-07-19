using System;
using System.ComponentModel;
using System.Linq;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using PropertyChanged;

namespace Randomizer.Randomizers
{
    public enum RandomizationMode
    {
        ERandomizationMode_SelectAny,
        ERandomizationMode_Common,
        ERandomizationMode_Screed,
    }
    [AddINotifyPropertyChangedInterface]
    public class RandomizationOption
    {
        /// <summary>
        /// Used for forcing binding updates when the binded object is this object
        /// </summary>
        public RandomizationOption Self { get; set; }

        #region EventHandling
        public string CurrentOperation { get; set; }
        public int ProgressValue { get; set; }
        public int ProgressMax { get; set; }
        public bool ProgressIndeterminate { get; set; } = true;
        public void OnCurrentOperationChanged() { OnOperationUpdate?.Invoke(this, null); }
        public void OnProgressValueChanged() { OnOperationUpdate?.Invoke(this, null); }
        public void OnProgressMaxChanged() { OnOperationUpdate?.Invoke(this, null); }
        public void OnProgressIndeterminateChanged() { OnOperationUpdate?.Invoke(this, null); }

        public event EventHandler OnOperationUpdate;
        #endregion

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
        [AlsoNotifyFor(nameof(Self))]
        public bool OptionIsSelected { get; set; }

        public void OnOptionIsSelectedChanged()
        {
            StateChangingDelegate?.Invoke(this);
        }

        public RandomizationOption()
        {
            Self = this;
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
        /// Adds a button to the randomizer, allowing you to click it and run an action to 'setup' the randomizer.
        /// </summary>
        public Action<RandomizationOption> SetupRandomizerDelegate { get; set; }

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

        /// <summary>
        /// Converter for slider to text. This is called whenever the Slider Value changed. IN Randomizer.cs define this before setting the value so it is properly populated the first time
        /// </summary>
        public Func<double, string> SliderToTextConverter { get; set; }

        /// <summary>
        /// The export-specific randomization method pointer. It takes an export entry, random, and returns a bool if the operation was run on the export.
        /// </summary>
        public Func<GameTarget, ExportEntry, RandomizationOption, bool> PerformRandomizationOnExportDelegate { get; set; }
        /// <summary>
        /// The callback to perform is this is not an export randomizer. This is run first and can also be used for initialization purposes for an option
        /// </summary>
        public Func<GameTarget, RandomizationOption, bool> PerformSpecificRandomizationDelegate { get; set; }
        /// <summary>
        /// Randomization method that is invoked before a file's export delegate randomization occurs
        /// </summary>
        public Func<GameTarget, IMEPackage, RandomizationOption, bool> PerformFileSpecificRandomization { get; set; }

        /// <summary>
        /// List of suboptions this option may have
        /// </summary>
        public ObservableCollectionExtended<RandomizationOption> SubOptions { get; init; }

        /// <summary>
        /// Specifies if this is an export randomizer, or if it's a specialized randomizer that operates in a specific context.
        /// </summary>
        public bool IsExportRandomizer => PerformRandomizationOnExportDelegate != null;
        /// <summary>
        /// If this option is selectable/unselectable. Essentially changes the behavior of other randomizers only (parent) and does not have its own algorithm
        /// </summary>
        public bool IsOptionOnly { get; set; }
        public Action<RandomizationOption> StateChangingDelegate { get; internal set; }
        /// <summary>
        /// Tooltip to show on the slider
        /// </summary>
        public string SliderTooltip { get; set; }
        /// <summary>
        /// If the randomizer runs post-export/files
        /// </summary>
        public bool IsPostRun { get; set; }
        /// <summary>
        /// The text to display on the button that is shown when the SetupDelegate is set.
        /// </summary>
        public string SetupRandomizerButtonText { get; set; }
        /// <summary>
        /// The tooltip to display on the setup randomizer button
        /// </summary>
        public string SetupRandomizerButtonToolTip { get; set; }

        #region REQUIRES
        /// <summary>
        /// If this option requires gesture packages to load into memory for use
        /// </summary>
        public bool RequiresGestures { get; set; }
        /// <summary>
        /// If this randomization option requires loading TLKs. This can speed up randomization if a TLK option is not chosen.
        /// </summary>
        public bool RequiresTLK { get; set; }
        /// <summary>
        /// If this randomization option requires extracting the audio folder.
        /// </summary>
        public bool RequiresAudio { get; set; }
        #endregion

        /// <summary>
        /// Check if this option has a suboption that is selected with the specified key
        /// </summary>
        /// <param name="optionName"></param>
        /// <returns></returns>
        public bool HasSubOptionSelected(string optionName)
        {
            return SubOptions != null && SubOptions.Any(x => x.SubOptionKey == optionName && x.OptionIsSelected);
        }

        /// <summary>
        /// Thread-safe progressvalue increment
        /// </summary>
        public int IncrementProgressValue()
        {
            lock (Self)
            {
                ProgressValue++;
                return ProgressValue;
            }
        }
    }
}
