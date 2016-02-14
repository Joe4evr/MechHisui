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

    public class UserBucks
    {
        public ulong UserId { get; set; }
        public int Bucks { get; set; }
    }

    public class BankOfHisui
    {
        public IList<UserBucks> Accounts = new List<UserBucks>();

        public void ReadBank(string path)
        {
            using (TextReader tr = new StreamReader(path))
            {
                Accounts = JsonConvert.DeserializeObject<List<UserBucks>>(tr.ReadToEnd());
            }
        }

        public void WriteBank(string path)
        {
            using (TextWriter tw = new StreamWriter(path))
            {
                tw.Write(JsonConvert.SerializeObject(Accounts, Formatting.Indented));
            }
        }
    }
}
