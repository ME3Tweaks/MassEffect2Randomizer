using ByteSizeLib;
using Flurl.Http;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MassEffectRandomizer.Classes;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MassEffectRandomizer
{
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public string WatermarkText { get; set; }

        public void InitLogUploaderUI()
        {
            Combobox_LogSelector.Items.Clear();
            var directory = new DirectoryInfo(App.LogDir);
            var logfiles = directory.GetFiles("applog*.txt").OrderByDescending(f => f.LastWriteTime).ToList();
            foreach (var file in logfiles)
            {
                Combobox_LogSelector.Items.Add(new LogItem(file.FullName));
            }

            if (Combobox_LogSelector.Items.Count > 0)
            {
                Combobox_LogSelector.SelectedIndex = 0;
            }
        }


        public string GetSelectedLogText(string logpath)
        {
            string temppath = logpath + ".tmp";
            File.Copy(logpath, temppath);
            string log = File.ReadAllText(temppath);
            File.Delete(temppath);

            string eventAndCrashLogs = GetLogsForAppending();
            string diagnostics = GetSystemInfo();
            return log + "\n[STARTOFDIAG]\n" + diagnostics + "\n" + eventAndCrashLogs + "\n";
        }

        private string GetSystemInfo()
        {
            string gamePath = Utilities.GetGamePath();
            StringBuilder diagBuilder = new StringBuilder();
            diagBuilder.AppendLine("Mass Effect Randomizer " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version + " Game Diagnostic & log");
            diagBuilder.AppendLine("Diagnostic generated on " + DateTime.Now.ToShortDateString());
            diagBuilder.AppendLine("System culture: " + Thread.CurrentThread.CurrentCulture.Name);
            diagBuilder.AppendLine("Game is installed at " + Utilities.GetGamePath());
            string pathroot = Path.GetPathRoot(gamePath);
            pathroot = pathroot.Substring(0, 1);
            if (pathroot == @"\")
            {
                diagBuilder.AppendLine("Installation appears to be on a network drive (first character in path is \\)");
            }
            else
            {
                if (Utilities.IsWindows10OrNewer())
                {
                    int backingType = Utilities.GetPartitionDiskBackingType(pathroot);
                    string type = "Unknown type";
                    switch (backingType)
                    {
                        case 3:
                            type = "Hard disk drive";
                            break;
                        case 4:
                            type = "Solid state drive";
                            break;
                        default:
                            type += ": " + backingType;
                            break;
                    }

                    diagBuilder.AppendLine("Installed on disk type: " + type);
                }
            }

            try
            {
                ALOTVersionInfo avi = Utilities.GetInstalledALOTInfo();

                string exePath = Utilities.GetGameEXEPath();
                if (File.Exists(exePath))
                {
                    var versInfo = FileVersionInfo.GetVersionInfo(exePath);
                    diagBuilder.AppendLine("===Executable information");
                    diagBuilder.AppendLine("Version: " + versInfo.FileMajorPart + "." + versInfo.FileMinorPart + "." + versInfo.FileBuildPart + "." + versInfo.FilePrivatePart);

                    var hash = Utilities.CalculateMD5(Utilities.GetGameEXEPath());
                    diagBuilder.AppendLine("[EXEHASH-1]" + hash);
                    var HASH_SUPPORTED = Utilities.CheckIfHashIsSupported(hash);
                    Tuple<bool, string> exeInfo = Utilities.GetRawGameSourceByHash(hash);
                    if (exeInfo.Item1)
                    {
                        diagBuilder.AppendLine("$$$" + exeInfo.Item2);
                    }
                    else
                    {
                        diagBuilder.AppendLine("[ERROR]" + exeInfo.Item2);
                    }

                    string d3d9file = Path.GetDirectoryName(exePath) + "\\d3d9.dll";
                    if (File.Exists(d3d9file))
                    {
                        diagBuilder.AppendLine("~~~d3d9.dll exists - External dll is hooking via DirectX into game process");
                    }

                    string fpscounter = Path.GetDirectoryName(exePath) + @"\fpscounter\fpscounter.dll";
                    if (File.Exists(fpscounter))
                    {
                        diagBuilder.AppendLine("~~~fpscounter.dll exists - FPS Counter plugin detected");
                    }

                    string dinput8 = Path.GetDirectoryName(exePath) + "\\dinput8.dll";
                    if (File.Exists(dinput8))
                    {
                        diagBuilder.AppendLine("~~~dinput8.dll exists - External dll is hooking via input dll into game process");
                    }
                }


                diagBuilder.AppendLine("===System information");
                OperatingSystem os = Environment.OSVersion;
                Version osBuildVersion = os.Version;

                //Windows 10 only
                string releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
                string productName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString();
                string verLine = "Running " + productName;
                if (osBuildVersion.Major == 10)
                {
                    verLine += " " + releaseId;
                }

                diagBuilder.AppendLine(verLine);
                diagBuilder.AppendLine("Version " + osBuildVersion);
                diagBuilder.AppendLine("");
                diagBuilder.AppendLine("$$$Processors");
                diagBuilder.AppendLine(Utilities.GetCPUString());
                long ramInBytes = Utilities.GetInstalledRamAmount();
                diagBuilder.AppendLine("$$$System Memory: " + ByteSize.FromKiloBytes(ramInBytes));
                if (ramInBytes == 0)
                {
                    diagBuilder.AppendLine("~~~Unable to get the read amount of physically installed ram. This may be a sign of impending hardware failure in the SMBIOS");
                }

                ManagementObjectSearcher objvide = new ManagementObjectSearcher("select * from Win32_VideoController");
                int vidCardIndex = 1;
                foreach (ManagementObject obj in objvide.Get())
                {
                    diagBuilder.AppendLine("");
                    diagBuilder.AppendLine("$$$Video Card " + vidCardIndex);
                    diagBuilder.AppendLine("Name: " + obj["Name"]);

                    //Get Memory
                    string vidKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\";
                    vidKey += (vidCardIndex - 1).ToString().PadLeft(4, '0');
                    object returnvalue = null;
                    try
                    {
                        returnvalue = Registry.GetValue(vidKey, "HardwareInformation.qwMemorySize", 0L);
                    }
                    catch (Exception ex)
                    {
                        diagBuilder.AppendLine("~~~Warning: Unable to read memory size from registry. Reading from WMI instead (" + ex.GetType().ToString() + ")");
                    }

                    string displayVal = "Unable to read value from registry";
                    if (returnvalue != null && (long)returnvalue != 0)
                    {
                        displayVal = ByteSize.FromBytes((long)returnvalue).ToString();
                    }
                    else
                    {
                        try
                        {
                            UInt32 wmiValue = (UInt32)obj["AdapterRam"];
                            displayVal = ByteSize.FromBytes((long)wmiValue).ToString();
                            if (displayVal == "4GB" || displayVal == "4 GB")
                            {
                                displayVal += " (possibly more, variable is 32-bit unsigned)";
                            }
                        }
                        catch (Exception)
                        {
                            displayVal = "Unable to read value from registry/WMI";

                        }
                    }

                    diagBuilder.AppendLine("Memory: " + displayVal);
                    diagBuilder.AppendLine("DriverVersion: " + obj["DriverVersion"]);
                    vidCardIndex++;
                }
            }
            catch (Exception e)
            {
                diagBuilder.AppendLine("[ERROR] Error getting system information: " + App.FlattenException(e));
            }

            return diagBuilder.ToString();
        }

        private void Combobox_LogSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WatermarkText = Combobox_LogSelector.SelectedIndex == 0 ? "Latest log" : "Older log";
        }

        private void LogUploaderFlyout_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (LogUploaderFlyout.IsOpen)
            {
                InitLogUploaderUI();
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current,
                    ThemeManager.GetAccent("Cyan"),
                    ThemeManager.GetAppTheme("BaseDark")); // or appStyle.Item1
            }
            else
            {
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current,
                    ThemeManager.GetAccent(App.MainThemeColor),
                    ThemeManager.GetAppTheme("BaseDark")); // or appStyle.Item1
            }
        }

        private void Button_CancelLog_Click(object sender, RoutedEventArgs e)
        {
            LogUploaderFlyoutOpen = false;
        }

        private async Task<string> UploadLog(bool isPreviousCrashLog, string logfile, bool openPageWhenFinished = true)
        {
            BackgroundWorker bw = new BackgroundWorker();
            string outfile = Path.Combine(Utilities.GetAppDataFolder(), "logfile_forUpload.lzma");
            byte[] lzmalog = null;
            string randomizerVer = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            ProgressDialogController progresscontroller = await this.ShowProgressAsync("Collecting logs", "Mass Effect Randomizer is currently collecting log information, please wait...", true);
            progresscontroller.SetIndeterminate();

            bw.DoWork += (a, b) =>
            {
                Log.Information("Collecting log information...");
                if (logfile == null)
                {
                    //latest
                    var directory = new DirectoryInfo(App.LogDir);
                    var logfiles = directory.GetFiles("applog*.txt").OrderByDescending(f => f.LastWriteTime).ToList();
                    if (logfiles.Count() > 0)
                    {
                        logfile = logfiles.ElementAt(0).FullName;
                    }
                    else
                    {
                        Log.Information("No logs available, somehow. Canceling upload");
                        return;
                    }
                }
                string log = GetSelectedLogText(logfile);
                //var lzmaExtractedPath = Path.Combine(Path.GetTempPath(), "lzma.exe");

                ////Extract LZMA so we can compress log for upload
                //using (Stream stream = Utilities.GetResourceStream("MassEffectRandomizer.staticfiles.lzma.exe"))
                //{
                //    using (var file = new FileStream(lzmaExtractedPath, FileMode.Create, FileAccess.Write))
                //    {
                //        stream.CopyTo(file);
                //    }
                //}

                var lzmaExtractedPath = Path.Combine(Utilities.GetAppDataFolder(), "executables", "lzma.exe");


                

                string zipStaged = Path.Combine(Utilities.GetAppDataFolder(), "logfile_forUpload");
                File.WriteAllText(zipStaged, log);

                //Compress with LZMA for VPS Upload
                string args = "e \"" + zipStaged + "\" \"" + outfile + "\" -mt2";
                Utilities.runProcess(lzmaExtractedPath, args);
                File.Delete(zipStaged);
                File.Delete(lzmaExtractedPath);
                lzmalog = File.ReadAllBytes(outfile);
                File.Delete(outfile);
                Log.Information("Finishing log collection thread");
            };

            bw.RunWorkerCompleted += async (a, b) =>
            {
                progresscontroller.SetTitle("Uploading log");
                progresscontroller.SetMessage("Uploading log to ME3Tweaks log viewer, please wait...");
                try
                {
                    var responseString = await "https://me3tweaks.com/masseffectrandomizer/logservice/logupload.php".PostUrlEncodedAsync(new { LogData = Convert.ToBase64String(lzmalog), MassEffectRandomizerVersion = randomizerVer, Type = "log", CrashLog = isPreviousCrashLog }).ReceiveString();
                    Uri uriResult;
                    bool result = Uri.TryCreate(responseString, UriKind.Absolute, out uriResult)
                                  && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                    if (result)
                    {
                        //should be valid URL.
                        //diagnosticsWorker.ReportProgress(0, new ThreadCommand(SET_DIAGTASK_ICON_GREEN, Image_Upload));
                        //e.Result = responseString;
                        await progresscontroller.CloseAsync();
                        Log.Information("Result from server for log upload: " + responseString);
                        if (openPageWhenFinished)
                        {
                            Utilities.OpenWebPage(responseString);
                        }
                    }
                    else
                    {
                        File.Delete(outfile);

                        //diagnosticsWorker.ReportProgress(0, new ThreadCommand(SET_DIAG_TEXT, "Error from oversized log uploader: " + responseString));
                        //diagnosticsWorker.ReportProgress(0, new ThreadCommand(SET_DIAGTASK_ICON_RED, Image_Upload));
                        await progresscontroller.CloseAsync();
                        Log.Error("Error uploading log. The server responded with: " + responseString);
                        //e.Result = "Diagnostic complete.";
                        await this.ShowMessageAsync("Log upload error", "The server rejected the upload. The response was: " + responseString);
                        //Utilities.OpenAndSelectFileInExplorer(diagfilename);
                    }
                }
                catch (FlurlHttpTimeoutException)
                {
                    // FlurlHttpTimeoutException derives from FlurlHttpException; catch here only
                    // if you want to handle timeouts as a special case
                    await progresscontroller.CloseAsync();
                    Log.Error("Request timed out while uploading log.");
                    await this.ShowMessageAsync("Log upload timed out", "The log took too long to upload. You will need to upload your log manually.");

                }
                catch (Exception ex)
                {
                    // ex.Message contains rich details, inclulding the URL, verb, response status,
                    // and request and response bodies (if available)
                    await progresscontroller.CloseAsync();
                    Log.Error("Handled error uploading log: " + Utilities.FlattenException(ex));
                    string exmessage = ex.Message;
                    var index = exmessage.IndexOf("Request body:");
                    if (index > 0)
                    {
                        exmessage = exmessage.Substring(0, index);
                    }

                    await this.ShowMessageAsync("Log upload failed", "The log was unable to upload. The error message is: " + exmessage + "You will need to upload your log manually.");
                }
                Log.Information("Finishing log upload");
                LogUploaderFlyoutOpen = false;

            };
            bw.RunWorkerAsync();
            return ""; //Async requires this
        }


        private async void Button_SelectLog_Click(object sender, RoutedEventArgs e)
        {
            await UploadLog(false, ((LogItem)Combobox_LogSelector.SelectedValue).filepath);
        }

        private string GetLogsForAppending()
        {
            //GET LOGS
            StringBuilder crashLogs = new StringBuilder();
            var sevenDaysAgo = DateTime.Now.AddDays(-7);


            //Get event logs
            EventLog ev = new EventLog("Application");
            List<EventLogEntry> entries = ev.Entries
                .Cast<EventLogEntry>()
                .Where(z => z.InstanceId == 1001 && z.TimeGenerated > sevenDaysAgo && (GenerateLogString(z).Contains("MassEffect2.exe") || GenerateLogString(z).Contains("ME2Game.exe")))
                .ToList();

            if (entries.Count > 0)
            {
                string cutoffStr = "Attached files:";
                crashLogs.AppendLine("[STARTOFEVENTS]");
                crashLogs.AppendLine("===Mass Effect crash logs found in Event Viewer");
                foreach (var entry in entries)
                {
                    string str = string.Join("\n", GenerateLogString(entry).Split('\n').ToList().Take(17).ToList());
                    crashLogs.AppendLine($"!!!MassEffect2.exe Event Log {entry.TimeGenerated}\n{str}");
                }
                crashLogs.AppendLine("===Mass Effect 2 crash logs found in Event Viewer");
            }
            else
            {
                crashLogs.AppendLine("No crash events found in Event Viewer");
                crashLogs.AppendLine("===Mass Effect 2 crash logs found in Event Viewer");
            }

            return crashLogs.ToString();
        }

        public string GenerateLogString(EventLogEntry CurrentEntry) => $"Event type: {CurrentEntry.EntryType.ToString()}\nEvent Message: {CurrentEntry.Message + CurrentEntry}\nEvent Time: {CurrentEntry.TimeGenerated.ToShortTimeString()}\nEvent {CurrentEntry.UserName}\n";
    }

    class LogItem
    {
        public string filepath;
        public LogItem(string filepath)
        {
            this.filepath = filepath;
        }

        public override string ToString()
        {
            return System.IO.Path.GetFileName(filepath) + " - " + ByteSize.FromBytes(new FileInfo(filepath).Length);
        }
    }
}
