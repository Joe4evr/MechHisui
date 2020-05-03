using System;
using Discord;

namespace SharedExtensions
{
#nullable disable warnings
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
            bool print = parameters.ContainsIgnoreCase("-pa");
            int lastIdx = parameters.Length - 1;
            var p = new Params();
            for (int i = 0; i < parameters.Length; i++)
            {
                string? next = (i < lastIdx)
                    ? parameters[i + 1]
                    : null;
                switch (parameters[i])
                {
                    case "-c":
                    case "-config":
                        if (print)
                            Log(nameof(ConfigPath), next);
                        p.ConfigPath = next;
                        continue;
                    case "-conn":
                    case "-db":
                    case "-dbconn":
                        if (print)
                            Log(nameof(ConnectionString), next);
                        p.ConnectionString = next;
                        continue;
                    case "-l":
                    case "-log":
                        if (print)
                            Log(nameof(LogSeverity), next);
                        p.LogSeverity = (LogSeverity)Enum.Parse(typeof(LogSeverity), next, ignoreCase: true);
                        continue;
                    case "-lp":
                    case "-logpath":
                        if (print)
                            Log(nameof(LogPath), next);
                        p.LogPath = next;
                        continue;
                    case "-s":
                    case "-shards":
                        if (print)
                            Log(nameof(Shards), next);
                        p.Shards = Int32.Parse(next);
                        continue;
                    case "-sid":
                    case "-shardid":
                        if (print)
                            Log(nameof(ShardId), next);
                        p.ShardId = Int32.Parse(next);
                        continue;
                    case "-t":
                        p.Token = next;
                        continue;
                    default:
                        continue;
                }
            }
            return p;
        }

        private static void Log(string p, string v)
            => Console.WriteLine($"[{nameof(Params)}] {p} = '{v}'");
    }
}
