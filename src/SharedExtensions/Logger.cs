using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace SharedExtensions
{
    internal sealed class Logger
    {
        private readonly LogSeverity _minimum;
        private readonly StreamWriter _logFile;

        public Logger(LogSeverity minimum)
        {
            _minimum = minimum;
            string logdir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logdir);
            _logFile = File.AppendText(Path.Combine(logdir, $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.log"));
        }

        public Task Log(LogMessage lmsg)
        {
            string logline = $"{DateTime.Now,-19} [{lmsg.Severity,8}] {lmsg.Source}: {lmsg.Message}";
            _logFile.WriteLine(logline);
            _logFile.Flush();
            if (lmsg.Severity <= _minimum)
            {
                var cc = Console.ForegroundColor;
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
                Console.ForegroundColor = cc;
            }
            return Task.CompletedTask;
        }
    }
}
