using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Preconditions;
using Discord.Addons.SimplePermissions;
using SharedExtensions;

namespace MechHisui.HisuiBets
{
    [Name("HisuiBets"), RequireContext(ContextType.Guild)]
    [Permission(MinimumPermission.Everyone)]
    public sealed class HisuiBetsModule : ModuleBase<SocketCommandContext>
    {
        private const char _symbol = '\u050A';

        private readonly HisuiBankService _service;
        private readonly Random _rng;

        private IBankAccount _account;
        private BetGame _game;
        private int _minimumBet = 50;

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
                await ReplyAsync(await _game.ProcessBet(new Bet
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
                await ReplyAsync(await _game.ProcessBet(new Bet
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
                await ReplyAsync(await _game.ProcessBet(new Bet
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

        [Command("bet"), Hidden]
        [Priority(0), RequiresGameType(GameType.HungerGame)]
        public async Task Bet([LimitTo(StringComparison.OrdinalIgnoreCase, "all", "allin")] string allin, [Remainder] string target)
        {
            if (_account != null && await CheckPreReqs(_account.Balance).ConfigureAwait(false))
                await Bet(_account.Balance, target).ConfigureAwait(false);
        }

        [Command("bet"), Hidden]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        public async Task Bet([LimitTo(StringComparison.OrdinalIgnoreCase, "all", "allin")] string allin, int district)
        {
            if (_account != null && await CheckPreReqs(_account.Balance).ConfigureAwait(false))
                await Bet(_account.Balance, district).ConfigureAwait(false);
        }

        [Command("bet"), Hidden]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        public async Task Bet([LimitTo(StringComparison.OrdinalIgnoreCase, "all", "allin")] string allin, SaltyBetTeam team)
        {
            if (_account != null && await CheckPreReqs(_account.Balance).ConfigureAwait(false))
                await Bet(_account.Balance, team).ConfigureAwait(false);
        }

        [Command("bet it all"), Hidden]
        [Priority(5), RequiresGameType(GameType.Any)]
        public Task BetItAll()
            => Context.Channel.SendFileAsync(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "kappa.png"),
                    text: $"**{Context.User.Username}** has bet all their bucks. Good luck.");

        [Command("newgame"), Permission(MinimumPermission.Special)]
        public Task NewGame(GameType gameType = GameType.HungerGame)
        {
            var actualType = (gameType == GameType.Any)
                ? GameType.HungerGame
                : gameType;
            var game = new BetGame(_service.Bank, (ITextChannel)Context.Channel, actualType, _rng, Context.User);

            return (_service.TryAddNewGame(Context.Channel.Id, game))
                ? game.StartGame()
                : ReplyAsync("Game is already in progress.");
        }

        [Command("checkbets"), Permission(MinimumPermission.Special)]
        [RequiresGameType(GameType.Any)]
        public async Task CheckBets()
        {
            var game = await _service.Bank.GetLastGameInChannel(Context.Channel as ITextChannel).ConfigureAwait(false);
            var sb = new StringBuilder($"(#{game.Id}) The following bets have been made:\n```\n", 2000);

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
            if (!_game.BetsOpen)
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
            => await ReplyAsync(await _game.Winner(winner).ConfigureAwait(false)).ConfigureAwait(false);

        [Command("setwin"), Permission(MinimumPermission.Special)]
        public async Task SetWinner(int gameId, [Remainder] string winner)
        {
            var channel = Context.Channel as ITextChannel;
            var game = await _service.Bank.GetGameInChannelById(channel, gameId).ConfigureAwait(false);
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
                await _service.Bank.CollectBets(game).ConfigureAwait(false);
            }

            var result = await _service.Bank.CashOut(new BetCollection(game), winner).ConfigureAwait(false);
            var wholeSum = game.Bets.Sum(b => b.BettedAmount);

            await ReplyAsync(await BetGame.EndMessage(result, _service.Bank, channel, gameId, wholeSum).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Command("bucks"), Alias("mybucks")]
        public Task Bucks()
            => ReplyAsync($"**{Context.User.Username}** currently has {_service.Bank.CurrencySymbol}{_account.Balance}.");

        [Command("donate"), Ratelimit(5, 10, Measure.Minutes)]
        public async Task Donate(int amount, IUser recipient)
        {
            if (amount <= 0)
            {
                await ReplyAsync("Cannot make a donation of 0 or less.").ConfigureAwait(false);
            }
            if (recipient.IsBot || _service.Blacklist.Contains(recipient.Id))
            {
                await ReplyAsync("Not allowed to donate to that account.").ConfigureAwait(false);
            }

            var donationResult = await _service.Bank.Donate(new DonationRequest((uint)amount, _account, recipient)).ConfigureAwait(false);
            switch (donationResult)
            {
                case DonationResult.DonationSuccess:
                    await ReplyAsync($"**{Context.User.Username}** donated {_service.Bank.CurrencySymbol}{amount} to **{recipient.Username}**.").ConfigureAwait(false);
                    return;

                case DonationResult.DonorNotEnoughMoney:
                    await ReplyAsync($"**{Context.User.Username}** currently does not have enough HisuiBucks to make that donation.").ConfigureAwait(false);
                    return;

                case DonationResult.DonorNotFound:
                case DonationResult.RecipientNotFound:
                case DonationResult.MiscError:
                default:
                    await ReplyAsync("Failed to transfer donation.").ConfigureAwait(false);
                    return;
            }
        }

        [Command("top"), Permission(MinimumPermission.Special)]
        public async Task Tops()
        {
            var tops = (await _service.Bank.GetAllUsers().ConfigureAwait(false))
                .Where(a => a.Balance > 2500)
                .OrderByDescending(a => a.Balance)
                .Take(10)
                .Select(a => new
                {
                    Name = Context.Guild.GetUser(a.UserId).Username,
                    a.Balance
                })
                .ToList();

            var sb = new StringBuilder("```\n")
                .AppendSequence(tops, (s, a) => s.AppendLine($"{a.Name,20}: {_service.Bank.CurrencySymbol}{a.Balance,-7}"))
                .Append("```");

            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }
    }
}
