﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using MechHisui.SecretHitler;

namespace MechHisui.Commands
{
    public static class RegisterSecHitCommands
    {
        private static SecretHitler.SecretHitler game;
        private static IList<User> players;
        private static IList<SecretHitlerConfig> configs;
        internal static bool gameOpen = false;
        private static HouseRules rules;

        public static void RegisterSecretHitler(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'Secret Hitler'...");
            ReloadConfigs(Path.Combine(config["other"], "shitler.json"));

            client.GetService<CommandService>().CreateCommand("rules")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Quick summary of the rules.")
                .Do(async cea =>
                {
                    var sb = new StringBuilder("How to play:\n")
                        .AppendLine("There are three roles: Liberal, Fascist, and Hitler.")
                        .AppendLine("Hitler does not know who his fellow Fascists are, but the Fascists know who Hitler is (except in 5 or 6 player games).")
                        .AppendLine("Liberals will always start off not knowing anything.")
                        .AppendLine("If 6 Fascist Policies are enacted, or Hitler is chosen as Chancellor in the late-game, the Fascists win.")
                        .AppendLine("If 5 Liberal Policies are enacted, or Hitler is successfully killed, the Liberals win.")
                        .AppendLine($"The following themes are available too: `{String.Join("`, `", configs.Select(c => c.Key))}`")
                        .AppendLine("For more details: https://dl.dropboxusercontent.com/u/502769/Secret_Hitler_Rules.pdf ")
                        .Append("Good luck, have fun.");
                    
                    await cea.Channel.SendMessage(sb.ToString());
                });

            client.GetService<CommandService>().CreateCommand("opensh")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                //.Parameter("type", ParameterType.Optional)
                .Description("Open up a game of Secret ~~Angry Manjew~~ Hitler for people to join.")
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        rules = HouseRules.None;
                        players = new List<User>();
                        await cea.Channel.SendMessage("Opening up a round of Secret Hitler.");
                    }
                });

            client.GetService<CommandService>().CreateCommand("reload")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Hide()
                .Do(cea => ReloadConfigs(Path.Combine(config["other"], "shitler.json")));

            //client.GetService<CommandService>().CreateCommand("testsh")
            //    .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]))
            //    .Hide()
            //    .Do(async cea =>
            //    {
            //        gameOpen = true;
            //        var joe = cea.User;
            //        var Game = new SecretHitler.SecretHitler(SecretHitlerConfig.Default, cea.Channel, new List<User> { joe, joe, joe, joe, joe });
            //        await Game.SetupGame();
            //        await Game.TestTurn(joe);
            //        await Game.TestNomination(joe, joe);
            //        game = Game;
            //    });

            client.GetService<CommandService>().CreateCommand("players")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Display the currently joined players.")
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage($"Current players are {String.Join(", ", players.Select(p => p.Name))}. ({players.Count})");
                });

            client.GetService<CommandService>().CreateCommand("join")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Join the game when it is open to play. At least 5 and at most 10 people can play. Using Voice is advised.")
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        if (players.Count == 10)
                        {
                            await cea.Channel.SendMessage($"The list is already full.");
                            return;
                        }

                        if (!players.Any(p => p.Id == cea.User.Id))
                        {
                            players.Add(cea.User);
                            await cea.Channel.SendMessage($"{cea.User.Name} joined the game.");
                        }
                    }
                    else
                    {
                        await cea.Channel.SendMessage($"Game is already in progress.");
                    }
                });

            client.GetService<CommandService>().CreateCommand("leave")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Leave a game if it has not been started yet.")
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        if (players.Any(p => p.Id == cea.User.Id))
                        {
                            players.Remove(cea.User);
                            await cea.Channel.SendMessage($"{cea.User.Name} left the game.");
                        }
                    }
                });

            client.GetService<CommandService>().CreateCommand("startsh")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Parameter("type", ParameterType.Optional)
                .Description("Start a game. `.opensh` has to be called before this and at least 5 people must join.")
                .Do(async cea =>
                {
                    if (!gameOpen && players != null)
                    {
                        if (players.Count >= 5 && players.Count <= 10)
                        {
                            gameOpen = true;
                            await cea.Channel.SendMessage($"Setting up game with {players.Count} players.");
                            var gameConfig = configs.SingleOrDefault(c => c.Key == cea.Args[0]) ?? SecretHitlerConfig.Default;
                            game = new SecretHitler.SecretHitler(gameConfig, cea.Channel, players);
                            await game.SetupGame();
                        }
                    }
                });

            client.GetService<CommandService>().CreateCommand("house")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Parameter("rule", ParameterType.Unparsed)
                .Description("Apply a House Rule.")
                .Do(async cea =>
                {
                    if (gameOpen)
                    {
                        await cea.Channel.SendMessage("Cannot change rules while game is in progress.");
                        return;
                    }

                    switch (cea.Args[0])
                    {
                        case "skipfirst":
                            rules |= HouseRules.SkipFirstElection;
                            await cea.Channel.SendMessage("House rule added.");
                            break;
                        default:
                            await cea.Channel.SendMessage("Unknown parameter.");
                            break;
                    }


                });

            client.GetService<CommandService>().CreateCommand("state")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Display the current state of the game.")
                .Do(async cea =>
                {
                    if (gameOpen)
                    {
                        await cea.Channel.SendMessage(game.GetGameState());
                    }
                    else
                    {
                        await cea.Channel.SendMessage("No game going on.");
                    }
                });

            client.GetService<CommandService>().CreateCommand("turn")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Advance to the next turn.")
                .Do(async cea => await game.StartTurn());

            client.GetService<CommandService>().CreateCommand("endsh")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("End the game early.")
                .Do(async cea => {
                    await game.EndGame();
                    gameOpen = true;
                });

            client.GetService<CommandService>().CreateCommand("cancel")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Hide()
                .Do(async cea =>
                {
                    gameOpen = false;
                    await cea.Channel.SendMessage("Game canceled.");
                });
        }

        private static void ReloadConfigs(string path)
        {
            configs = JsonConvert.DeserializeObject<List<SecretHitlerConfig>>(File.ReadAllText(path));
        }
    }
}
