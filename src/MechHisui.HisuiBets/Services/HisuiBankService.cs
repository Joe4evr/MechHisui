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
        private static readonly IEqualityComparer<SocketGuildUser> _userComparer = DiscordComparers.UserComparer;
        private readonly Timer _interestTimer;
        private readonly Func<LogMessage, Task> _logger;
        private readonly ConcurrentDictionary<ulong, BetGame> _games = new ConcurrentDictionary<ulong, BetGame>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        internal IBankOfHisui Bank { get; }
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

        public HisuiBankService(
            IBankOfHisui bank,
            DiscordSocketClient client,
            Func<LogMessage, Task> logger = null)
        {
            Bank = bank;
            _logger = logger ?? (_ => Task.CompletedTask);
            _interestTimer = new Timer(_ =>
            {
                Log(LogSeverity.Verbose, "Increasing users' HisuiBucks.");
                Bank.InterestAsync();
            }, null,
            TimeSpan.FromMinutes(60 - DateTime.Now.Minute),
            TimeSpan.FromHours(1));

            client.Ready += () =>
            {
                Task.Run(async () =>
                {
                    var uncashed = await Bank.GetUncashedGamesAsync().ConfigureAwait(false);
                    if (uncashed.Any())
                    {
                        foreach (var game in uncashed)
                        {
                            if (client.GetChannel(game.ChannelId) is SocketTextChannel channel)
                                await channel.SendMessageAsync($"Found betting game that is not cashed out. Use `setwin {game.Id} <winner>` to pay out the stakes.").ConfigureAwait(false);
                        }
                    }
                });
                return Task.CompletedTask;
            };
            client.UserJoined += user =>
            {
                Task.Run(async () =>
                {
                    if (!user.IsBot && !Blacklist.Contains(user.Id))
                    {
                        await Bank.AddUserAsync(user).ConfigureAwait(false);
                        //if (ac != null)
                        //    await Log(LogSeverity.Verbose, $"Registered {user.Username} for a bank account.").ConfigureAwait(false);
                    }
                });
                return Task.CompletedTask;
            };
            client.GuildAvailable += guild =>
            {
                Task.Run(async () =>
                {
                    await guild.DownloadUsersAsync().ConfigureAwait(false);
                    await _semaphore.WaitAsync().ConfigureAwait(false);

                    var registered = (await Bank.GetAllUsersAsync().ConfigureAwait(false))
                        .Select(a => guild.GetUser(a.UserId));
                    var newUsers = guild.Users
                        .Where(u => !u.IsBot && !Blacklist.Contains(u.Id))
                        .Except(registered, _userComparer)
                        .ToList();

                    if (newUsers.Count > 0)
                    {
                        await Log(LogSeverity.Info, $"Registering new users in {guild.Name}").ConfigureAwait(false);
                        await Bank.AddUsersAsync(newUsers).ConfigureAwait(false);
                    }
                    _semaphore.Release();
                });
                return Task.CompletedTask;
            };
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "HisuiBank", msg));
        }

        internal bool TryAddNewGame(IMessageChannel channel, BetGame game)
        {
            var success = _games.TryAdd(channel.Id, game);
            if (success)
            {
                game.GameEnd = OnGameEnd;
            }

            return success;
        }

        private void OnGameEnd(IMessageChannel channel)
            => _games.TryRemove(channel.Id, out var game);
    }

    //public static class HisuiBankExtensions
    //{
    //    public static Task UseHisuiBank(
    //        this CommandService commands,
    //        IServiceCollection map,
    //        IBankOfHisui bank,
    //        DiscordSocketClient client,
    //        Func<LogMessage, Task> logger = null)
    //    {
    //        map.AddSingleton(new HisuiBankService(bank, client, logger ?? (msg => Task.CompletedTask)));
    //        return commands.AddModuleAsync<HisuiBetsModule>();
    //    }
    //}
}
