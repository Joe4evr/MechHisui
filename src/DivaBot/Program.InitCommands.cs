using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using SharedExtensions;


namespace DivaBot
{
    using DivaBotConfigStore = EFConfigStore<DivaBotConfig, DivaGuild, DivaChannel, DivaUser>;
    internal partial class Program
    {
        private static IServiceProvider ConfigureServices(
            DiscordSocketClient client,
            Params parameters,
            CommandService commands,
            Func<LogMessage, Task> logger = null)
        {
            var map = new ServiceCollection();
            var store = new DivaBotConfigStore(commands, map, logger);

            map.AddSingleton(new PermissionsService(store, commands, client, logger));




            return map.BuildServiceProvider();
        }


        private async Task InitCommands()
        {
            await Log(LogSeverity.Verbose, "Initializing commands");
            //await _commands.UseSimplePermissions(_client, _store, _map, _logger);
            //await _commands.AddTagResponses(_map, _store, _client);
            //await _commands.AddScoreAttack(_map, _store, _client);
            //using (var config = _store.Load())
            //{
                //_map.AddSingleton(new EightBallService(config.Additional8BallOptions));
                //await _commands.AddModuleAsync<EightBallModule>();
//#if !ARM
                //await _commands.UseAudio<AudioModImpl>(_map, config.AudioConfig, _logger);
//#endif
            //}

            //await _commands.AddModuleAsync<TestModule>();

        }
    }
}
