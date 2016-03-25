using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
            _countDown = new Timer(async cb =>
            {
                CloseOff();
                await _channel.SendMessage($"Bets are closed. {ActiveBets.Count} bets are in. The pot is {symbol}{ActiveBets.Sum(b => b.BettedAmount)}.");
            },
            null,
            TimeSpan.FromSeconds((GType == GameType.SaltyBet ? 30 : 45)),
            Timeout.InfiniteTimeSpan);
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

        public string Winner(string winner)
        {
            GameOpen = false;
            var wholeSum = ActiveBets.Sum(b => b.BettedAmount);
            var winners = ActiveBets
                .Where(b => b.Tribute.Equals(winner, StringComparison.InvariantCultureIgnoreCase));
            if (winners.Count() > 0)
            {
                if (winners.Count() == 1)
                {
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
                        var payout = (int)((loserSum / winnerSum) * user.BettedAmount);
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
