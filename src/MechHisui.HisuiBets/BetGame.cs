using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace MechHisui.HisuiBets
{
    internal sealed class BetGame
    {
        private const char _symbol = '\u050A';
        //private static readonly Random _rng = new Random();

        //internal BetCollection ActiveBets { get; } = new BetCollection();
        internal bool BetsOpen { get; private set; } = true;
        internal uint Bonus { get; private set; } = 0;
        internal ulong GameMaster { get; }
        internal GameType GameType { get; }


        //internal readonly List<IBet> _betTracker = new List<IBet>();
        private readonly Timer _countDown;
        private readonly Random _rng;
        private readonly ITextChannel _channel;
        private readonly IBankOfHisui _bank;

        private bool _isClosing = false;

        public BetGame(
            IBankOfHisui bank,
            ITextChannel channel,
            GameType type,
            Random rng,
            ulong master)
        {
            _bank = bank;
            _channel = channel;
            _rng = rng;
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
                await CloseOff().ConfigureAwait(false);
                var allBets = await _bank.RetrieveAllBets(_channel).ConfigureAwait(false);

                var highest = allBets.OrderByDescending(b => b.BettedAmount).First();
                var most = allBets.GroupBy(b => b.Target, StringComparer.OrdinalIgnoreCase)
                    .Select(b => new { Count = b.Count(), Tribute = b.Key })
                    .OrderByDescending(b => b.Count)
                    .First();

                _finalBets = new BetCollection(allBets);

                var sb = new StringBuilder($"Bets are closed. {allBets.Count()} bets are in.\n", 150);
                if ((!atEnd && _rng.Next(maxValue: 250) > 245) //TODO: fiddle with values
                    || Bonus > 0)
                {
                    var b = (Bonus > 0) ? Bonus : (uint)(allBets.Count() * 300); //TODO: fiddle with values
                    _finalBets.Bonus = await _bank.RetrieveFromVault(b).ConfigureAwait(false);
                    sb.AppendLine($"BONUS: An additional {_symbol}{b} is added to the pot.");
                }
                sb.AppendLine($"The pot is {_symbol}{_finalBets.Bets.Sum(b => b.BettedAmount) + _finalBets.Bonus}.\n")
                    .AppendLine($"The highest bet is {_symbol}{highest.BettedAmount} on `{highest.Target}`.")
                    .Append($"The most bets are {most.Count} on `{most.Tribute}`.");

                await _channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
            }
        }

        internal async Task CloseOff()
        {
            BetsOpen = false;
            await _bank.WithdrawAll((await _bank.RetrieveAllBets(_channel).ConfigureAwait(false)).Select(b => new WithdrawalRequest(b.BettedAmount, b.UserId))).ConfigureAwait(false);
        }

        public async Task<string> ProcessBet(Bet bet)
        {
            bet.GameType = GameType;

            switch (await _bank.RecordOrUpdateBet(bet).ConfigureAwait(false))
            {
                case RecordingResult.BetAdded:
                    return $"Added **{bet.UserName}**'s bet of {_symbol}{bet.BettedAmount} to {bet.Target}.";

                case RecordingResult.BetReplaced:
                    return $"Replaced **{bet.UserName}**'s bet with {_symbol}{bet.BettedAmount} to {bet.Target}.";

                case RecordingResult.CannotReplaceOldBet:
                    return "Can only bet once per game in this format.";

                case RecordingResult.NewBetLessThanOldBet:
                    return "Not allowed to replace an existing bet with an amount less than previous bet.";

                case RecordingResult.MiscError:
                default:
                    return "Could not record that bet for some reason.";
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

            var result = await _bank.CashOut(_finalBets, winner).ConfigureAwait(false);
            var wholeSum = _finalBets.WholeSum;

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

                    await _bank.AddToVault(result.RoundingLoss).ConfigureAwait(false);
                    sb.Append($"and {_symbol}{wholeSum - result.RoundingLoss} was stashed.").ToString();
                }
            }
            else
            {
                await _bank.AddToVault(wholeSum).ConfigureAwait(false);
                sb.Append("No bets were made on the winner of this game. The stakes were stashed in the vault.");
            }
#pragma warning disable CA2007 // Do not directly await a Task
            var ge = GameEnd;
            if (ge != null)
                await ge(_channel);
#pragma warning restore CA2007 // Do not directly await a Task
            return sb.ToString();
        }

#pragma warning disable CA1710
        internal event Func<IMessageChannel, Task> GameEnd;
#pragma warning restore CA1710

        internal void AddBonus(uint amount)
        {
            if (Bonus == 0) //YOBO: You Only Bonus Once (per game)
            {
                Bonus = amount;
            }
        }
    }
}
