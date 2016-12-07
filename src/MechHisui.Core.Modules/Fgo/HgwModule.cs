using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JiiLib;
using Discord;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.FateGOLib.Modules
{
    public sealed class HgwModule : ModuleBase
    {
        private readonly StatService _statService;

        public HgwModule(StatService statService)
        {
            _statService = statService;
        }



        [Command("hgw"), Permission(MinimumPermission.Everyone)]
        [Summary("Set up a random Holy Grail War. Discuss.")]
        public async Task HgwCmd()
        {
            var rng = new Random();
            var masters = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                FgoHelpers.Masters = (List<string>)FgoHelpers.Masters.Shuffle();
                string temp;
                do temp = FgoHelpers.Masters.ElementAt(rng.Next(maxValue: FgoHelpers.Masters.Count));
                while (masters.Contains(temp));
                masters.Add(temp);
            }

            var templist = FgoHelpers.ServantProfiles.Concat(FgoHelpers.FakeServantProfiles)
                .Where(pred)
                .Select(p => new NameOnlyServant { Class = p.Class, Name = p.Name })
                .Concat(FgoHelpers.NameOnlyServants);

            var servants = new List<NameOnlyServant>();
            for (int i = 0; i < 7; i++)
            {
                templist = templist.Shuffle();
                NameOnlyServant temp;
                do temp = templist.ElementAt(rng.Next(maxValue: templist.Count()));
                while (servants.Select(s => s.Class).Contains(temp.Class));
                servants.Add(temp);
            }

            var hgw = new Dictionary<string, string>
                {
                    { masters.ElementAt(0), servants.Single(p => p.Class == ServantClass.Saber.ToString()).Name },
                    { masters.ElementAt(1), servants.Single(p => p.Class == ServantClass.Archer.ToString()).Name },
                    { masters.ElementAt(2), servants.Single(p => p.Class == ServantClass.Lancer.ToString()).Name },
                    { masters.ElementAt(3), servants.Single(p => p.Class == ServantClass.Rider.ToString()).Name },
                    { masters.ElementAt(4), servants.Single(p => p.Class == ServantClass.Caster.ToString()).Name },
                    { masters.ElementAt(5), servants.Single(p => p.Class == ServantClass.Assassin.ToString()).Name },
                    { masters.ElementAt(6), servants.Single(p => p.Class == ServantClass.Berserker.ToString()).Name }
                };

            var sb = new StringBuilder($"**Team Saber:** {hgw.ElementAt(0).Key} + {hgw.ElementAt(0).Value}\n")
                .AppendLine($"**Team Archer:** {hgw.ElementAt(1).Key} + {hgw.ElementAt(1).Value}")
                .AppendLine($"**Team Lancer:** {hgw.ElementAt(2).Key} + {hgw.ElementAt(2).Value}")
                .AppendLine($"**Team Rider:** {hgw.ElementAt(3).Key} + {hgw.ElementAt(3).Value}")
                .AppendLine($"**Team Caster:** {hgw.ElementAt(4).Key} + {hgw.ElementAt(4).Value}")
                .AppendLine($"**Team Assassin:** {hgw.ElementAt(5).Key} + {hgw.ElementAt(5).Value}")
                .AppendLine($"**Team Berserker:** {hgw.ElementAt(6).Key} + {hgw.ElementAt(6).Value}")
                .Append("Discuss.");

            await ReplyAsync(sb.ToString());
        }

        private static bool pred(ServantProfile p) =>
            p.Class == ServantClass.Saber.ToString()    ||
            p.Class == ServantClass.Archer.ToString()   ||
            p.Class == ServantClass.Lancer.ToString()   ||
            p.Class == ServantClass.Rider.ToString()    ||
            p.Class == ServantClass.Caster.ToString()   ||
            p.Class == ServantClass.Assassin.ToString() ||
            p.Class == ServantClass.Berserker.ToString();

        //            manager.Client.GetService<CommandService>().CreateCommand("addhgw")
        //                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(_config["Owner"]) && ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
        //                .Hide()
        //                .Parameter("cat", ParameterType.Required)
        //                .Parameter("name", ParameterType.Multiple)
        //                .Do(async cea =>
        //                {
        //                    switch (cea.Args[0])
        //                    {
        //                        case "servant":
        //                            var temp = cea.Args[1].Split(' ');
        //                            var name = String.Join(" ", temp.Skip(1));
        //                            FgoHelpers.NameOnlyServants.Add(
        //                                new NameOnlyServant
        //                                {
        //                                    Class = temp[0],
        //                                    Name = name
        //                                });
        //                            using (TextWriter tw = new StreamWriter(Path.Combine(_config["other"], "nameonlyservants.json")))
        //                            {
        //                                tw.Write(JsonConvert.SerializeObject(FgoHelpers.NameOnlyServants, Formatting.Indented));
        //                            }
        //                            await cea.Channel.SendMessage($"Added `{name}` as a `{temp[0]}`.");
        //                            break;
        //                        case "master":
        //                            FgoHelpers.Masters.Add(cea.Args[1]);
        //                            using (TextWriter tw = new StreamWriter(Path.Combine(_config["other"], "masters.json")))
        //                            {
        //                                tw.Write(JsonConvert.SerializeObject(FgoHelpers.Masters, Formatting.Indented));
        //                            }
        //                            await cea.Channel.SendMessage($"Added `{cea.Args[1]}` as a Master.");
        //                            break;
        //                        default:
        //                            await cea.Channel.SendMessage("Unsupported catagory.");
        //                            break;
        //                    }
        //        }
    }
}
