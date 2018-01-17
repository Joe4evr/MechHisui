using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;

using MechHisui.SecretHitler;
using MechHisui.SymphoXDULib;

namespace MechHisui.Core
{
    public partial class MechHisuiConfig : EFBaseConfigContext<HisuiGuild, HisuiChannel, HisuiUser>
    {
        public MechHisuiConfig(DbContextOptions options, CommandService commandService) : base(options, commandService)
        {
        }

        //misc
        public DbSet<StringKeyValuePair> Strings { get; set; }
        public DbSet<SecretHitlerConfig> SHConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HisuiUser>()
                .HasOne(u => u.FriendData);


            modelBuilder.Entity<XduSkill>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<CharacterVariation>()
                .Property(v => v.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<CharacterVariation>()
                .HasMany(v => v.Skills);

            modelBuilder.Entity<Memoria>()
                .Property(v => v.Id)
                .ValueGeneratedNever();

            base.OnModelCreating(modelBuilder);
        }
    }

    public class StringKeyValuePair
    {
        [Key]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
