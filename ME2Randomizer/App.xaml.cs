using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using CommandLine;
using RandomizerUI.Classes;
using RandomizerUI.Classes.Controllers;
using Serilog;

namespace RandomizerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal const string REGISTRY_KEY = @"Software\MassEffect2Randomizer";
        internal const string BACKUP_REGISTRY_KEY = @"Software\ALOTAddon"; //Shared. Do not change

        private static bool POST_STARTUP = false;
        public const string DISCORD_INVITE_LINK = "https://discord.gg/s8HA6dc";

#if DEBUG
        public static bool IsDebug => true;
#else
        public static bool IsDebug => false;
#endif
        public static Visibility IsDebugVisibility => IsDebug ? Visibility.Visible : Visibility.Collapsed;
        public static bool BetaAvailable { get; set; }

        public static string AppVersion
        {
            get
            {
                Version assemblyVersion = Assembly.GetEntryAssembly().GetName().Version;
                string version = $@"{assemblyVersion.Major}.{assemblyVersion.Minor}";
                if (assemblyVersion.Revision != 0 || assemblyVersion.Build != 0)
                {
                    version += @"." + assemblyVersion.Build;
                    if (assemblyVersion.Revision != 0)
                    {
                        version += @"." + assemblyVersion.Revision;
                    }
                }

                return version;
            }
        }

        [STAThread]
        public static void Main()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            try
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();
            }
            catch (Exception e)
            {
                OnFatalCrash(e);
                throw;
            }
        }

        public App() : base()
        {
            handleCommandLine();

            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            POST_STARTUP = true;
        }

        /// <summary>
        /// Called when an unhandled exception occurs. This method can only be invoked after startup has completed. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Exception to process</param>
        static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("Mass Effect 2 Randomizer has crashed! This is the exception that caused the crash:");
            string st = FlattenException(e.Exception);
            Log.Fatal(errorMessage);
            Log.Fatal(st);
        }

        /// <summary>
        /// Called when a fatal crash occurs. Only does something if startup has not completed.
        /// </summary>
        /// <param name="e">The fatal exception.</param>
        public static void OnFatalCrash(Exception e)
        {
            if (!POST_STARTUP)
            {
                string errorMessage = string.Format("Mass Effect 2 Randomizer has encountered a fatal startup crash:\n" + FlattenException(e));
                File.WriteAllText(Path.Combine(MERUtilities.GetAppDataFolder(), "FATAL_STARTUP_CRASH.txt"), errorMessage);
            }
        }

        /// <summary>
        /// Flattens an exception into a printable string
        /// </summary>
        /// <param name="exception">Exception to flatten</param>
        /// <returns>Printable string</returns>
        public static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.GetType().Name + ": " + exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }

        private void handleCommandLine()
        {

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                var result = Parser.Default.ParseArguments<Options>(args);
                if (result is Parsed<Options> parsedCommandLineArgs)
                {
                    //Parsing completed
                    if (parsedCommandLineArgs.Value.UpdateBoot)
                    {
                        //Update unpacked and process was run.
                        // Exit the process as we have completed the extraction process for single file .net core
                        Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                        return;
                    }

                    if (parsedCommandLineArgs.Value.UpdateRebootDest != null)
                    {
                        Log.Logger = LogCollector.CreateLogger();
                        Log.Information(LogCollector.SessionStartString);
                        copyAndRebootUpdate(parsedCommandLineArgs.Value.UpdateRebootDest);
                        return;
                    }

                    // Set passthroughs
                    if (parsedCommandLineArgs.Value.PassthroughME1Path != null)
                    {
                        StartupUIController.PassthroughME1Path = parsedCommandLineArgs.Value.PassthroughME1Path;
                    }

                    if (parsedCommandLineArgs.Value.PassthroughME2Path != null)
                    {
                        StartupUIController.PassthroughME2Path = parsedCommandLineArgs.Value.PassthroughME2Path;
                    }

                    if (parsedCommandLineArgs.Value.PassthroughME3Path != null)
                    {
                        StartupUIController.PassthroughME3Path = parsedCommandLineArgs.Value.PassthroughME3Path;
                    }
                }
                else
                {
                    Log.Error("Could not parse command line arguments! Args: " + string.Join(' ', args));
                }
            }
        }

        #region Updates

        /// <summary>
        /// V4 update reboot and swap
        /// </summary>
        /// <param name="updateRebootDest"></param>
        private void copyAndRebootUpdate(string updateRebootDest)
        {
            Thread.Sleep(2000); //SLEEP WHILE WE WAIT FOR PARENT PROCESS TO STOP.
            Log.Information("In update mode. Update destination: " + updateRebootDest);
            int i = 0;
            while (i < 5)
            {
                i++;
                try
                {
                    Log.Information("Applying update");
                    if (File.Exists(updateRebootDest)) File.Delete(updateRebootDest);
                    File.Copy(Utilities.GetExecutablePath(), updateRebootDest);
                    ProcessStartInfo psi = new ProcessStartInfo(updateRebootDest)
                    {
                        WorkingDirectory = Directory.GetParent(updateRebootDest).FullName
                    };
                    Process.Start(psi);
                    Environment.Exit(0);
                    break;
                }
                catch (Exception e)
                {
                    Log.Error("Error applying update: " + e.Message);
                    if (i < 5)
                    {
                        Thread.Sleep(1000);
                        Log.Information("Attempt #" + (i + 1));
                    }
                    else
                    {
                        Log.Fatal("Unable to apply update after 5 attempts. We are giving up.");
                        MessageBox.Show($"Update was unable to apply. The last error message was {e.Message}.\nSee the logs directory in {LogCollector.LogDir} for more information.\n\nUpdate file: {Utilities.GetExecutablePath()}\nDestination file: {updateRebootDest}\n\nIf this continues to happen please come to the ME3Tweaks discord or download a new release from GitHub.");
                        Environment.Exit(1);
                    }
                }
            }

            #endregion
        }

        class Options
        {
            [Option("update-dest-path",
                HelpText = "Copies this program's executable to the specified location, runs the new executable, and then exits this process.")]
            public string UpdateRebootDest { get; private set; }

            [Option("me1path",
                HelpText = "Sets the path for Mass Effect on app boot. It must point to the game root directory.")]
            public string PassthroughME1Path { get; private set; }

            [Option("me2path",
                HelpText = "Sets the path for Mass Effect 2 on app boot. It must point to the game root directory.")]
            public string PassthroughME2Path { get; private set; }

            [Option("me3path",
                HelpText = "Sets the path for Mass Effect 3 on app boot. It must point to the game root directory.")]
            public string PassthroughME3Path { get; private set; }

            [Option("update-boot",
                HelpText = "Indicates that the process should run in update mode for a single file .net core executable. The process will exit upon starting because the platform extraction process will have completed.")]
            public bool UpdateBoot { get; private set; }

        }
    }
}