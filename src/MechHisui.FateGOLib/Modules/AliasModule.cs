using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class AliasModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;
        private readonly string[] _types = new[] { "servant", "ce", "mystic" };

        public AliasModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Checkalias'...");
            manager.Client.GetService<CommandService>().CreateCommand("checkalias")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Parameter("type", ParameterType.Required)
                .Parameter("name", ParameterType.Required)
                .Do(async cea =>
                {
                    string msg = cea.Args[0] == _types[0]
                        ? GetServantAliases(cea.Args[1])
                        : (cea.Args[0] == _types[1]
                            ? GetCeAliases(cea.Args[1])
                            : (cea.Args[0] == _types[2]
                                ? GetMysticAliases(cea.Args[1])
                                : "Invalid search type specified."));

                    await cea.Channel.SendWithRetry(msg);
                });
        }

        private string GetServantAliases(string name)
        {
            var results = _statService.LookupStats(name, true).ToList();
            return results.Count == 0
                ? "No result found."
                : String.Join("\n", results.Select(r => $"**{r.Name}:** *({String.Join(", ", FgoHelpers.ServantDict.Where(a => a.Value == r.Name).Select(a => a.Key))})*"));
        }

        private string GetCeAliases(string name)
        {
            var results = _statService.LookupCE(name, true).ToList();
            return results.Count == 0
                ? "No result found."
                : String.Join("\n", results.Select(r => $"**{r.Name}:** *({String.Join(", ", FgoHelpers.CEDict.Where(a => a.Value == r.Name).Select(a => a.Key))})*"));
        }

        private string GetMysticAliases(string name)
        {
            var results = _statService.LookupMystic(name, true).ToList();
            return results.Count == 0
                ? "No result found."
                : String.Join("\n", results.Select(r => $"**{r.Code}:** *({String.Join(", ", FgoHelpers.MysticCodeDict.Where(a => a.Value == r.Code).Select(a => a.Key))})*"));
        }
    }
}
