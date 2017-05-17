﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.SimpleAudio;
using Discord.Addons.SimplePermissions;
using Discord.Addons.TriviaGames;
using Discord.Commands;
using Discord.WebSocket;
using SharedExtensions;
using WS4NetCore;

namespace Kohaku
{
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
    internal class Program
    {
        private readonly IServiceCollection _map = new ServiceCollection();
        private readonly IConfigStore<KohakuConfig> _store;
        private readonly Func<LogMessage, Task> _logger;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        private static async Task Main(string[] args)
        {
            var p = new Program(Params.Parse(args));
            try
            {
                await p.Start();
            }
            catch (Exception e)
            {
                await p.Log(LogSeverity.Critical, $"Unhandled Exception: {e}");
            }
        }

        public Program(Params p)
        {
            var minlog = p.LogSeverity ?? LogSeverity.Info;
            _logger = new Logger(minlog).Log;

            Log(LogSeverity.Info, $"Loading config from: {p.ConfigPath}");
            _store = new JsonConfigStore<KohakuConfig>(p.ConfigPath);
            using (var config = _store.Load())
            {
                if (config.AudioConfig == null)
                {
                    config.AudioConfig = new AudioConfig();
                    config.Save();
                }
            }

            Log(LogSeverity.Verbose, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 50,
                LogLevel = minlog,
                WebSocketProvider = WS4NetProvider.Instance
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

        private async Task Start()
        {
            _client.Log += _logger;
            _client.Ready += () => Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}");

            await InitCommands();

            using (var config = _store.Load())
            {
                //await _commands.UseFgoService(_depmap, config.FgoConfig);

                await _client.LoginAsync(TokenType.Bot, config.LoginToken);
            }

            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task InitCommands()
        {
            await _commands.UseSimplePermissions(_client, _store, _map, _logger);
            using (var config = _store.Load())
            {
                //await _commands.UseFgoService(_map, config.FgoConfig);

                //_map.Add(config.TestProfiles);
                //await _commands.AddModuleAsync<TestModule>();

                await _commands.AddTrivia<TriviaImpl>(_client, _map, config.TriviaData, _logger);

                await _commands.UseAudio<AudioModuleImpl>(_map, config.AudioConfig, _logger);
            }

            _client.MessageReceived += HandleCommand;
            //_services = _map.BuildServiceProvider();
        }

        private async Task HandleCommand(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.Channel is IPrivateChannel
                || (msg.Channel is SocketGuildChannel sgc
                    && sgc.Guild.CurrentUser.GetPermissions(sgc).SendMessages))
            {
                int pos = 0;
                var user = _client.CurrentUser;
                if (msg.HasCharPrefix('~', ref pos) || msg.HasMentionPrefix(user, ref pos))
                {
                    var context = new SocketCommandContext(_client, msg);
                    var result = await _commands.ExecuteAsync(context, pos, services: _map.BuildServiceProvider());

                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                        await msg.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
}