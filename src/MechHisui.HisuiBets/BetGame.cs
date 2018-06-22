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
        internal bool BetsOpen { get; private set; } = true;
        internal uint Bonus { get; private set; } = 0;
        internal IUser GameMaster { get; }
        internal GameType GameType { get; }

        private readonly int _gameId;
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
            IUser master)
        {
            _bank = bank ?? throw new ArgumentNullException();
            _channel = channel ?? throw new ArgumentNullException();
            _rng = rng ?? throw new ArgumentNullException();
            GameType = type;
            GameMaster = master;
            _gameId = bank.CreateGame(channel, type).Id;

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
                var game = await _bank.GetGameInChannelById(_channel, _gameId).ConfigureAwait(false);
                var allBets = game.Bets;

                var highest = allBets.OrderByDescending(b => b.BettedAmount).First();
                var most = allBets.GroupBy(b => b.Target, StringComparer.OrdinalIgnoreCase)
                    .Select(b => new { Count = b.Count(), Tribute = b.Key })
                    .OrderByDescending(b => b.Count)
                    .First();

                _finalBets = new BetCollection(game);

                var sb = new StringBuilder(LogString(_gameId, $"Bets are closed. {allBets.Count()} bets are in.\n"), 150);
                if ((!atEnd && _rng.Next(maxValue: 250) > 245) //TODO: fiddle with values
                    || Bonus > 0)
                {
                    var b = (Bonus > 0) ? Bonus : (uint)(allBets.Count() * 300); //TODO: fiddle with values
                    _finalBets.Bonus = await _bank.RetrieveFromVault(b).ConfigureAwait(false);
                    sb.AppendLine($"BONUS: An additional {_bank.CurrencySymbol}{b} is added to the pot.");
                }
                sb.AppendLine($"The pot is {_bank.CurrencySymbol}{_finalBets.Bets.Sum(b => b.BettedAmount) + _finalBets.Bonus}.\n")
                    .AppendLine($"The highest bet is {_bank.CurrencySymbol}{highest.BettedAmount} on `{highest.Target}`.")
                    .Append($"The most bets are {most.Count} on `{most.Tribute}`.");

                await _channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
            }
        }

        internal async Task CloseOff()
        {
            BetsOpen = false;
            var game = await _bank.GetGameInChannelById(_channel, _gameId).ConfigureAwait(false);
            await _bank.CollectBets(game).ConfigureAwait(false);
        }

        public async Task<string> ProcessBet(IBet bet)
        {
            var game = await _bank.GetGameInChannelById(_channel, _gameId).ConfigureAwait(false);

            switch (await _bank.RecordOrUpdateBet(game, bet).ConfigureAwait(false))
            {
                case RecordingResult.BetAdded:
                    return LogString(_gameId, $"Added **{bet.UserName}**'s bet of {_bank.CurrencySymbol}{bet.BettedAmount} to **{bet.Target}**.");

                case RecordingResult.BetReplaced:
                    return LogString(_gameId, $"Replaced **{bet.UserName}**'s bet with {_bank.CurrencySymbol}{bet.BettedAmount} to **{bet.Target}**.");

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
                    await _channel.SendMessageAsync(LogString(_gameId, "A new game is starting. You may place your bets now.")).ConfigureAwait(false);
                    break;
                case GameType.HungerGameDistrictsOnly:
                    await _channel.SendMessageAsync(LogString(_gameId, "A new game is starting. You can only bet on District numbers.")).ConfigureAwait(false);
                    break;
                case GameType.SaltyBet:
                    await _channel.SendMessageAsync(LogString(_gameId, "Starting a SaltyBet game. Bets will close shortly.")).ConfigureAwait(false);
                    _countDown.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
                    break;
                default:
                    break;
            }
        }

        public async Task<string> Winner(string winner)
        {
            if (_isClosing)
            {
                _countDown.Change(Timeout.Infinite, Timeout.Infinite);
                await Close(true).ConfigureAwait(false);
            }

            var result = await _bank.CashOut(_finalBets, winner).ConfigureAwait(false);
            var wholeSum = _finalBets.WholeSum;

            var reply = await EndMessage(result, _bank, _channel, _gameId, wholeSum).ConfigureAwait(false);

            var ge = GameEnd;
            if (ge != null)
                await ge(_channel).ConfigureAwait(false);

            return reply;
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

        //NOTE: This method has to be callable without an instance
        internal static async Task<string> EndMessage(
            BetResult result,
            IBankOfHisui bank,
            ITextChannel channel,
            int gameId,
            int wholeSum)
        {
            var sb = new StringBuilder();
            if (result.Winners.Count > 0)
            {
                if (result.Winners.Count == 1)
                {
                    var name = await channel.GetUserAsync(result.Winners.Single().Key).ConfigureAwait(false);
                    sb.Append(LogString(gameId, $"**{name}** has won the whole pot of {bank.CurrencySymbol}{wholeSum}."));
                }
                else
                {
                    sb.Append(LogString(gameId, "This game's winners: "));
                    foreach (var user in result.Winners)
                    {
                        var name = await channel.GetUserAsync(user.Key).ConfigureAwait(false);
                        sb.Append($"**{name}** ({bank.CurrencySymbol}{user.Value}), ");
                    }

                    await bank.AddToVault(result.RoundingLoss).ConfigureAwait(false);
                    sb.Append($"and {bank.CurrencySymbol}{wholeSum - result.RoundingLoss} was stashed.").ToString();
                }
            }
            else
            {
                await bank.AddToVault(wholeSum).ConfigureAwait(false);
                sb.Append(LogString(gameId, "No bets were made on the winner of this game. The stakes were stashed in the vault."));
            }

            return sb.ToString();
        }

        private static string LogString(int gameId, string input)
            => $"(\uFF03{gameId}) {input}";
    }
}
