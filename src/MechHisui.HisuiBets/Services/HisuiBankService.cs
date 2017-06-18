using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MechHisui.HisuiBets
{
    public sealed class HisuiBankService
    {
        public const char Symbol = '\u050A';
        private static readonly IEqualityComparer<SocketGuildUser> _userComparer = Comparers.UserComparer;
        private readonly Timer _upTimer;
        private readonly Func<LogMessage, Task> _logger;
        private readonly ConcurrentDictionary<ulong, BetGame> _games = new ConcurrentDictionary<ulong, BetGame>();

        internal BankOfHisui Bank { get; }
        internal ulong[] Blacklist { get; } = new[]
        {
            121687861350105089ul,
            124939115396333570ul,
            145964622984380416ul,
            168215800946098176ul,
            168263616250904576ul,
            168298664698052617ul,
            175851451988312064ul
        };
        internal string[] Allins { get; } = new[] { "all", "allin" };

        internal IReadOnlyDictionary<ulong, BetGame> Games => _games;

        internal HisuiBankService(BankOfHisui bank, DiscordSocketClient client, Func<LogMessage, Task> logger)
        {
            Bank = bank;
            _logger = logger;
            _upTimer = new Timer(cb =>
            {
                Log(LogSeverity.Info, "Increasing users' HisuiBucks.");
                Bank.Interest();
                //foreach (var user in Bank.Accounts)
                //{
                //    if (user.Bucks < 2500)
                //    {
                //        user.Bucks += 10;
                //    }
                //}
                //Bank.WriteBank();
            },
            null,
            TimeSpan.FromMinutes(60 - DateTime.Now.Minute),
            TimeSpan.FromHours(1));

            client.UserJoined += async user => //Bank.AddUser;
            {
                if (!user.IsBot && !Blacklist.Contains(user.Id))
                {
                    await Bank.AddUser(user);
                    await Log(LogSeverity.Info, $"Registered {user.Username} for a bank account.");
                }
            };
            client.GuildAvailable += async guild =>
            {
                foreach (var user in guild.Users.Except(Bank.GetAllUsers().Select(a => guild.GetUser(a.UserId)), _userComparer))
                {
                    if (!user.IsBot && !Blacklist.Contains(user.Id))
                    {
                        await Bank.AddUser(user);
                        await Log(LogSeverity.Info, $"Registered {user.Username} for a bank account.");
                    }
                }
            };
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "HisuiBank", msg));
        }

        public bool TryAddNewGame(ulong channelId, BetGame game)
        {
            var success = _games.TryAdd(channelId, game);
            if (success)
            {
                game.GameEnd += _onGameEnd;
            }

            return success;
        }

        private Task _onGameEnd(ulong channelId)
        {
            if (_games.TryRemove(channelId, out var game))
            {
                game.GameEnd -= _onGameEnd;
            }
            return Task.CompletedTask;
        }
    }

    public static class HisuiBankExtensions
    {
        public static Task UseHisuiBank(
            this CommandService commands,
            IServiceCollection map,
            BankOfHisui bank,
            DiscordSocketClient client,
            Func<LogMessage, Task> logger = null)
        {
            map.AddSingleton(new HisuiBankService(bank, client, logger ?? (msg => Task.CompletedTask)));
            return commands.AddModuleAsync<HisuiBetsModule>();
        }
    }
}
