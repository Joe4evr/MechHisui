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
        internal int Bonus { get => _bonus; private set => _bonus = value; }
        internal IUser GameMaster { get; }
        internal GameType GameType { get; }

        private readonly IBetGame _game;
        private readonly Timer _countDown;
        private readonly Random _rng;
        private readonly ITextChannel _channel;
        private readonly IBankOfHisui _bank;

        private bool _isClosing = false;
        private int _bonus = 0;

        public BetGame(IBankOfHisui bank, ITextChannel channel,
            GameType type, Random rng, IUser master)
        {
            _bank = bank ?? throw new ArgumentNullException();
            _channel = channel ?? throw new ArgumentNullException();
            _rng = rng ?? throw new ArgumentNullException();
            GameType = type;
            GameMaster = master;
            _game = bank.CreateGame(channel, type);

            _countDown = new Timer(async cb => await Close(false).ConfigureAwait(false), null,
            Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void ClosingGame()
        {
            _isClosing = true;
            _countDown.Change(TimeSpan.FromSeconds(45), Timeout.InfiniteTimeSpan);
        }

        private BetCollection? _finalBets;

        private async Task Close(bool atEnd)
        {
            if (_isClosing)
            {
                _isClosing = false;
                await CloseOff().ConfigureAwait(false);
                var game = await _bank.GetGameInChannelByIdAsync(_channel, _game.Id).ConfigureAwait(false);
                var allBets = new List<IBet>(game!.Bets);

                var highest = allBets.OrderByDescending(b => b.BettedAmount).FirstOrDefault();
                var most = allBets.GroupBy(b => b.Target, StringComparer.OrdinalIgnoreCase)
                    .Select(b => new { Count = b.Count(), Tribute = b.Key })
                    .OrderByDescending(b => b.Count)
                    .FirstOrDefault();

                _finalBets = new BetCollection(game);

                var sb = new StringBuilder(LogString(_game.Id, $"Bets are closed. {allBets.Count} bets are in.\n"), 150);
                if ((!atEnd && _rng.NextDouble() >= 0.85) //TODO: fiddle with values
                    || Bonus > 0)
                {
                    var b = (Bonus > 0) ? Bonus : (allBets.Count * 300); //TODO: fiddle with values
                    _finalBets.Bonus = await _bank.RetrieveFromVaultAsync(b).ConfigureAwait(false);
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
            //var game = await _bank.GetGameInChannelById(_channel, _game.Id).ConfigureAwait(false);
            await _bank.CollectBetsAsync(_game.Id).ConfigureAwait(false);
        }

        public async Task<string> ProcessBet(IBet bet)
        {
            //var game = await _bank.GetGameInChannelById(_channel, _game.Id).ConfigureAwait(false);

            return (await _bank.RecordOrUpdateBetAsync(_game, bet).ConfigureAwait(false)) switch
            {
                RecordingResult.BetAdded => LogString(_game.Id, $"Added **{bet.UserName}**'s bet of {_bank.CurrencySymbol}{bet.BettedAmount} to **{bet.Target}**."),
                RecordingResult.BetReplaced => LogString(_game.Id, $"Replaced **{bet.UserName}**'s bet with {_bank.CurrencySymbol}{bet.BettedAmount} to **{bet.Target}**."),
                RecordingResult.CannotReplaceOldBet => "Can only bet once per game in this format.",
                RecordingResult.NewBetLessThanOldBet => "Not allowed to replace an existing bet with an amount less than previous bet.",
                _ => "Could not record that bet for some reason.",
            };
        }

        public async Task StartGame()
        {
            switch (GameType)
            {
                case GameType.HungerGame:
                    await _channel.SendMessageAsync(LogString(_game.Id, "A new game is starting. You may place your bets now.")).ConfigureAwait(false);
                    break;
                case GameType.HungerGameDistrictsOnly:
                    await _channel.SendMessageAsync(LogString(_game.Id, "A new game is starting. You can only bet on District numbers.")).ConfigureAwait(false);
                    break;
                case GameType.SaltyBet:
                    await _channel.SendMessageAsync(LogString(_game.Id, "Starting a SaltyBet game. Bets will close shortly.")).ConfigureAwait(false);
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

            var result = await _bank.CashOutAsync(_finalBets!, winner).ConfigureAwait(false);
            var wholeSum = _finalBets!.WholeSum;

            var reply = await EndMessage(result, _bank, _channel, _game.Id, wholeSum).ConfigureAwait(false);

            GameEnd?.Invoke(_channel);

            return reply;
        }

        internal Action<IMessageChannel>? GameEnd { private get; set; }

        internal void AddBonus(int amount)
            => Interlocked.CompareExchange(ref _bonus, amount, 0); // YOBO: You Only Bonus Once (per game)

        // IMPORTANT: This method is static because it
        // has to be callable without an instance
        internal static async Task<string> EndMessage(
            BetResult result, IBankOfHisui bank, ITextChannel channel,
            int gameId, int wholeSum)
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
                        sb.Append($"**{name}** ({bank.CurrencySymbol}{user.Value})");
                    }


                    if (result.RoundingLoss > 0)
                    {
                        await bank.AddToVaultAsync(result.RoundingLoss).ConfigureAwait(false);
                        sb.Append($", and {bank.CurrencySymbol}{result.RoundingLoss} was stashed.").ToString();
                    }
                    else
                    {
                        sb.Append('.');
                    }
                }
            }
            else
            {
                await bank.AddToVaultAsync(wholeSum).ConfigureAwait(false);
                sb.Append(LogString(gameId, "No bets were made on the winner of this game. The stakes were stashed in the vault."));
            }

            return sb.ToString();
        }

        private static string LogString(int gameId, string input)
            => $"(\uFF03{gameId}) {input}";
    }
}
