using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using MechHisui.Core;
using MechHisui.FateGOLib;
using MechHisui.HisuiBets;
using MechHisui.SecretHitler;
using MechHisui.Superfight;
using Newtonsoft.Json;
using SharedExtensions;
using WS4NetCore;

namespace MechHisui
{
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
    public class Program
    {
        private readonly IServiceCollection _map = new ServiceCollection();
        private readonly IConfigStore<MechHisuiConfig> _store;
        private readonly Func<LogMessage, Task> _logger;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        public static void Main(string[] args)
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
            _store = new JsonConfigStore<MechHisuiConfig>(p.ConfigPath);
            //using (var config = _store.Load())
            //{
            //}

            Log(LogSeverity.Verbose, $"Constructing {nameof(DiscordSocketClient)}");
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
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

        //try
        //{
        //    //Convert our sync method to an async one and block the Main function until the bot disconnects
        //    client.ExecuteAndWait(async () =>
        //    {
        //        await client.Connect(config["LoginToken"]);
        //        Console.WriteLine($"Logged in as {client.CurrentUser.Name}");
        //        Console.WriteLine($"MH v. 0.3.0");
        //        await Task.Delay(3000);
        //        if (!client.Servers.Any())
        //        {
        //            Console.WriteLine("Not a member of any server");
        //        }
        //        else
        //        {
        //            foreach (var channel in Helpers.IterateChannels(client.Servers, printServerNames: true, printChannelNames: true))
        //            {
        //                if (!channel.IsPrivate && Helpers.IsWhilested(channel, client))
        //                {
        //                    //Console.CancelKeyPress += async (s, e) => await client.SendMessage(channel, config["Goodbye"]);
        //                    if (channel.Id != UInt64.Parse(config["API_testing"]))
        //                    {
        //                        if (Debugger.IsAttached)
        //                        {
        //                            // await channel.SendMessage("MechHisui started in debug mode. Not all commands will be available.");
        //                        }
        //                        else if (lastcode != -1 && channel.Id != UInt64.Parse(config["FGO_events"]))
        //                        {
        //                            await channel.SendMessage(config["Hello"]);
        //                        }
        //                    }
        //                }
        //            }
        //            client.GetModule<HisuiBetsModule>().Instance.AddNewHisuiBetsUsers(client, config);
        //            Console.WriteLine($"Started up at {DateTime.Now}.");
        //        }
        //    });
        //}
        //catch (Exception ex)
        //{
        //    File.AppendAllText(Path.Combine(config["Logs"], "crashlogs.txt"), $"{DateTime.Now} - {ex.Message}\n{ex.StackTrace}\n");
        //    Environment.Exit(-1);
        //}

        private async Task AsyncMain()
        {
            _client.Log += _logger;
            _client.Ready += () => Log(LogSeverity.Info, $"Logged in as {_client.CurrentUser.Username}");

            _client.MessageUpdated += async (before, after, channel) =>
            {
                ulong myid = _client.CurrentUser.Id;
                if (!(channel.GetCachedMessages(20).Any(m => m.Author.Id == myid)))
                {
                    await HandleCommand(after);
                }
            };

            await InitCommands();

            using (var config = _store.Load())
            {
                await _client.LoginAsync(TokenType.Bot, config.Token);
            }

            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task InitCommands()
        {
            var fgo = new FgoConfig
            {
                GetServants = () =>
                {
                    using (var config = _store.Load())
                    {
                        return JsonConvert.DeserializeObject<List<ServantProfile>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "Servants.json")));
                    }
                },
                GetFakeServants = () =>
                {
                    using (var config = _store.Load())
                    {
                        return JsonConvert.DeserializeObject<List<ServantProfile>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "FakeServants.json")));
                    }
                },
                //GetServantAliases = () =>
                //{
                //    using (var config = _store.Load())
                //    {
                //        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "ServantAlias.json")))
                //            .Join(config.GetAllServants(), kv => kv.Value, s => s.Name, (kv, s) => new ServantAlias { Alias = kv.Key, Servant = s });
                //    }
                //},
                AddServantAlias = (name, alias) =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetAllServants();
                        var srv = all.SingleOrDefault(s => s.Name == name);
                        if (srv == null)
                        {
                            return false;
                        }
                        else
                        {
                            srv.Aliases.Add(alias);
                            File.WriteAllText(Path.Combine(config.FgoBasePath, "Servants.json"), JsonConvert.SerializeObject(all, Formatting.Indented));
                            return true;
                        }
                    }
                },
                GetCEs = () =>
                {
                    using (var config = _store.Load())
                    {
                        return config.GetAllCEs();
                    }
                },
                //GetCEAliases = () =>
                //{
                //    using (var config = _store.Load())
                //    {
                //        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "CEAlias.json")))
                //            .Join(config.GetAllCEs(), kv => kv.Value, ce => ce.Name, (kv, ce) => new CEAlias { Alias = kv.Key, CE = ce });
                //    }
                //},
                AddCEAlias = (ce, alias) =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetAllCEs();
                        var p = all.SingleOrDefault(c => c.Name == ce);
                        if (p == null)
                        {
                            return false;
                        }
                        else
                        {
                            p.Aliases.Add(alias);
                            File.WriteAllText(Path.Combine(config.FgoBasePath, "CEs.json"), JsonConvert.SerializeObject(all, Formatting.Indented));
                            return true;
                        }
                    }
                },
                GetMystics = () =>
                {
                    using (var config = _store.Load())
                    {
                        return config.GetAllMystics();
                    }
                },
                //GetMysticAliases = () =>
                //{
                //    using (var config = _store.Load())
                //    {
                //        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "MysticAlias.json")))
                //            .Join(config.GetAllMystics(), kv => kv.Value, myst => myst.Code, (kv, myst) => new MysticAlias { Alias = kv.Key, Code = myst });
                //    }
                //},
                AddMysticAlias = (code, alias) =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetAllMystics();
                        var mystic = all.SingleOrDefault(m => m.Code == code);
                        if (mystic == null)
                        {
                            return false;
                        }
                        else
                        {
                            mystic.Aliases.Add(alias);
                            File.WriteAllText(Path.Combine(config.FgoBasePath, "Mystics.json"), JsonConvert.SerializeObject(all, Formatting.Indented));
                            return true;
                        }
                    }
                },
                GetEvents = () =>
                {
                    using (var config = _store.Load())
                    {
                        return JsonConvert.DeserializeObject<List<Event>>(File.ReadAllText(Path.Combine(config.FgoBasePath, "Events.json")));
                    }
                }
            };
            var bank = new BankOfHisui
            {
                GetAllUsers = () =>
                {
                    using (var config = _store.Load())
                    {
                        return config.GetBankAccounts();
                    }
                },
                GetUser = (id) =>
                {
                    using (var config = _store.Load())
                    {
                        return config.GetBankAccounts().Single(u => u.UserId == id);
                    }
                },
                CashOut = (bets, winner) =>
                {
                    int l = 0;
                    var winners = bets.Where(b => b.Tribute.Equals(winner, StringComparison.OrdinalIgnoreCase)).ToList();
                    var wholeSum = bets.Sum(b => b.BettedAmount);
                    decimal loserSum = bets
                        .Where(b => !b.Tribute.Equals(winner, StringComparison.OrdinalIgnoreCase))
                        .Sum(b => b.BettedAmount);
                    decimal winnerSum = wholeSum - loserSum;

                    var windict = new Dictionary<ulong, int>();

                    using (var config = _store.Load())
                    {
                        var all = config.GetBankAccounts();
                        foreach (var user in winners)
                        {
                            var payout = (int)((loserSum / winnerSum) * user.BettedAmount) + user.BettedAmount;
                            var us = all.SingleOrDefault(u => u.UserId == user.UserId);
                            us.Bucks += payout;
                            windict.Add(us.UserId, payout);
                            l += payout;
                        }
                        config.Save();
                    }
                    return new BetResult
                    {
                        RoundingLoss = l,
                        Winners = windict
                    };
                },
                Interest = () =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetBankAccounts().Where(u => u.Bucks < 2500);
                        foreach (var u in all)
                        {
                            u.Bucks += 10;
                        }
                        config.Save();
                    }
                },
                Donate = (donor, receiver, amount) =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetBankAccounts();

                        all.Single(u => donor == u.UserId).Bucks -= amount;
                        all.Single(u => receiver == u.UserId).Bucks += amount;
                        config.Save();
                    }
                },
                Take = (id, amount) =>
                {
                    using (var config = _store.Load())
                    {
                        var all = config.GetBankAccounts();
                        all.Single(u => u.UserId == id).Bucks -= amount;
                        config.Save();
                    }
                }
            };

            await _commands.UseSimplePermissions(_client, _store, _map, _logger);
            await _commands.UseFgoService(_map, fgo, _client);
            await _commands.UseHisuiBank(_map, bank, _logger);
            await _commands.AddDiceRoll();

            using (var config = _store.Load())
            {
                await _commands.AddSecretHitler(_map, JsonConvert.DeserializeObject<List<SecretHitlerConfig>>(File.ReadAllText(Path.Combine(config.SHConfigPath, "shitler.json"))));
                await _commands.AddSuperFight(_map, config.SuperfightBasePath);
            }

            _client.MessageReceived += HandleCommand;
        }

        private async Task HandleCommand(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            int pos = 0;
            var user = _client.CurrentUser;
            if (msg.HasCharPrefix('.', ref pos) || msg.HasMentionPrefix(user, ref pos))
            {
                var context = new SocketCommandContext(_client, msg);
                //var cmd = _commands.Search(context, pos);
                var result = await _commands.ExecuteAsync(context, pos, services: _map.BuildServiceProvider());

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    //switch (result)
                    //{

                    //    default:
                    //        break;
                    //}
                    //await msg.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
}
