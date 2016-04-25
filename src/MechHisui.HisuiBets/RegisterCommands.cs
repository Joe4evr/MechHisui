using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using JiiLib;
using MechHisui.HisuiBets;

namespace MechHisui.Commands
{
    public static class BetsExtensions
    {
        private static Timer upTimer;
        private static BankOfHisui bank;
        private static Game game;

        public static void RegisterHisuiBetsCommands(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'HiusiBets'...");
            bank = new BankOfHisui(config["bank"]);
            bank.ReadBank();

            const char symbol = '\u050A';

            client.UserJoined += (s, e) =>
            {
                if (e.Server.Id == UInt64.Parse(config["FGO_server"]) && !bank.Accounts.Any(u => u.UserId == e.User.Id))
                {
                    Console.WriteLine($"{DateTime.Now} - Registering {e.User.Name} for a bank account.");
                    bank.Accounts.Add(new UserBucks { UserId = e.User.Id, Bucks = 100 });
                }
            };

            client.GetService<CommandService>().CreateCommand("mybucks")
                .Alias("bucks")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
                .Do(async cea =>
                {
                    var bucks = bank.Accounts.Single(u => u.UserId == cea.User.Id).Bucks;
                    await cea.Channel.SendMessage($"**{cea.User.Name}** currently has {symbol}{bucks}.");
                });

            client.GetService<CommandService>().CreateCommand("newgame")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"])))
                .Parameter("gametype", ParameterType.Optional)
                .Do(async cea =>
                {
                    if (game == null || !game.GameOpen)
                    {
                        switch (cea.Args[0].ToLowerInvariant())
                        {
                            case "sb":
                            case "salty":
                                await cea.Channel.SendMessage("Starting a SaltyBet game. Bets will close shortly.");
                                game = new Game(bank, cea.Channel, GameType.SaltyBet);
                                game.ClosingGame();
                                break;
                            default:
                                await cea.Channel.SendMessage("A new game is starting. You may place your bets now.");
                                game = new Game(bank, cea.Channel, GameType.HungerGame);
                                break;
                        }
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Game is already in progress.");
                    }
                });

            var allins = new[] { "all", "all in", "allin" };
            var sbColors = new[] { "red", "blue" };
            client.GetService<CommandService>().CreateCommand("bet")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) /*&& !(Debugger.IsAttached && u.Id == UInt64.Parse(config["Owner"]))*/)
                .Parameter("amount", ParameterType.Required)
                .Parameter("tribute", ParameterType.Multiple)
                .Description("Bet an amount of HisuiBucks on a specified tribute")
                .Do(async cea =>
                {
                    if (game?.GType == GameType.HungerGame && cea.User.Id == UInt64.Parse(config["Hgame_Master"]))
                    {
                        await cea.Channel.SendMessage("The game master is not allowed to bet in a Hunger Game.");
                        return;
                    }
                    if (game?.GameOpen == false || game?.BetsOpen == false)
                    {
                        await cea.Channel.SendMessage("Bets are currently closed at this time.");
                        return;
                    }

                    var userBucks = bank.Accounts.SingleOrDefault(u => u.UserId == cea.User.Id).Bucks;
                    if (userBucks == 0)
                    {
                        await cea.Channel.SendMessage("You currently have no HisuiBucks.");
                        return;
                    }
                    
                    int amount;
                    if (allins.Contains(cea.Args[0].ToLowerInvariant()))
                    {
                        amount = userBucks;
                    }
                    else if (!Int32.TryParse(cea.Args[0], out amount))
                    {
                        await cea.Channel.SendMessage("Could not parse amount as a number.");
                        return;
                    }
                    else if (amount > userBucks)
                    {
                        await cea.Channel.SendMessage($"**{cea.User.Name}** currently does not have enough HisuiBucks to make that bet.");
                        return;
                    }

                    var target = String.Join(" ", cea.Args.Skip(1));
                    if (game?.GType == GameType.SaltyBet && !sbColors.ContainsIgnoreCase(target))
                    {
                        await cea.Channel.SendMessage("Argument must be `red` or `blue` in a SaltyBet.");
                        return;
                    }

                    await cea.Channel.SendMessage(game.ProcessBet(new Bet
                    {
                        UserName = cea.User.Name,
                        UserId = cea.User.Id,
                        Tribute = target,
                        BettedAmount = amount
                    }));
                });

            client.GetService<CommandService>().CreateCommand("checkbets")
                .Alias("betstats")
                .AddCheck((c, u, ch) =>ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"])))
                .Do(async cea =>
                {
                    Console.WriteLine("Checking for an open game");
                    if (game?.GameOpen == false)
                    {
                        Console.WriteLine("No game open, aborting gracefully.");
                        await cea.Channel.SendMessage("No game is going on at this time.");
                        return;
                    }

                    Console.WriteLine("Finding longest name in the active bets");
                    int longestName = game.ActiveBets.OrderBy(b => b.UserName.Length).First().UserName.Length;
                    Console.WriteLine($"Longest name is {longestName} chars");
                    Console.WriteLine("Finding longest bet in active bets");
                    int longestBet = game.ActiveBets.OrderBy(b => b.BettedAmount.ToString().Length).First().BettedAmount.ToString().Length;
                    Console.WriteLine($"Longest bet is {longestBet} chars");
                    Console.WriteLine("Constructing StringBuilder");
                    var sb = new StringBuilder("The following bets have been made:\n```\n");
                    Console.WriteLine("Looping through active bets");
                    foreach (var bet in game.ActiveBets)
                    {
                        var nameSpaces = new String(' ', (longestName - bet.UserName.Length) + 1);
                        var betSpaces = new String(' ', (longestBet - bet.BettedAmount.ToString().Length));
                        var outstring = $"{bet.UserName}:{nameSpaces}{symbol}{betSpaces}{bet.BettedAmount} - {bet.Tribute}";
                        Console.WriteLine(outstring);
                        sb.AppendLine(outstring);

                        if (sb.Length > 1700)
                        {

                            sb.Append("```");
                            await cea.Channel.SendMessage(sb.ToString());
                            sb = new StringBuilder("```\n");
                        }
                    }
                    sb.Append("```");
                    Console.WriteLine("Sending result to channel");
                    await cea.Channel.SendMessage(sb.ToString());
                });

            client.GetService<CommandService>().CreateCommand("closebets")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"])))
                .Do(async cea =>
                {
                    if (game?.GameOpen == false)
                    {
                        await cea.Channel.SendMessage("No game is going on at this time.");
                        return;
                    }
                    if (game?.BetsOpen == false)
                    {
                        await cea.Channel.SendMessage("Bets are already closed.");
                        return;
                    }
                    if (game?.GType == GameType.SaltyBet)
                    {
                        await cea.Channel.SendMessage("This type of game closes automatically.");
                        return;
                    }
                    await cea.Channel.SendMessage("Bets are going to close soon. Please place your final bets now.");
                    game.ClosingGame();
                });

            client.GetService<CommandService>().CreateCommand("winner")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"])))
                .Parameter("name", ParameterType.Multiple)
                .Do(async cea =>
                {
                    if (game?.BetsOpen == true)
                    {
                        game.CloseOff();
                    }
                    if (game?.GameOpen == true)
                    {
                        await cea.Channel.SendMessage(await game.Winner(String.Join(" ", cea.Args)));
                    }
                });

            client.GetService<CommandService>().CreateCommand("donate")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]))
                .Parameter("amount", ParameterType.Required)
                .Parameter("donatee", ParameterType.Required)
                .Do(async cea =>
                {
                    DateTime lastDonation;
                    if (_donationTimeouts.TryGetValue(cea.User.Id, out lastDonation))
                    {
                        if ((DateTime.UtcNow - lastDonation).TotalMilliseconds == TimeSpan.FromMinutes(10).TotalMilliseconds)
                        {
                            await cea.Channel.SendMessage("You are currently in donation timeout.");
                            return;
                        }
                        else
                        {
                            _donationTimeouts.Remove(cea.User.Id);
                        }
                    }

                    int bucks;
                    if (Int32.TryParse(cea.Args[0], out bucks))
                    {
                        if (bucks <= 0)
                        {
                            await cea.Channel.SendMessage("Cannot make a donation of 0 or less.");
                            return;
                        }
                        if (bucks > bank.Accounts.Single(u => u.UserId == cea.User.Id).Bucks)
                        {
                            await cea.Channel.SendMessage($"**{cea.User.Name}** currently does not have enough HisuiBucks to make that donation.");
                            return;
                        }

                        var target = cea.Server.FindUsers(cea.Args[1]).FirstOrDefault();
                        if (target != null)
                        {
                            bank.Accounts.Single(p => p.UserId == cea.User.Id).Bucks -= bucks;
                            bank.Accounts.Single(p => p.UserId == target.Id).Bucks += bucks;
                            await cea.Channel.SendMessage($"**{cea.User.Name}** donated {symbol}{bucks} to **{target.Name}**.");
                            bank.WriteBank(config["bank"]);

                            _donationCounters.AddOrUpdate(cea.User.Id, 1, (k, v) => v++);
                            int d;
                            if (_donationCounters.TryGetValue(cea.User.Id, out d) && d == 10)
                            {
                                _donationTimeouts.Add(cea.User.Id, DateTime.UtcNow);
                                _donationCounters.TryRemove(cea.User.Id, out d);
                            }
                        }
                        else
                        {
                            await cea.Channel.SendMessage("Could not find that user.");
                        }
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Could not parse amount as a number.");
                    }
                });
        }

        private static ConcurrentDictionary<ulong, int> _donationCounters = new ConcurrentDictionary<ulong, int>();
        private static Dictionary<ulong, DateTime> _donationTimeouts = new Dictionary<ulong, DateTime>();

        public static void AddNewHisuiBetsUsers(this DiscordClient client, IConfiguration config)
        {
            var fgo = client.GetServer(UInt64.Parse(config["FGO_server"]));
            var accounts = bank.Accounts.Select(u => u.UserId);
            foreach (var user in fgo.Users)
            {
                if (!accounts.Contains(user.Id) && user.Id != 0)
                {
                    bank.Accounts.Add(new UserBucks { UserId = user.Id, Bucks = 100 });
                }
            }
            bank.WriteBank(config["bank"]);

            upTimer = new Timer(cb =>
            {
                Console.WriteLine($"{DateTime.Now}: Increasing users' HisuiBucks.");
                foreach (var user in bank.Accounts)
                {
                    if (user.Bucks < 2500)
                    {
                        user.Bucks += 10;
                    }
                }
                bank.WriteBank(config["bank"]);
            },
            null,
            TimeSpan.FromMinutes(60 - DateTime.Now.Minute),
            TimeSpan.FromHours(1));
        }
    }
}
