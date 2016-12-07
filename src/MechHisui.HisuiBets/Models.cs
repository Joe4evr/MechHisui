
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MechHisui.HisuiBets
{
    public class Bet
    {
        public string UserName { get; internal set; }
        public ulong UserId { get; internal set; }
        public string Tribute { get; internal set; }
        public int BettedAmount { get; internal set; }
    }

    public class UserAccount
    {
        public ulong UserId { get; internal set; }
        public int Bucks { get; internal set; }

        public override int GetHashCode()
        {
            return this.UserId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is UserAccount) ? ((UserAccount)obj).UserId == this.UserId : false;
        }
    }

    public class BankOfHisui
    {
        public HashSet<UserAccount> Accounts = new HashSet<UserAccount>();
        public string Path { get; }

        public BankOfHisui(string path)
        {
            Path = path;
        }

        public void ReadBank()
        {
            Accounts = JsonConvert.DeserializeObject<HashSet<UserAccount>>(File.ReadAllText(Path));
        }

        public void WriteBank()
        {
            File.WriteAllText(Path, JsonConvert.SerializeObject(Accounts, Formatting.Indented));
        }
    }

    public enum GameType
    {
        Any,
        HungerGame,
        HungerGameDistrictsOnly,
        SaltyBet
    }

    public enum SaltyBetTeam
    {
        Red,
        Blue
    }
}
