using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace MechHisui.HisuiBets
{
    public sealed class HisuiBankService
    {
        public const char symbol = '\u050A';
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
        private readonly ConcurrentDictionary<ulong, BetGame> _games = new ConcurrentDictionary<ulong, BetGame>();

        internal readonly BankOfHisui Bank;
        private readonly Timer _upTimer;

        internal IReadOnlyDictionary<ulong, BetGame> Games => _games;

        public HisuiBankService(DiscordSocketClient client, BankOfHisui bank)
        {
            Bank = bank;
            Bank.ReadBank();
            _upTimer = new Timer(cb =>
            {
                Console.WriteLine($"{DateTime.Now}: Increasing users' HisuiBucks.");
                foreach (var user in Bank.Accounts)
                {
                    if (user.Bucks < 2500)
                    {
                        user.Bucks += 10;
                    }
                }
                Bank.WriteBank();
            },
            null,
            TimeSpan.FromMinutes(60 - DateTime.Now.Minute),
            TimeSpan.FromHours(1));

            client.UserJoined += user =>
            {
                if (!user.IsBot && !_blacklist.Contains(user.Id) && Bank.Accounts.Add(new UserAccount { UserId = user.Id, Bucks = 100 }))
                {
                    Bank.WriteBank();
                    Console.WriteLine($"{DateTime.Now} - Registered {user.Username} for a bank account.");
                }
                return Task.CompletedTask;
            };
        }

        public bool TryAddNewGame(ulong channelId, BetGame game)
        {
            var success = _games.TryAdd(channelId, game);
            if (success)
                game.GameEnd += _onGameEnd;

            return success;
        }
        private Task _onGameEnd(ulong channelId)
        {
            BetGame game;
            if (_games.TryRemove(channelId, out game))
            {
                game.GameEnd -= _onGameEnd;
            }
            return Task.CompletedTask;
        }
    }
}
