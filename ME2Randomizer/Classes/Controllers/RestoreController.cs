using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using MahApps.Metro.Controls.Dialogs;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Services.Backup;
using ME3TweaksCore.Services.Restore;
using ME3TweaksCore.Targets;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using Randomizer.MER;
using RandomizerUI.Classes.Randomizers.Utility;

namespace RandomizerUI.Classes.Controllers
{
    class RestoreController
    {
        public static async void StartRestore(MainWindow mw, GameTarget target, bool isQuick, Action postRestoreDelegate = null)
        {
            var pd = await mw.ShowProgressAsync("Restoring game", "Preparing to restore game");
            pd.SetIndeterminate();
            await Task.Run(() =>
            {

                if (isQuick)
                {
                    // Nuke the DLC
                    MERLog.Information(@"Quick restore started");
                    pd.SetMessage("Removing randomize DLC component");
                    var dlcModPath = MERFileSystem.GetDLCModPath(target);
                    if (Directory.Exists(dlcModPath))
                    {
                        MERLog.Information($@"Deleting {dlcModPath}");
                        MUtilities.DeleteFilesAndFoldersRecursively(dlcModPath);
                    }

                    mw.DLCComponentInstalled = false;


                    // Restore basegame only files
                    pd.SetMessage("Restoring randomized basegame files");

                    var backupPath = BackupService.GetGameBackupPath(target.Game, false);
                    var gameCookedPath = M3Directories.GetCookedPath(target);
                    var backupCookedPath = MEDirectories.GetCookedPath(target.Game, backupPath);
                    foreach (var bgf in EntryImporter.FilesSafeToImportFrom(target.Game))
                    {
                        var srcPath = Path.Combine(backupCookedPath, bgf);
                        if (File.Exists(srcPath))
                        {
                            var destPath = Path.Combine(gameCookedPath, bgf);
                            MERLog.Information($@"Restoring {bgf}");
                            File.Copy(srcPath, destPath, true);
                        }
                        else
                        {
                            Debug.WriteLine($@"Skipping in quick restore: {srcPath}");
                        }
                    }

#if __GAME2__
                    var isControllerModInstalled = target.Game.IsOTGame() && Randomizer.Randomizers.Game2.Misc.SFXGame.IsControllerBasedInstall(target);
                    if (isControllerModInstalled)
                    {
                        // We must also restore Coalesced.ini or it will reference a UI that is no longer available and game will not boot
                        MERLog.Information(@"Controller based install detected, also restoring Coalesced.ini to prevent startup crash");
                        File.Copy(Path.Combine(backupPath, "BioGame", "Config", "PC", "Cooked", "Coalesced.ini"), Path.Combine(target.TargetPath, "BioGame", "Config", "PC", "Cooked", "Coalesced.ini"), true);
                    }
#endif
                    // Delete basegame TFC
                    var baseTFC = MERFileSystem.GetTFCPath(target, false);
                    if (File.Exists(baseTFC))
                    {
                        File.Delete(baseTFC);
                    }

                    // Done!
                }
                else
                {

                    // Full restore
                    MERLog.Information($@"Performing full game restore on {target.TargetPath} target after restore");

                    object syncObj = new object();
                    var gr = new GameRestore(target.Game)
                    {
                        ConfirmationCallback = (title, message) =>
                        {
                            bool response = false;
                            Application.Current.Dispatcher.Invoke(async () =>
                            {
                                response = await mw.ShowMessageAsync(title, message,
                                    MessageDialogStyle.AffirmativeAndNegative,
                                    new MetroDialogSettings()
                                    {
                                        AffirmativeButtonText = "OK",
                                        NegativeButtonText = "Cancel",
                                    }) == MessageDialogResult.Affirmative;
                                lock (syncObj)
                                {
                                    Monitor.Pulse(syncObj);
                                }
                            });
                            lock (syncObj)
                            {
                                Monitor.Wait(syncObj);
                            }

                            return response;
                        },
                        BlockingErrorCallback = (title, message) => { Application.Current.Dispatcher.Invoke(async () => { await mw.ShowMessageAsync(title, message); }); },
                        RestoreErrorCallback = (title, message) => { Application.Current.Dispatcher.Invoke(async () => { await mw.ShowMessageAsync(title, message); }); },
                        UpdateStatusCallback = message =>
                            Application.Current.Dispatcher.Invoke(() => pd.SetMessage(message)),
                        UpdateProgressCallback = (done, total) =>
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                pd.SetProgress(done * 1d / total);
                                if (total != 0)
                                {
                                    TaskbarHelper.SetProgressState(TaskbarProgressBarState.Normal);
                                    TaskbarHelper.SetProgress(done * 1.0 / total);
                                }
                            }),
                        SetProgressIndeterminateCallback = indeterminate => Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (indeterminate) pd.SetIndeterminate();
                            TaskbarHelper.SetProgressState(indeterminate ? TaskbarProgressBarState.Indeterminate : TaskbarProgressBarState.Normal);
                        }),
                        SelectDestinationDirectoryCallback = (title, message) =>
                        {
                            string selectedPath = null;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Not sure if this has to be synced
                                CommonOpenFileDialog ofd = new CommonOpenFileDialog()
                                {
                                    Title = "Select restore destination directory",
                                    IsFolderPicker = true,
                                    EnsurePathExists = true
                                };
                                if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
                                {
                                    selectedPath = ofd.FileName;
                                }
                            });
                            return selectedPath;
                        }
                    };
                    gr.PerformRestore(target, target.TargetPath);
                    mw.DLCComponentInstalled = false;
                    MERLog.Information(@"Reloading target after restore");
                    target.ReloadGameTarget(false, false);
                    mw.SetupTargetDescriptionText();
                }
            }).ContinueWithOnUIThread(async x =>
            {
                TaskbarHelper.SetProgressState(TaskbarProgressBarState.NoProgress);
                await pd.CloseAsync();
                postRestoreDelegate?.Invoke();
            });
        }
    }
}