using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed partial class MechHisuiConfig : EFBaseConfigContext<HisuiGuild, HisuiChannel, HisuiUser>
    {
        public MechHisuiConfig(DbContextOptions options)
            : base(options)
        {
        }

        //misc
        public DbSet<Variant> Scalars { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureFgoModel(modelBuilder);
            ConfigureBetsModel(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class ConfigFactory : IDesignTimeDbContextFactory<MechHisuiConfig>
    {
        public MechHisuiConfig CreateDbContext(string[] args)
        {
            var map = new ServiceCollection()
                .AddDbContext<MechHisuiConfig>(opt => opt.UseSqlite(@"Data Source=test.sqlite"))
                .BuildServiceProvider();

            return map.GetService<MechHisuiConfig>();
        }
    }
}
