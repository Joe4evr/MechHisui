using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using JiiLib;
using Discord.WebSocket;

namespace MechHisui.FateGOLib
{
    public class StatService
    {
        private readonly Timer _logintimer;
        internal readonly FgoConfig Config;

        internal StatService(FgoConfig config, DiscordSocketClient client)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));

            _logintimer = new Timer(async o =>
            {
                await (client.GetChannel(120979035290468352ul) as SocketTextChannel)
                    .SendMessageAsync("Login bonuses have been distributed.").ConfigureAwait(false);
            }, null,
            new DateTimeWithZone(DateTime.UtcNow, FgoExtensions.JpnTimeZone)
                .TimeUntilNextLocalTimeAt(new TimeSpan(hours: 3, minutes: 59, seconds: 59)),
            TimeSpan.FromHours(24));
        }

        public IEnumerable<ServantProfile> LookupStats(string servant, bool fullsearch = false)
        {
            var list = Config.GetServants();
            var servants = list
                .Where(p => p.Name.Equals(servant, StringComparison.OrdinalIgnoreCase));

            if (!servants.Any() || fullsearch)
            {
                servants = servants.Concat(list.Where(p => RegexMatchOneWord(p.Name, servant)));

                if (!servants.Any() || fullsearch)
                {
                    var lookup = list
                        .Where(s => s.Aliases.Any(a => a.Equals(servant, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = lookup.Concat(list
                            .Where(s => s.Aliases.Any(a => RegexMatchOneWord(a, servant))))
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        servants = servants.Concat(list.Where(p => lookup.Any(l => l.Id == p.Id)));
                    }
                }
            }

            return servants.ToList();
        }

        public IEnumerable<CEProfile> LookupCE(string name, bool fullsearch = false)
        {
            var list = Config.GetCEs();
            var ces = list
                .Where(ce => ce.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (!ces.Any() || fullsearch)
            {
                ces = ces.Concat(list.Where(ce => RegexMatchOneWord(ce.Name, name)));

                if (!ces.Any() || fullsearch)
                {
                    var lookup = list
                        .Where(ce => ce.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = lookup.Concat(list
                            .Where(ce => ce.Aliases.Any(a => RegexMatchOneWord(a, name))))
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        ces = ces.Concat(list.Where(ce => lookup.Any(l => l.Id == ce.Id)));
                    }
                }
            }

            return ces.ToList();
        }

        public IEnumerable<MysticCode> LookupMystic(string code, bool fullsearch = false)
        {
            var list = Config.GetMystics();
            var mystics = list
                .Where(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (mystics.Count == 0 || fullsearch)
            {
                mystics = mystics.Concat(list.Where(m => RegexMatchOneWord(m.Code, code))).ToList();

                if (mystics.Count == 0 || fullsearch)
                {
                    var lookup = list
                        .Where(m => m.Aliases.Any(a => a.Equals(code, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (lookup.Count == 0 || fullsearch)
                    {
                        lookup = lookup.Concat(list
                            .Where(m => m.Aliases.Any(a => RegexMatchOneWord(a, code))))
                            .ToList();
                    }

                    if (lookup.Count > 0)
                    {
                        mystics = mystics.Concat(list.Where(m => lookup.Any(l => l.Code == m.Code))).ToList();
                    }
                }
            }

            return mystics;
        }

        //public void ReadAliasList()
        //{
        //    FgoHelpers.ServantDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Config.ServantAliasesPath));
        //    FgoHelpers.CEDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Config.CEAliasesPath));
        //    FgoHelpers.MysticCodeDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Config.MysticAliasesPath));
        //}

        private static bool RegexMatchOneWord(string hay, string needle)
            => Regex.Match(hay, String.Concat(_b, needle, _b), RegexOptions.IgnoreCase).Success;

        private const string _b = @"\b";
    }
}
