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
        internal readonly ulong[] _blacklist = new[]
        {
            121687861350105089ul,
            124939115396333570ul,
            145964622984380416ul,
            168215800946098176ul,
            168263616250904576ul,
            168298664698052617ul,
            175851451988312064ul
        };
        internal readonly string[] allins = new[] { "all", "allin" };
        private readonly Func<LogMessage, Task> _logger;
        private readonly ConcurrentDictionary<ulong, BetGame> _games = new ConcurrentDictionary<ulong, BetGame>();

        internal readonly BankOfHisui Bank;
        private readonly Timer _upTimer;

        internal IReadOnlyDictionary<ulong, BetGame> Games => _games;

        internal HisuiBankService(BankOfHisui bank, Func<LogMessage, Task> logger)
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

            //client.UserJoined += Bank.AddUser;
            //{
            //    if (!user.IsBot && !_blacklist.Contains(user.Id) && Bank.Accounts.Add(new UserAccount { UserId = user.Id, Bucks = 100 }))
            //    {
            //        Log(LogSeverity.Info, $"Registered {user.Username} for a bank account.");
            //    }
            //    return Task.CompletedTask;
            //};
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
            Func<LogMessage, Task> logger = null)
        {
            map.AddSingleton(new HisuiBankService(bank, logger ?? (msg => Task.CompletedTask)));
            return commands.AddModuleAsync<HisuiBetsModule>();
        }
    }
}
