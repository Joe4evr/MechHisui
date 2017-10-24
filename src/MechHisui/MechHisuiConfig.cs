using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
using Discord.Addons.SimplePermissions;
using MechHisui.FateGOLib;
using MechHisui.SecretHitler;
using MechHisui.HisuiBets;
using MechHisui.SymphoXDULib;
using Newtonsoft.Json;
using System.Linq;
using Discord.WebSocket;
using Discord.Commands;

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
            //var fakes = JsonConvert.DeserializeObject<List<ServantProfile>>(File.ReadAllText(Path.Combine(FgoBasePath, "FakeServants.json")));
            return reals.ToList();
        }

        public IEnumerable<CEProfile> GetAllCEs()
        {
            return JsonConvert.DeserializeObject<List<CEProfile>>(File.ReadAllText(Path.Combine(FgoBasePath, "CEs.json")));
        }

        public IEnumerable<MysticCode> GetAllMystics()
        {
            return JsonConvert.DeserializeObject<List<MysticCode>>(File.ReadAllText(Path.Combine(FgoBasePath, "MysticCodes.json")));
        }

        public IEnumerable<FgoEvent> GetAllEvents()
        {
            return JsonConvert.DeserializeObject<List<FgoEvent>>(File.ReadAllText(Path.Combine(FgoBasePath, "Events.json")));
        }
    }

    //public class MechHisuiConfig : EFBaseConfigContext<HisuiGuild, HisuiChannel, HisuiUser>
    //{
    //    public MechHisuiConfig(DbContextOptions options, CommandService commandService) : base(options, commandService)
    //    {
    //    }

    //    //FGO
    //    public DbSet<ServantProfile> Servants { get; set; }
    //    public DbSet<ServantSkill> FgoSkills { get; set; }
    //    public DbSet<ServantTrait> Traits { get; set; }
    //    public DbSet<ServantAlias> ServantAliases { get; set; }
    //    public DbSet<CEProfile> CEs { get; set; }
    //    public DbSet<CEAlias> CEAliases { get; set; }
    //    public DbSet<MysticCode> MysticCodes { get; set; }
    //    public DbSet<MysticAlias> MysticAliases { get; set; }
    //    public DbSet<FgoEvent> FgoEvents { get; set; }
    //    //public DbSet<NameOnlyServant> NameOnlyServants { get; set; }
    //    public DbSet<FgoFriendData> FgoFriendData { get; set; }

    //    //XDU
    //    //public DbSet<XduProfile> XduCharacters { get; set; }
    //    public DbSet<CharacterVariation> XduCharacters { get; set; }
    //    public DbSet<XduSkill> XduSkills { get; set; }
    //    public DbSet<Memoria> Memorias { get; set; }
    //    public DbSet<XduSong> XduSongs { get; set; }
    //    public DbSet<XduEvent> XduEvents { get; set; }

    //    //misc
    //    public DbSet<StringKeyValuePair> Strings { get; set; }
    //    public DbSet<SecretHitlerConfig> SHConfigs { get; set; }

    //    protected override void OnModelCreating(ModelBuilder modelBuilder)
    //    {
    //        modelBuilder.Entity<HisuiUser>()
    //            .HasOne(u => u.FriendData);


    //        modelBuilder.Entity<XduSkill>()
    //            .Property(s => s.Id)
    //            .ValueGeneratedOnAdd();

    //        modelBuilder.Entity<CharacterVariation>()
    //            .Property(v => v.Id)
    //            .ValueGeneratedNever();

    //        modelBuilder.Entity<CharacterVariation>()
    //            .HasMany(v => v.Skills);

    //        modelBuilder.Entity<Memoria>()
    //            .Property(v => v.Id)
    //            .ValueGeneratedNever();

    //        base.OnModelCreating(modelBuilder);
    //    }
    //}

    //public class StringKeyValuePair
    //{
    //    [Key]
    //    public string Key { get; set; }

    //    [Required]
    //    public string Value { get; set; }
    //}
}
