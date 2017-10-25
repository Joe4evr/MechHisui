using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace Kohaku
{
    public class DesignTimeFactory : IDesignTimeDbContextFactory<KohakuConfig>
    {
        public KohakuConfig CreateDbContext(string[] args)
        {
            var map = new ServiceCollection()
                .AddSingleton(new CommandService())
                .AddDbContext<KohakuConfig>(options => options.UseSqlite(@"Data Source=..\Kohaku.sqlite"))
                .BuildServiceProvider();

            return map.GetService<KohakuConfig>();
        }
    }
}
