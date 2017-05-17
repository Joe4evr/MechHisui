using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using Discord.WebSocket;
using Discord.Commands;
using SharedExtensions;
using WS4NetCore;

namespace DivaBot
{
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
    internal class Program
    {
        private readonly IServiceCollection _map = new ServiceCollection();
        private readonly IConfigStore<DivaBotConfig> _store;
        private readonly Func<LogMessage, Task> _logger;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        private static void Main(string[] args)
        {
            var p = new Program(Params.Parse(args));
            try
            {
                p.AsyncMain().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                p.Log(LogSeverity.Critical, $"Unhandled Exception: {e}").GetAwaiter().GetResult();
            }
        }

        private Program(Params p)
        {
            var minlog = p.LogSeverity ?? LogSeverity.Info;
            _logger = new Logger(minlog).Log;

            Log(LogSeverity.Info, $"Loading config from: {p.ConfigPath}");
            _store = new JsonConfigStore<DivaBotConfig>(p.ConfigPath);
            using (var config = _store.Load())
            {
                if (config.AutoResponses == null)
                {
                    config.AutoResponses = new Dictionary<string, string[]>();
                    config.Save();
                }

                if (config.CurrentChallenges == null)
                {
                    config.CurrentChallenges = new Dictionary<ulong, ScoreAttackChallenge>();
                    config.Save();
                }

                if (config.TagResponses == null)
                {
                    config.TagResponses = new Dictionary<string, string>();
                    config.Save();
                }

                if (config.Additional8BallOptions == null)
                {
                    config.Additional8BallOptions = new List<string>();
                    config.Save();
                }

                if (config.AudioConfig == null)
                {
                    config.AudioConfig = new AudioConfig();
                    config.Save();
                }
            }

            Log(LogSeverity.Verbose, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                //AudioMode = Discord.Audio.AudioMode.Outgoing,
                MessageCacheSize = 50,
                LogLevel = minlog,
#if !ARM
                WebSocketProvider = WS4NetProvider.Instance
#endif
            });

            Log(LogSeverity.Verbose, $"Constructing {nameof(CommandService)}");
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync
            });
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "Main", msg));
        }

        private async Task AsyncMain()
        {
            _client.Log += _logger;
            _client.Ready += () => Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}");

            await InitCommands().ConfigureAwait(false);

            using (var config = _store.Load())
            {
                await _client.LoginAsync(TokenType.Bot, config.LoginToken).ConfigureAwait(false);
            }
            await _client.StartAsync().ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private async Task InitCommands()
        {
            await Log(LogSeverity.Verbose, "Initializing commands");
            await _commands.UseSimplePermissions(_client, _store, _map, _logger);
            await _commands.AddTagResponses(_map, _store, _client);
            await _commands.AddScoreAttack(_map, _store, _client);
            using (var config = _store.Load())
            {
                _map.AddSingleton(new EightBallService(config.Additional8BallOptions));
                await _commands.AddModuleAsync<EightBallModule>();
#if !ARM
                await _commands.UseAudio<AudioModImpl>(_map, config.AudioConfig, _logger);
#endif
            }

            //await _commands.AddModuleAsync<TestModule>();

            _client.MessageReceived += CmdHandler;
        }

        private async Task CmdHandler(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.Channel is IPrivateChannel
                || (msg.Channel is SocketGuildChannel sgc
                    && sgc.Guild.CurrentUser.GetPermissions(sgc).SendMessages))
            {
                int pos = 0;
                var user = _client.CurrentUser;
                if (msg.HasCharPrefix('!', ref pos) || msg.HasMentionPrefix(user, ref pos))
                {
                    var context = new SocketCommandContext(_client, msg);
                    var result = await _commands.ExecuteAsync(context, pos, services: _map.BuildServiceProvider(validateScopes: true));

                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                        await msg.Channel.SendMessageAsync(result.ErrorReason).ConfigureAwait(false);
                }
            }
        }
    }
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
}