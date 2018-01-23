using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Preconditions;
using Discord.Addons.SimplePermissions;
using SharedExtensions;
using System.IO;
using System.Reflection;

namespace MechHisui.HisuiBets
{
    [Name("HisuiBets"), RequireContext(ContextType.Guild)]
    public sealed class HisuiBetsModule : ModuleBase<SocketCommandContext>
    {
        private readonly HisuiBankService _service;
        //private readonly Random _rng;

        private IBankAccount _account;
        private BetGame _game;

        public HisuiBetsModule(HisuiBankService service)
        {
            _service = service;
            //_rng = rng;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            _service.Games.TryGetValue(Context.Channel.Id, out _game);
            _account = _service.Bank.GetUser(Context.User.Id);
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(0), RequiresGameType(GameType.HungerGame)]
        public async Task Bet(int amount, [Remainder] string target)
        {
            if (await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = (uint)amount,
                    Tribute = target
                })).ConfigureAwait(false);
            }
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        public async Task Bet(int amount, int district)
        {
            if (await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = (uint)amount,
                    Tribute = district.ToString()
                })).ConfigureAwait(false);
            }
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        public async Task Bet(int amount, SaltyBetTeam team)
        {
            if (await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = (uint)amount,
                    Tribute = team.ToString()
                })).ConfigureAwait(false);
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
                && Context.User.Id == _game.GameMaster)
            {
                await ReplyAsync("The Game Master is not allowed to bet.").ConfigureAwait(false);
                return false;
            }
            if (bettedAmount <= 0)
            {
                await ReplyAsync("Cannot make a bet of 0 or less.").ConfigureAwait(false);
                return false;
            }
            if (_account?.Bucks == 0)
            {
                await ReplyAsync("You currently have no HisuiBucks.").ConfigureAwait(false);
                return false;
            }
            if (_account?.Bucks < bettedAmount)
            {
                await ReplyAsync("You do not have enough HisuiBucks to make that bet.").ConfigureAwait(false);
                return false;
            }
            return true;
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(0), RequiresGameType(GameType.HungerGame)]
        [Hidden]
        public Task Bet([LimitTo(StringComparison.OrdinalIgnoreCase, "all", "allin")] string allin, [Remainder] string target)
            => (_account != null) ?
                Bet(_account.Bucks, target) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        [Hidden]
        public Task Bet([LimitTo(StringComparison.OrdinalIgnoreCase, "all", "allin")] string allin, int district)
            => (_account != null) ?
                Bet(_account.Bucks, district) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        [Hidden]
        public Task Bet([LimitTo(StringComparison.OrdinalIgnoreCase, "all", "allin")] string allin, SaltyBetTeam team)
            => (_account != null)?
                Bet(_account.Bucks, team) :
                Task.CompletedTask;

        [Command("bet it all"), Permission(MinimumPermission.Everyone)]
        [Priority(3), RequiresGameType(GameType.Any), Hidden]
#pragma warning disable RCS1163 // Unused parameter.
        public Task Bet()
            => Context.Channel.SendFileAsync(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "kappa.png"),
                    text: $"**{Context.User.Username}** has bet all their bucks. Good luck.");
#pragma warning restore RCS1163 // Unused parameter.

        [Command("newgame"), Permission(MinimumPermission.Special)]
        public Task NewGame(GameType gameType = GameType.HungerGame)
        {
            var actualType = (gameType == GameType.Any) ? GameType.HungerGame : gameType;
            var game = new BetGame(_service.Bank, (ITextChannel)Context.Channel, actualType, Context.User.Id);

            return (_service.TryAddNewGame(Context.Channel.Id, game)) ?
                game.StartGame() :
                ReplyAsync("Game is already in progress.");
        }

        [Command("checkbets"), Permission(MinimumPermission.Special)]
        [RequiresGameType(GameType.Any)]
        public async Task CheckBets()
        {
            var sb = new StringBuilder("The following bets have been made:\n```\n", 2000);

            foreach (var bet in _game._betTracker)
            {
                sb.AppendLine($"{bet.UserName,-20}: {HisuiBankService.Symbol}{bet.BettedAmount,-7} - {bet.Tribute}");

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
#pragma warning disable RCS1174 // Remove redundant async/await.
        public async Task SetWinner([Remainder] string winner)
            => await ReplyAsync(await _game.Winner(winner).ConfigureAwait(false)).ConfigureAwait(false);
#pragma warning restore RCS1174 // Remove redundant async/await.

        [Command("bucks"), Permission(MinimumPermission.Everyone)]
        [Alias("mybucks")]
        public Task Bucks()
        {
            return ReplyAsync($"**{Context.User.Username}** currently has {HisuiBankService.Symbol}{_account.Bucks}.");
        }

        [Command("donate"), Permission(MinimumPermission.Everyone)]
        [Ratelimit(10, 10, Measure.Minutes)]
        public Task Donate(int amount, IUser recipient)
        {
            if (amount <= 0)
            {
                return ReplyAsync("Cannot make a donation of 0 or less.");
            }
            if (amount > _account.Bucks)
            {
                return ReplyAsync($"**{Context.User.Username}** currently does not have enough HisuiBucks to make that donation.");
            }
            if (recipient.IsBot || _service.Blacklist.Contains(recipient.Id))
            {
                return ReplyAsync("Unable to donate to Bot accounts.");
            }

            _service.Bank.Donate(new DonationRequest((uint)amount, _account.UserId, recipient.Id));
            return ReplyAsync($"**{Context.User.Username}** donated {HisuiBankService.Symbol}{amount} to **{recipient.Username}**.");
        }

        [Command("top"), Permission(MinimumPermission.Special)]
        public async Task Tops()
        {
            var tops = (await _service.Bank.GetAllUsers())
                .Where(a => a.Bucks > 2500)
                .OrderByDescending(a => a.Bucks)
                .Take(10)
                .Select(a => new
                {
                    Name = Context.Guild.GetUser(a.UserId).Username,
                    a.Bucks
                })
                .ToList();

            var sb = new StringBuilder("```\n")
                .AppendSequence(tops, (s, a) => s.AppendLine($"{a.Name,20}: {HisuiBankService.Symbol}{a.Bucks,-7}"))
                .Append("```");

            await ReplyAsync(sb.ToString());
        }
    }
}
