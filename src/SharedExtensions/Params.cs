using System;
using Discord;

namespace SharedExtensions
{
    internal sealed class Params
    {
        public string ConfigPath { get; private set; }
        public string ConnectionString { get; private set; }
        public string LogPath { get; private set; }
        public LogSeverity? LogSeverity { get; private set; }
        public int? Shards { get; private set; }
        public int? ShardId { get; private set; }
        public string Token { get; private set; }

        private Params() { }

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
                        continue;
                    case "-conn":
                    case "-db":
                    case "-dbconn":
                        p.ConnectionString = parameters[i + 1];
                        continue;
                    case "-l":
                    case "-log":
                        p.LogSeverity = (LogSeverity)Enum.Parse(typeof(LogSeverity), parameters[i + 1], ignoreCase: true);
                        continue;
                    case "-lp":
                    case "-logpath":
                        p.LogPath = parameters[i + 1];
                        continue;
                    case "-s":
                    case "-shards":
                        p.Shards = Int32.Parse(parameters[i + 1]);
                        continue;
                    case "-sid":
                    case "-shardid":
                        p.ShardId = Int32.Parse(parameters[i + 1]);
                        continue;
                    case "-t":
                        p.Token = parameters[i + 1];
                        break;
                    default:
                        continue;
                }
            }
            return p;
        }
    }
}
