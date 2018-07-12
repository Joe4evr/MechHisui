using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using MechHisui.Core;
//using MechHisui.ExplodingKittens;
using MechHisui.FateGOLib;
using MechHisui.HisuiBets;
//using MechHisui.SecretHitler;
//using MechHisui.Superfight;
//using MechHisui.SymphoXDULib;
using SharedExtensions;

namespace MechHisui
{
    using MechHisuiConfigStore = EFConfigStore<MechHisuiConfig, HisuiGuild, HisuiChannel, HisuiUser>;
    public partial class Program
    {
        private static IServiceProvider ConfigureServices(
            DiscordSocketClient client,
            Params parameters,
            CommandService commands,
            Func<LogMessage, Task> logger = null)
        {
            try
            {
                logger?.Invoke(new LogMessage(LogSeverity.Info, "Main", "Constructing ConfigStore"));
                //logger?.Invoke(new LogMessage(LogSeverity.Debug, "Main", $"ConnectionString value: '{parameters.ConnectionString}'"));
                var map = new ServiceCollection();
                var store = new MechHisuiConfigStore(commands, map, logger);

                var bank     = new BankOfHisui(store);
                var fgo      = new FgoConfig(store);
                //var shconfig = new SecretHitlerConfig(store);
                //var sfconfig = new SuperfightConfig(store);
                //var xdu      = new XduConfig(store);

                commands.AddTypeReader<DiceRoll>(new DiceTypeReader());
                //commands.AddTypeReader<ServantFilterOptions>(new ServantFilterTypeReader());

                map.AddSingleton(new Random())
                    .AddSingleton(new PermissionsService(store, commands, client, logger))
                    .AddDbContext<MechHisuiConfig>(options => options.UseSqlite(parameters.ConnectionString))
                    .AddSingleton(new HisuiBankService(bank, client, logger))
                    .AddSingleton(new FgoStatService(fgo, client, logger))
                    //.AddSingleton(new SecretHitlerService(shconfig, client, logger))
                    //.AddSingleton(new SuperfightService(sfconfig, client, logger))
                    //.AddSingleton(new ExKitService(client, logger))
                    //.AddSingleton(new XduStatService(xdu, client, logger))
                    ;

                var services = map.BuildServiceProvider();
                //store.Services = services;

                //using (var cfg = store.Load())
                //{

                //}

                return services;
            }
            catch (Exception ex)
            {
                logger?.Invoke(new LogMessage(LogSeverity.Critical, "Main", "Uncaught Exception", ex));
                throw;
            }
        }

        private async Task InitCommands()
        {
            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            //await _commands.AddModuleAsync<MiscModule>(_services);
            await _commands.AddModuleAsync<PermissionsModule>(_services);
            await _commands.AddModuleAsync<DiceRollModule>(_services);
            await _commands.AddModuleAsync<HisuiBankModule>(_services);
            await _commands.AddModuleAsync<HisuiBetsModule>(_services);
            await _commands.AddModuleAsync<FgoModule>(_services);
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
