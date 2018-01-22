using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace MechHisui.HisuiBets
{
    public interface IBankOfHisui
    {
        UserAccount GetUser(ulong id);
        Task<IEnumerable<UserAccount>> GetAllUsers();

        Task<bool> AddUser(SocketGuildUser user);
        Task<BetResult> CashOut(BetCollection bets, string winner);
        bool Donate(ulong donorId, ulong recepientId, uint amount);
        void Withdraw(ulong debtorId, uint amount);
        void Interest();

        void AddToVault(uint amount);
        int RetrieveFromVault(uint amount);
    }
}
