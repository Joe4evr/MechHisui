using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using MechHisui.HisuiBets;
using SharedExtensions;

namespace MechHisui.Core
{
    public sealed class BankOfHisui : IBankOfHisui
    {
        private readonly IConfigStore<MechHisuiConfig> _store;
        private readonly SemaphoreSlim _vaultLock = new SemaphoreSlim(1, 1);

        public BankOfHisui(IConfigStore<MechHisuiConfig> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        //read-only operations
        char IBankOfHisui.CurrencySymbol => '\u050A';

        async Task<IEnumerable<IBankAccount>> IBankOfHisui.GetAllUsersAsync()
        {
            using (var config = _store.Load())
            {
                return await config.Users
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        IBankAccount IBankOfHisui.GetAccount(IUser user)
        {
            using (var config = _store.Load())
            {
                return GetConfigUser(user.Id, config);
                //var cuser = GetConfigUser(user.Id, config);
                //return (cuser != null)
                //    ? new UserAccount { UserId = user.Id, Balance = cuser.BankBalance }
                //    : null;
            }
        }

        async Task<IEnumerable<IBetGame>> IBankOfHisui.GetUncashedGamesAsync()
        {
            using (var config = _store.Load())
            {
                return await config.BetGames
                    .AsNoTracking()
                    .Include(g => g.Channel)
                    .Include(g => g.Bets)
                    .ThenInclude(b => b.User)
                    .Where(g => !g.IsCashedOut)
                    .ToListAsync();
            }
        }

        async Task<IBetGame> IBankOfHisui.GetLastGameInChannelAsync(ITextChannel channel)
        {
            using (var config = _store.Load())
            {
                return await config.BetGames
                    .AsNoTracking()
                    .Include(g => g.Channel)
                    .Include(g => g.Bets)
                    .ThenInclude(b => b.User)
                    .LastOrDefaultAsync(g => g.Channel.ChannelId == channel.Id);
            }
        }

        async Task<IBetGame> IBankOfHisui.GetGameInChannelByIdAsync(ITextChannel channel, int gameId)
        {
            using (var config = _store.Load())
            {
                return await config.BetGames
                    .AsNoTracking()
                    .Include(g => g.Channel)
                    .Include(g => g.Bets)
                    .ThenInclude(b => b.User)
                    .SingleOrDefaultAsync(g => g.Channel.ChannelId == channel.Id && g.Id == gameId);
            }
        }

        async Task<IBet> IBankOfHisui.RetrieveBetAsync(IUser user, IBetGame game)
        {
            using (var config = _store.Load())
            {
                return await config.RecordedBets
                    .AsNoTracking()
                    .SingleOrDefaultAsync(b => b.BetGame.Id == game.Id && b.User.UserId == user.Id);
            }
        }

        Task<int> IBankOfHisui.GetVaultWorthAsync()
        {
            using (var config = _store.Load())
            {
                return Task.FromResult(GetVault(config).IntValue);
            }
        }

        //writing operations
        IBetGame IBankOfHisui.CreateGame(ITextChannel channel, GameType gameType)
        {
            using (var config = _store.Load())
            {
                var game = new BetGame
                {
                    Channel = GetConfigChannel(channel.Id, config),
                    GameType = gameType
                };
                config.BetGames.Add(game);
                config.SaveChanges();
                return game;
            }
        }

        Task<IBankAccount> IBankOfHisui.AddUserAsync(IUser user)
        {
            using (var config = _store.Load())
            {
                return Task.FromResult<IBankAccount>(GetConfigUser(user.Id, config));
            //    var ac = GetAccount(user.Id, config);
            //    if (ac == null)
            //    {
            //        ac = new BankAccount { UserId = user.Id };
            //        config.BankAccounts.Add(ac);
            //        await config.SaveChangesAsync().ConfigureAwait(false);
            //    }
            //    return ac;
            }
        }

        Task IBankOfHisui.AddUsersAsync(IEnumerable<IUser> users)
        {
            return Task.CompletedTask;
            //using (var config = _store.Load())
            //{
            //    foreach (var user in users)
            //    {
            //        config.BankAccounts.Add(new BankAccount { UserId = user.Id });
            //    }
            //    await config.SaveChangesAsync().ConfigureAwait(false);
            //}
        }

        async Task<BetResult> IBankOfHisui.CashOutAsync(BetCollection betcollection, string winner)
        {
            var winningBets = betcollection.Bets.Where(b => b.Target.Equals(winner, StringComparison.OrdinalIgnoreCase)).ToList();

            decimal loserSum = betcollection.Bets
                .Where(b => !b.Target.Equals(winner, StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.BettedAmount);

            int total = betcollection.WholeSum;
            decimal winnerSum = total - loserSum;

            var windict = new Dictionary<ulong, int>();

            using (var config = _store.Load())
            {
                foreach (var bet in winningBets)
                {
                    var payout = (int)Math.Floor(((loserSum / winnerSum) * bet.BettedAmount) + bet.BettedAmount);
                    var ac = GetConfigUser(bet.UserId, config);
                    ac.Balance += payout;
                    windict.Add(ac.UserId, payout);
                    total -= payout;
                }
                var game = await config.BetGames
                    .Include(g => g.Channel)
                    .Include(g => g.Bets)
                    .ThenInclude(b => b.User)
                    .SingleOrDefaultAsync(g => g.Id == betcollection.GameId);
                game.IsCashedOut = true;

                await config.SaveChangesAsync().ConfigureAwait(false);
            }
            return new BetResult(total, windict);
        }

        async Task<DonationResult> IBankOfHisui.DonateAsync(DonationRequest request)
        {
            using (var config = _store.Load())
            {
                var donor = GetConfigUser(request.DonorId, config);
                if (donor == null)
                    return DonationResult.DonorNotFound;

                var recepient = GetConfigUser(request.RecepientId, config);
                if (recepient == null)
                    return DonationResult.RecipientNotFound;

                var amount = (int)request.Amount;
                if (donor.Balance < amount)
                    return DonationResult.DonorNotEnoughMoney;

                donor.Balance -= amount;
                recepient.Balance += amount;
                await config.SaveChangesAsync().ConfigureAwait(false);
                return DonationResult.DonationSuccess;
            }
        }

        async Task IBankOfHisui.InterestAsync()
        {
            using (var config = _store.Load())
            {
                //// The ideal way:
                //config.Users.Where(u => u.BankBalance < 2500)
                //    .Update(u => new { u.BankBalance },
                //        o => new { BankBalance = o.BankBalance + 10 })
                //    .Load();

                ////The "what's supposed to be the alternative but is broken" way:
                //config.Users.FromSql(@"UPDATE Users SET BankBalance = BankBalance + 10 WHERE BankBalance < 2500").Load();

                //// The slow but working way:
                //var updating = config.Users.Where(u => u.BankBalance < 2500).ToList();
                //foreach (var user in updating)
                //{
                //    user.BankBalance += 10;
                //    config.Users.Update(user);
                //}

                // The "meh" but working way:
                await config.Users.Where(u => u.Balance < 2500)
                    .ForEachAsync(u => u.Balance += 10).ConfigureAwait(false);

                await config.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        async Task<RecordingResult> IBankOfHisui.RecordOrUpdateBetAsync(IBetGame game, IBet bet)
        {
            using (var config = _store.Load())
            {
                var recBet = QuerySingleBet(bet.UserId, game.Id, config);
                if (recBet == null) //new bet
                {
                    var user = GetConfigUser(bet.UserId, config);
                    if (user != null)
                    {
                        config.RecordedBets.Add(new Bet
                        {
                            BetGame = config.BetGames.Single(g => g.Id == game.Id),
                            User = user,
                            UserName = bet.UserName,
                            Target = bet.Target,
                            BettedAmount = bet.BettedAmount
                        });
                        await config.SaveChangesAsync().ConfigureAwait(false);
                        return RecordingResult.BetAdded;
                    }
                }
                else
                {
                    if (recBet.BetGame.GameType == GameType.SaltyBet)
                    {
                        return RecordingResult.CannotReplaceOldBet;
                    }

                    int bettedAmount = bet.BettedAmount;
                    if (bettedAmount < recBet.BettedAmount)
                    {
                        return RecordingResult.NewBetLessThanOldBet;
                    }

                    recBet.Target = bet.Target;
                    recBet.BettedAmount = bettedAmount;
                    config.RecordedBets.Update(recBet);
                    await config.SaveChangesAsync().ConfigureAwait(false);
                    return RecordingResult.BetReplaced;
                }
            }
            return RecordingResult.MiscError;
        }

        async Task<WithdrawalResult> IBankOfHisui.WithdrawAsync(WithdrawalRequest request)
        {
            using (var config = _store.Load())
            {
                var debtor = GetConfigUser(request.AccountId, config);
                if (debtor == null)
                    return WithdrawalResult.AccountNotFound;

                int amount = request.Amount;
                if (debtor.Balance < amount)
                    return WithdrawalResult.AccountNotEnoughMoney;

                debtor.Balance -= amount;
                await config.SaveChangesAsync().ConfigureAwait(false);
                return WithdrawalResult.WithdrawalSuccess;
            }
        }

        async Task IBankOfHisui.CollectBetsAsync(int gameId)
        {
            using (var config = _store.Load())
            {
                var game = await config.BetGames
                    .Include(g => g.Channel)
                    .Include(g => g.Bets)
                    .ThenInclude(b => b.User)
                    .SingleOrDefaultAsync(g => g.Id == gameId).ConfigureAwait(false);
                foreach (var bet in game.Bets)
                {
                    var debtor = GetConfigUser(bet.User.UserId, config);
                    if (debtor != null)
                    {
                        int amount = bet.BettedAmount;
                        if (debtor.Balance < amount) continue;

                        debtor.Balance -= amount;
                    }
                }
                //var mgame = await config.BetGames.SingleOrDefaultAsync(g => g.Id == game.Id);
                game.IsCollected = true;

                await config.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        async Task IBankOfHisui.AddToVaultAsync(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            using (await _vaultLock.UsingLock().ConfigureAwait(false))
            using (var config = _store.Load())
            {
                GetVault(config).IntValue += amount;
                await config.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        async Task<int> IBankOfHisui.RetrieveFromVaultAsync(int amount)
        {
            if (amount <= 50)
                return 0;

            using (await _vaultLock.UsingLock().ConfigureAwait(false))
            using (var config = _store.Load())
            {
                var vault = GetVault(config);
                var withdrawing = Math.Min(vault.IntValue, amount);

                vault.IntValue -= withdrawing;
                await config.SaveChangesAsync().ConfigureAwait(false);
                return withdrawing;
            }
        }

        //query helpers
        private static HisuiUser GetConfigUser(ulong userId, MechHisuiConfig config)
            => config.Users.SingleOrDefault(u => u.UserId == userId);

        //private static IBankAccount GetAccount(ulong userId, MechHisuiConfig config)
        //    => config.Users.SingleOrDefault(a => a.UserId == userId);

        private static HisuiChannel GetConfigChannel(ulong channelId, MechHisuiConfig config)
            => config.Channels.SingleOrDefault(c => c.ChannelId == channelId);

        private static NamedScalar GetVault(MechHisuiConfig config)
        {
            var vault = config.Scalars.SingleOrDefault(s => s.Key == "Vault");
            if (vault == null)
            {
                vault = new NamedScalar { Key = "Vault" };
                config.Scalars.Add(vault);
                config.SaveChanges();
            }

            return vault;
        }

        private static NamedScalar GetIdTracker(MechHisuiConfig config)
            => config.Scalars.SingleOrDefault(s => s.Key == "GameId")
                    ?? new NamedScalar { Key = "GameId" };

        private static IQueryable<Bet> QueryBets(MechHisuiConfig config)
            => config.RecordedBets
                .Include(b => b.BetGame)
                .Include(b => b.User);

        private static IQueryable<Bet> QueryAllBetsIn(ulong channelId, MechHisuiConfig config)
            => QueryBets(config).Where(b => b.BetGame.Channel.ChannelId == channelId);

        private static Bet QuerySingleBet(ulong userId, int gameId, MechHisuiConfig config)
            => QueryBets(config).SingleOrDefault(b => b.BetGame.Id == gameId && b.User.UserId == userId);
    }

    //static class Ex
    //{
    //    public static IQueryable<T> Update<T, TProp>(
    //        this IQueryable<T> source,
    //        Expression<Func<T, TProp>> propertySelector,
    //        Expression<Func<TProp, TProp>> updateValue)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
