using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace MechHisui.HisuiBets
{
    public sealed class Bet
    {
        public string UserName { get; internal set; }
        public ulong UserId { get; internal set; }
        public string Tribute { get; internal set; }
        public int BettedAmount { get; internal set; }
    }

    public sealed class UserAccount
    {
        public ulong UserId { get; set; }
        public int Bucks { get; set; }
    }

    public sealed class BankOfHisui
    {
        public Func<SocketGuildUser, Task> AddUser { get; set; }
        public Func<IEnumerable<UserAccount>> GetAllUsers { get; set; }
        public Func<ulong, UserAccount> GetUser { get; set; }
        public Func<IEnumerable<Bet>, string, BetResult> CashOut { get; set; }

        public Action Interest { get; set; }
        public Action<ulong, ulong, int> Donate { get; set; }
        public Action<ulong, int> Take { get; set; }
    }

    public sealed class BetResult
    {
        public int RoundingLoss { get; set; }
        public Dictionary<ulong, int> Winners { get; set; }
    }

    public enum GameType
    {
        Any = 0,
        HungerGame = 1,
        HungerGameDistrictsOnly = 2,
        SaltyBet = 3
    }

    public enum SaltyBetTeam
    {
        Red = 0,
        Blue = 1
    }
}
