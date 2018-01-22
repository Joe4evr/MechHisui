﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Addons.SimplePermissions;
using Discord.WebSocket;
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
        public Task<IEnumerable<UserAccount>> GetAllUsers()
        {
            using (var config = _store.Load())
            {
                IEnumerable<UserAccount> uas = config.Users.Select(u => new UserAccount { UserId = u.UserId, Bucks = u.BankBalance }).ToList();
                return Task.FromResult(uas);
            }
        }

        public UserAccount GetUser(ulong id)
        {
            using (var config = _store.Load())
            {
                var user = GetConfigUser(id, config);
                return (user != null)
                    ? new UserAccount { UserId = id, Bucks = user.BankBalance }
                    : null;
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

        public Task<BetResult> CashOut(BetCollection betcollection, string winner)
        {
            uint loss = 0;
            var winners = betcollection.Bets.Where(b => b.Tribute.Equals(winner, StringComparison.OrdinalIgnoreCase)).ToList();
            var wholeSum = betcollection.Bets.Sum(b => b.BettedAmount) + betcollection.Bonus;
            decimal loserSum = betcollection.Bets
                .Where(b => !b.Tribute.Equals(winner, StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.BettedAmount);
            decimal winnerSum = wholeSum - loserSum;

            var windict = new Dictionary<ulong, uint>();

            using (var config = _store.Load())
            {
                foreach (var user in winners)
                {
                    var payout = (uint)((loserSum / winnerSum) * user.BettedAmount) + user.BettedAmount;
                    var us = GetConfigUser(user.UserId, config);
                    us.BankBalance += (int)payout;
                    windict.Add(us.UserId, payout);
                    loss += payout;
                }
                config.SaveChanges();
            }
            return Task.FromResult(new BetResult
            {
                RoundingLoss = loss,
                Winners = windict
            });
        }

        public bool Donate(ulong donorId, ulong recepientId, uint amount)
        {
            using (var config = _store.Load())
            {
                var donor = GetConfigUser(donorId, config);
                var recepient = GetConfigUser(recepientId, config);
                if (donor != null && recepient != null && donor.BankBalance >= amount)
                {
                    donor.BankBalance -= (int)amount;
                    recepient.BankBalance += (int)amount;
                    config.SaveChanges();
                    return true;
                }
                return false;
            }
        }

        public void Interest()
        {
            using (var config = _store.Load())
            {
                config.Users.FromSql(@"UPDATE Users SET BankBalance = BankBalance + 10 WHERE BankBalance < 2500");
                config.SaveChanges();
            }
        }

        public void Withdraw(ulong debtorId, uint amount)
        {
            using (var config = _store.Load())
            {
                var debtor = GetConfigUser(debtorId, config);
                if (debtor != null)
                {
                    debtor.BankBalance -= (int)amount;
                    config.SaveChanges();
                }
            }
        }

        public void AddToVault(uint amount)
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
        }

        public int RetrieveFromVault(uint amount)
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
                        return withdraw;
                    }
                }
                return 0;
            }
        }

        //query helpers
        private static HisuiUser GetConfigUser(ulong userId, MechHisuiConfig config)
            => config.Users.SingleOrDefault(u => u.UserId == userId);

        private static NamedScalar GetVault(MechHisuiConfig config)
            => config.Scalars.SingleOrDefault(s => s.Key == "Vault");
    }
}
