using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using JiiLib;
using Newtonsoft.Json;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class MysticCodeStatsModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;

        public MysticCodeStatsModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Mystic Codes'...");
            manager.Client.GetService<CommandService>().CreateCommand("mystic")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Description("Relay information on available Mystic Codes.")
                .Parameter("code", ParameterType.Unparsed)
                .Do(async cea =>
                {
                    string arg = cea.Args[0];

                    var codes = _statService.LookupMystic(arg);

                    if (codes.Count() == 1)
                    {
                        await cea.Channel.SendMessage(FormatMysticCodeProfile(codes.Single()));
                    }
                    else if (codes.Count() > 1)
                    {
                        var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                            .AppendSequence(codes, (s, m) => s.AppendLine($"**{m.Code}** *({String.Join(", ", FgoHelpers.CEDict.Single(d => d.CE == m.Code).Alias)})*"));

                        await cea.Channel.SendMessage(sb.ToString());
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Specified Mystic Code not found. Please use `.listmystic` for the list of available Mystic Codes.");
                    }
                });

            manager.Client.GetService<CommandService>().CreateCommand("listmystic")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Description("Relay the names of available Mystic Codes.")
                .Do(async cea =>
                {
                    var sb = new StringBuilder("**Available Mystic Codes:**\n");
                    foreach (var code in FgoHelpers.MysticCodeList)
                    {
                        sb.AppendLine(code.Code);
                    }
                    await cea.Channel.SendMessage(sb.ToString());
                });

            Console.WriteLine("Registering 'Mystic alias'...");
            manager.Client.GetService<CommandService>().CreateCommand("mysticalias")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(_config["FGO_Admins"])) && ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Hide()
                .Parameter("mystic", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var newAlias = FgoHelpers.MysticCodeDict.SingleOrDefault(p => p.Code == cea.Args[0]);
                    var arg = cea.Args[1].ToLowerInvariant();
                    var test = FgoHelpers.MysticCodeDict.Where(a => a.Alias.Contains(arg)).FirstOrDefault();
                    if (test != null)
                    {
                        await cea.Channel.SendMessage($"Alias `{arg}` already exists for CE `{test.Code}`.");
                        return;
                    }
                    else
                    if (newAlias != null)
                    {
                        newAlias.Alias.Add(arg);
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not find Mystic Code to add alias for.");
                        return;
                    }

                    File.WriteAllText(Path.Combine(_config["AliasPath"], "mystic.json"), JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                    await cea.Channel.SendMessage($"Added alias `{arg}` for `{newAlias.Code}`.");
                });
        }

        private static string FormatMysticCodeProfile(MysticCode code)
        {
            StringBuilder sb = new StringBuilder()
                .AppendLine($"**Name:** {code.Code}")
                .AppendLine($"**Skill 1:** {code.Skill1} - *{code.Skill1Effect}*")
                .AppendLine($"**Skill 2:** {code.Skill2} - *{code.Skill2Effect}*")
                .AppendLine($"**Skill 3:** {code.Skill3} - *{code.Skill3Effect}*")
                .Append(code.Image);
            return sb.ToString();
        }
    }
}
