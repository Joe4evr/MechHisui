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
        Task<BetResult> CashOut(IEnumerable<Bet> bets, string winner);
        void Interest();
        void Donate(ulong donorId, ulong recepientId, uint amount);
        void Take(ulong debtorId, uint amount);
    }
}
