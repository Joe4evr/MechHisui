using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;

namespace DivaBot
{
    internal partial class Program
    {
        private async Task InitCommands()
        {
            await Log(LogSeverity.Verbose, "Initializing commands");
            await _commands.UseSimplePermissions(_client, _store, _map, _logger);
            //await _commands.AddTagResponses(_map, _store, _client);
            //await _commands.AddScoreAttack(_map, _store, _client);
            //using (var config = _store.Load())
            //{
                //_map.AddSingleton(new EightBallService(config.Additional8BallOptions));
                //await _commands.AddModuleAsync<EightBallModule>();
#if !ARM
                //await _commands.UseAudio<AudioModImpl>(_map, config.AudioConfig, _logger);
#endif
            //}

            //await _commands.AddModuleAsync<TestModule>();

            _client.MessageReceived += CmdHandler;
        }
    }
}
