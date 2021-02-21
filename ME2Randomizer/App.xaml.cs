using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CommandLine;
using MassEffectRandomizer.Classes;
using Serilog;

namespace ME2Randomizer
{


    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal const string REGISTRY_KEY = @"Software\MassEffect2Randomizer";
        internal const string BACKUP_REGISTRY_KEY = @"Software\ALOTAddon"; //Shared. Do not change
        public static string LogDir;
        internal static string MainThemeColor = "Violet";
        private static bool POST_STARTUP = false;
        public const string DISCORD_INVITE_LINK = "https://discord.gg/s8HA6dc";
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
            LogDir = Path.Combine(Utilities.GetAppDataFolder(), "logs");
            string[] args = Environment.GetCommandLineArgs();
            Parsed<Options> parsedCommandLineArgs = null;
            string updateDestinationPath = null;

            #region Update boot
            if (args.Length > 1)
            {
                var result = Parser.Default.ParseArguments<Options>(args);
                if (result.GetType() == typeof(Parsed<Options>))
                {
                    //Parsing succeeded - have to do update check to keep logs in order...
                    parsedCommandLineArgs = (Parsed<Options>)result;
                    if (parsedCommandLineArgs.Value.UpdateDest != null)
                    {
                        //if (File.Exists(parsedCommandLineArgs.Value.UpdateDest))
                        //{
                        //    updateDestinationPath = parsedCommandLineArgs.Value.UpdateDest;
                        //}
                        //if (parsedCommandLineArgs.Value.BootingNewUpdate)
                        //{
                        //    Thread.Sleep(1000); //Delay boot to ensure update executable finishes
                        //    try
                        //    {
                        //        string updateFile = Path.Combine(exeFolder, "MassEffectRandomizer-Update.exe");
                        //        if (File.Exists(updateFile))
                        //        {
                        //            File.Delete(updateFile);
                        //            Log.Information("Deleted staged update");
                        //        }
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        Log.Warning("Unable to delete staged update: " + e.ToString());
                        //    }
                        //}
                    }
                }
            }
            #endregion

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(LogDir, $"me2rlog.txt"), rollingInterval: RollingInterval.Day, flushToDiskInterval: new TimeSpan(0, 0, 15))
#if DEBUG
      //.WriteTo.Debug()
#endif
      .CreateLogger();
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            POST_STARTUP = true;
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
            Log.Information("===========================================================================");

            string version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            Log.Information("Mass Effect 2 Randomizer " + version);
            Log.Information("Application boot: " + DateTime.UtcNow.ToString());

            #region Update mode boot
            if (updateDestinationPath != null)
            {
                Log.Information(" >> In update mode. Update destination: " + updateDestinationPath);
                int i = 0;
                while (i < 8)
                {

                    i++;
                    //try
                    //{
                    //    Log.Information("Applying update");
                    //    File.Copy(assembly.Location, updateDestinationPath, true);
                    //    Log.Information("Update applied, restarting...");
                    //    break;
                    //}
                    //catch (Exception e)
                    //{
                    //    Log.Error("Error applying update: " + e.Message);
                    //    if (i < 8)
                    //    {
                    //        Thread.Sleep(1000);
                    //        Log.Warning("Attempt #" + (i + 1));
                    //    }
                    //    else
                    //    {
                    //        Log.Fatal("Unable to apply update after 8 attempts. We are giving up.");
                    //        MessageBox.Show("Update was unable to apply. See the application log for more information. If this continues to happen please come to the ME3Tweaks discord, or download a new copy from GitHub.");
                    //        Environment.Exit(1);
                    //    }
                    //}
                }
                Log.Information("Rebooting into normal mode to complete update");
                ProcessStartInfo psi = new ProcessStartInfo(updateDestinationPath);
                psi.WorkingDirectory = updateDestinationPath;
                psi.Arguments = "--completing-update";
                Process.Start(psi);
                Environment.Exit(0);
                Current.Shutdown();
            }
            #endregion
            System.Windows.Controls.ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(Control),
           new FrameworkPropertyMetadata(true));

            //try
            //{
            //    var application = new App();
            //    application.InitializeComponent();
            //    application.Run();
            //}
            //catch (Exception e)
            //{
            //    OnFatalCrash(e);
            //    throw e;
            //}
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
            //Log.Information("Forcing beta mode off before exiting...");
            //Utilities.WriteRegistryKey(Registry.CurrentUser, AlotAddOnGUI.MainWindow.REGISTRY_KEY, AlotAddOnGUI.MainWindow.SETTINGSTR_BETAMODE, 0);
            //File.Create(Utilities.GetAppCrashFile());
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
                File.WriteAllText(Path.Combine(Utilities.GetAppDataFolder(), "FATAL_STARTUP_CRASH.txt"), errorMessage);
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
    }


    class Options
    {
        [Option('u', "update-dest-path",
          HelpText = "Indicates where this booting instance of Mass Effect 2 Randomizer should attempt to copy itself and reboot to")]
        public string UpdateDest { get; set; }

        [Option('c', "completing-update",
            HelpText = "Indicates that we are booting a new copy of Mass Effect 2 Randomizer that has just been upgraded")]
        public bool BootingNewUpdate { get; set; }
    }
}

