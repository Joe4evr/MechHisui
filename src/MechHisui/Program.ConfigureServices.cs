using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using MechHisui.Core;
using MechHisui.ExplodingKittens;
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
            Params parameters,
            out CommandService commands,
            Func<LogMessage, Task> logger = null)
        {
            logger?.Invoke(new LogMessage(LogSeverity.Info, "Main", $"Constructing {nameof(CommandService)}"));
            commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync
            });

            try
            {
                logger?.Invoke(new LogMessage(LogSeverity.Info, "Main", "Constructing ConfigStore"));
                //logger?.Invoke(new LogMessage(LogSeverity.Debug, "Main", $"ConnectionString value: '{parameters.ConnectionString}'"));
                var store = new MechHisuiConfigStore(commands,
                    options =>  options.UseSqlite(parameters.ConnectionString));

                //using (var config = store.Load())
                //{
                //}

                var bank = new BankOfHisui(store);
                var fgo = new FgoConfig(store);
                //var shconfig = new SecretHitlerConfig(store);
                //var sfconfig = new SuperfightConfig(store);
                //var xdu      = new XduConfig(store);

                commands.AddTypeReader<DiceRoll>(new DiceTypeReader());
                //commands.AddTypeReader<ServantFilterOptions>(new ServantFilterTypeReader());

                var map = new ServiceCollection()
                    .AddSingleton(new Random())
                    .AddSingleton(new PermissionsService(store, commands, client, logger))
                    .AddSingleton(new FgoStatService(fgo, client, logger))
                    //.AddSingleton(new SecretHitlerService(shconfig, client, logger))
                    //.AddSingleton(new SuperfightService(sfconfig, client, logger))
                    //.AddSingleton(new XduStatService(xdu, client, logger))
                    //.AddSingleton(new ExKitService(client, logger))
                    .AddSingleton(new HisuiBankService(bank, client, logger));

                return map.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                logger?.Invoke(new LogMessage(LogSeverity.Critical, "Main", "Uncaught Exception", ex));
                throw;
            }
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
