using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Discord.Commands;
using Discord.Modules;
using Discord;

namespace MechHisui.Superfight
{
    public class SuperfightModule : IModule
    {
        private readonly IConfiguration _config;
        private readonly string _basePath;

        private bool gameOpen = false;
        private bool gameStarted = false;
        private List<User> players;
        private Superfight game;

        public SuperfightModule(IConfiguration config, string basePath)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (basePath == null) throw new ArgumentNullException(nameof(basePath));

            _config = config;
            _basePath = basePath;
        }

        void IModule.Install(ModuleManager manager)
        {
            manager.Client.GetService<CommandService>().CreateCommand("opensf")
                .AddCheck((c, u, ch) => u.Roles.Contains(ch.Server.GetRole(UInt64.Parse(_config["FGO_Admins"]))) && ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Description("Open up a game of Superfight for people to join.")
                .Do(async cea =>
                {
                    players = new List<User>();
                    gameOpen = true;
                    await cea.Channel.SendWithRetry("Opening a game of Superfight.");
                });

            manager.Client.GetService<CommandService>().CreateCommand("join")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Description("Join a game if it is open to join. At least three people need to be present to play.")
                .Do(async cea =>
                {
                    if (!gameOpen || gameStarted)
                    {
                        await cea.Channel.SendWithRetry("Cannot join at this time.");
                        return;
                    }

                    if (!players.Any(p => p.Id == cea.User.Id))
                    {
                        players.Add(cea.User);
                        await cea.Channel.SendWithRetry($"{cea.User.Name} has joined.");
                    }
                });

            manager.Client.GetService<CommandService>().CreateCommand("leave")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Description("Leave the game if you have joined but it has not started yet.")
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        if (gameStarted)
                        {
                            await cea.Channel.SendWithRetry("Game is already in progress.");
                        }
                        else
                        {
                            await cea.Channel.SendWithRetry("No game is currently open.");
                        }
                    }
                    else
                    {
                        if (players.Any(p => p.Id == cea.User.Id))
                        {
                            players.Remove(cea.User);
                            await cea.Channel.SendWithRetry($"{cea.User.Name} has left.");
                        }
                    }
                });

            manager.Client.GetService<CommandService>().CreateCommand("players")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Description("Display the currently joined players.")
                .Do(async cea => await cea.Channel.SendWithRetry($"Current players are {String.Join(", ", players.Select(p => p.Name))}. ({players.Count})"));

            manager.Client.GetService<CommandService>().CreateCommand("state")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Do(async cea => await cea.Channel.SendWithRetry(game.GetGameState()));

            manager.Client.GetService<CommandService>().CreateCommand("startsf")
                .AddCheck((c, u, ch) => u.Roles.Contains(ch.Server.GetRole(UInt64.Parse(_config["FGO_Admins"]))) && ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        await cea.Channel.SendWithRetry("There is no open game.");
                        return;
                    }
                    else if (gameStarted)
                    {
                        await cea.Channel.SendWithRetry("There is already a game in progress.");
                        return;
                    }

                    gameOpen = false;
                    gameStarted = true;
                    await cea.Channel.SendWithRetry("Starting game.");
                    game = new Superfight(players, cea.Channel, _basePath);
                    await game.StartGame();
                });

            manager.Client.GetService<CommandService>().CreateCommand("startvote")
                .AddCheck((c, u, ch) => u.Roles.Contains(ch.Server.GetRole(UInt64.Parse(_config["FGO_Admins"]))) && ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Do(async cea =>
                {
                    game.StartVote();
                    await cea.Channel.SendWithRetry("Discussion time is over, it's time to vote who would win.");
                });

            manager.Client.GetService<CommandService>().CreateCommand("endsf")
                .AddCheck((c, u, ch) => u.Roles.Contains(ch.Server.GetRole(UInt64.Parse(_config["FGO_Admins"]))) && ch.Id == UInt64.Parse(_config["FGO_Powerlevels"]))
                .Do(async cea =>
                {
                    await cea.Channel.SendWithRetry(game.EndGame());
                    gameStarted = false;
                });
        }
    }
}
