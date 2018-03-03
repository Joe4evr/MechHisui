using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace SharedExtensions
{
    internal sealed class Logger
    {
        public static Func<LogMessage, Task> NoOpLogger { get; } = (_ => Task.CompletedTask);

        private readonly LogSeverity _minimum;
        private readonly StreamWriter _logFile;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public Logger(LogSeverity minimum, string logPath = null)
        { 
            _minimum = minimum;
            string logdir = Path.Combine(Directory.GetCurrentDirectory(), logPath ?? "logs");
            Directory.CreateDirectory(logdir);
            _logFile = File.AppendText(Path.Combine(logdir, $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.log"));
            _logFile.AutoFlush = true;
        }

        [DebuggerStepThrough]
        public async Task Log(LogMessage lmsg)
        {
            string logline = $"{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),-19} [{lmsg.Severity,8}] {lmsg.Source}: {lmsg.Message} {lmsg.Exception}";

            await _lock.WaitAsync().ConfigureAwait(false);
            await _logFile.WriteLineAsync(logline).ConfigureAwait(false);
            _lock.Release();

            if (lmsg.Severity <= _minimum)
            {
                switch (lmsg.Severity)
                {
                    case LogSeverity.Critical:
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogSeverity.Verbose:
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                }
                Console.WriteLine(logline);
                Console.ResetColor();
            }
        }
    }
}
