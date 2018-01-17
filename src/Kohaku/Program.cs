using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.SimplePermissions;
//using Discord.Addons.TriviaGames;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using SharedExtensions;
using SharedExtensions.Collections;
using WS4NetCore;

namespace Kohaku
{
    using KohakuConfigStore = EFConfigStore<KohakuConfig, KohakuUser>;

#pragma warning disable CA2007, CA1001 // Call 'ConfigureAwait(false)'.
    internal partial class Program
    {
        private readonly IConfigStore<KohakuConfig> _store;
        private readonly Func<LogMessage, Task> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;

        private static async Task Main(string[] args)
        {
            var p = Params.Parse(args);
            var app = new Program(p);
            try
            {
                await app.Start(p);
            }
            catch (Exception e)
            {
                await app.Log(LogSeverity.Critical, $"Unhandled Exception: {e}");
            }
        }

        public Program(Params p)
        {
            var minlog = p.LogSeverity ?? LogSeverity.Info;
            _logger = new Logger(minlog).Log;

            Log(LogSeverity.Verbose, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
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

            Log(LogSeverity.Verbose, "Constructing ConfigStore");
            _store = new KohakuConfigStore(_commands,
                options => options
                    //.UseSqlServer(p.ConnectionString)
                    .UseSqlite(p.ConnectionString)
                );
            //_store = new JsonConfigStore<KohakuConfig>(p.ConfigPath, _commands);
            //Log(LogSeverity.Verbose, "Loading Config");
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

            _client.Log += _logger;
            _commands.Log += _logger;

            _services = ConfigureServices(_client, /*_commands,*/ _store, _logger);
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "Main", msg));
        }

        private async Task Start(Params p)
        {
            _client.Ready += () => Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}");

            //await InitCommands();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            _client.ReactionAdded += CheckReactionAdded;
            _client.MessageReceived += HandleCommand;

            using (var config = _store.Load())
            {
                var newModules = _commands.Modules.Select(m => m.Name).Except(config.Modules.Select(m => m.ModuleName), StringComparer.OrdinalIgnoreCase);
                if (newModules.Any())
                {
                    foreach (var mName in newModules)
                    {
                        config.Modules.Add(new ConfigModule { ModuleName = mName });
                    }
                    config.Save();
                }

                //await _commands.UseFgoService(_depmap, config.FgoConfig);
                var token = config.Strings.SingleOrDefault(t => t.Key == "LoginToken")?.Value;
                if (token != null)
                {
                    await _client.LoginAsync(TokenType.Bot, token);
                }
            }

            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task HandleCommand(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

            if (msg.Channel is IPrivateChannel
                || (msg.Channel is SocketGuildChannel sgc
                    && sgc.Guild.CurrentUser.GetPermissions(sgc).SendMessages))
            {
                int pos = 0;
                var user = _client.CurrentUser;
                if (msg.HasCharPrefix('~', ref pos) || msg.HasMentionPrefix(user, ref pos))
                {
                    var context = new SocketCommandContext(_client, msg);
                    var result = await _commands.ExecuteAsync(context, pos, _services);

                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                        await msg.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}