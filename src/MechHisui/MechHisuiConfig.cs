using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
using Discord.Addons.SimplePermissions;
using MechHisui.FateGOLib;
using MechHisui.SecretHitler;
using MechHisui.HisuiBets;
using Newtonsoft.Json;
using System.Linq;
using Discord.WebSocket;

namespace MechHisui.Core
{
    public class MechHisuiConfig : JsonConfigBase
    {
        public string Token { get; set; }
        public string FgoBasePath { get; set; }
        public string BankBasePath { get; set; }
        public string XduBasePath { get; set; }
        public string SHConfigPath { get; set; }
        public string SuperfightBasePath { get; set; }
        //public Dictionary<string, SecretHitlerConfig> SHConfigs { get; set; }

        public void AddBankAccount(SocketUser user)
        {
            var accounts = JsonConvert.DeserializeObject<List<UserAccount>>(File.ReadAllText(Path.Combine(BankBasePath, "bank.json")));
            accounts.Add(new UserAccount { UserId = user.Id, Bucks = 100 });
            File.WriteAllText(Path.Combine(BankBasePath, "bank.json"), JsonConvert.SerializeObject(accounts, Formatting.Indented));
        }

        public IEnumerable<UserAccount> GetBankAccounts()
        {
            return JsonConvert.DeserializeObject<List<UserAccount>>(File.ReadAllText(Path.Combine(BankBasePath, "bank.json")));
        }

        public IEnumerable<ServantProfile> GetAllServants()
        {
            var reals = JsonConvert.DeserializeObject<List<ServantProfile>>(File.ReadAllText(Path.Combine(FgoBasePath, "Servants.json")));
            var fakes = JsonConvert.DeserializeObject<List<ServantProfile>>(File.ReadAllText(Path.Combine(FgoBasePath, "FakeServants.json")));
            return reals.Concat(fakes);
        }

        public IEnumerable<CEProfile> GetAllCEs()
        {
            return JsonConvert.DeserializeObject<List<CEProfile>>(File.ReadAllText(Path.Combine(FgoBasePath, "CEs.json")));
        }

        public IEnumerable<MysticCode> GetAllMystics()
        {
            return JsonConvert.DeserializeObject<List<MysticCode>>(File.ReadAllText(Path.Combine(FgoBasePath, "MysticCodes.json")));
        }

        public IEnumerable<Event> GetAllEvents()
        {
            return JsonConvert.DeserializeObject<List<Event>>(File.ReadAllText(Path.Combine(FgoBasePath, "Events.json")));
        }
    }

    //    public class MechHisuiConfig : EFBaseConfigContext<HisuiGuild, HisuiChannel, HisuiUser>
    //    {
    //        public DbSet<ServantProfile> Servants { get; set; }
    //        public DbSet<ServantProfile> FakeServants { get; set; }
    //        public DbSet<ServantAlias> ServantAliases { get; set; }
    //        public DbSet<CEProfile> CEs { get; set; }
    //        public DbSet<CEAlias> CEAliases { get; set; }
    //        public DbSet<MysticCode> MysticCodes { get; set; }
    //        public DbSet<MysticAlias> MysticAliases { get; set; }
    //        public DbSet<Event> Events { get; set; }
    //        public DbSet<NameOnlyServant> NameOnlyServants { get; set; }
    //        public DbSet<FriendData> FriendData { get; set; }
    //        public DbSet<StringDict> MiscStrings { get; set; }
    //        public DbSet<SecretHitlerConfig> SHConfigs { get; set; }

    //        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //        {
    //            optionsBuilder.UseSqlite(File.ReadAllText("conn.txt"));

    //            base.OnConfiguring(optionsBuilder);
    //        }
    //    }

    //    public sealed class HisuiUser : ConfigUser
    //    {
    //        public int Bucks { get; set; } = 100;
    //    }

    //    public sealed class HisuiChannel : ConfigChannel<HisuiUser>
    //    {
    //    }

    //    public sealed class HisuiGuild : ConfigGuild<HisuiChannel, HisuiUser>
    //    {
    //    }

    //    public sealed class StringDict
    //    {
    //        public int Id { get; set; }
    //        public string Key { get; set; }
    //        public string Value { get; set; }
    //    }
}
