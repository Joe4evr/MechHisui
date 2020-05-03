using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Techsola;
using SharedExtensions;

namespace MechHisui
{
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
    public partial class Program
    {
        private const string _version = "MHOS v2.0-test";

        private static async Task Main(string[] args)
        {
            AmbientTasks.BeginContext();

            var p = Params.Parse(args);
            var app = new Program(p);
            try
            {
                await app.Start(p);
            }
            catch (Exception ex)
            {
                await app.Log(LogSeverity.Critical, $"Unhandled Exception: {ex}");
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
                LogLevel = minlog
            });

            Log(LogSeverity.Info, $"Constructing {nameof(CommandService)}");
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = minlog,
                DefaultRunMode = RunMode.Sync
            });
            _services = ConfigureServices(_client, _commands, p, _logger);

            _commands.Log += _logger;
            _client.Log += _logger;
            _client.MessageReceived += HandleCommand;
        }

        [DebuggerStepThrough]
        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "Main", msg));
        }

        private async Task Start(Params p)
        {
            await InitCommands();
            _client.Ready += async () =>
            {
                await Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}");
                await _client.SetGameAsync(_version);
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

            await Task.Delay(Timeout.Infinite);
        }

        private Task HandleCommand(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage msg))
                return Task.CompletedTask;

            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot)
                return Task.CompletedTask;

            if (msg.Channel is IPrivateChannel
                || (msg.Channel is SocketGuildChannel sgc
                    && sgc.Guild.CurrentUser.GetPermissions(sgc).SendMessages))
            {
                int pos = 0;
                if (msg.HasCharPrefix('.', ref pos) || msg.HasMentionPrefix(_client.CurrentUser, ref pos))
                {
                    AmbientTasks.Add(RunCommand(msg, pos));
                }
            }
            return Task.CompletedTask;

            // saves on closure allocation
            async Task RunCommand(SocketUserMessage message, int position)
            {
                await Task.Yield();
                //using (message.Channel.EnterTypingState())

                using var scope = _services.CreateScope();
                var context = new SocketCommandContext(_client, message);
                var result = await _commands.ExecuteAsync(context, position, services: scope.ServiceProvider);

                if (!result.IsSuccess
                    && (result.Error != CommandError.UnknownCommand
                        || context.Guild?.Id == 161445678633975808ul))
                {
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}
