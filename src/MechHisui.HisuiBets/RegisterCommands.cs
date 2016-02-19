using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using MechHisui.HisuiBets;
using System.Text;
using System.Diagnostics;

namespace MechHisui.Commands
{
    public static class BetsExtensions
    {
        private static bool gameOpen = false;
        private static bool betsOpen = false;
        private static Timer countDown;
        private static Timer upTimer;
        private static BankOfHisui bank = new BankOfHisui();
        private static List<Bet> ActiveBets = new List<Bet>();

        public static void RegisterHisuiBetsCommands(this DiscordClient client, IConfiguration config)
        {
            Console.WriteLine("Registering 'HiusiBets'...");
            bank.ReadBank(config["bank"]);

            const char symbol = '\u050A';

            client.UserJoined += (s, e) =>
            {
                if (e.Server.Id == UInt64.Parse(config["FGO_server"]) && !bank.Accounts.Any(u => u.UserId == e.User.Id))
                {
                    bank.Accounts.Add(new UserBucks { UserId = e.User.Id, Bucks = 100 });
                }
            };

            client.Services.Get<CommandService>().CreateCommand("mybucks")
                .Alias("bucks")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]))
                .Do(async cea =>
                {
                    var bucks = bank.Accounts.Single(u => u.UserId == cea.User.Id).Bucks;
                    await cea.Channel.SendMessage($"**{cea.User.Name}** currently has {symbol}{bucks}.");
                });

            client.Services.Get<CommandService>().CreateCommand("newgame")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"])))
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        ActiveBets = new List<Bet>();
                        gameOpen = true;
                        betsOpen = true;
                        await cea.Channel.SendMessage("A new game is starting. You may place your bets now.");
                    }
                    else
                    {
                        await cea.Channel.SendMessage("Game is already in progress.");
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("bet")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) /*&& !(Debugger.IsAttached && u.Id == UInt64.Parse(config["Owner"]))*/)
                .Parameter("amount", ParameterType.Required)
                .Parameter("tribute", ParameterType.Multiple)
                .Description("Bet an amount of HisuiBucks on a specified tribute")
                .Do(async cea =>
                {
                    var userBucks = bank.Accounts.SingleOrDefault(u => u.UserId == cea.User.Id).Bucks;
                    if (!gameOpen || !betsOpen)
                    {
                        await cea.Channel.SendMessage("Bets are currently closed at this time.");
                        return;
                    }
                    else if (userBucks == 0)
                    {
                        await cea.Channel.SendMessage("You currently have no HisuiBucks.");
                        return;
                    }
                    else if (cea.User.Id == UInt64.Parse(config["Hgame_Master"]))
                    {
                        await cea.Channel.SendMessage("The game master is not allowed to bet.");
                        return;
                    }

                    bool replace = false;
                    var tmp = ActiveBets.SingleOrDefault(b => b.UserId == cea.User.Id);
                    if (tmp != null)
                    {
                        ActiveBets.Remove(tmp);
                        replace = true;
                    }

                    int amount;
                    if (new[] { "all in", "allin" }.Contains(cea.Args[0].ToLowerInvariant()))
                    {
                        amount = userBucks;
                    }
                    else if (!Int32.TryParse(cea.Args[0], out amount))
                    {
                        await cea.Channel.SendMessage("Could not parse amount as a number.");
                        return;
                    }
                    else if (amount <= 0)
                    {
                        await cea.Channel.SendMessage("Cannot make bets of 0 or less.");
                        return;
                    }
                    else if (amount > userBucks)
                    {
                        await cea.Channel.SendMessage($"**{cea.User.Name}** currently does not have enough HisuiBucks to make that bet.");
                        return;
                    }

                    var bet = new Bet
                    {
                        UserName = cea.User.Name,
                        UserId = cea.User.Id,
                        Tribute = String.Join(" ", cea.Args.Skip(1)),
                        BettedAmount = amount
                    };
                    ActiveBets.Add(bet);
                    if (replace)
                    {
                        await cea.Channel.SendMessage($"Replaced **{cea.User.Name}**'s bet with {symbol}{amount} to {bet.Tribute}.");
                    }
                    else
                    {
                        await cea.Channel.SendMessage($"Added **{cea.User.Name}**'s bet of {symbol}{amount} to {bet.Tribute}.");
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("checkbets")
                .AddCheck((c, u, ch) =>
                {
                    Console.WriteLine("Checking requisites for command 'checkbets'");
                    Console.WriteLine($"Invoked channel is {ch.Name}, ID {ch.Id}");
                    Console.WriteLine($"Invoked user is {u.Name}, ID: {u.Id}");
                    return ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"]));
                })
                .Do(async cea =>
                {
                    Console.WriteLine("Checking for an open game");
                    if (!gameOpen)
                    {
                        Console.WriteLine("No game open, aborting gracefully.");
                        await cea.Channel.SendMessage("No game is going on at this time.");
                        return;
                    }

                    Console.WriteLine("Finding longest name in the active bets");
                    int longestName = ActiveBets.OrderBy(b => b.UserName.Length).First().UserName.Length;
                    Console.WriteLine($"Longest name is {longestName} chars");
                    Console.WriteLine("Finding longest bet in active bets");
                    int longestBet = ActiveBets.OrderBy(b => b.BettedAmount.ToString().Length).First().BettedAmount.ToString().Length;
                    Console.WriteLine($"Longest bet is {longestBet} chars");
                    Console.WriteLine("Constructing StringBuilder");
                    var sb = new StringBuilder("The following bets have been made:\n```\n");
                    Console.WriteLine("Looping through active bets");
                    foreach (var bet in ActiveBets)
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

            client.Services.Get<CommandService>().CreateCommand("closebets")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"])))
                .Do(async cea =>
                {
                    if (!gameOpen)
                    {
                        await cea.Channel.SendMessage("No game is going on at this time.");
                        return;
                    }
                    if (!betsOpen)
                    {
                        await cea.Channel.SendMessage("Bets are already closed.");
                        return;
                    }
                    await cea.Channel.SendMessage("Bets are going to close soon. Please place your final bets now.");

                    countDown = new Timer(async cb =>
                    {
                        betsOpen = false;
                        foreach (var bet in ActiveBets)
                        {
                            bank.Accounts.Single(u => u.UserId == bet.UserId).Bucks -= bet.BettedAmount;
                        }
                        await cea.Channel.SendMessage($"Bets are closed. {ActiveBets.Count} bets are in. The pot is {symbol}{ActiveBets.Sum(b => b.BettedAmount)}.");
                    },
                    null,
                    TimeSpan.FromSeconds(45),
                    Timeout.InfiniteTimeSpan);
                });

            client.Services.Get<CommandService>().CreateCommand("winner")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]) && (u.Id == UInt64.Parse(config["Hgame_Master"]) || u.Id == UInt64.Parse(config["Owner"])))
                .Parameter("name", ParameterType.Required)
                .Do(async cea =>
                {
                    gameOpen = false;
                    var winners = ActiveBets.Where(b => b.Tribute.Equals(cea.Args[0], StringComparison.InvariantCultureIgnoreCase)).Select(b => cea.Server.GetUser(b.UserId));
                    if (winners.Count() > 0)
                    {
                        var payout = ActiveBets.Sum(b => b.BettedAmount) / winners.Count();
                        var rounding = ActiveBets.Sum(b => b.BettedAmount) % winners.Count();

                        foreach (var user in winners)
                        {
                            bank.Accounts.SingleOrDefault(u => u.UserId == user.Id).Bucks += payout;
                        }
                        bank.WriteBank(config["bank"]);

                        if (winners.Count() == 1)
                        {
                            await cea.Channel.SendMessage($"{winners.Single().Name} has won the whole pot of {symbol}{payout}.");
                        }
                        else
                        {
                            await cea.Channel.SendMessage($"{String.Join(", ", winners.Select(u => u.Name))} have won {symbol}{payout} each. {symbol}{rounding} has been lost due to rounding.");
                        }
                    }
                    else
                    {
                        await cea.Channel.SendMessage($"No bets were made on the winner of this game.");
                    }
                });

            client.Services.Get<CommandService>().CreateCommand("donate")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_Hgames"]))
                .Parameter("amount", ParameterType.Required)
                .Parameter("donatee",ParameterType.Required)
                .Do(async cea =>
                {
                    int bucks;
                    if (Int32.TryParse(cea.Args[0], out bucks))
                    {
                        if (bucks <= 0)
                        {
                            await cea.Channel.SendMessage("Cannot make a donation of 0 or less.");
                            return;
                        }

                        var target = cea.Server.FindUsers(cea.Args[1]).FirstOrDefault();
                        if (target != null)
                        {
                            bank.Accounts.Single(p => p.UserId == cea.User.Id).Bucks -= bucks;
                            bank.Accounts.Single(p => p.UserId == target.Id).Bucks += bucks;
                            await cea.Channel.SendMessage($"**{cea.User.Name}** donated {symbol}{bucks} to **{target.Name}**.");
                            bank.WriteBank(config["bank"]);
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

        public static void AddNewHisuiBetsUsers(this DiscordClient client, IConfiguration config)
        {
            var fgo = client.GetServer(UInt64.Parse(config["FGO_server"]));
            foreach (var user in fgo.Users)
            {
                if (!bank.Accounts.Any(u => u.UserId == user.Id) && user.Id != 0)
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
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1));
        }
    }
}
