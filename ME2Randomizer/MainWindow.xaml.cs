using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ME3TweaksCore.Diagnostics;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Services.Backup;
using ME3TweaksCore.Services.Restore;
using ME3TweaksCore.Targets;
using PropertyChanged;
using Randomizer.MER;
using Randomizer.Randomizers;
using RandomizerUI.Classes;
using RandomizerUI.Classes.Controllers;
using RandomizerUI.DebugTools;
using RandomizerUI.ui;

namespace RandomizerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : MetroWindow
    {
#if __GAME1__
        private static string FaqLink = "https://me3tweaks.com/masseffectrandomizer/faq";
#elif __GAME2__
        private static string FaqLink = "https://me3tweaks.com/masseffect2randomizer/faq";
#elif __GAME3__
        private static string FaqLink = "https://me3tweaks.com/masseffect3randomizer/faq";
#endif

        /// <summary>
        /// Which index is currently selected in the dropdown for which game to run for
        /// </summary>
        public int SelectedGameIndex { get; set; }

        /// <summary>
        /// The current selected game target
        /// </summary>
        public GameTarget SelectedTarget { get; set; }

        /// <summary>
        /// The list of targets in the dropdown
        /// </summary>
        public ObservableCollectionExtended<GameTarget> SelectableTargets { get; } = new();

        public bool UseMultiThreadRNG { get; set; } = true;

        #region Flyouts
        public bool LogUploaderFlyoutOpen { get; set; }
        public bool FirstRunFlyoutOpen { get; set; }
        #endregion

        public string GamePathString { get; set; } = "Please wait";
        public bool ShowProgressPanel { get; set; }
        public RandomizationMode SelectedRandomizeMode { get; set; }

        public ObservableCollectionExtended<ImageCredit> ImageCredits { get; } = new();
        public ObservableCollectionExtended<string> ContributorCredits { get; } = new();
        public ObservableCollectionExtended<LibraryCredit> LibraryCredits { get; } = new();

        public void OnSelectedRandomizeModeChanged()
        {
            UpdateCheckboxSettings();
        }

        /// <summary>
        /// The list of options shown
        /// </summary>
        public ObservableCollectionExtended<RandomizationGroup> RandomizationGroups { get; } = new ObservableCollectionExtended<RandomizationGroup>();
        public bool AllowOptionsChanging { get; set; } = true;
        public bool PerformReroll { get; set; } = true;
        public int CurrentProgressValue { get; set; }
        public string CurrentOperationText { get; set; }
        public double ProgressBar_Bottom_Min { get; set; }
        public double ProgressBar_Bottom_Max { get; set; }
        public bool ProgressBarIndeterminate { get; set; }
        public bool ShowUninstallButton { get; set; }
        public bool DLCComponentInstalled { get; set; }

        public void OnDLCComponentInstalledChanged()
        {
            if (!DLCComponentInstalled)
            {
                ShowUninstallButton = false;
            }
            else
            {
                // Refresh the bindings
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public LogItem SelectedLogForUpload { get; set; }
        public ObservableCollectionExtended<LogItem> LogsAvailableForUpload { get; } = new();

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                MERUtilities.OpenWebPage(((Hyperlink)sender).NavigateUri.AbsoluteUri);
            }
            catch (Exception)
            {

            }
        }
        private void UpdateCheckboxSettings()
        {
            foreach (var group in RandomizationGroups)
            {
                foreach (var option in group.Options)
                {
                    SetOptionOnRecommendation(option);
                }
            }
        }

        private void SetOptionOnRecommendation(RandomizationOption option)
        {
            if (SelectedRandomizeMode == RandomizationMode.ERandomizationMode_Screed) option.OptionIsSelected = option.Dangerousness < RandomizationOption.EOptionDangerousness.Danger_RIP;
            if (SelectedRandomizeMode == RandomizationMode.ERandomizationMode_SelectAny) option.OptionIsSelected = false;
            if (SelectedRandomizeMode == RandomizationMode.ERandomizationMode_Common) option.OptionIsSelected = option.IsRecommended;
            if (option.SubOptions != null)
            {
                foreach (var subOption in option.SubOptions)
                {
                    SetOptionOnRecommendation(subOption);
                }
            }
        }


        public MainWindow()
        {
            DataContext = this;
            ProgressBar_Bottom_Max = 100;
            ProgressBar_Bottom_Min = 0;
            ShowProgressPanel = true;
            LoadCommands();
            InitializeComponent();
        }

        private void optionStateChanging(RandomizationOption obj)
        {
            if (obj.MutualExclusiveSet != null && obj.OptionIsSelected)
            {
                var allOptions = RandomizationGroups.SelectMany(x => x.Options).Where(x => x.MutualExclusiveSet == obj.MutualExclusiveSet);
                foreach (var option in allOptions)
                {
                    if (option != obj)
                    {
                        option.OptionIsSelected = false; // turn off other options
                    }
                }
            }
        }

        internal List<string> GetContributorCredits()
        {
            var contributors = new List<string>();
            contributors.Add("Mellin - 3D modeling");
            contributors.Add("Jenya - 3D modeling, testing");
            contributors.Add("Audemus - Textures");
            contributors.Add("JadeBarker - Technical assistance");
            contributors.Add("StrifeTheHistorian - Psychological profiles");
            contributors.Add("Not_Female - UwU Emoticons implementation");
            contributors.Add("AdmiralKirk - Testing and feedback");
            contributors.Sort();
            return contributors;
        }

        #region Commands
        public GenericCommand StartRandomizationCommand { get; set; }
        public GenericCommand CloseLogUICommand { get; set; }
        public GenericCommand UploadSelectedLogCommand { get; set; }
        public RelayCommand SetupRandomizerCommand { get; set; }
        public GenericCommand UninstallDLCCommand { get; set; }

        private void LoadCommands()
        {
            StartRandomizationCommand = new GenericCommand(StartRandomization, CanStartRandomization);
            CloseLogUICommand = new GenericCommand(() => LogUploaderFlyoutOpen = false, () => LogUploaderFlyoutOpen);
            UploadSelectedLogCommand = new GenericCommand(CollectAndUploadLog, () => SelectedLogForUpload != null);
            SetupRandomizerCommand = new RelayCommand(SetupRandomizer, CanSetupRandomizer);
            UninstallDLCCommand = new GenericCommand(UninstallDLCComponent, CanUninstallDLCComponent);
        }

        private async void UninstallDLCComponent()
        {
            var dlcModPath = MERFileSystem.GetDLCModPath(SelectedTarget);
            if (Directory.Exists(dlcModPath))
            {
                var pd = await this.ShowProgressAsync("Deleting DLC component", "Please wait while the DLC mod component of your current randomization is deleted.");
                pd.SetIndeterminate();
                Task.Run(() =>
                    {
                        MUtilities.DeleteFilesAndFoldersRecursively(dlcModPath);
                        DLCComponentInstalled = false;
                        Thread.Sleep(2000);
                    })
                    .ContinueWithOnUIThread(async x =>
                    {
                        await pd.CloseAsync();
                        await this.ShowMessageAsync("DLC component uninstalled", "The DLC component of the randomization has been uninstalled. A few files that cannot be placed into DLC may remain, you will need to repair your game to remove them.\n\nFor faster restores in the future, make a backup with an ME3Tweaks program. Mass Effect 2 randomization uninstallation only takes a few seconds when an ME3Tweaks backup is available.");
                        CommandManager.InvalidateRequerySuggested();
                    });
            }
        }

        private bool CanUninstallDLCComponent()
        {
            if (SelectedTarget == null) return false;
            var status = BackupService.GetBackupStatus(SelectedTarget.Game);
            var canUninstall = ShowUninstallButton = status != null && !status.BackedUp && DLCComponentInstalled;
            return canUninstall;
        }

        private bool CanSetupRandomizer(object obj)
        {
            return obj is RandomizationOption option && option.OptionIsSelected && option.SetupRandomizerDelegate != null;
        }

        private void SetupRandomizer(object obj)
        {
            if (obj is RandomizationOption option)
            {
                option.SetupRandomizerDelegate?.Invoke(option);
            }
        }

        #endregion


        public async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartupUIController.BeginFlow(this);
        }

        public string BackupRestoreText { get; set; }

        public bool DirectionsTextVisible { get; set; }

        public string FirstRunBackgroundImage
        {
            get
            {
#if __GAME1__
                return "/images/game3/firstrun_bg.jpg";
#elif __GAME2__
                return "/images/game2/firstrun_bg.jpg";
#elif __GAME3__
                return "/images/game3/firstrun_bg.jpg";
#endif
                throw new Exception("NOT A VALID BUILD");
            }
        }

        public string MainWindowBackgroundImage
        {
            get
            {
#if __GAME1__
                if (SelectedTarget != null && SelectedTarget.Game.IsLEGame())
                {
                    return "/images/game1/lebackground.jpg";
                }
                return "/images/game1/otbackground.jpg";
#elif __GAME2__
                if (SelectedTarget != null && SelectedTarget.Game.IsLEGame())
                {
                    return "/images/game2/lebackground.jpg";
                }
                return "/images/game2/otbackground.jpg";
#elif __GAME3__
                if (SelectedTarget != null && SelectedTarget.Game.IsLEGame())
                {
                    return "/images/game3/lebackground.jpg";
                }
                return "/images/game3/otbackground.jpg";
#endif
                throw new Exception("NOT A VALID BUILD");
            }
        }

        private bool CanStartRandomization()
        {
            if (SeedTextBox == null || !int.TryParse(SeedTextBox.Text, out var value) || value == 0)
                return false;

            // Target not found or texture modded
            if (SelectedTarget == null || SelectedTarget.TextureModded)
                return false;
            return true;
        }

        private async void StartRandomization()
        {
            var modPath = MERFileSystem.GetDLCModPath(SelectedTarget);
            var backupStatus = BackupService.GetBackupStatus(SelectedTarget.Game);
            if (!backupStatus.BackedUp && !Directory.Exists(modPath))
            {
                var settings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "Continue anyways",
                    NegativeButtonText = "Cancel"
                };
                var result = await this.ShowMessageAsync("No ME3Tweaks-based backup available", "It is recommended that you create an ME3Tweaks-based backup before randomization, as this allows much faster re-rolls. You can take a backup using the button on the bottom left of the interface.", MessageDialogStyle.AffirmativeAndNegative, settings);
                if (result == MessageDialogResult.Negative)
                {
                    // Do nothing. User canceled
                    return;
                }
            }


            if (Directory.Exists(modPath) && PerformReroll)
            {
                var isControllerInstalled = false;
#if __GAME2__
                // Only ME2 controller mod matters here (for now)
                isControllerInstalled = Randomizer.Randomizers.Game2.Misc.SFXGame.IsControllerBasedInstall(SelectedTarget);
#endif
                if (!isControllerInstalled)
                {
                    if (backupStatus.BackedUp)
                    {
                        var settings = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = "Quick restore",
                            NegativeButtonText = "No restore",
                            FirstAuxiliaryButtonText = "Cancel",
                            DefaultButtonFocus = MessageDialogResult.Affirmative
                        };
                        var result = await this.ShowMessageAsync("Existing randomization already installed", "An existing randomization is already installed. It is highly recommended that you perform a quick restore before re-rolling so that basegame changes do not stack or are left installed if your new options do not include these changes.\n\nPerform a quick restore before randomization?", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, settings);
                        if (result == MessageDialogResult.FirstAuxiliary)
                        {
                            // Do nothing. User canceled
                            return;
                        }

                        if (result == MessageDialogResult.Affirmative)
                        {
                            // Perform quick restore first
                            RestoreController.StartRestore(this, SelectedTarget, true, InternalStartRandomization);
                            return; // Return, we will run randomization after this
                        }

                        // User did not want to restore, just run 
                    }
                    else
                    {
                        var settings = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = "Continue anyways",
                            NegativeButtonText = "Cancel",
                        };
                        var result = await this.ShowMessageAsync("Existing randomization already installed", "An existing randomization is already installed. Some basegame only randomized files may remain after the DLC component is removed, and if options that modify these files are selected, the effects will stack. It is recommended you 'Remove Randomization' in the bottom left window, then repair your game to ensure you have a fresh installation for a re-roll.\n\nAn ME3Tweaks-based backup is recommended to avoid this procedure, which can be created in the bottom left of the application. It enables the quick restore feature, which only takes a few seconds.", MessageDialogStyle.AffirmativeAndNegative, settings);
                        if (result == MessageDialogResult.Negative)
                        {
                            // Do nothing. User canceled
                            return;
                        }
                    }
                }
                else
                {
#if __GAME2__
                    if (backupStatus.BackedUp)
                    {
                        var settings = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = "Perform quick restore + randomize",
                            NegativeButtonText = "Perform only quick restore",
                            FirstAuxiliaryButtonText = "Install anyways",
                            SecondAuxiliaryButtonText = "Cancel",

                            DefaultButtonFocus = MessageDialogResult.Negative
                        };
                        var result = await this.ShowMessageAsync("Controller mod detected", "Performing a quick restore will undo changes made by the ME2Controller mod. After performing a quick restore, but before randomization, you should reinstall ME2Controller.", MessageDialogStyle.AffirmativeAndNegativeAndDoubleAuxiliary, settings);
                        if (result == MessageDialogResult.SecondAuxiliary)
                        {
                            // Do nothing. User canceled
                            return;
                        }

                        if (result == MessageDialogResult.Affirmative)
                        {
                            // Perform quick restore first
                            RestoreController.StartRestore(this, SelectedTarget, true, InternalStartRandomization);
                            return; // Return, we will run randomization after this
                        }

                        if (result == MessageDialogResult.Negative)
                        {
                            RestoreController.StartRestore(this, SelectedTarget, true);
                            return; // Return, we will run randomization after this
                        }
                        // User did not want to restore, just run 
                    }
                    else
                    {
#endif
                        // no backup, can't quick restore
                        var settings = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = "Continue anyways",
                            NegativeButtonText = "Cancel",
                        };
                        var result = await this.ShowMessageAsync("Existing randomization already installed", "An existing randomization is already installed. Some basegame only randomized files may remain after the DLC component is removed, and if options that modify these files are selected, the effects will stack. It is recommended you 'Remove Randomization' in the bottom left window, then repair your game to ensure you have a fresh installation for a re-roll.\n\nAn ME3Tweaks-based backup is recommended to avoid this procedure, which can be created in the bottom left of the application. It enables the quick restore feature, which only takes a few seconds.", MessageDialogStyle.AffirmativeAndNegative, settings);
                        if (result == MessageDialogResult.Negative)
                        {
                            // Do nothing. User canceled
                            return;
                        }
#if __GAME2__
                    }
#endif
                }
            }

            InternalStartRandomization();
        }

        private async void InternalStartRandomization()
        {
            if (!MERUtilities.IsGameRunning(SelectedTarget.Game))
            {
                ShowProgressPanel = true;
#if __GAME1__
                var randomizer = new Randomizer.Randomizers.Game1.Randomizer();
#elif __GAME2__
                var randomizer = new Randomizer.Randomizers.Game2.Randomizer();
#elif __GAME3__
                var randomizer = new Randomizer.Randomizers.Game3.Randomizer();
#endif

                AllowOptionsChanging = false;

                var op = new OptionsPackage()
                {
                    Seed = int.Parse(SeedTextBox.Text),
                    SelectedOptions = RandomizationGroups.SelectMany(x => x.Options.Where(x => x.OptionIsSelected)).ToList(),
                    UseMultiThread = UseMultiThreadRNG,
                    Reroll = PerformReroll
                };
                randomizer.Randomize(op);
            }
            else
            {
                await this.ShowMessageAsync("Mass Effect 2 is running", "Cannot randomize the game while Mass Effect 2 is running. Please close the game and try again.");
            }
        }

        private void Image_ME3Tweaks_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start("https://me3tweaks.com");
            }
            catch (Exception)
            {

            }
        }

        public void FinalizeInterfaceLoad()
        {
#if __GAME1__
            Randomizer.Randomizers.Game1.Randomizer.SetupOptions(RandomizationGroups, optionStateChanging);
#elif __GAME2__
            Randomizer.Randomizers.Game2.Randomizer.SetupOptions(RandomizationGroups, optionStateChanging);
#elif __GAME3__
            Randomizer.Randomizers.Game3.Randomizer.SetupOptions(RandomizationGroups, optionStateChanging);
#endif
            ShowProgressPanel = false;
        }

        private void Logs_Click(object sender, RoutedEventArgs e)
        {
            LogUploaderFlyoutOpen = true;
        }

        private async void BackupRestore_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTarget == null)
                return; // Do not allow!

            string path = BackupService.GetGameBackupPath(SelectedTarget.Game);
            if (path != null)
            {

                if (SelectedTarget.TextureModded)
                {
                    var restore = new GameRestore(SelectedTarget.Game)
                    {
                        // ENTER CALLBACKS
                    };
                    restore.PerformRestore(SelectedTarget, SelectedTarget.TargetPath);
                }
                else
                {
                    MetroDialogSettings settings = new MetroDialogSettings();
                    settings.NegativeButtonText = "Full";
                    settings.FirstAuxiliaryButtonText = "Cancel";
                    settings.AffirmativeButtonText = "Quick";
                    settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
                    var result = await this.ShowMessageAsync("Select restore mode", $"Select which restore mode you would like to perform:\n\nQuick: Restores basegame files modifiable by {MERUI.GetRandomizerName()}, deletes the DLC mod component\n\nFull: Deletes entire game installation and restores the backup in its place. Fully resets the game to the backup state", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, settings);
                    if (result == MessageDialogResult.FirstAuxiliary)
                    {
                        // Do nothing. User canceled
                    }
                    else
                    {
                        RestoreController.StartRestore(this, SelectedTarget, result == MessageDialogResult.Affirmative);
                    }
                }
            }
            //else if (gameTarget == null)
            //{
            //    await this.ShowMessageAsync($"{MERFileSystem.Game.ToGameName()} not found", $"{MERFileSystem.Game.ToGameName()} was not found, and as such, cannot be restored by {MERFileSystem.Game.ToGameName()} Randomizer. Repair your game using Steam, Origin, or your DVD, or restore your backup using ME3Tweaks Mod Manager.");
            //}
        }

        private void DebugCloseDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            //DiagnosticsFlyoutOpen = false;
        }

        private void Button_FirstTimeRunDismiss_Click(object sender, RoutedEventArgs e)
        {
            MERSettings.WriteSettingBool(ESetting.SETTING_FIRSTRUN, true);
            FirstRunFlyoutOpen = false;
        }

        private void Flyout_Mousedown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Flyout_Doubleclick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void FAQ_Click(object sender, RoutedEventArgs e)
        {
            MERUtilities.OpenWebPage(FaqLink);
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            //ME3Tweaks Discord
            MERUtilities.OpenWebPage(App.DISCORD_INVITE_LINK);
        }

        public void OnLogUploaderFlyoutOpenChanged()
        {
            if (LogUploaderFlyoutOpen)
            {
                LogsAvailableForUpload.ReplaceAll(LogCollector.GetLogsList());
                SelectedLogForUpload = LogsAvailableForUpload.FirstOrDefault();
            }
            else
            {
                LogsAvailableForUpload.ClearEx();
                SelectedLogForUpload = null;
            }
        }

        private void DebugWindow_Click(object sender, RoutedEventArgs e)
        {
            new DebugWindow(this).Show();
        }

        private async void CollectAndUploadLog()
        {
            var pd = await this.ShowProgressAsync("Uploading log", $"Please wait while the application log is uploaded to the ME3Tweaks Log Viewing Service.");
            pd.SetIndeterminate();

            NamedBackgroundWorker nbw = new NamedBackgroundWorker("DiagnosticsWorker");
            nbw.DoWork += (a, b) =>
            {
                //ProgressIndeterminate = true;
                //GameTarget target = GameChosen != null ? Locations.GetTarget(GameChosen.Value) : null;
                StringBuilder logUploadText = new StringBuilder();

                string logText = "";
                //if (target != null)
                //{
                //    logUploadText.Append("[MODE]diagnostics\n"); //do not localize
                //    logUploadText.Append(LogCollector.PerformDiagnostic(target, FullDiagChosen,
                //            x => DiagnosticStatusText = x,
                //            x =>
                //            {
                //                ProgressIndeterminate = false;
                //                ProgressValue = x;
                //            },
                //            () => ProgressIndeterminate = true));
                //    logUploadText.Append("\n"); //do not localize
                //}

                if (SelectedLogForUpload != null)
                {
                    logUploadText.Append("[MODE]logs\n"); //do not localize
                    logUploadText.AppendLine(LogCollector.CollectLogs(SelectedLogForUpload.filepath));
                    logUploadText.Append("\n"); //do not localize
                }

                //DiagnosticStatusText = "Uploading to log viewing service";
                //ProgressIndeterminate = true;
                var response = LogUploader.UploadLog(logUploadText.ToString(), "https://me3tweaks.com/masseffect2randomizer/logservice/logupload");
                if (response.uploaded)
                {
                    var DiagnosticResultText = response.result;
                    if (response.result.StartsWith("http"))
                    {
                        MERUtilities.OpenWebPage(response.result);
                    }
                }


                if (!response.uploaded || QuickFixHelper.IsQuickFixEnabled(QuickFixHelper.QuickFixName.ForceSavingLogLocally))
                {
                    // Upload failed.
                    var GeneratedLogPath = Path.Combine(MCoreFilesystem.GetLogDir(), $"FailedLogUpload_{DateTime.Now.ToString("s").Replace(":", ".")}.txt");
                    File.WriteAllText(GeneratedLogPath, logUploadText.ToString());
                }

                //DiagnosticComplete = true;
                //DiagnosticInProgress = false;
            };
            nbw.RunWorkerCompleted += async (sender, args) =>
            {
                CommandManager.InvalidateRequerySuggested();
                LogUploaderFlyoutOpen = false;
                await pd.CloseAsync();
            };
            //DiagnosticInProgress = true;
            nbw.RunWorkerAsync();
        }

        #region Settings

        public const string SETTING_FIRSTRUN = "FirstRunCompleted";
        private void FirstRunShowButton_Click(object sender, RoutedEventArgs e)
        {
            FirstRunFlyoutOpen = true;
        }

        #endregion

        public void SetupTargetDescriptionText()
        {
            if (SelectedTarget == null)
            {
                var gameName = Randomizer.MER.MERUtilities.GetGameUIName(SelectedGameIndex == 0);
                GamePathString = $"{gameName} not detected. Repair and run your game to fix detection.";
            }
            else if (SelectedTarget.TextureModded)
            {
                GamePathString = "Cannot randomize, game is texture modded";
            }
            else
            {
                DirectionsTextVisible = true;
                GamePathString = $"Randomization target: {SelectedTarget.TargetPath}";
            }
        }

        private void ROClickHACK_Click(object sender, MouseButtonEventArgs e)
        {
            if (AllowOptionsChanging && sender is FrameworkElement fe && fe.DataContext is RandomizationOption option)
            {
                // Toggle
                option.OptionIsSelected = !option.OptionIsSelected;
            }
        }

        private void SwitchToOT_Click(object sender, RoutedEventArgs e)
        {
            SelectedTarget = Locations.GetTarget(false);
        }

        private void SwitchToLE_Click(object sender, RoutedEventArgs e)
        {
            SelectedTarget = Locations.GetTarget(true);
        }

        private void OnSelectedTargetChanged()
        {
            if (SelectedTarget != null)
            {
                GameLocationTextbox.Text = SelectedTarget.TargetPath;
            }
        }
    }
}