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
                            .AppendSequence(codes, (s, m) => s.AppendLine($"**{m.Code}** *({String.Join(", ", FgoHelpers.MysticCodeDict.Where(d => d.Value == m.Code).Select(d => d.Key))})*"));

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
                    var mystic = cea.Args[0];
                    if (!FgoHelpers.MysticCodeDict.Values.Contains(mystic))
                    {
                        await cea.Channel.SendMessage("Could not find Mystic Code to add alias for.");
                        return;
                    }

                    var alias = cea.Args[1].ToLowerInvariant();
                    try
                    {
                        FgoHelpers.MysticCodeDict.Add(alias, mystic);
                        File.WriteAllText(Path.Combine(_config["AliasPath"], "mystics.json"), JsonConvert.SerializeObject(FgoHelpers.MysticCodeDict, Formatting.Indented));
                        await cea.Channel.SendMessage($"Added alias `{alias}` for `{mystic}`.");
                    }
                    catch (ArgumentException)
                    {
                        await cea.Channel.SendMessage($"Alias `{alias}` already exists for CE `{FgoHelpers.MysticCodeDict[alias]}`.");
                        return;
                    }
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
