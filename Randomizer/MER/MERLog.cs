﻿using System;
using System.IO;
using ME3TweaksCore.Helpers;
using Serilog;
using Serilog.Sinks.RollingFile.Extension;

namespace Randomizer.MER
{
    /// <summary>
    /// Interposer used to prefix MERLog messages with their source component. Call only from MER code
    /// </summary>
    public static class MERLog
    {
#if __GAME1__
        private const string Prefix = "MER";
#elif __GAME2__
        private const string Prefix = "ME2R";
#elif __GAME3__
        private const string Prefix = "ME3R";
#endif
        public static void Exception(Exception exception, string preMessage, bool fatal = false)
        {
            var prefix = $"[{Prefix}] ";
            Log.Error($"{prefix}{preMessage}");

            // MERLog exception
            while (exception != null)
            {
                var line1 = exception.GetType().Name + ": " + exception.Message;
                foreach (var line in line1.Split("\n"))
                {
                    if (fatal)
                        Log.Fatal(prefix + line);
                    else
                        Log.Error(prefix + line);

                }

                if (exception.StackTrace != null)
                {
                    foreach (var line in exception.StackTrace.Split("\n"))
                    {
                        if (fatal)
                            Log.Fatal(prefix + line);
                        else
                            Log.Error(prefix + line);
                    }
                }

                exception = exception.InnerException;
            }
        }

        public static void Information(string message)
        {
            var prefix = $"[{Prefix}] ";
            Log.Information($"{prefix}{message}");
        }

        public static void Warning(string message)
        {
            var prefix = $"[{Prefix}] ";
            Log.Warning($"{prefix}{message}");
        }

        public static void Error(string message)
        {
            var prefix = $"[{Prefix}] ";
            Log.Error($"{prefix}{message}");
        }

        public static void Fatal(string message)
        {
            var prefix = $"[{Prefix}] ";
            Log.Fatal($"{prefix}{message}");
        }

        public static void Debug(string message)
        {
            var prefix = $"[{Prefix}] ";
            Log.Debug($"{prefix}{message}");
        }

        /// <summary>
        /// Creates an ILogger for the randomizer application. This does NOT assign it to MERLog.Logger.
        /// </summary>
        /// <returns></returns>
        public static ILogger CreateLogger()
        {
            return new LoggerConfiguration().WriteTo.SizeRollingFile(Path.Combine(MCoreFilesystem.GetAppDataFolder(), "logs", $"{Prefix.ToLower()}log.txt"),
                                    retainedFileDurationLimit: TimeSpan.FromDays(14),
                                    fileSizeLimitBytes: 1024 * 1024 * 10) // 10MB  
#if DEBUG
                            .WriteTo.Debug()
#endif
                            .CreateLogger();
        }
    }
}