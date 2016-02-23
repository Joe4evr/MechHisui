using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using MechHisui.SecretHitler;

namespace MechHisui.Commands
{
    public static class RegisterSecHitCommands
    {
        private static SecretHitler.SecretHitler game;
        private static bool gameOpen = false;
        private static IList<User> players;

        public static void RegisterSecretHitler(this DiscordClient client, IConfiguration config)
        {
            client.Services.Get<CommandService>().CreateCommand("opensh")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                //.Parameter("type", ParameterType.Optional)
                .Description("")
                .Hide()
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        players = new List<User>();
                        await cea.Channel.SendMessage("Opening up a round of Secret Hitler.");
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("testsh")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]))
                .Hide()
                .Do(async cea =>
                {
                    gameOpen = true;
                    var joe = cea.User;
                    var Game = new SecretHitler.SecretHitler(SecretHitlerConfig.Default, cea.Channel, new List<User> { joe, joe, joe, joe, joe });
                    await Game.SetupGame();
                    await Game.TestTurn(joe);
                    await Game.TestNomination(joe, joe);
                    game = Game;
                });

            client.Services.Get<CommandService>().CreateCommand("players")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage($"Current players are {String.Join(", ", players.Select(p => p.Name))}.");
                });

            client.Services.Get<CommandService>().CreateCommand("join")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                //.Parameter("type", ParameterType.Optional)
                .Description("")
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
                            await cea.Channel.SendMessage($"{cea.User.Name} joined for the game.");
                        }
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("startsh")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Parameter("type", ParameterType.Optional)
                .Description("")
                .Do(async cea =>
                {
                    if (!gameOpen && players != null)
                    {
                        if (players.Count >= 5 && players.Count <= 10)
                        {
                            gameOpen = true;
                            await cea.Channel.SendMessage($"Setting up game with {players.Count} players.");
                            var gameConfig = SecretHitlerConfig.Default;
                            switch (cea.Args[0])
                            {
                                case "angrymanjew":
                                    gameConfig = SecretHitlerConfig.AngryManjew;
                                    break;
                                default:
                                    break;
                            }
                            game = new SecretHitler.SecretHitler(SecretHitlerConfig.Default, cea.Channel, players);
                            await game.SetupGame();
                        }
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("state")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
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

            client.Services.Get<CommandService>().CreateCommand("turn")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Do(async cea => await game.StartTurn());

            client.Services.Get<CommandService>().CreateCommand("endsh")
                .AddCheck((c, u, ch) => u.Id == UInt64.Parse(config["Owner"]) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Hide()
                .Do(cea => gameOpen = false);
        }
    }
}
