using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Discord.Commands;
using Discord.Modules;
using Newtonsoft.Json;
using JiiLib.Net;

namespace MechHisui.PkmnGoLib
{
    public class PgoModule : IModule
    {
        private readonly PgoDataService _pgoData;
        private readonly List<Mon> _monsList;
        private readonly IConfiguration _config;
        private readonly string _historyFile;

        public PgoModule(IConfiguration config)
        {
            Console.WriteLine("Connecting to data service (PGO)...");
            var api = new GoogleScriptApiService(
                Path.Combine(config["Google_Pgo_Calcs"], "client_secret.json"),
                Path.Combine(config["Google_Pgo_Calcs"], "scriptcreds"),
                "MechHisui",
                config["Pgo_Key"],
                "exportSheet",
                new string[]
                {
                    "https://www.googleapis.com/auth/spreadsheets",
                    "https://www.googleapis.com/auth/drive",
                    "https://spreadsheets.google.com/feeds/"
                });

            _pgoData = new PgoDataService(api);
            _pgoData.Init().GetAwaiter().GetResult();
            _historyFile = Path.Combine(config["Google_Pgo_Calcs"], "history.json");
            _monsList = JsonConvert.DeserializeObject<List<Mon>>(File.ReadAllText(_historyFile)).OrderBy(m => m.Timestamp).ToList();
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'IV'...");
            manager.Client.GetService<CommandService>().CreateCommand("iv")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_PGO"]))
                .Parameter("species", ParameterType.Required)
                .Parameter("CP", ParameterType.Required)
                .Parameter("HP", ParameterType.Required)
                .Parameter("dustPrice", ParameterType.Required)
                .Parameter("poweredUp", ParameterType.Optional)
                .Do(async cea =>
                {
                    var poke = PgoHelpers.KnownMons.SingleOrDefault(p => p.Name == cea.Args[0]);
                    if (poke == null)
                    {
                        await cea.Channel.SendMessage("Unknown Species provided.");
                        return;
                    }

                    double cp;
                    double hp;
                    double dp;
                    bool poweredUp = cea.Args[4].ToLowerInvariant() == "true";
                    if (double.TryParse(cea.Args[1], out cp) && double.TryParse(cea.Args[2], out hp) && double.TryParse(cea.Args[3], out dp))
                    {
                        if (!PgoHelpers.StardustPerLevel.Any(sdl => sdl.Stardust == dp))
                        {
                            await cea.Channel.SendMessage("Specified Stardust amount not valid.");
                            return;
                        }

                        var ivs = new List<IVs>();
                        var prevData = _monsList.LastOrDefault(m => m.Owner == cea.User.Id && m.Species == cea.Args[0]);
                        var mon = new Mon
                        {
                            Timestamp = DateTime.UtcNow,
                            Owner = cea.User.Id,
                            Species = poke.Name,
                            Num = (prevData != null && poweredUp ? prevData.Num++ : 1),
                            CP = cp,
                            HP = hp,
                            DustPrice = dp,
                            PotentialIVs = ivs
                        };

                        var index = PgoHelpers.StardustPerLevel.FindIndex(sdl => sdl.Stardust == mon.DustPrice);
                        double minlvl = PgoHelpers.StardustPerLevel[index].Level;
                        double maxlvl = PgoHelpers.StardustPerLevel[index + 1].Level - 0.5;
                        var multipliers = PgoHelpers.CPMultiplier.Where(c => c.Level >= minlvl && c.Level <= maxlvl);
                        foreach (var mp in multipliers)
                        {
                            if (mp.Level != (int)mp.Level || hp < GetHP(poke, mp.CpMultiplier, 0) || hp > GetHP(poke, mp.CpMultiplier, 15)) continue;

                            for (int sta = 0; sta < 16; sta++)
                            {
                                if (GetHP(poke, mp.CpMultiplier, sta) != hp) continue;

                                for (int atk = 0; atk < 16; atk++)
                                {
                                    for (int def = 0; def < 16; def++)
                                    {
                                        var f = (sta == 4 && atk == 10 && def == 10);

                                        if (GetCP(poke, mp.CpMultiplier, sta, atk, def) == cp && CheckPrevData(prevData, mp.Level, atk, def, sta))
                                        {
                                            ivs.Add(new IVs
                                            {
                                                Level = mp.Level,
                                                StaIV = sta,
                                                AtkIV = atk,
                                                DefIV = def
                                            });
                                        }
                                    }
                                }
                            }
                        }

                        if (ivs.Count == 0)
                        {
                            await cea.Channel.SendMessage("Could not find appropriate IVs for the given parameters.");
                        }
                        else if (ivs.Count == 1)
                        {
                            var iv = ivs.Single();
                            await cea.Channel.SendMessage($"**Exact match:** Level {iv.Level}, Attack: {iv.AtkIV}, Defense: {iv.DefIV}, Stamina: {iv.StaIV} ({iv.GetPercentage()}%).");
                        }
                        else
                        {
                            _monsList.Add(mon);
                            File.WriteAllText(_historyFile, JsonConvert.SerializeObject(_monsList));
                            var min = ivs.OrderBy(iv => iv.GetPercentage()).First();
                            var max = ivs.OrderByDescending(iv => iv.GetPercentage()).First();
                            var sb = new StringBuilder($"**Found {ivs.Count} combinations**\n")
                                .AppendLine($"Minimum: Level {min.Level}, Attack: {min.AtkIV}, Defense: {min.DefIV}, Stamina: {min.StaIV} ({min.GetPercentage()}%).")
                                .Append($"Maximum: Level {max.Level}, Attack: {max.AtkIV}, Defense: {max.DefIV}, Stamina: {max.StaIV} ({max.GetPercentage()}%).");
                            await cea.Channel.SendMessage(sb.ToString());
                        }
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not parse one or more parameters as a number.");
                    }
                });
        }

        private static double CompositeSTA(Pokemon mon, double multiplier, int iv)
            => (mon.Stamina + iv) * multiplier;

        private static double CompositeATK(Pokemon mon, double multiplier, int iv)
            => (mon.Attack + iv) * multiplier;
        private static double CompositeDEF(Pokemon mon, double multiplier, int iv)
            => (mon.Defense + iv) * multiplier;

        private static double GetHP(Pokemon mon, double mp, int sta)
            => Math.Floor(CompositeSTA(mon, mp, sta));

        private static double GetCP(Pokemon mon, double mp, int sta, int atk, int def)
            => Math.Floor(0.1 * Math.Pow(CompositeSTA(mon, mp, sta), 0.5) * CompositeATK(mon, mp, atk) * Math.Pow(CompositeDEF(mon, mp, def), 0.5));

        private static bool CheckPrevData(Mon mon, double lvl, int atk, int def, int sta)
        {
            if (mon == null || mon.PotentialIVs.Count == 0) return true;

            foreach (var d in mon.PotentialIVs)
            {
                if (lvl == d.Level + 0.5 && atk == d.AtkIV && def == d.DefIV && sta == d.StaIV) return true;
            }

            return false;
        }
    }
}
