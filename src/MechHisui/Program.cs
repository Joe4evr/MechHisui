using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.SimplePermissions;
using MechHisui.HisuiBets;
using SharedExtensions;
using MechHisui.FateGOLib;

namespace MechHisui
{
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
    public partial class Program
    {
        private const string Version = "MH v1.3-test";

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

        private readonly Func<LogMessage, Task> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;

        private Program(Params p)
        {
            var minlog = p.LogSeverity ?? LogSeverity.Info;
            _logger = new Logger(minlog, p.LogPath).Log;

            Log(LogSeverity.Info, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = minlog,
#if !ARM
                WebSocketProvider = WS4NetCore.WS4NetProvider.Instance
#endif
            });

            Log(LogSeverity.Info, $"Constructing {nameof(CommandService)}");
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = minlog,
                DefaultRunMode = RunMode.Sync
            });
            _services = ConfigureServices(_client, p, _commands, _logger);

            _commands.Log += _logger;
            _client.Log += _logger;
            _client.MessageReceived += HandleCommand;
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "Main", msg));
        }

        private async Task Start(Params p)
        {
            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            await _commands.AddModuleAsync<PermissionsModule>();
            await _commands.AddModuleAsync<DiceRollModule>();
            await _commands.AddModuleAsync<HisuiBetsModule>();
            await _commands.AddModuleAsync<FgoModule>();

            _client.Ready += async () =>
            {
                await Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}");
                await _client.SetGameAsync(Version);
            };


            _client.MessageUpdated += async (before, after, channel) =>
            {
                ulong myid = _client.CurrentUser.Id;
                if (!(channel.GetCachedMessages(after.Id, Direction.After).Any(m => m.Author.Id == myid)))
                {
                    await HandleCommand(after);
                }
            };

            if (p.Token != null)
            {
                await _client.LoginAsync(TokenType.Bot, p.Token);
                await _client.StartAsync();
            }

            await Task.Delay(-1);
        }

        private Task HandleCommand(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return Task.CompletedTask;

            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return Task.CompletedTask;

            if (msg.Channel is IPrivateChannel
                || (msg.Channel is SocketGuildChannel sgc
                    && sgc.Guild.CurrentUser.GetPermissions(sgc).SendMessages))
            {
                int pos = 0;
                var user = _client.CurrentUser;
                if (msg.HasCharPrefix('.', ref pos) || msg.HasMentionPrefix(user, ref pos))
                {
                    Task.Run(async () =>
                    {
                        var context = new SocketCommandContext(_client, msg);
                        var result = await _commands.ExecuteAsync(context, pos, services: _services);

                        if (!result.IsSuccess && (result.Error != CommandError.UnknownCommand
                            || context.Guild?.Id == 161445678633975808ul))
                        {
                            await msg.Channel.SendMessageAsync(result.ErrorReason);
                        }
                    });
                }
            }
            return Task.CompletedTask;
        }
    }
}
