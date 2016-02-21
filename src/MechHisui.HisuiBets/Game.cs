using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MechHisui.HisuiBets
{
    public class Game
    {
        public IList<Bet> ActiveBets { get; }
        public GameType Type { get; }
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
            Type = gameType;
            GameOpen = true;
            BetsOpen = true;
        }

        public void ClosingGame()
        {
            _countDown = new Timer(async cb =>
            {
                BetsOpen = false;
                foreach (var bet in ActiveBets)
                {
                    _bank.Accounts.Single(u => u.UserId == bet.UserId).Bucks -= bet.BettedAmount;
                }
                _bank.WriteBank();
                await _channel.SendMessage($"Bets are closed. {ActiveBets.Count} bets are in. The pot is {symbol}{ActiveBets.Sum(b => b.BettedAmount)}.");
            },
            null,
            TimeSpan.FromSeconds((Type == GameType.SaltyBet ? 30 : 45 )),
            Timeout.InfiniteTimeSpan);
        }

        public string ProcessBet(Bet bet)
        {
            if (bet.BettedAmount <= 0) return "Cannot make bets of 0 or less.";

            bool replace = false;
            var tmp = ActiveBets.SingleOrDefault(b => b.UserId == bet.UserId);
            if (tmp != null)
            {
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
            var winners = ActiveBets.Where(b => b.Tribute.Equals(winner, StringComparison.InvariantCultureIgnoreCase))
                .Select(b => _channel.Server.GetUser(b.UserId));
            if (winners.Count() > 0)
            {
                var payout = ActiveBets.Sum(b => b.BettedAmount) / winners.Count();
                var rounding = ActiveBets.Sum(b => b.BettedAmount) % winners.Count();

                foreach (var user in winners)
                {
                    _bank.Accounts.SingleOrDefault(u => u.UserId == user.Id).Bucks += payout;
                }
                _bank.WriteBank();

                if (winners.Count() == 1)
                {
                    return $"{winners.Single().Name} has won the whole pot of {symbol}{payout}.";
                }
                else
                {
                    return $"{String.Join(", ", winners.Select(u => u.Name))} have won {symbol}{payout} each. {symbol}{rounding} has been lost due to rounding.";
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
