using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.Core
{
    public sealed partial class MechHisuiConfig : EFBaseConfigContext<HisuiGuild, HisuiChannel, HisuiUser>
    {
        public MechHisuiConfig(DbContextOptions options, CommandService commandService)
            : base(options, commandService)
        {
        }

        //misc
        public DbSet<NamedScalar> Scalars { get; set; }
        public DbSet<SecretHitlerTheme> SHThemes { get; set; }
        public DbSet<SuperfightCard> SFCards { get; set; }
        public DbSet<PreliminaryBet> RecordedBets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureFgoModel(modelBuilder);

            modelBuilder.Entity<PreliminaryBet>()
                .HasOne(b => b.Channel);

            modelBuilder.Entity<PreliminaryBet>()
                .HasOne(b => b.User);

            //modelBuilder.Entity<XduSkill>()
            //    .Property(s => s.Id)
            //    .ValueGeneratedOnAdd();

            //modelBuilder.Entity<CharacterVariation>()
            //    .Property(v => v.Id)
            //    .ValueGeneratedNever();

            //modelBuilder.Entity<CharacterVariation>()
            //    .HasMany(v => v.Skills);

            //modelBuilder.Entity<Memoria>()
            //    .Property(v => v.Id)
            //    .ValueGeneratedNever();

            base.OnModelCreating(modelBuilder);
        }
    }

    public class ConfigFactory : IDesignTimeDbContextFactory<MechHisuiConfig>
    {
        public MechHisuiConfig CreateDbContext(string[] args)
        {
            var map = new ServiceCollection()
                .AddSingleton(new CommandService())
                .AddDbContext<MechHisuiConfig>(opt => opt.UseSqlite(@"Data Source=test.sqlite"))
                .BuildServiceProvider();

            return map.GetService<MechHisuiConfig>();
        }
    }
}
