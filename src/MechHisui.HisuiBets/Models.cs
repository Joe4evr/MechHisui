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
        public string Path { get; }

        public BankOfHisui(string path)
        {
            Path = path;
        }

        public void ReadBank(string path = null)
        {
            using (TextReader tr = new StreamReader(path ?? Path))
            {
                Accounts = JsonConvert.DeserializeObject<List<UserBucks>>(tr.ReadToEnd());
            }
        }

        public void WriteBank(string path = null)
        {
            using (TextWriter tw = new StreamWriter(path ?? Path))
            {
                tw.Write(JsonConvert.SerializeObject(Accounts, Formatting.Indented));
            }
        }
    }
}
