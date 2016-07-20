﻿using System;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using JiiLib;
using Newtonsoft.Json;
using Discord;
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
                    if (String.IsNullOrWhiteSpace(cea.Args[0]))
                    {
                        return;
                    }

                    int id;
                    if (Int32.TryParse(cea.Args[0], out id) && id <= FgoHelpers.CEProfiles.Max(p => p.Id))
                    {
                        var ce = FgoHelpers.CEProfiles.SingleOrDefault(p => p.Id == id);
                        await cea.Channel.SendWithRetry(FormatCEProfile(ce));
                    }
                    else
                    {
                        var potentials = _statService.LookupCE(cea.Args[0]);
                        if (potentials.Count() == 1)
                        {
                            await cea.Channel.SendWithRetry(FormatCEProfile(potentials.Single()));
                        }
                        else if (potentials.Count() > 1)
                        {
                            var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
                                .AppendSequence(potentials, (s, pr) => s.AppendLine($"**{pr.Name}** *({String.Join(", ", FgoHelpers.CEDict.Where(d => d.Value == pr.Name).Select(d => d.Key))})*"));

                            await cea.Channel.SendWithRetry(sb.ToString());
                        }
                        else
                        {
                            await cea.Channel.SendWithRetry("No such entry found. Please try another name.");
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
                    if (String.IsNullOrWhiteSpace(cea.Args[0]))
                    {
                        return;
                    }

                    var arg = cea.Args[0];
                    if (String.IsNullOrEmpty(arg))
                    {
                        await cea.Channel.SendWithRetry("No effect specified.");
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
                                await cea.Channel.SendWithRetry(sb.ToString());
                                sb = sb.Clear();
                            }
                        }
                        await cea.Channel.SendWithRetry(sb.ToString());
                    }
                    else
                    {
                        await cea.Channel.SendWithRetry("No such CEs found. Please try another term.");
                    }
                });

            Console.WriteLine("Registering 'CE alias'...");
            manager.Client.GetService<CommandService>().CreateCommand("cealias")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(ulong.Parse(_config["FGO_Admins"])) && ch.Server.Id == ulong.Parse(_config["FGO_server"]))
                .Hide()
                .Parameter("ce", ParameterType.Required)
                .Parameter("alias", ParameterType.Required)
                .Do(async cea =>
                {
                    var ce = cea.Args[0];
                    if (!FgoHelpers.CEProfiles.Select(c => c.Name).Contains(ce))
                    {
                        await cea.Channel.SendWithRetry("Could not find name to add alias for.");
                        return;
                    }

                    var alias = cea.Args[1].ToLowerInvariant();
                    try
                    {
                        FgoHelpers.CEDict.Add(alias, ce);
                        File.WriteAllText(Path.Combine(_config["AliasPath"], "ces.json"), JsonConvert.SerializeObject(FgoHelpers.CEDict, Formatting.Indented));
                        await cea.Channel.SendWithRetry($"Added alias `{alias}` for `{ce}`.");
                    }
                    catch (ArgumentException)
                    {
                        await cea.Channel.SendWithRetry($"Alias `{alias}` already exists for CE `{FgoHelpers.CEDict[alias]}`.");
                        return;
                    }
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
