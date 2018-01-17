using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
//using MechHisui.FateGOLib;

namespace Kohaku
{
    //internal class KohakuConfig : JsonConfigBase
    //{
    //    public string LoginToken { get; set; }

    //    //public AudioConfig AudioConfig { get; set; }

    //    //public Dictionary<string, string[]> TriviaData { get; set; }

    //    //[JsonIgnore]
    //    //public List<ServantProfile> TestProfiles { get; set; }
    //}

    public class KohakuUser : ConfigUser
    {
        public string FgoFriendCode { get; set; }
    }

    public sealed class KohakuConfig : EFBaseConfigContext<KohakuUser>
    {
        public DbSet<StringKeyValuePair> Strings { get; set; }

        //public DbSet<ServantProfile> Servants { get; set; }
        //public DbSet<ServantSkill> Skills { get; set; }
        //public DbSet<ServantTrait> Traits { get; set; }
        //public DbSet<ServantAlias> ServantAliases { get; set; }

        public KohakuConfig(DbContextOptions options, CommandService commandService)
            : base(options, commandService)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<ServantProfile>()
            //    .HasMany(s => s.ActiveSkills);

            //modelBuilder.Entity<ServantProfile>()
            //    .HasMany(s => s.PassiveSkills);

            //modelBuilder.Entity<ServantProfile>()
            //    .HasMany(s => s.Traits);

            //modelBuilder.Entity<ServantProfile>()
            //    .HasMany(s => s.Aliases)
            //    .WithOne(a => a.Servant);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class StringKeyValuePair
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }
    }

    public class Factory : IDesignTimeDbContextFactory<KohakuConfig>
    {
        public KohakuConfig CreateDbContext(string[] args)
        {
            var map = new ServiceCollection()
                .AddSingleton(new CommandService())
                .AddDbContext<KohakuConfig>(opt => opt.UseSqlite(@"Data Source=test.sqlite"))
                .BuildServiceProvider();

            return map.GetService<KohakuConfig>();
        }
    }
}
