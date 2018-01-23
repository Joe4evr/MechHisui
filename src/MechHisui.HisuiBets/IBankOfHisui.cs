using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace MechHisui.HisuiBets
{
    public interface IBankOfHisui
    {
        IBankAccount GetUser(ulong id);
        Task<IEnumerable<IBankAccount>> GetAllUsers();

        Task<bool> AddUser(SocketGuildUser user);
        Task<BetResult> CashOut(BetCollection bets, string winner);
        bool Donate(DonationRequest request);
        void Withdraw(WithdrawalRequest request);
        void Withdraw(IEnumerable<WithdrawalRequest> requests);
        void Interest();

        void AddToVault(uint amount);
        int RetrieveFromVault(uint amount);
    }
}
