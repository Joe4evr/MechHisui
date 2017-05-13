using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Preconditions;
using Discord.Addons.SimplePermissions;

namespace MechHisui.HisuiBets
{
    [Name("HisuiBets")]
    public sealed class HisuiBetsModule : ModuleBase<ICommandContext>
    {
        private readonly HisuiBankService _service;
        private UserAccount _account;
        private BetGame _game;

        public HisuiBetsModule(HisuiBankService service)
        {
            _service = service;
        }

        protected override void BeforeExecute()
        {
            base.BeforeExecute();
            _service.Games.TryGetValue(Context.Channel.Id, out _game);
            _account = _service.Bank.GetUser(Context.User.Id);
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(0), RequiresGameType(GameType.HungerGame)]
        [RequireContext(ContextType.Guild)]
        public async Task Bet(int amount, [Remainder] string target)
        {
            if (await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Tribute = target
                })).ConfigureAwait(false);
            }
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        [RequireContext(ContextType.Guild)]
        public async Task Bet(int amount, int district)
        {
            if (await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Tribute = district.ToString()
                })).ConfigureAwait(false);
            }
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        [RequireContext(ContextType.Guild)]
        public async Task Bet(int amount, SaltyBetTeam team)
        {
            if (await CheckPreReqs(amount).ConfigureAwait(false))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
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
            if (_service._blacklist.Contains(Context.User.Id))
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
        [RequireContext(ContextType.Guild), Hidden]
        public Task Bet(string allin, [Remainder] string target)
            => (_service.allins.Contains(allin.ToLowerInvariant())
                && _account != null) ?
                Bet(_account.Bucks, target) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        [RequireContext(ContextType.Guild), Hidden]
        public Task Bet(string allin, int district)
            => (_service.allins.Contains(allin.ToLowerInvariant())
                && _account != null) ?
                Bet(_account.Bucks, district) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        [RequireContext(ContextType.Guild), Hidden]
        public Task Bet(string allin, SaltyBetTeam team)
            => (_service.allins.Contains(allin.ToLowerInvariant())
                && _account != null)?
                Bet(_account.Bucks, team) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(-1), RequiresGameType(GameType.Any)]
        [RequireContext(ContextType.Guild), Hidden]
#pragma warning disable RCS1163 // Unused parameter.
        public Task Bet([Remainder, LimitTo(StringComparison.OrdinalIgnoreCase, "it all")] string itall)
            => Context.Channel.SendFileAsync("kappa.png",
                    text: $"**{Context.User.Username}** has bet all their bucks. Good luck.");
#pragma warning restore RCS1163 // Unused parameter.

        [Command("newgame"), Permission(MinimumPermission.Special)]
        [RequireContext(ContextType.Guild)]
        public Task NewGame(GameType gameType = GameType.HungerGame)
        {
            var actualType = (gameType == GameType.Any) ? GameType.HungerGame : gameType;
            var game = new BetGame(_service.Bank, (ITextChannel)Context.Channel, actualType, Context.User.Id);

            return (_service.TryAddNewGame(Context.Channel.Id, game)) ?
                game.StartGame() :
                ReplyAsync("Game is already in progress.");
        }

        [Command("checkbets"), Permission(MinimumPermission.Special)]
        [RequireContext(ContextType.Guild), RequiresGameType(GameType.Any)]
        public async Task CheckBets()
        {
            var sb = new StringBuilder("The following bets have been made:\n```\n", 2000);

            foreach (var bet in _game.ActiveBets)
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
        [RequireContext(ContextType.Guild), RequiresGameType(GameType.Any)]
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
        [RequireContext(ContextType.Guild), RequiresGameType(GameType.Any)]
#pragma warning disable RCS1174 // Remove redundant async/await.
        public async Task SetWinner(string winner)
            => await ReplyAsync(await _game.Winner(winner).ConfigureAwait(false)).ConfigureAwait(false);
#pragma warning restore RCS1174 // Remove redundant async/await.

        [Command("bucks"), Permission(MinimumPermission.Everyone)]
        [Alias("mybucks"), RequireContext(ContextType.Guild)]
        public Task Bucks()
        {
            return ReplyAsync($"**{Context.User.Username}** currently has {HisuiBankService.Symbol}{_account.Bucks}.");
        }

        [Command("donate"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild), Ratelimit(10, 10, Measure.Minutes)]
        public async Task Donate(int amount, IUser user)
        {
            if (amount <= 0)
            {
                await ReplyAsync("Cannot make a donation of 0 or less.").ConfigureAwait(false);
                return;
            }
            if (amount > _service.Bank.GetUser(Context.User.Id).Bucks)
            {
                await ReplyAsync($"**{Context.User.Username}** currently does not have enough HisuiBucks to make that donation.").ConfigureAwait(false);
                return;
            }
            if (user.IsBot || _service._blacklist.Contains(user.Id))
            {
                await ReplyAsync("Unable to donate to Bot accounts.").ConfigureAwait(false);
                return;
            }

            _service.Bank.Donate(Context.User.Id, user.Id, amount);
            await ReplyAsync($"**{Context.User.Username}** donated {HisuiBankService.Symbol}{amount} to **{user.Username}**.").ConfigureAwait(false);
        }

        [Command("top"), Permission(MinimumPermission.Special)]
        [RequireContext(ContextType.Guild)]
        public Task Tops()
        {
            var tops = _service.Bank.GetAllUsers()
                .Where(a => a.Bucks > 2500)
                .OrderByDescending(a => a.Bucks)
                .Take(10)
                .Select(a => new
                {
                    Name = Context.Guild.GetUserAsync(a.UserId).GetAwaiter().GetResult().Username,
                    a.Bucks
                })
                .ToList();

            var sb = new StringBuilder("```\n")
                .AppendSequence(tops, (s, a) => s.AppendLine($"{a.Name,20}: {HisuiBankService.Symbol}{a.Bucks,-7}"))
                .Append("```");

            return ReplyAsync(sb.ToString());
        }
    }
}
