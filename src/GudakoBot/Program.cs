using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using SharedExtensions;

using NodaTime;
using NodaTime.TimeZones;

namespace GudakoBot
{
    public class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly ConfigStore _store;
        private readonly Func<LogMessage, Task> _logger;
        private ulong _owner;

        private static void Main(string[] args)
        {
            var provider = DateTimeZoneProviders.Tzdb;
            var jptz = provider["Japan"];
            var utcNow = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow).InZone(jptz);

            var startdate = utcNow.TimeUntilNextOccurrance(new AnnualDate(month: 4, day: 1));
            var enddate = startdate + Duration.FromDays(1);

            var logins = utcNow.TimeUntilNextOccurrance(new LocalTime(hour: 4, minute: 0));


            //var p = Params.Parse(args);
            //var app = new Program(p);
            //try
            //{
            //    await app.AsyncMain(p);
            //}
            //catch (Exception e)
            //{
            //    await app.Log(LogSeverity.Critical, $"Unhandled Exception: {e}");
            //}
        }

        private Program(Params p)
        {
            var minlog = p.LogSeverity ?? LogSeverity.Info;
            _logger = new Logger(minlog, p.LogPath).Log;

            Log(LogSeverity.Info, $"Loading config from: {p.ConfigPath}");
            _store = new ConfigStore(p.ConfigPath);

            Log(LogSeverity.Verbose, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = minlog,
#if !ARM
                WebSocketProvider = WS4NetCore.WS4NetProvider.Instance
#endif
            });
            _client.Log += _logger;

            //_owner = _client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner.Id;
        }

        private Task Log(LogSeverity severity, string msg)
        {
            return _logger(new LogMessage(severity, "Main", msg));
        }

        private PeriodicMessageService _periodic;
        private AprilFools _aprilFools;

        private async Task AsyncMain(Params p)
        {
            var config = _store.Load();
            await Log(LogSeverity.Info, $"Loaded {config.Lines.Count()} lines.");
            _periodic = new PeriodicMessageService(_client, config.FgoGeneral, config.Lines, _logger);
            _aprilFools = new AprilFools(_client, _periodic, config.FgoGeneral);

            _client.MessageReceived += async msg =>
            {
                if (msg.Author.Id == _owner && msg.Content == "-new")
                {
                    await Log(LogSeverity.Info, $"{DateTime.Now}: Reloading lines").ConfigureAwait(false);
                    _periodic.Lines = _store.Load().Lines;
                    await msg.Channel.SendMessageAsync(config.Lines.Last()).ConfigureAwait(false);
                }
            };

            _client.Ready += async () =>
            {
                await Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}").ConfigureAwait(false);
                await Log(LogSeverity.Info, $"Started up at {DateTime.Now}.").ConfigureAwait(false);
                _owner = (await _client.GetApplicationInfoAsync().ConfigureAwait(false)).Owner.Id;
            };

            await _client.LoginAsync(TokenType.Bot, config.LoginToken);
            await _client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
