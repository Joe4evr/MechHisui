using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
//using Discord.Addons.TriviaGames;
using Discord.Commands;
using Discord.WebSocket;
using SharedExtensions;
using WS4NetCore;

namespace Kohaku
{
    //using KohakuConfigStore = EFConfigStore<KohakuConfig, KohakuUser>;

#pragma warning disable CA2007, CA1001 // Call 'ConfigureAwait(false)'.
    internal partial class Program
    {
        //private readonly IConfigStore<KohakuConfig> _store;
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

            _client.Log += _logger;
            _commands.Log += _logger;

            //Log(LogSeverity.Verbose, "Constructing ConfigStore");
            //_store = new KohakuConfigStore(_commands,
            //    options => options
            //        //.UseSqlServer(p.ConnectionString)
            //        .UseSqlite(p.ConnectionString)
            //    );
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

            _services = ConfigureServices(_client, _commands, p, _logger);
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
            await _commands.AddModuleAsync<PermissionsModule>();
            _client.ReactionAdded += CheckReactionAdded;
            _client.MessageReceived += HandleCommand;

            if (p.Token != null)
            {
                await _client.LoginAsync(TokenType.Bot, p.Token);
                await _client.StartAsync();
            }

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

                    if (!result.IsSuccess)
                        await msg.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}