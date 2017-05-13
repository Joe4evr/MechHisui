using Discord;
using System;

namespace SharedExtensions
{
    internal class Params
    {
        public string ConfigPath { get; private set; }
        public LogSeverity? LogSeverity { get; private set; }

        private Params()
        {
        }

        public static Params Parse(string[] parameters)
        {
            var p = new Params();
            for (int i = 0; i < parameters.Length; i++)
            {
                switch (parameters[i])
                {
                    case "-c":
                    case "-config":
                        p.ConfigPath = parameters[i + 1];
                        break;
                    case "-l":
                    case "-log":
                        p.LogSeverity = (LogSeverity)Enum.Parse(typeof(LogSeverity), parameters[i + 1], ignoreCase: true);
                        break;
                    default:
                        continue;
                }
            }
            return p;
        }
    }
}
