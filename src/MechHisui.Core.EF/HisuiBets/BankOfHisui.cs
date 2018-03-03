using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using MechHisui.HisuiBets;

namespace MechHisui.Core
{
    public sealed class BankOfHisui : IBankOfHisui
    {
        private readonly IConfigStore<MechHisuiConfig> _store;

        public BankOfHisui(IConfigStore<MechHisuiConfig> store)
        {
            _store = store;
        }


        //read-only operations
        public Task<IEnumerable<IBankAccount>> GetAllUsers()
        {
            using (var config = _store.Load())
            {
                IEnumerable<IBankAccount> uas = config.Users.Select(u => new UserAccount { UserId = u.UserId, Bucks = u.BankBalance }).ToList();
                return Task.FromResult(uas);
            }
        }

        public IBankAccount GetAccount(IUser user)
        {
            using (var config = _store.Load())
            {
                var cuser = GetConfigUser(user.Id, config);
                return (user != null)
                    ? new UserAccount { UserId = user.Id, Bucks = cuser.BankBalance }
                    : null;
            }
        }

        public Task<IBet> RetrieveBet(IUser user, ITextChannel channel)
        {
            using (var config = _store.Load())
            {
                return Task.FromResult<IBet>(QuerySingleBet(user.Id, channel.Id, config));
            }
        }

        public Task<IEnumerable<IBet>> RetrieveAllBets(ITextChannel channel)
        {
            using (var config = _store.Load())
            {
                return Task.FromResult<IEnumerable<IBet>>(QueryBets(config)
                    .Where(b => b.Channel.ChannelId == channel.Id)
                    .ToList());
            }
        }

        //writing operations
        public Task<bool> AddUser(SocketGuildUser user)
        {
            using (var config = _store.Load())
            {
                var exists = GetConfigUser(user.Id, config);
                if (exists == null)
                {
                    config.Users.Add(new HisuiUser { UserId = user.Id });
                    config.SaveChanges();
                    return Task.FromResult(true);
                }
                else
                {
                    return Task.FromResult(false);
                }
            }
        }

        public Task AddUsers(IEnumerable<SocketGuildUser> users)
        {
            using (var config = _store.Load())
            {
                foreach (var user in users)
                {
                    if (!config.Users.Any(u => u.UserId == user.Id))
                    {
                        config.Users.Add(new HisuiUser { UserId = user.Id });
                        config.SaveChanges();
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task<BetResult> CashOut(BetCollection betcollection, string winner)
        {
            uint loss = 0;
            var winners = betcollection.Bets.Where(b => b.Target.Equals(winner, StringComparison.OrdinalIgnoreCase)).ToList();

            decimal loserSum = betcollection.Bets
                .Where(b => !b.Target.Equals(winner, StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.BettedAmount);

            decimal winnerSum = betcollection.WholeSum - loserSum;

            var windict = new Dictionary<ulong, uint>();

            using (var config = _store.Load())
            {
                foreach (var user in winners)
                {
                    var payout = (uint)(((loserSum / winnerSum) * user.BettedAmount) + user.BettedAmount);
                    var us = GetConfigUser(user.UserId, config);
                    us.BankBalance += (int)payout;
                    windict.Add(us.UserId, payout);
                    loss += payout;
                }
                config.SaveChanges();
            }
            return Task.FromResult(new BetResult(loss, windict));
        }

        public Task<DonationResult> Donate(DonationRequest request)
        {
            using (var config = _store.Load())
            {
                var donor = GetConfigUser(request.DonorId, config);
                if (donor == null) return DonationResult.DonorNotFound;

                var recepient = GetConfigUser(request.RecepientId, config);
                if (recepient == null) return DonationResult.RecipientNotFound;

                var amount = (int)request.Amount;
                if (donor.BankBalance < amount) return DonationResult.DonorNotEnoughMoney;

                donor.BankBalance -= amount;
                recepient.BankBalance += amount;
                config.SaveChanges();
                return DonationResult.DonationSuccess;
                //return false;
            }
        }

        public async Task Interest()
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
                await config.Users.Where(u => u.BankBalance < 2500)
                    .ForEachAsync(u => u.BankBalance += 10);
                config.SaveChanges();
            }
        }

        public Task<RecordingResult> RecordOrUpdateBet(IBet bet)
        {
            using (var config = _store.Load())
            {
                var recBet = QuerySingleBet(bet.UserId, bet.ChannelId, config);
                if (recBet == null)
                {
                    var user = GetConfigUser(bet.UserId, config);
                    var chan = GetConfigChannel(bet.ChannelId, config);
                    if (user != null && chan != null)
                    {
                        config.RecordedBets.Add(new PreliminaryBet
                        {
                            Channel = chan,
                            User = user,
                            UserName = bet.UserName,
                            Target = bet.Target,
                            BettedAmount = (int)bet.BettedAmount
                        });
                        config.SaveChanges();
                        return Task.FromResult(RecordingResult.BetAdded);
                    }
                }
                else
                {
                    if (bet.GameType == GameType.SaltyBet)
                    {
                        return Task.FromResult(RecordingResult.CannotReplaceOldBet);
                    }

                    int bettedAmount = (int)bet.BettedAmount;
                    if (bettedAmount < recBet.BettedAmount)
                    {
                        return Task.FromResult(RecordingResult.NewBetLessThanOldBet);
                    }

                    recBet.Target = bet.Target;
                    recBet.BettedAmount = bettedAmount;
                    config.RecordedBets.Update(recBet);
                    config.SaveChanges();
                    return Task.FromResult(RecordingResult.BetReplaced);
                }
            }
            return Task.FromResult(RecordingResult.MiscError);
        }

        public Task<WithdrawalResult> Withdraw(WithdrawalRequest request)
        {
            using (var config = _store.Load())
            {
                var debtor = GetConfigUser(request.AccountId, config);
                if (debtor == null) return Task.FromResult(WithdrawalResult.AccountNotFound);

                int amount = (int)request.Amount;
                if (debtor.BankBalance < amount) return Task.FromResult(WithdrawalResult.AccountNotEnoughMoney);

                debtor.BankBalance -= amount;
                config.SaveChanges();
                return Task.FromResult(WithdrawalResult.WithdrawalSuccess);
            }
        }

        public Task WithdrawAll(IEnumerable<WithdrawalRequest> requests)
        {
            using (var config = _store.Load())
            {
                foreach (var request in requests)
                {
                    var debtor = GetConfigUser(request.AccountId, config);
                    if (debtor != null)
                    {
                        int amount = (int)request.Amount;
                        if (debtor.BankBalance < amount) continue;

                        debtor.BankBalance -= amount;
                        config.SaveChanges();
                    }
                }
            }
            return Task.CompletedTask;
        }

        public Task AddToVault(uint amount)
        {
            using (var config = _store.Load())
            {
                var vault = GetVault(config);
                if (vault != null)
                {
                    vault.IntValue += (int)amount;
                    config.SaveChanges();
                }
            }
            return Task.CompletedTask;
        }

        public Task<int> RetrieveFromVault(uint amount)
        {
            using (var config = _store.Load())
            {
                var vault = GetVault(config);
                if (vault != null)
                {
                    var withdraw = (int)amount;
                    if (vault.IntValue > withdraw)
                    {
                        vault.IntValue -= withdraw;
                        config.SaveChanges();
                        return Task.FromResult(withdraw);
                    }
                }
                return Task.FromResult(0);
            }
        }

        //query helpers
        private static HisuiUser GetConfigUser(ulong userId, MechHisuiConfig config)
            => config.Users.SingleOrDefault(u => u.UserId == userId);

        private static HisuiChannel GetConfigChannel(ulong channelId, MechHisuiConfig config)
            => config.Channels.SingleOrDefault(c => c.ChannelId == channelId);

        private static NamedScalar GetVault(MechHisuiConfig config)
            => config.Scalars.SingleOrDefault(s => s.Key == "Vault");

        private static IQueryable<PreliminaryBet> QueryBets(MechHisuiConfig config)
            => config.RecordedBets
                .Include(b => b.Channel)
                .Include(b => b.User);

        private static PreliminaryBet QuerySingleBet(ulong userId, ulong channelId, MechHisuiConfig config)
            => QueryBets(config).SingleOrDefault(b => b.Channel.ChannelId == channelId && b.User.UserId == userId);
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
