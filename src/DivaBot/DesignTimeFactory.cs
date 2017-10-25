using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using SharedExtensions;

namespace DivaBot
{
    public class DesignTimeFactory : IDesignTimeDbContextFactory<DivaBotConfig>
    {
        public DivaBotConfig CreateDbContext(string[] args)
        {
            //var p = Params.Parse(args);
            var map = new ServiceCollection()
                .AddSingleton(new CommandService())
                .AddDbContext<DivaBotConfig>(options => options.UseSqlite(@"Data Source=..\DivaBot.sqlite"))
                .BuildServiceProvider();

            return map.GetService<DivaBotConfig>();
        }
    }
}