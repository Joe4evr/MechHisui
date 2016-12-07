using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace MechHisui.HisuiBets
{
    public class BetGame
    {
        const char symbol = '\u050A';
        public List<Bet> ActiveBets { get; } = new List<Bet>();
        public bool BetsOpen { get; private set; }

        internal readonly GameType GameType;
        private readonly Timer _countDown;
        private readonly ITextChannel _channel;
        private readonly BankOfHisui _bank;
        private bool _isClosing = false;

        public BetGame(BankOfHisui bank, ITextChannel channel, GameType type)
        {
            _bank = bank;
            _channel = channel;
            GameType = type;

            BetsOpen = true;
            _countDown = new Timer(async cb => await close(), null,
            Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void ClosingGame()
        {
            _isClosing = true;
            _countDown.Change(TimeSpan.FromSeconds(45), Timeout.InfiniteTimeSpan);
        }

        private async Task close()
        {
            if (_isClosing)
            {
                _isClosing = false;
                CloseOff();
                var highest = ActiveBets.OrderByDescending(b => b.BettedAmount).First();
                var most = ActiveBets.GroupBy(b => b.Tribute, StringComparer.OrdinalIgnoreCase)
                    .Select(b => new { Count = b.Count(), Tribute = b.Key })
                    .OrderByDescending(b => b.Count)
                    .First();
                var sb = new StringBuilder($"Bets are closed. {ActiveBets.Count} bets are in. The pot is {symbol}{ActiveBets.Sum(b => b.BettedAmount)}.\n")
                    .AppendLine($"The highest bet is {symbol}{highest.BettedAmount} on `{highest.Tribute}`.")
                    .Append($"The most bets are {most.Count} on `{most.Tribute}`.");

                await _channel.SendMessageAsync(sb.ToString());
            }
        }

        internal void CloseOff()
        {
            BetsOpen = false;
            foreach (var bet in ActiveBets)
            {
                _bank.Accounts.Single(u => u.UserId == bet.UserId).Bucks -= bet.BettedAmount;
            }
            _bank.WriteBank();
        }

        public string ProcessBet(Bet bet)
        {
            if (bet.BettedAmount < 50) return "Minimum bet must be 50.";

            bool replace = false;
            var tmp = ActiveBets.SingleOrDefault(b => b.UserId == bet.UserId);
            if (tmp != null)
            {
                if (GameType == GameType.SaltyBet)
                {
                    return "Can only bet once per game in this format.";
                }
                else if (bet.BettedAmount < tmp.BettedAmount)
                {
                    return "Not allowed to replace an existing bet with less than previous bet.";
                }

                ActiveBets.Remove(tmp);
                replace = true;
            }

            ActiveBets.Add(bet);
            if (replace)
            {
                return $"Replaced **{bet.UserName}**'s bet with {symbol}{bet.BettedAmount} to {bet.Tribute}.";
            }
            else
            {
                return $"Added **{bet.UserName}**'s bet of {symbol}{bet.BettedAmount} to {bet.Tribute}.";
            }
        }

        public async Task StartGame()
        {
            switch (GameType)
            {
                case GameType.HungerGame:
                    await _channel.SendMessageAsync("A new game is starting. You may place your bets now.");
                    break;
                case GameType.HungerGameDistrictsOnly:
                    await _channel.SendMessageAsync("A new game is starting. You can only bet on District numbers.");
                    break;
                case GameType.SaltyBet:
                    await _channel.SendMessageAsync("Starting a SaltyBet game. Bets will close shortly.");
                    _countDown.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
                    break;
                default:
                    break;
            }
        }

        public async Task<string> Winner(string winner)
        {
            string result;
            if (_isClosing)
            {
                _countDown.Change(Timeout.Infinite, Timeout.Infinite);
                await close();
            }
            var wholeSum = ActiveBets.Sum(b => b.BettedAmount);
            var winners = ActiveBets
                .Where(b => b.Tribute.Equals(winner, StringComparison.OrdinalIgnoreCase));

            if (winners.Count() > 0)
            {
                if (winners.Count() == 1)
                {
                    _bank.Accounts.SingleOrDefault(u => u.UserId == winners.Single().UserId).Bucks += wholeSum;
                    result = $"**{winners.Single().UserName}** has won the whole pot of {symbol}{wholeSum}.";
                }
                else
                {
                    decimal loserSum = ActiveBets
                        .Where(b => !b.Tribute.Equals(winner, StringComparison.OrdinalIgnoreCase))
                        .Sum(b => b.BettedAmount);
                    decimal winnerSum = wholeSum - loserSum;

                    var sb = new StringBuilder("This game's winners: ");
                    int t = 0;
                    foreach (var user in winners)
                    {
                        var payout = (int)((loserSum / winnerSum) * user.BettedAmount) + user.BettedAmount;
                        _bank.Accounts.SingleOrDefault(u => u.UserId == user.UserId).Bucks += payout;
                        t += payout;
                        sb.Append($"**{user.UserName}** ({symbol}{payout}), ");
                    }
                    _bank.WriteBank();

                    result = sb.Append($"and {symbol}{wholeSum - t} has been lost due to rounding.").ToString();
                }
            }
            else
            {
                result = $"No bets were made on the winner of this game.";
            }
            await GameEnd?.Invoke(_channel.Id);
            return result;
        }

        internal event Func<ulong, Task> GameEnd;
    }
}