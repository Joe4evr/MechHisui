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
        private static readonly Random _rng = new Random();

        //internal BetCollection ActiveBets { get; } = new BetCollection();
        internal bool BetsOpen { get; private set; } = true;
        internal uint Bonus { get; private set; } = 0;
        internal ulong GameMaster { get; }
        internal GameType GameType { get; }


        internal readonly List<Bet> _betTracker = new List<Bet>();
        private readonly Timer _countDown;
        private readonly ITextChannel _channel;
        private readonly IBankOfHisui _bank;

        private bool _isClosing = false;

        public BetGame(
            IBankOfHisui bank,
            ITextChannel channel,
            GameType type,
            //Random rng,
            ulong master)
        {
            _bank = bank;
            _channel = channel;
            //_rng = rng;
            GameType = type;
            GameMaster = master;

            _countDown = new Timer(async cb => await Close(false).ConfigureAwait(false), null,
            Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void ClosingGame()
        {
            _isClosing = true;
            _countDown.Change(TimeSpan.FromSeconds(45), Timeout.InfiniteTimeSpan);
        }

        private BetCollection _finalBets;

        private async Task Close(bool atEnd)
        {
            if (_isClosing)
            {
                _isClosing = false;
                CloseOff();
                var highest = _betTracker.OrderByDescending(b => b.BettedAmount).First();
                var most = _betTracker.GroupBy(b => b.Tribute, StringComparer.OrdinalIgnoreCase)
                    .Select(b => new { Count = b.Count(), Tribute = b.Key })
                    .OrderByDescending(b => b.Count)
                    .First();

                _finalBets = new BetCollection(_betTracker);

                var sb = new StringBuilder($"Bets are closed. {_betTracker.Count} bets are in.\n");
                if ((!atEnd && _rng.Next(maxValue: 250) > 245) //TODO: fiddle with values
                    || Bonus > 0)
                {
                    var b = (Bonus > 0) ? Bonus : 1500u; //TODO: fiddle with values
                    _finalBets.Bonus = _bank.RetrieveFromVault(b);
                    sb.AppendLine($"BONUS: An additional {_symbol}{b} is added to the pot.");
                }
                sb.AppendLine($"The pot is {_symbol}{_finalBets.Bets.Sum(b => b.BettedAmount) + _finalBets.Bonus}.\n")
                    .AppendLine($"The highest bet is {_symbol}{highest.BettedAmount} on `{highest.Tribute}`.")
                    .Append($"The most bets are {most.Count} on `{most.Tribute}`.");

                await _channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
            }
        }

        internal void CloseOff()
        {
            BetsOpen = false;
            foreach (var bet in _betTracker)
            {
                _bank.Withdraw(bet.UserId, bet.BettedAmount);
            }
        }

        public string ProcessBet(Bet bet)
        {
            if (bet.BettedAmount < 50) return "Minimum bet must be 50.";

            bool replace = false;
            var tmp = _betTracker.SingleOrDefault(b => b.UserId == bet.UserId);
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

                _betTracker.Remove(tmp);
                replace = true;
            }

            _betTracker.Add(bet);
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
            var sb = new StringBuilder();
            if (_isClosing)
            {
                _countDown.Change(Timeout.Infinite, Timeout.Infinite);
                await Close(true).ConfigureAwait(false);
            }

            var wholeSum = (uint)_finalBets.Bets.Sum(b => (int)b.BettedAmount);
            var result = await _bank.CashOut(_finalBets, winner);

            if (result.Winners.Count > 0)
            {
                if (result.Winners.Count == 1)
                {
                    var name = await _channel.GetUserAsync(result.Winners.Single().Key).ConfigureAwait(false);
                    sb.Append($"**{name}** has won the whole pot of {_symbol}{wholeSum}.");
                }
                else
                {
                    sb.Append("This game's winners: ");
                    foreach (var user in result.Winners)
                    {
                        var name = await _channel.GetUserAsync(result.Winners.Single().Key).ConfigureAwait(false);
                        sb.Append($"**{user.Key}** ({_symbol}{user.Value}), ");
                    }

                    _bank.AddToVault(result.RoundingLoss);
                    sb.Append($"and {_symbol}{wholeSum - result.RoundingLoss} was stashed.").ToString();
                }
            }
            else
            {
                _bank.AddToVault(wholeSum);
                sb.Append("No bets were made on the winner of this game. The stakes were stashed in the vault.");
            }
            await GameEnd?.Invoke(_channel.Id);
            return sb.ToString();
        }

        internal event Func<ulong, Task> GameEnd;

        internal void AddBonus(uint amount)
        {
            if (Bonus == 0) //YOBO: You Only Bonus Once (per game)
            {
                Bonus = amount;
            }
        }

    }
}
