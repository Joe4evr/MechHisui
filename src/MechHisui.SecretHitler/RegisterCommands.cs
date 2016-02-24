using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using MechHisui.SecretHitler;
using System.Text;

namespace MechHisui.Commands
{
    public static class RegisterSecHitCommands
    {
        private static SecretHitler.SecretHitler game;
        internal static bool gameOpen = false;
        private static IList<User> players;

        public static void RegisterSecretHitler(this DiscordClient client, IConfiguration config)
        {
            client.Services.Get<CommandService>().CreateCommand("rules")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Quick summary of the rules.")
                .Do(async cea =>
                {
                    var sb = new StringBuilder("How to play:")
                        .AppendLine("There are three roles: Liberal, Fascist, and Hitler.")
                        .AppendLine("Hitler does not know who his fellow Fascists are, but the Fascists know who Hitler is (except in 5 or 6 player games).")
                        .AppendLine("Liberals will always start off not knowing anything.")
                        .AppendLine("If 6 Fascist Policies are enacted, or Hitler is chosen as Chancellor in the late-game, the Fascists win.")
                        .AppendLine("If 5 Liberal Policies are enacted, or Hitler is successfully killed, the Liberals win.")
                        .AppendLine("The following themes are available too: `jjba`")
                        .Append("Good luck, have fun.");
                    
                    await cea.Channel.SendMessage(sb.ToString());
                });

            client.Services.Get<CommandService>().CreateCommand("opensh")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                //.Parameter("type", ParameterType.Optional)
                .Description("Open up a game of Secret ~~Angry Manjew~~ Hitler for people to join.")
                .Hide()
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        players = new List<User>();
                        await cea.Channel.SendMessage("Opening up a round of Secret Hitler.");
                    }
                });

            //client.Services.Get<CommandService>().CreateCommand("testsh")
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

            client.Services.Get<CommandService>().CreateCommand("players")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Display the currently joined players.")
                .Do(async cea =>
                {
                    await cea.Channel.SendMessage($"Current players are {String.Join(", ", players.Select(p => p.Name))}.");
                });

            client.Services.Get<CommandService>().CreateCommand("join")
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

            client.Services.Get<CommandService>().CreateCommand("leave")
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

            client.Services.Get<CommandService>().CreateCommand("startsh")
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
                            var gameConfig = SecretHitlerConfig.Default;
                            switch (cea.Args[0])
                            {
                                //case "am":
                                //case "angrymanjew":
                                //    gameConfig = SecretHitlerConfig.AngryManjew;
                                //    break;
                                case "jjba":
                                case "jojo":
                                    gameConfig = SecretHitlerConfig.JojosBizarreAdventure;
                                    break;
                                default:
                                    break;
                            }
                            game = new SecretHitler.SecretHitler(gameConfig, cea.Channel, players);
                            await game.SetupGame();
                        }
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("state")
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

            client.Services.Get<CommandService>().CreateCommand("turn")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("Advance to the next turn.")
                .Do(async cea => await game.StartTurn());

            client.Services.Get<CommandService>().CreateCommand("endsh")
                .AddCheck((c, u, ch) => u.Roles.Select(r => r.Id).Contains(UInt64.Parse(config["FGO_Admins"])) && ch.Id == UInt64.Parse(config["FGO_SecretHitler"]))
                .Description("End the game early.")
                .Do(async cea => {
                    await game.EndGame();
                    gameOpen = true;
                });
        }
    }
}
