using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using JiiLib;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class DropsModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;

        public DropsModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Drops'...");
            manager.Client.GetService<CommandService>().CreateCommand("drops")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_playground"]))
                .Description("Relay information about item drop locations.")
                .Parameter("item", ParameterType.Unparsed)
                .Do(async cea =>
                {
                    var arg = cea.Args[0];
                    if (String.IsNullOrWhiteSpace(arg))
                    {
                        await cea.Channel.SendMessage("Provide an item to find among drops.");
                        return;
                    }

                    var potentials = FgoHelpers.ItemDropsList.Where(d => d.ItemDrops?.ContainsIgnoreCase(arg) == true);
                    if (potentials.Any())
                    {
                        string result = String.Join("\n", potentials.Select(p => $"**{p.Map} - {p.NodeJP} ({p.NodeEN}):** {p.ItemDrops}"));
                        if (result.Length > 1900)
                        {
                            for (int i = 0; i < result.Length; i += 1750)
                            {
                                if (i == 0)
                                {
                                    await cea.Channel.SendMessage($"Found in the following {potentials.Count()} locations:\n{result.Substring(i, i + 1750)}...");
                                }
                                else if (i + 1750 > result.Length)
                                {
                                    await cea.Channel.SendMessage($"...{result.Substring(i)}");
                                }
                                else
                                {
                                    await cea.Channel.SendMessage($"...{result.Substring(i, i + 1750)}");
                                }
                            }
                        }
                        else
                        {
                            await cea.Channel.SendMessage($"Found in the following {potentials.Count()} locations:\n{result}");
                        }

                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not find specified item among location drops.");
                    }
                });
        }
    }
}
