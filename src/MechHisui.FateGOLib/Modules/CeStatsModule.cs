using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JiiLib;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class CeStatsModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;

        public CeStatsModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'CE'...");
            manager.Client.GetService<CommandService>().CreateCommand("ce")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Parameter("cename", ParameterType.Unparsed)
                .Description($"Relay information on the specified Craft Essence. Alternative names acceptable.")
                .Do(async cea =>
                {
                    CEProfile ce;
                    int id;
                    if (Int32.TryParse(cea.Args[0], out id) && id <= FgoHelpers.CEProfiles.Max(p => p.Id))
                    {
                        ce = FgoHelpers.CEProfiles.SingleOrDefault(p => p.Id == id);
                    }
                    else
                    {
                        ce = _statService.LookupCE(cea.Args[0]);
                    }

                    if (ce != null)
                    {
                        await cea.Channel.SendMessage(FormatCEProfile(ce));
                    }
                    else
                    {
                        var potentials = FgoHelpers.CEDict.Where(c => c.Alias.Any(a => a.ContainsIgnoreCase(cea.Args[0])) || c.CE.ContainsIgnoreCase(cea.Args[0]));
                        if (potentials.Any())
                        {
                            if (potentials.Count() > 1)
                            {
                                string res = String.Join("\n", potentials.Select(p => $"**{p.CE}** *({String.Join(", ", p.Alias)})*"));
                                await cea.Channel.SendMessage($"Entry ambiguous. Did you mean one of the following?\n{res}");
                            }
                            else
                            {
                                await cea.Channel.SendMessage($"**CE:** {potentials.First().CE}\nMore information TBA.");
                            }
                        }
                        else
                        {
                            await cea.Channel.SendMessage("No such entry found. Please try another name.");
                        }
                    }
                });

            Console.WriteLine("Registering 'All CE'...");
            manager.Client.GetService<CommandService>().CreateCommand("allce")
                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Parameter("ceeffect", ParameterType.Unparsed)
                .Description($"Relay information on CEs having the specified effect.")
                .Do(async cea =>
                {
                    var arg = cea.Args[0];
                    if (String.IsNullOrEmpty(arg))
                    {
                        await cea.Channel.SendMessage("No effect specified.");
                        return;
                    }

                    var ces = FgoHelpers.CEProfiles.Where(c => c.Effect.ContainsIgnoreCase(arg)).ToList();

                    if (ces.Count() > 0)
                    {
                        var sb = new StringBuilder($"**{arg}:**\n");
                        foreach (var c in ces)
                        {
                            sb.AppendLine($"**{c.Name}** - {c.Effect}");
                            if (sb.Length > 1700)
                            {
                                await cea.Channel.SendMessage(sb.ToString());
                                sb = sb.Clear();
                            }
                        }
                        await cea.Channel.SendMessage(sb.ToString());
                    }
                    else
                    {
                        await cea.Channel.SendMessage("No such CEs found. Please try another term.");
                    }
                });

            Console.WriteLine("Registering 'CE alias'...");
            manager.Client.GetService<CommandService>().CreateCommand("cealias")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(_config["FGO_Admins"])) && ch.Server.Id == UInt64.Parse(_config["FGO_server"]))
                .Hide()
                .Parameter("ce", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    CEAlias newAlias = FgoHelpers.CEDict.SingleOrDefault(p => p.CE == cea.Args[0]);
                    var arg = cea.Args[1].ToLowerInvariant();
                    var test = FgoHelpers.CEDict.Where(a => a.Alias.Contains(arg)).FirstOrDefault();
                    if (test != null)
                    {
                        await cea.Channel.SendMessage($"Alias `{arg}` already exists for CE `{test.CE}`.");
                        return;
                    }
                    else if (newAlias != null)
                    {
                        newAlias.Alias.Add(arg);
                    }
                    else
                    {
                        CEProfile ce = FgoHelpers.CEProfiles.SingleOrDefault(s => s.Name == cea.Args[0]);
                        if (ce != null)
                        {
                            newAlias = new CEAlias
                            {
                                Alias = new List<string> { arg },
                                CE = ce.Name
                            };
                        }
                        else
                        {
                            await cea.Channel.SendMessage("Could not find name to add alias for.");
                            return;
                        }
                    }

                    File.WriteAllText(Path.Combine(_config["AliasPath"], "ces.json"), JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                    await cea.Channel.SendMessage($"Added alias `{arg}` for `{newAlias.CE}`.");
                });
        }

        private static string FormatCEProfile(CEProfile ce)
        {
            return new StringBuilder()
                .AppendLine($"**Collection ID:** {ce.Id}")
                .AppendLine($"**Rarity:** {ce.Rarity}☆")
                .AppendLine($"**CE:** {ce.Name}")
                .AppendLine($"**Cost:** {ce.Cost}")
                .AppendLine($"**ATK:** {ce.Atk}")
                .AppendLine($"**HP:** {ce.HP}")
                .AppendLine($"**Effect:** {ce.Effect}")
                .AppendLine($"**Max ATK:** {ce.AtkMax}")
                .AppendLine($"**Max HP:** {ce.HPMax}")
                .AppendLine($"**Max Effect:** {ce.EffectMax}")
                .Append(ce.Image)
                .ToString();
        }
    }
}
