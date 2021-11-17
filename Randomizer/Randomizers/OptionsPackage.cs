using System;
using System.Collections.Generic;
using ME3TweaksCore.Misc;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers
{
    public class OptionsPackage
    {
        /// <summary>
        /// The seed used for this run
        /// </summary>
        public int Seed { get; set; }
        /// <summary>
        /// If randomization should be multithreaded. Multithreaded randomizations cannot use a seed as the thread that picks up a file cannot be guaranteed (at least, not without a lot of extra work).
        /// </summary>
        public bool UseMultiThread { get; set; }
        /// <summary>
        /// The list of randomization options that were selected
        /// </summary>
        public List<RandomizationOption> SelectedOptions { get; set; }
        /// <summary>
        /// If true, remove the DLC mod component before install, which prevents stacking for most randomization. If false, the DLC component will stay, which stacks changes
        /// </summary>
        public bool Reroll { get; set; }
        /// <summary>
        /// Target to run randomization on
        /// </summary>
        public GameTarget RandomizationTarget { get; set; }

        #region UI INTEROP
        /// <summary>
        /// Callback to set the taskbar progress values
        /// </summary>
        public Action<long, long> SetTaskbarProgress { get; set; }

        /// <summary>
        /// Callback to change the taskbar progress state. The parameter will need converted.
        /// </summary>
        public Action<MTaskbarState> SetTaskbarState { get; set; }

        /// <summary>
        /// Callback to set the current operation status text
        /// </summary>
        public Action<string> SetCurrentOperationText { get; set; }

        /// <summary>
        /// Callback to set the in-window progressbar to indeterminate (or not)
        /// </summary>
        public Action<bool> SetOperationProgressBarIndeterminate { get; set; }

        /// <summary>
        /// Callback to set the current operation progress values
        /// </summary>
        public Action<long, long> SetOperationProgressBarProgress { get; set; }

        /// <summary>
        /// Callback to tell the UI that randomization is in progress and that options should not be able to be changed
        /// </summary>
        public Action<bool> SetRandomizationInProgress { get; set; }

        /// <summary>
        /// Callback to tell the UI that the DLC component has been installed (or not installed)
        /// </summary>
        public Action<bool> NotifyDLCComponentInstalled { get; set; }

        #endregion

    }
}
