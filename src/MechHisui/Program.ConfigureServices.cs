using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using MechHisui.Core;
using MechHisui.FateGOLib;
using MechHisui.HisuiBets;
using MechHisui.SecretHitler;
using MechHisui.Superfight;
using MechHisui.SymphoXDULib;
using SharedExtensions;

namespace MechHisui
{
    using MechHisuiConfigStore = EFConfigStore<MechHisuiConfig, HisuiGuild, HisuiChannel, HisuiUser>;
    public partial class Program
    {
        private static IServiceProvider ConfigureServices(
            DiscordSocketClient client,
            CommandService commands,
            Params parameters,
            Program program,
            Func<LogMessage, Task> logger = null)
        {
            program.Log(LogSeverity.Info, $"Creating ConfigStore");
            var store = new MechHisuiConfigStore(commands,
                options => options.UseSqlite(parameters.ConnectionString));

            var bank = new BankOfHisui(store);
            var fgo = new FgoConfig(store);
            var xdu = new XduConfig(store);

            using (var config = store.Load())
            {
                var shconfigs = config.SHConfigs.ToDictionary(c => c.Key);
                var sfcfgpath = config.Strings.SingleOrDefault(s => s.Key == "SuperfightPath")?.Value;

                var map = new ServiceCollection()
                    .AddSingleton(new PermissionsService(store, commands, client, logger))
                    .AddSingleton(new HisuiBankService(bank, client, logger))
                    .AddSingleton(new FgoStatService(fgo, client, logger))
                    .AddSingleton(new XduStatService(xdu, client, logger))
                    .AddSingleton(new SecretHitlerService(shconfigs, client, logger))
                    .AddSingleton(new SuperfightService(client, sfcfgpath, logger));

                return map.BuildServiceProvider();
            }




            ////var eval = EvalService.Builder.BuilderWithSystemAndLinq()
            ////    .Add(new EvalReference(MetadataReference.CreateFromFile(typeof(StatService).Assembly.Location),
            ////        "MechHisui.FateGOLib"))
            ////    .Build(@"(FgoConfig stats)
            ////{
            ////    Servants = () => stats.GetServants().Concat(stats.GetFakedServants());
            ////    CEs = stats.GetCEs;
            ////}
            ////private readonly Func<IEnumerable<ServantProfile>> Servants;
            ////private readonly Func<IEnumerable<CEProfile>> CEs;");
            ////_map.AddSingleton(eval);
            ////await _commands.AddModuleAsync<EvalModule>();

            //await _commands.AddDiceRoll();
        }
    }
}
