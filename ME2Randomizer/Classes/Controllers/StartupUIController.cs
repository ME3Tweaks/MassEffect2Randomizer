using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LegendaryExplorerCore;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using MahApps.Metro.Controls.Dialogs;
using ME3TweaksCore;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Services.Backup;
using ME3TweaksCore.Targets;
using Microsoft.AppCenter;
using Randomizer.MER;
using Randomizer.Randomizers;
using RandomizerUI.Classes.Telemetry;
using Serilog;

namespace RandomizerUI.Classes.Controllers
{
    public class StartupUIController
    {
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
            //RegistryHandler.RegistrySettingsPath = @"HKEY_CURRENT_USER\Software\MassEffect2Randomizer";
            //RegistryHandler.CurrentUserRegistrySubpath = @"Software\MassEffect2Randomizer";
            LegendaryExplorerCoreLib.SetSynchronizationContext(TaskScheduler.FromCurrentSynchronizationContext());

            try
            {
                // This is in a try catch because this is a critical no-crash zone that is before launch
                window.Title = $"{MERUI.GetRandomizerName()} {MLibraryConsumer.GetAppVersion()}";
            }
            catch { }

            if (MLibraryConsumer.GetExecutablePath().StartsWith(Path.GetTempPath(), StringComparison.InvariantCultureIgnoreCase))
            {
                // Running from temp! This is not allowed
                await window.ShowMessageAsync("Cannot run from temp directory", $"{MERUI.GetRandomizerName()} cannot be run from the system's Temp directory. If this executable was run from within an archive, it needs to be extracted first.");
                Environment.Exit(1);
            }

            var pd = await window.ShowProgressAsync("Starting up", $"{MERUI.GetRandomizerName()} is starting up. Please wait.");
            pd.SetIndeterminate();
            NamedBackgroundWorker bw = new NamedBackgroundWorker("StartupThread");
            bw.DoWork += (a, b) =>
            {
                // Setup telemetry handlers
                TelemetryInterposer.SetEventCallback(TelemetryController.TrackEvent);
                TelemetryInterposer.SetErrorCallback(TelemetryController.TrackError);

                // Initialize core libraries
                ME3TweaksCoreLib.Initialize(RunOnUIThread, MERLog.CreateLogger);
                //ALOTInstallerCoreLib.Startup(SetWrapperLogger, RunOnUIThread, startTelemetry, stopTelemetry, $"Mass Effect 2 Randomizer {App.AppVersion} starting up", false);
                // Logger is now available


                // Setup the InteropPackage for the update check
                #region Update interop
                CancellationTokenSource ct = new CancellationTokenSource();

                AppUpdateInteropPackage interopPackage = new AppUpdateInteropPackage()
                {
                    // TODO: UPDATE THIS
                    GithubOwner = MERUpdater.GetGithubOwner(),
                    GithubReponame = MERUpdater.GetGithubRepoName(),
                    UpdateAssetPrefix = MERUpdater.GetGithubAssetPrefix(),
                    UpdateFilenameInArchive = MERUpdater.GetExpectedExeName(),
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
                    ApplicationName = MERUI.GetRandomizerName(),
                    RequestHeader = MERUI.GetRandomizerName().Replace(" ",""),
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
                    pd.SetMessage($"Loading {MERUI.GetRandomizerName()} framework");
                    ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(Control), new FrameworkPropertyMetadata(true));
                    ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

                    //ALOTInstallerCoreLib.PostCriticalStartup(x => pd.SetMessage(x), RunOnUIThread, false);

                    MEPackageHandler.GlobalSharedCacheEnabled = false; // ME2R does not use the global shared cache.

                    handleM3Passthrough();
                    foreach (var game in Locations.SupportedGames)
                    {
                        target = Locations.GetTarget(game.IsLEGame());
                        if (target == null)
                        {
                            var gamePath = MEDirectories.GetDefaultGamePath(game);
                            if (Directory.Exists(gamePath))
                            {
                                target = new GameTarget(game, gamePath, true);
                                var validationFailedReason = target.ValidateTarget();
                                if (validationFailedReason == null)
                                {
                                    // CHECK NOT TEXTURE MODIFIED
                                    if (target.TextureModded)
                                    {
                                        MERLog.Error($@"Game target is texture modded: {target.TargetPath}. This game target is not targetable");
                                        object o = new object();
                                        Application.Current.Dispatcher.Invoke(async () =>
                                        {
                                            if (Application.Current.MainWindow is MainWindow mw)
                                            {
                                                await mw.ShowMessageAsync("Mass Effect 2 target is texture modded", $"The game located at {target.TargetPath} has had textures modified. {MERUI.GetRandomizerName()} cannot randomize texture modified games, as it adds package files. If you want to texture mod your game, it must be done after randomization.", ContentWidthPercent: 75);
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
                                    Locations.SetTarget(target);
                                }
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
                        mw.FinalizeInterfaceLoad();
                    }
                });
            };
            bw.RunWorkerCompleted += async (a, b) =>
                {
                    // Post critical startup
                    window.SelectableTargets.AddRange(Locations.GetAllAvailableTargets());

                    // Initial selected game
                    if (Locations.GetTarget(true) != null)
                    {
                        window.SelectedTarget = Locations.GetTarget(true);
                        window.LEGameRadioButton.IsChecked = true;
                    }
                    else if (Locations.GetTarget(false) != null)
                    {
                        window.SelectedTarget = Locations.GetTarget(false);
                        window.OTGameRadioButton.IsChecked = true;
                    }

                    // Disable games not installed or found
                    window.LEGameRadioButton.IsEnabled = Locations.GetTarget(true) != null;
                    window.OTGameRadioButton.IsEnabled = Locations.GetTarget(false) != null;

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
                    window.TextBlock_AssemblyVersion.Text = $"Version {MLibraryConsumer.GetAppVersion()}";
                    window.SelectedRandomizeMode = RandomizationMode.ERandomizationMode_SelectAny;


                    if (MERSettings.GetSettingBool(ESetting.SETTING_FIRSTRUN))
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
                    Debug.WriteLine("PERIODIC REFRESH NOT IMPLEMENTED");
                    // Is DLC component installed?
                    //var dlcModPath = MERFileSystem.GetDLCModPath();
                    //mw.DLCComponentInstalled = dlcModPath != null ? Directory.Exists(dlcModPath) : false;
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
                        Locations.SetTarget(gt);
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