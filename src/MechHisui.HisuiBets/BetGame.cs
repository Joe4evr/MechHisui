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
        private const char _symbol = '\u050A';
        public List<Bet> ActiveBets { get; } = new List<Bet>();
        public bool BetsOpen { get; private set; }
        public ulong GameMaster { get; }

        internal readonly GameType GameType;
        private readonly Timer _countDown;
        private readonly ITextChannel _channel;
        private readonly BankOfHisui _bank;
        private bool _isClosing = false;

        public BetGame(BankOfHisui bank, ITextChannel channel, GameType type, ulong master)
        {
            _bank = bank;
            _channel = channel;
            GameType = type;
            GameMaster = master;

            BetsOpen = true;
            _countDown = new Timer(async cb => await Close().ConfigureAwait(false), null,
            Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void ClosingGame()
        {
            _isClosing = true;
            _countDown.Change(TimeSpan.FromSeconds(45), Timeout.InfiniteTimeSpan);
        }

        private async Task Close()
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
                var sb = new StringBuilder($"Bets are closed. {ActiveBets.Count} bets are in. The pot is {_symbol}{ActiveBets.Sum(b => b.BettedAmount)}.\n")
                    .AppendLine($"The highest bet is {_symbol}{highest.BettedAmount} on `{highest.Tribute}`.")
                    .Append($"The most bets are {most.Count} on `{most.Tribute}`.");

                await _channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
            }
        }

        internal void CloseOff()
        {
            BetsOpen = false;
            foreach (var bet in ActiveBets)
            {
                _bank.Take(bet.UserId, bet.BettedAmount);
            }
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
                return $"Replaced **{bet.UserName}**'s bet with {_symbol}{bet.BettedAmount} to {bet.Tribute}.";
            }
            else
            {
                return $"Added **{bet.UserName}**'s bet of {_symbol}{bet.BettedAmount} to {bet.Tribute}.";
            }
        }

        public async Task StartGame()
        {
            switch (GameType)
            {
                case GameType.HungerGame:
                    await _channel.SendMessageAsync("A new game is starting. You may place your bets now.").ConfigureAwait(false);
                    break;
                case GameType.HungerGameDistrictsOnly:
                    await _channel.SendMessageAsync("A new game is starting. You can only bet on District numbers.").ConfigureAwait(false);
                    break;
                case GameType.SaltyBet:
                    await _channel.SendMessageAsync("Starting a SaltyBet game. Bets will close shortly.").ConfigureAwait(false);
                    _countDown.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
                    break;
                default:
                    break;
            }
        }

        public async Task<string> Winner(string winner)
        {
            string ret;
            if (_isClosing)
            {
                _countDown.Change(Timeout.Infinite, Timeout.Infinite);
                await Close().ConfigureAwait(false);
            }

            int wholeSum = ActiveBets.Sum(b => b.BettedAmount);
            var result = _bank.CashOut(ActiveBets, winner);

            if (result.Winners.Count > 0)
            {
                if (result.Winners.Count == 1)
                {
                    var name = await _channel.GetUserAsync(result.Winners.Single().Key).ConfigureAwait(false);
                    ret = $"**{name}** has won the whole pot of {_symbol}{wholeSum}.";
                }
                else
                {
                    var sb = new StringBuilder("This game's winners: ");
                    foreach (var user in result.Winners)
                    {
                        var name = await _channel.GetUserAsync(result.Winners.Single().Key).ConfigureAwait(false);
                        sb.Append($"**{user.Key}** ({_symbol}{user.Value}), ");
                    }

                    ret = sb.Append($"and {_symbol}{wholeSum - result.RoundingLoss} has been lost due to rounding.").ToString();
                }
            }
            else
            {
                ret = "No bets were made on the winner of this game.";
            }
            await GameEnd?.Invoke(_channel.Id);
            return ret;
        }

        internal event Func<ulong, Task> GameEnd;
    }
}