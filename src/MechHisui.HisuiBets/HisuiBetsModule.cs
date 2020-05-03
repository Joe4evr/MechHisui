using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Preconditions;
using Discord.Addons.SimplePermissions;
using SharedExtensions;
using System.Diagnostics;

namespace MechHisui.HisuiBets
{
    [Name("HisuiBets"), RequireContext(ContextType.Guild)]
    [Permission(MinimumPermission.Everyone)]
    public sealed partial class HisuiBetsModule : ModuleBase<SocketCommandContext>
    {
        //private const char _symbol = '\u050A';
        private static readonly int _minimumBet = 50;

        private readonly HisuiBankService _service;
        private readonly Random _rng;

        private IBankAccount? _account;
        private BetGame? _game;

        public HisuiBetsModule(HisuiBankService service, Random rng)
        {
            _service = service;
            _rng = rng;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            _service.Games.TryGetValue(Context.Channel.Id, out _game);
            _account = _service.Bank.GetAccount(Context.User);
        }

        [Command("bet")]
        [Priority(0), RequiresGameType(GameType.HungerGame)]
        public async Task Bet(int amount, [Remainder] string target)
        {
            if (_account != null && await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(await _game!.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Target = target
                }).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Command("bet")]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        public async Task Bet(int amount, int district)
        {
            if (_account != null && await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(await _game!.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Target = $"District {district}"
                }).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [Command("bet")]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        public async Task Bet(int amount, SaltyBetTeam team)
        {
            if (_account != null && await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(await _game!.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Target = team.ToString()
                }).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        private async Task<bool> CheckPreReqs(int bettedAmount)
        {
            if (_game == null)
            {
                await ReplyAsync("No game to bet on.").ConfigureAwait(false);
                return false;
            }
            if (!_game.BetsOpen)
            {
                await ReplyAsync("Bets are currently closed at this time.").ConfigureAwait(false);
                return false;
            }
            if (_service.Blacklist.Contains(Context.User.Id))
            {
                await ReplyAsync("Not allowed to bet.").ConfigureAwait(false);
                return false;
            }
            if ((_game.GameType == GameType.HungerGame || _game.GameType == GameType.HungerGameDistrictsOnly)
                && Context.User.Id == _game.GameMaster.Id)
            {
                await ReplyAsync("The Game Master is not allowed to bet.").ConfigureAwait(false);
                return false;
            }
            if (bettedAmount <= 0)
            {
                await ReplyAsync("Cannot make a bet of 0 or less.").ConfigureAwait(false);
                return false;
            }
            if (bettedAmount < _minimumBet)
            {
                await ReplyAsync($"Minimum bet must be {_minimumBet}.").ConfigureAwait(false);
                return false;
            }
            if (_account?.Balance == 0)
            {
                await ReplyAsync("You currently have no HisuiBucks.").ConfigureAwait(false);
                return false;
            }
            if (_account?.Balance < bettedAmount)
            {
                await ReplyAsync("You do not have enough HisuiBucks to make that bet.").ConfigureAwait(false);
                return false;
            }
            return true;
        }

        [Command("bet allin"), Alias("allin", "bet all")]
        [Priority(10), RequiresGameType(GameType.HungerGame)]
        public Task BetAllIn([Remainder] string target)
            => Bet(_account!.Balance, target);

        [Command("bet allin"), Alias("allin", "bet all")]
        [Priority(11), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        public Task BetAllIn(int district)
            => Bet(_account!.Balance, district);

        [Command("bet allin"), Alias("allin", "bet all")]
        [Priority(12), RequiresGameType(GameType.SaltyBet)]
        public Task BetAllIn(SaltyBetTeam team)
            => Bet(_account!.Balance, team);

        [Command("bet it all"), Hidden]
        [Priority(20), RequiresGameType(GameType.Any)]
        public Task BetItAll()
            => Context.Channel.SendFileAsync(KappaPath,
                    text: $"**{Context.User.Username}** has bet all their bucks. Good luck.");

        [Command("newgame"), Permission(MinimumPermission.Special)]
        [Summary("Starts a new game.")]
        public Task NewGame([Summary("Available game types: 1: 'HungerGames' (default), 2: 'HungerGameDistrictsOnly', 3: 'SaltyBet'")] GameType gameType = GameType.HungerGame)
        {
            var actualType = (gameType == GameType.Any)
                ? GameType.HungerGame
                : gameType;
            var tChan = (ITextChannel)Context.Channel;
            var game = new BetGame(_service.Bank, tChan, actualType, _rng, Context.User);

            return (_service.TryAddNewGame(tChan, game))
                ? game.StartGame()
                : ReplyAsync("Another game is already in progress.");
        }

        [Command("checkbets"), Permission(MinimumPermission.Special)]
        [RequiresGameType(GameType.Any)]
        public async Task CheckBets(int? gameId = null)
        {
            var tChan = (ITextChannel)Context.Channel;
            var game = (gameId.HasValue)
                ? await _service.Bank.GetGameInChannelByIdAsync(tChan, gameId.Value).ConfigureAwait(false)
                : await _service.Bank.GetLastGameInChannelAsync(tChan).ConfigureAwait(false);

            if (game == null)
            {
                await ReplyAsync("No game found.").ConfigureAwait(false);
                return;
            }
            var sb = new StringBuilder($"(\uFF03{game.Id}) The following bets have been made:\n```\n", 2000);

            foreach (var bet in game.Bets)
            {
                sb.AppendLine($"{bet.UserName,-20}: {_service.Bank.CurrencySymbol}{bet.BettedAmount,-7} - {bet.Target}");

                if (sb.Length > 1700)
                {
                    sb.Append("```");
                    await ReplyAsync(sb.ToString()).ConfigureAwait(false);
                    sb.Clear().AppendLine("```");
                }
            }
            sb.Append("```");
            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("closebets"), Permission(MinimumPermission.Special)]
        [RequiresGameType(GameType.Any)]
        public async Task CloseBets()
        {
            if (!_game!.BetsOpen)
            {
                await ReplyAsync("Bets are already closed.").ConfigureAwait(false);
                return;
            }
            if (_game.GameType == GameType.SaltyBet)
            {
                await ReplyAsync("This type of game closes automatically.").ConfigureAwait(false);
                return;
            }

            await ReplyAsync("Bets are going to close soon. Please place your final bets now.").ConfigureAwait(false);
            _game.ClosingGame();
        }

        [Command("winner"), Permission(MinimumPermission.Special)]
        [RequiresGameType(GameType.Any)]
        public async Task SetWinner([Remainder] string winner)
            => await ReplyAsync(await _game!.Winner(winner).ConfigureAwait(false)).ConfigureAwait(false);

        [Command("setwin"), Permission(MinimumPermission.Special)]
        public async Task SetWinner(int gameId, [Remainder] string winner)
        {
            if (Context.Channel is SocketTextChannel channel)
            {
                var game = await _service.Bank.GetGameInChannelByIdAsync(channel, gameId).ConfigureAwait(false);
                if (game == null)
                {
                    await ReplyAsync("Could not find a game by that ID.").ConfigureAwait(false);
                    return;
                }

                if (game.IsCashedOut)
                {
                    await ReplyAsync("Specified game is already cashed out.").ConfigureAwait(false);
                    return;
                }

                if (!game.IsCollected)
                {
                    await _service.Bank.CollectBetsAsync(game.Id).ConfigureAwait(false);
                }

                var result = await _service.Bank.CashOutAsync(new BetCollection(game), winner).ConfigureAwait(false);
                var wholeSum = game.Bets.Sum(b => b.BettedAmount);

                await ReplyAsync(await BetGame.EndMessage(result, _service.Bank, channel, gameId, wholeSum).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        private static string? _kappaPath;
        private string KappaPath => _kappaPath ??= Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)!, "kappa.png");
    }
}
