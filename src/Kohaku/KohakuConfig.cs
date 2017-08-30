using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using Microsoft.EntityFrameworkCore;
using MechHisui.FateGOLib;

namespace Kohaku
{
    //internal class KohakuConfig : JsonConfigBase
    //{
    //    public string LoginToken { get; set; }

    //    public AudioConfig AudioConfig { get; set; }

    //    //public Dictionary<string, string[]> TriviaData { get; set; }

    //    //[JsonIgnore]
    //    //public List<ServantProfile> TestProfiles { get; set; }
    //}

    internal class KohakuConfig : EFBaseConfigContext
    {
        public DbSet<StringKeyValuePair> Strings { get; set; }

        public DbSet<ServantProfile> Servants { get; set; }
        public DbSet<ServantSkill> Skills { get; set; }
        public DbSet<ServantTrait> Traits { get; set; }
        public DbSet<ServantAlias> ServantAliases { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase();

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServantProfile>()
                .HasMany(s => s.ActiveSkills);
            modelBuilder.Entity<ServantProfile>()
                .HasMany(s => s.PassiveSkills);
            modelBuilder.Entity<ServantProfile>()
                .HasMany(s => s.Traits);
            modelBuilder.Entity<ServantProfile>()
                .HasMany(s => s.Aliases)
                .WithOne(a => a.Servant);


            base.OnModelCreating(modelBuilder);
        }
    }

    public class StringKeyValuePair
    {
        [Key]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
