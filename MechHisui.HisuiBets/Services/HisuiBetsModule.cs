using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.SimplePermissions;
using Discord;
using System.Text;
using JiiLib;

namespace MechHisui.HisuiBets
{
    public sealed class HisuiBetsModule : ModuleBase
    {
        private readonly BetGame _game;
        private readonly HisuiBankService _service;
        private readonly UserAccount _account;

        private BankOfHisui _bank => _service.Bank;
        public HisuiBetsModule(HisuiBankService service)
        {
            service.Games.TryGetValue(Context.Channel.Id, out _game);
            _account = service.Bank.Accounts.SingleOrDefault(a => a.UserId == Context.User.Id);
            _service = service;
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(0), RequiresGameType(GameType.HungerGame)]
        [RequireContext(ContextType.Guild)]
        public async Task Bet(int amount, [Remainder] string target)
        {
            if (await checkPreReqs(amount))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Tribute = target
                }));
            }
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        [RequireContext(ContextType.Guild)]
        public async Task Bet(int amount, int district)
        {
            if (await checkPreReqs(amount))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Tribute = district.ToString()
                }));
            }
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        [RequireContext(ContextType.Guild)]
        public async Task Bet(int amount, SaltyBetTeam team)
        {
            if (await checkPreReqs(amount))
            {
                await ReplyAsync(_game.ProcessBet(new Bet
                {
                    UserName = Context.User.Username,
                    UserId = Context.User.Id,
                    BettedAmount = amount,
                    Tribute = team.ToString()
                }));
            }
        }

        private async Task<bool> checkPreReqs(int bettedAmount)
        {
            if (_game == null)
            {
                await ReplyAsync("No game to bet on.");
                return false;
            }
            if (!_game.BetsOpen)
            {
                await ReplyAsync("Bets are currently closed at this time.");
                return false;
            }
            if (_service._blacklist.Contains(Context.User.Id))
            {
                await ReplyAsync("Not allowed to bet.");
                return false;
            }
            if (bettedAmount <= 0)
            {
                await ReplyAsync("Cannot make a bet of 0 or less.");
                return false;
            }
            if (_account?.Bucks == 0)
            {
                await ReplyAsync("You currently have no HisuiBucks.");
                return false;
            }
            if (_account?.Bucks < bettedAmount)
            {
                await ReplyAsync("You do not have enough HisuiBucks to make that bet.");
                return false;
            }
            return true;
        }

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(0), RequiresGameType(GameType.HungerGame)]
        [RequireContext(ContextType.Guild)]
        public Task Bet(string allin, [Remainder] string target)
            => (_service.allins.Contains(allin.ToLowerInvariant())
                && _account != null) ?
                Bet(_account.Bucks, target) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(1), RequiresGameType(GameType.HungerGameDistrictsOnly)]
        [RequireContext(ContextType.Guild)]
        public Task Bet(string allin, int district)
            => (_service.allins.Contains(allin.ToLowerInvariant())
                && _account != null) ?
                Bet(_account.Bucks, district) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(2), RequiresGameType(GameType.SaltyBet)]
        [RequireContext(ContextType.Guild)]
        public Task Bet(string allin, SaltyBetTeam team)
            => (_service.allins.Contains(allin.ToLowerInvariant())
                && _account != null)?
                Bet(_account.Bucks, team) :
                Task.CompletedTask;

        [Command("bet"), Permission(MinimumPermission.Everyone)]
        [Priority(-1), RequiresGameType(GameType.Any), Hidden]
        [RequireContext(ContextType.Guild)]
        public Task Bet([Remainder, LimitTo(StringComparison.OrdinalIgnoreCase, "it all")] string itall)
            => Context.Channel.SendFileAsync("kappa.png",
                    text: $"**{Context.User.Username}** has bet all their bucks. Good luck.");

        [Command("newgame"), Permission(MinimumPermission.Special)]
        [RequireContext(ContextType.Guild)]
        public Task NewGame(GameType gameType = GameType.HungerGame)
        {
            var actualType = (gameType == GameType.Any) ? GameType.HungerGame : gameType;
            var game = new BetGame(_service.Bank, (ITextChannel)Context.Channel, actualType);

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
                sb.AppendLine($"{bet.UserName,-20}: {HisuiBankService.symbol}{bet.BettedAmount,-7} - {bet.Tribute}");

                if (sb.Length > 1700)
                {

                    sb.Append("```");
                    await ReplyAsync(sb.ToString());
                    sb.Clear().AppendLine("```");
                }
            }
            sb.Append("```");
            await ReplyAsync(sb.ToString());
        }

        [Command("closebets"), Permission(MinimumPermission.Special)]
        [RequireContext(ContextType.Guild), RequiresGameType(GameType.Any)]
        public async Task CloseBets()
        {
            if (!_game.BetsOpen)
            {
                await ReplyAsync("Bets are already closed.");
                return;
            }
            if (_game.GameType == GameType.SaltyBet)
            {
                await ReplyAsync("This type of game closes automatically.");
                return;
            }

            await ReplyAsync("Bets are going to close soon. Please place your final bets now.");
            _game.ClosingGame();
        }

        [Command("winner"), Permission(MinimumPermission.Special)]
        [RequireContext(ContextType.Guild), RequiresGameType(GameType.Any)]
        public async Task SetWinner(string winner) => await ReplyAsync(await _game.Winner(winner));

        [Command("donate"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild), Ratelimit(10, 10, Measure.Minutes)]
        public async Task Donate(int amount, IUser user)
        {
            if (amount <= 0)
            {
                await ReplyAsync("Cannot make a donation of 0 or less.");
                return;
            }
            if (amount > _bank.Accounts.Single(u => u.UserId == Context.User.Id).Bucks)
            {
                await ReplyAsync($"**{Context.User.Username}** currently does not have enough HisuiBucks to make that donation.");
                return;
            }
            if (user.IsBot || _service._blacklist.Contains(user.Id))
            {
                await ReplyAsync("Unable to donate to Bot accounts.");
                return;
            }

            _bank.Accounts.Single(p => p.UserId == Context.User.Id).Bucks -= amount;
            _bank.Accounts.Single(p => p.UserId == user.Id).Bucks += amount;
            _bank.WriteBank();
            await ReplyAsync($"**{Context.User.Username}** donated {HisuiBankService.symbol}{amount} to **{user.Username}**.");
        }

        [Command("top"), Permission(MinimumPermission.Special)]
        [RequireContext(ContextType.Guild)]
        public Task Tops()
        {
            var tops = _bank.Accounts.Where(a => a.Bucks > 2500)
                .OrderByDescending(a => a.Bucks)
                .Take(10)
                .Select(a => new
                {
                    Name = Context.Guild.GetUserAsync(a.UserId).GetAwaiter().GetResult().Username,
                    a.Bucks
                })
                .ToList();

            var sb = new StringBuilder("```\n")
                .AppendSequence(tops, (s, a) => s.AppendLine($"{a.Name,20}: {HisuiBankService.symbol}{a.Bucks,-7}"))
                .Append("```");

            return ReplyAsync(sb.ToString());
        }
    }
}
