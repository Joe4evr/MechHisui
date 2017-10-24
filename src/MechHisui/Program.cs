using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using MechHisui.Core;
using Newtonsoft.Json;
using SharedExtensions;

namespace MechHisui
{
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
    public partial class Program
    {
        private static async Task Main(string[] args)
        {
            var p = Params.Parse(args);
            var app = new Program(p);
            try
            {
                await app.Start();
            }
            catch (Exception e)
            {
                await app.Log(LogSeverity.Critical, $"Unhandled Exception: {e}");
            }
        }

        private readonly IServiceCollection _map = new ServiceCollection();
        private readonly IConfigStore<MechHisuiConfig> _store;
        private readonly Func<LogMessage, Task> _logger;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        private Program(Params p)
        {
            var minlog = p.LogSeverity ?? LogSeverity.Info;
            _logger = new Logger(minlog, p.LogPath).Log;

            Log(LogSeverity.Verbose, $"Constructing {nameof(CommandService)}");
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Sync
            });
            _commands.Log += _logger;

            Log(LogSeverity.Info, $"Loading config from: {p.ConfigPath}");
            _store = new JsonConfigStore<MechHisuiConfig>(p.ConfigPath, _commands);

            //using (var config = _store.Load())
            //{
            //    if (!config.Strings.Any())
            //    {
            //        config.Strings.AddRange(
            //            JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("strings.json"))
            //                .DictionarySelect((k, v) => new StringKeyValuePair { Key = k, Value = v }));
            //        config.Save();
            //    }
            //}

            Log(LogSeverity.Verbose, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = minlog,
#if !ARM
                WebSocketProvider = WS4NetCore.WS4NetProvider.Instance
#endif
            });
            _client.Log += _logger;
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "Main", msg));
        }

        private async Task Start()
        {
            _client.Ready += () => Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}");

            _client.MessageUpdated += async (before, after, channel) =>
            {
                ulong myid = _client.CurrentUser.Id;
                if (!(channel.GetCachedMessages(after.Id, Direction.After).Any(m => m.Author.Id == myid)))
                {
                    await HandleCommand(after);
                }
            };

            await InitCommands();

            using (var config = _store.Load())
            {
                //var token = config.Strings.SingleOrDefault(t => t.Key == "Login")?.Value;
                //if (token != null)
                    await _client.LoginAsync(TokenType.Bot, config.Token);
            }

            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task HandleCommand(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

            int pos = 0;
            var user = _client.CurrentUser;
            if (msg.HasCharPrefix('.', ref pos) || msg.HasMentionPrefix(user, ref pos))
            {
                var context = new SocketCommandContext(_client, msg);
                var result = await _commands.ExecuteAsync(context, pos, services: _map.BuildServiceProvider());

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand
                    && context.Guild?.Id == 161445678633975808ul)
                {
                    await msg.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
}
