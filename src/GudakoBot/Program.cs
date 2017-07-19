using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using SharedExtensions;
using WS4NetCore;

namespace GudakoBot
{
    public class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly ConfigStore _store;
        private readonly Func<LogMessage, Task> _logger;
        private ulong _owner;

        private static async Task Main(string[] args)
        {
            var p = new Program(Params.Parse(args));
            try
            {
                await p.AsyncMain();
            }
            catch (Exception e)
            {
                await p.Log(LogSeverity.Critical, $"Unhandled Exception: {e}");
            }
        }

        private Program(Params p)
        {
            var minlog = p.LogSeverity ?? LogSeverity.Info;
            _logger = new Logger(minlog).Log;

            Log(LogSeverity.Info, $"Loading config from: {p.ConfigPath}");
            _store = new ConfigStore(p.ConfigPath);

            Log(LogSeverity.Verbose, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = minlog,
#if !ARM
                WebSocketProvider = WS4NetProvider.Instance
#endif
            });

            //_owner = _client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner.Id;
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "Main", msg));
        }

        private async Task AsyncMain()
        {
            //Console.WriteLine("Loading config...");
            var config = _store.Load();
            await Log(LogSeverity.Info, $"Loaded {config.Lines.Count()} lines.").ConfigureAwait(false);

            //Display all log messages in the console
            _client.Log += _logger;

            _client.MessageReceived += async msg =>
            {
                if (msg.Author.Id == _owner && msg.Content == "-new")
                {
                    await Log(LogSeverity.Info, $"{DateTime.Now}: Reloading lines").ConfigureAwait(false);
                    config = _store.Load();
                    await msg.Channel.SendMessageAsync(config.Lines.Last()).ConfigureAwait(false);
                }
            };

            _client.Ready += async () =>
            {
                await Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}").ConfigureAwait(false);
                await Log(LogSeverity.Info, $"Started up at {DateTime.Now}.").ConfigureAwait(false);
                _owner = (await _client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner.Id;
            };

            await _client.LoginAsync(TokenType.Bot, config.LoginToken).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }
    }
}
