using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using MechHisui.Superfight.Preconditions;
using MechHisui.Superfight.Models;
using SharedExtensions;

namespace MechHisui.Superfight
{
    [Name("Superfight"), Group("sf")]
    [Permission(MinimumPermission.Everyone)]
    public sealed class SuperfightModule : MpGameModuleBase<SuperfightService, SuperfightGame, SuperfightPlayer>
    {
        private int _discusstimeout = 5;
        public SuperfightModule(SuperfightService service) : base(service)
        {
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            _discusstimeout = GameService.DiscussionTimer.GetValueOrDefault(Context.Channel, defaultValue: 5);
        }

        [Command("open"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override async Task OpenGameCmd()
        {
            if (GameInProgress != CurrentlyPlaying.None)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (OpenToJoin)
            {
                await ReplyAsync("There is already a game open to join.").ConfigureAwait(false);
            }
            else
            {
                if (await GameService.OpenNewGame(Context).ConfigureAwait(false))
                {
                    await ReplyAsync("Opening for a game.").ConfigureAwait(false);
                }
            }
        }

        [Command("join"), RequireContext(ContextType.Guild)]
        public override async Task JoinGameCmd()
        {
            if (Game != null)
            {
                await ReplyAsync("Cannot join a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to join.").ConfigureAwait(false);
            }
            else
            {
                if (await GameService.AddUser(Context.Channel, Context.User).ConfigureAwait(false))
                {
                    await ReplyAsync($"**{Context.User.Username}** has joined.").ConfigureAwait(false);
                }
            }
        }

        [Command("leave"), RequireContext(ContextType.Guild)]
        public override async Task LeaveGameCmd()
        {
            if (Game != null)
            {
                await ReplyAsync("Cannot leave a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to leave.").ConfigureAwait(false);
            }
            else
            {
                if (await GameService.RemoveUser(Context.Channel, Context.User).ConfigureAwait(false))
                {
                    await ReplyAsync($"**{Context.User.Username}** has left.").ConfigureAwait(false);
                }
            }
        }

        [Command("cancel"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override async Task CancelGameCmd()
        {
            if (GameInProgress == CurrentlyPlaying.ThisGame)
            {
                await ReplyAsync("Cannot cancel a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to cancel.").ConfigureAwait(false);
            }
            else
            {
                if (await GameService.CancelGame(Context.Channel).ConfigureAwait(false))
                {
                    await ReplyAsync("Game was canceled.").ConfigureAwait(false);
                }
            }
        }

        [Command("start"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override async Task StartGameCmd()
        {
            if (GameInProgress != CurrentlyPlaying.None)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game has been opened at this time.").ConfigureAwait(false);
            }
            else if (JoinedUsers.Count < 4)
            {
                await ReplyAsync("Not enough players have joined.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel, newValue: false, comparisonValue: true))
                {
                    var players = JoinedUsers.Select(u => new SuperfightPlayer(u, Context.Channel)).Shuffle(28);

                    var game = new SuperfightGame(Context.Channel, players, GameService.Config, _discusstimeout);
                    if (await GameService.TryAddNewGame(Context.Channel, game).ConfigureAwait(false))
                    {
                        await game.SetupGame().ConfigureAwait(false);
                        await game.StartGame().ConfigureAwait(false);
                    }
                }
            }
        }

        [Command("turn"), RequireGameState(GameState.EndOfTurn)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.Fighter)]
        public override Task NextTurnCmd()
            => (Game != null)
                ? Game.NextTurn()
                : ReplyAsync("No game in progress.");

        [Command("endearly"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override Task EndGameCmd()
            => (Game != null)
                ? Game.EndGame("Game ended early by moderator.")
                : ReplyAsync("No game in progress to end.");

        [Command("state"), RequireContext(ContextType.Guild)]
        public override Task GameStateCmd()
            => (Game != null)
                ? ReplyAsync(Game.GetGameState())
                : ReplyAsync("No game in progress.");

        [Command("vote"), RequirePlayerRole(PlayerRole.NonFighter)]
        [RequireContext(ContextType.Guild), RequireGameState(GameState.Voting)]
        public Task VoteCmd(SuperfightPlayer target)
            => (Game != null)
                ? Game.ProcessVote(voter: Player, target: target)
                : ReplyAsync("No game in progress.");

        [Command("pick"), RequirePlayerRole(PlayerRole.Fighter)]
        [RequireContext(ContextType.DM)]
        [RequireGameState(GameState.Choosing)]
        public Task ChooseCmd(int index)
            => (Game != null)
                ? ReplyAsync(Game.ChooseInternal(Player, index))
                : ReplyAsync("No game in progress.");

        [Command("confirm"), RequireGameState(GameState.Choosing)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.Fighter)]
        public Task ConfirmCmd()
            => (Game != null)
                ? Game.ConfirmInternal(Player)
                : ReplyAsync("No game in progress.");

        [Command("settimer"), RequireContext(ContextType.Guild)]
        [Permission(MinimumPermission.ModRole)]
        public Task SetTimerCmd(int minutes)
        {
            if (GameInProgress != CurrentlyPlaying.None)
            {
                return ReplyAsync("Command cannot be used during game.");
            }

            GameService.DiscussionTimer[Context.Channel] = minutes;
            return ReplyAsync($"Discussion timer now set to {minutes} minutes.");
        }
    }
}
