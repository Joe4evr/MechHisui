using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MechHisui.HisuiBets
{
    public interface IBankOfHisui
    {
        IBankAccount GetAccount(IUser user);
        Task<IEnumerable<IBankAccount>> GetAllUsers();

        Task<RecordingResult> RecordOrUpdateBet(IBet bet);
        Task<IBet> RetrieveBet(IUser user, ITextChannel channel);
        Task<IEnumerable<IBet>> RetrieveAllBets(ITextChannel channel);

        Task<bool> AddUser(SocketGuildUser user);
        Task AddUsers(IEnumerable<SocketGuildUser> users);

        Task<BetResult> CashOut(BetCollection bets, string winner);
        Task<DonationResult> Donate(DonationRequest request);

        Task<WithdrawalResult> Withdraw(WithdrawalRequest request);
        Task WithdrawAll(IEnumerable<WithdrawalRequest> requests);
        Task Interest();

        Task AddToVault(uint amount);
        Task<int> RetrieveFromVault(uint amount);
    }
}
