using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ALOTInstallerCore;
using ALOTInstallerCore.Helpers;
using ALOTInstallerCore.Helpers.AppSettings;
using ALOTInstallerCore.ModManager.ME3Tweaks;
using ALOTInstallerCore.ModManager.Objects;
using ALOTInstallerCore.ModManager.Services;
using ALOTInstallerCore.PlatformSpecific.Windows;
using ALOTInstallerCore.Steps;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME2Randomizer.Classes.Telemetry;
using ME3ExplorerCore;
using ME3ExplorerCore.Compression;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Gammtek.Extensions;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal.BinaryConverters;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Serilog;
using Utilities = ALOTInstallerCore.Utilities;

namespace ME2Randomizer.Classes.Controllers
{
    public class StartupUIController
    {
        private static void SetWrapperLogger(ILogger logger) => Log.Logger = logger;
        private static bool telemetryStarted = false;
        internal static string PassthroughME1Path;
        internal static string PassthroughME2Path;
        internal static string PassthroughME3Path;

        private static void startTelemetry()
        {
            initAppCenter();
            AppCenter.SetEnabledAsync(true);
        }

        private static void stopTelemetry()
        {
            AppCenter.SetEnabledAsync(false);
        }

        private static void initAppCenter()
        {
#if !DEBUG
            if (APIKeys.HasAppCenterKey && !telemetryStarted)
            {
                Microsoft.AppCenter.Crashes.Crashes.GetErrorAttachments = (ErrorReport report) =>
                {
                    var attachments = new List<ErrorAttachmentLog>();
                    // Attach some text.
                    string errorMessage = "ALOT Installer has crashed! This is the exception that caused the crash:\n" + report.StackTrace;
                    MERLog.Fatal(errorMessage);
                    Log.Error("Note that this exception may appear to occur in a follow up boot due to how appcenter works");
                    string log = LogCollector.CollectLatestLog(false);
                    if (log.Length < 1024 * 1024 * 7)
                    {
                        attachments.Add(ErrorAttachmentLog.AttachmentWithText(log, "crashlog.txt"));
                    }
                    else
                    {
                        //Compress log
                        var compressedLog = LZMA.CompressToLZMAFile(Encoding.UTF8.GetBytes(log));
                        attachments.Add(ErrorAttachmentLog.AttachmentWithBinary(compressedLog, "crashlog.txt.lzma", "application/x-lzma"));
                    }

                    // Attach binary data.
                    //var fakeImage = System.Text.Encoding.Default.GetBytes("Fake image");
                    //ErrorAttachmentLog binaryLog = ErrorAttachmentLog.AttachmentWithBinary(fakeImage, "ic_launcher.jpeg", "image/jpeg");

                    return attachments;
                };
                AppCenter.Start(APIKeys.AppCenterKey, typeof(Analytics), typeof(Crashes));
            }
#else
            if (!APIKeys.HasAppCenterKey)
            {
                Debug.WriteLine(" >>> This build is missing an API key for AppCenter!");
            }
            else
            {
                Debug.WriteLine("This build has an API key for AppCenter");
            }
#endif
            telemetryStarted = true;
        }

        public static async void BeginFlow(MainWindow window)
        {
            // PRE LIBRARY LOAD
            RegistryHandler.RegistrySettingsPath = @"HKEY_CURRENT_USER\Software\MassEffect2Randomizer";
            RegistryHandler.CurrentUserRegistrySubpath = @"Software\MassEffect2Randomizer";
            ME3ExplorerCoreLib.SetSynchronizationContext(TaskScheduler.FromCurrentSynchronizationContext());

            try
            {
                // This is in a try catch because this is a critical no-crash zone that is before launch
                window.Title = $"Mass Effect 2 Randomizer {App.AppVersion}";
            }
            catch { }

            if (Utilities.GetExecutablePath().StartsWith(Path.GetTempPath(), StringComparison.InvariantCultureIgnoreCase))
            {
                // Running from temp! This is not allowed
                await window.ShowMessageAsync("Cannot run from temp directory", $"Mass Effect 2 Randomizer cannot be run from the system's Temp directory. If this executable was run from within an archive, it needs to be extracted first.");
                Environment.Exit(1);
            }

            var pd = await window.ShowProgressAsync("Starting up", $"Mass Effect 2 Randomizer is starting up. Please wait.");
            pd.SetIndeterminate();
            NamedBackgroundWorker bw = new NamedBackgroundWorker("StartupThread");
            bw.DoWork += (a, b) =>
            {

                ALOTInstallerCoreLib.Startup(SetWrapperLogger, RunOnUIThread, startTelemetry, stopTelemetry, $"Mass Effect 2 Randomizer {App.AppVersion} starting up", false);
                // Logger is now available

                // Setup telemetry handlers
                CoreAnalytics.TrackEvent = TelemetryController.TrackEvent;
                CoreCrashes.TrackError = TelemetryController.TrackError;
                CoreCrashes.TrackError2 = TelemetryController.TrackError2;
                CoreCrashes.TrackError3 = TelemetryController.TrackError3;

                // Setup the InteropPackage for the update check
                #region Update interop
                CancellationTokenSource ct = new CancellationTokenSource();

                AppUpdateInteropPackage interopPackage = new AppUpdateInteropPackage()
                {
                    GithubOwner = "Mgamerz",
                    GithubReponame = "MassEffect2Randomizer",
                    UpdateAssetPrefix = "ME2Randomizer",
                    UpdateFilenameInArchive = "ME2Randomizer.exe",
                    ShowUpdatePromptCallback = (title, text, updateButtonText, declineButtonText) =>
                    {
                        bool response = false;
                        object syncObj = new object();
                        Application.Current.Dispatcher.Invoke(async () =>
                        {
                            if (Application.Current.MainWindow is MainWindow mw)
                            {
                                var result = await mw.ShowMessageAsync(title, text, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                                {
                                    AffirmativeButtonText = updateButtonText,
                                    NegativeButtonText = declineButtonText,
                                    DefaultButtonFocus = MessageDialogResult.Affirmative
                                },
                                    75);
                                response = result == MessageDialogResult.Affirmative;
                                lock (syncObj)
                                {
                                    Monitor.Pulse(syncObj);
                                }
                            }
                        });
                        lock (syncObj)
                        {
                            Monitor.Wait(syncObj);
                        }
                        return response;
                    },
                    ShowUpdateProgressDialogCallback = (title, initialmessage, canCancel) =>
                    {
                        // We don't use this as we are already in a progress dialog
                        pd.SetCancelable(canCancel);
                        pd.SetMessage(initialmessage);
                        pd.SetTitle(title);
                    },
                    SetUpdateDialogTextCallback = s =>
                    {
                        pd.SetMessage(s);
                    },
                    ProgressCallback = (done, total) =>
                    {
                        pd.SetProgress(done * 1d / total);
                        pd.SetMessage($"Downloading update {FileSize.FormatSize(done)} / {FileSize.FormatSize(total)}");
                    },
                    ProgressIndeterminateCallback = () =>
                    {
                        pd.SetIndeterminate();
                    },
                    ShowMessageCallback = (title, message) =>
                    {
                        object syncObj = new object();
                        Application.Current.Dispatcher.Invoke(async () =>
                        {
                            if (Application.Current.MainWindow is MainWindow mw)
                            {
                                await mw.ShowMessageAsync(title, message);
                                lock (syncObj)
                                {
                                    Monitor.Pulse(syncObj);
                                }
                            }
                        });
                        lock (syncObj)
                        {
                            Monitor.Wait(syncObj);
                        }
                    },
                    NotifyBetaAvailable = () =>
                    {
                        App.BetaAvailable = true;
                    },
                    DownloadCompleted = () =>
                    {
                        pd.SetCancelable(false);
                    },
                    cancellationTokenSource = ct,
                    ApplicationName = "Mass Effect 2 Randomizer",
                    RequestHeader = "ME2Randomizer",
                    ForcedUpgradeMaxReleaseAge = 3
                };

                #endregion



                pd.SetMessage("Checking for application updates");
                pd.Canceled += (sender, args) =>
                {
                    ct.Cancel();
                };
                AppUpdater.PerformGithubAppUpdateCheck(interopPackage);

                // If user aborts download
                pd.SetCancelable(false);
                pd.SetIndeterminate();
                pd.SetTitle("Starting up");

                void setStatus(string message)
                {
                    pd.SetIndeterminate();
                    pd.SetMessage(message);
                }

                GameTarget target = null;
                try
                {
                    pd.SetMessage("Loading Mass Effect 2 Randomizer framework");
                    ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(Control), new FrameworkPropertyMetadata(true));
                    ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

                    ALOTInstallerCoreLib.PostCriticalStartup(x => pd.SetMessage(x), RunOnUIThread, false);
                    MEPackageHandler.GlobalSharedCacheEnabled = false; // ME2R does not use the global shared cache.

                    handleM3Passthrough();
                    target = Locations.GetTarget(MERFileSystem.Game);
                    if (target == null)
                    {
                        var gamePath = MEDirectories.GetDefaultGamePath(MERFileSystem.Game);
                        if (Directory.Exists(gamePath))
                        {
                            target = new GameTarget(MERFileSystem.Game, gamePath, true);
                            var validationFailedReason = target.ValidateTarget();
                            if (validationFailedReason == null)
                            {
                                // CHECK NOT TEXTURE MODIFIED
                                if (target.TextureModded)
                                {
                                    MERLog.Error($@"Game target is texture modded: {target.TargetPath}. This game target is not targetable by ME2R");
                                    object o = new object();
                                    Application.Current.Dispatcher.Invoke(async () =>
                                    {
                                        if (Application.Current.MainWindow is MainWindow mw)
                                        {
                                            await mw.ShowMessageAsync("Mass Effect 2 target is texture modded", $"The game located at {target.TargetPath} has had textures modified. Mass Effect 2 Randomizer cannot randomize texture modified games, as it adds package files. If you want to texture mod your game, it must be done after randomization.", ContentWidthPercent: 75);
                                            lock (o)
                                            {
                                                Monitor.Pulse(o);
                                            }
                                        }
                                    });
                                    lock (o)
                                    {
                                        Monitor.Wait(o);
                                    }
                                }

                                // We still set target so we can restore game if necessary
                                Locations.SetTarget(target, false);
                            }
                        }
                    }


                    pd.SetMessage("Performing startup checks");
                    MERStartupCheck.PerformStartupCheck((title, message) =>
                    {
                        object o = new object();
                        Application.Current.Dispatcher.Invoke(async () =>
                        {
                            if (Application.Current.MainWindow is MainWindow mw)
                            {
                                await mw.ShowMessageAsync(title, message, ContentWidthPercent: 75);
                                lock (o)
                                {
                                    Monitor.Pulse(o);
                                }
                            }
                        });
                        lock (o)
                        {
                            Monitor.Wait(o);
                        }
                    }, x => pd.SetMessage(x));

                    // force initial refresh
                    MERPeriodicRefresh(null, null);
                }
                catch (Exception e)
                {
                    MERLog.Exception(e, @"There was an error starting up the framework!");
                }

                pd.SetMessage("Preparing interface");
                Thread.Sleep(250); // This will allow this message to show up for moment so user can see it.

                Application.Current.Dispatcher.Invoke(async () =>
                {
                    if (Application.Current.MainWindow is MainWindow mw)
                    {
                        mw.SetupTargetDescriptionText();


                        var backupStatus = BackupService.GetBackupStatus(MERFileSystem.Game);
                        mw.BackupRestoreText = backupStatus.BackupActionText;
                        mw.BackupRestore_Button.ToolTip = backupStatus.BackedUp ? "Click to restore game/uninstall randomizer mod" : "Click to backup game";

                        mw.FinalizeInterfaceLoad();

                        /*
                        if (!hasWorkingMEM)
                        {
                            await mw.ShowMessageAsync("Required components are not available",
                                "Some components for installation are not available, likely due to network issues (blocking, no internet, etc). To install these components, folow the 'How to install the Installer Support Package' directions on any of the ALOT pages on NexusMods. The installer will not work without these files installed.",
                                ContentWidthPercent: 75);
                        }*/

                        PeriodicRefresh.OnPeriodicRefresh += MERPeriodicRefresh;
                    }
                });
            };
            bw.RunWorkerCompleted += async (a, b) =>
                {
                    // Post critical startup
                    Random random = new Random();
                    var preseed = random.Next();
                    window.ImageCredits.ReplaceAll(ImageCredit.LoadImageCredits("imagecredits.txt", false));
                    window.ContributorCredits.ReplaceAll(window.GetContributorCredits());
                    window.LibraryCredits.ReplaceAll(LibraryCredit.LoadLibraryCredits("librarycredits.txt"));
#if DEBUG
                    window.SeedTextBox.Text = 529572808.ToString();
#else
                    window.SeedTextBox.Text = preseed.ToString();
#endif
                    window.TextBlock_AssemblyVersion.Text = $"Version {App.AppVersion}";
                    window.SelectedRandomizeMode = MainWindow.RandomizationMode.ERandomizationMode_SelectAny;


                    var hasFirstRun = RegistryHandler.GetRegistrySettingBool(MainWindow.SETTING_FIRSTRUN);
                    if (hasFirstRun == null || !hasFirstRun.Value)
                    {
                        window.FirstRunFlyoutOpen = true;
                    }
                    await pd.CloseAsync();
                };
            bw.RunWorkerAsync();
        }

        private static void MERPeriodicRefresh(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mw)
                {
                    // Is DLC component installed?
                    var dlcModPath = MERFileSystem.GetDLCModPath();
                    mw.DLCComponentInstalled = dlcModPath != null ? Directory.Exists(dlcModPath) : false;
                }
            });
        }

        private static void handleM3Passthrough()
        {
            if (PassthroughME1Path != null) handlePassthrough(MEGame.ME1, PassthroughME1Path);
            if (PassthroughME2Path != null) handlePassthrough(MEGame.ME2, PassthroughME2Path);
            if (PassthroughME3Path != null) handlePassthrough(MEGame.ME3, PassthroughME3Path);

            PassthroughME1Path = PassthroughME2Path = PassthroughME3Path = null;

            void handlePassthrough(MEGame game, string path)
            {
                if (path != null && Directory.Exists(path))
                {
                    GameTarget gt = new GameTarget(game, path, true, false);
                    var passThroughValidationResult = gt.ValidateTarget(false);
                    if (passThroughValidationResult != null)
                    {
                        MERLog.Error($@"{game} path passthrough failed game target validation: {passThroughValidationResult}");
                    }
                    else
                    {
                        MERLog.Information($@"Valid passthrough for game {game}. Assigning path.");
                        Locations.SetTarget(gt, false);
                    }
                }
            }
        }

        private static void RunOnUIThread(Action obj)
        {
            Application.Current.Dispatcher.Invoke(obj);
        }
    }
}