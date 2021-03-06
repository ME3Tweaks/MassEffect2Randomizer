using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ALOTInstallerCore.Helpers;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.Utility
{
    /// <summary>
    /// Interposer used to prefix log messages with their source component. Call only from MER code
    /// </summary>
    public static class MERLog
    {
        private const string Prefix = "ME2R";
        public static void Exception(Exception exception, string preMessage)
        {
            var prefix = $"[{ Prefix}] ";
            Log.Error($"{prefix}{preMessage}");

            // Log exception
            while (exception != null)
            {
                var line1 = exception.GetType().Name + ": " + exception.Message;
                foreach (var line in line1.Split("\n"))
                {
                    Log.Error(prefix + line);
                }

                if (exception.StackTrace != null)
                {
                    foreach (var line in exception.StackTrace.Split("\n"))
                    {
                        Log.Error(prefix + line);
                    }
                }

                exception = exception.InnerException;
            }
        }

        public static void Information(string message)
        {
            var prefix = $"[{ Prefix}] ";
            Log.Information($"{prefix}{message}");
        }

        public static void Warning(string message)
        {
            var prefix = $"[{ Prefix}] ";
            Log.Warning($"{prefix}{message}");
        }

        public static void Error(string message)
        {
            var prefix = $"[{ Prefix}] ";
            Log.Error($"{prefix}{message}");
        }

        public static void Fatal(string message)
        {
            var prefix = $"[{ Prefix}] ";
            Log.Fatal($"{prefix}{message}");
        }

        public static void Debug(string message)
        {
            var prefix = $"[{ Prefix}] ";
            Log.Debug($"{prefix}{message}");
        }
    }
}
