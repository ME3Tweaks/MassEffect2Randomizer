using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes
{
    public class RandomizationOption : INotifyPropertyChanged
    {
        /// <summary>
        /// The UI displayed text for this option
        /// </summary>
        public string HumanName { get; set; }
        /// <summary>
        /// If the option is checked when the UI is set to recommended mode
        /// </summary>
        public bool IsRecommended { get; set; }
        /// <summary>
        /// If this option is selected for operation
        /// </summary>
        public bool OptionIsSelected { get; set; }

        /// <summary>
        /// If the slider should be shown
        /// </summary>
        public bool HasSliderOption { get; set; }

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
        public Func<ExportEntry, Random, RandomizationOption, bool> PerformRandomizationOnExportDelegate { get; set; }
        /// <summary>
        /// The callback to perform is this is not an export randomizer
        /// </summary>
        public Func<Random, RandomizationOption, bool> PerformSpecificRandomizationDelegate { get; set; }


        /// <summary>
        /// Specifies if this is an export randomizer, or if it's a specialized randomizer that operates in a specific context.
        /// </summary>
        public bool IsExportRandomizer => PerformRandomizationOnExportDelegate != null;

#pragma warning disable
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore
    }
}
