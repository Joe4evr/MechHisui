using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MechHisui.HisuiBets
{
    public class Game
    {
        public IList<Bet> ActiveBets { get; }
        public GameType GType { get; }
        public bool GameOpen { get; private set; }
        public bool BetsOpen { get; private set; }

        private Timer _countDown;
        private readonly BankOfHisui _bank;
        private readonly Channel _channel;
        const char symbol = '\u050A';
        private bool _isClosing;

        public Game(BankOfHisui bank, Channel channel, GameType gameType = GameType.HungerGame)
        {
            ActiveBets = new List<Bet>();
            _bank = bank;
            _channel = channel;
            GType = gameType;
            GameOpen = true;
            BetsOpen = true;
        }

        public void ClosingGame()
        {
            _isClosing = true;
            _countDown = new Timer(async cb => await close(), null,
            TimeSpan.FromSeconds((GType == GameType.SaltyBet ? 30 : 45)),
            Timeout.InfiniteTimeSpan);
        }

        private async Task close()
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

            await _channel.SendMessage(sb.ToString());
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
                if (GType == GameType.SaltyBet)
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

        public async Task<string> Winner(string winner)
        {
            if (_isClosing)
            {
                _countDown.Change(Timeout.Infinite, Timeout.Infinite);
                await close();
            }
            GameOpen = false;
            var wholeSum = ActiveBets.Sum(b => b.BettedAmount);
            var winners = ActiveBets
                .Where(b => b.Tribute.Equals(winner, StringComparison.InvariantCultureIgnoreCase));
            if (winners.Count() > 0)
            {
                if (winners.Count() == 1)
                {
                    _bank.Accounts.SingleOrDefault(u => u.UserId == winners.Single().UserId).Bucks += wholeSum;
                    return $"**{winners.Single().UserName}** has won the whole pot of {symbol}{wholeSum}.";
                }
                else
                {
                    decimal loserSum = ActiveBets
                        .Where(b => !b.Tribute.Equals(winner, StringComparison.InvariantCultureIgnoreCase))
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

                    return sb.Append($"and {symbol}{wholeSum - t} has been lost due to rounding.").ToString();
                }
            }
            else
            {
                return $"No bets were made on the winner of this game.";
            }
        }
    }

    public enum GameType
    {
        HungerGame,
        SaltyBet
    }
}
