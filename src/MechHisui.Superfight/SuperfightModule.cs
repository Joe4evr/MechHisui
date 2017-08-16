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

namespace MechHisui.Superfight
{
    [Name("Superfight")]
    public sealed class SuperfightModule : MpGameModuleBase<SuperfightService, SuperfightGame, SuperfightPlayer>
    {
        private int _discusstimeout = 5;
        public SuperfightModule(SuperfightService service) : base(service)
        {
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            _discusstimeout = GameService.DiscussionTimer.GetValueOrDefault(Context.Channel.Id, defaultValue: 5);
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
                if (GameService.OpenNewGame(Context.Channel))
                {
                    await ReplyAsync("Opening for a game.").ConfigureAwait(false);
                }
            }
        }

        [Command("join"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild)]
        public override async Task JoinGameCmd()
        {
            if (GameInProgress == CurrentlyPlaying.ThisGame)
            {
                await ReplyAsync("Cannot join a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to join.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.AddUser(Context.Channel, Context.User))
                {
                    await ReplyAsync($"**{Context.User.Username}** has joined.").ConfigureAwait(false);
                }
            }
        }

        [Command("leave"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild)]
        public override async Task LeaveGameCmd()
        {
            if (GameInProgress == CurrentlyPlaying.ThisGame)
            {
                await ReplyAsync("Cannot leave a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to leave.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.RemoveUser(Context.Channel, Context.User))
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
                if (GameService.CancelGame(Context.Channel))
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
            else if (PlayerList.Count < 4)
            {
                await ReplyAsync("Not enough players have joined.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel, newValue: false, comparisonValue: true))
                {
                    var players = PlayerList.Select(u => new SuperfightPlayer(u, Context.Channel)).Shuffle(28);

                    var game = new SuperfightGame(Context.Channel, players, GameService.Config, _discusstimeout);
                    if (GameService.TryAddNewGame(Context.Channel, game))
                    {
                        await game.SetupGame().ConfigureAwait(false);
                        await game.StartGame().ConfigureAwait(false);
                    }
                }
            }
        }

        [Command("turn"), RequireGameState(GameState.EndOfTurn)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.Fighter)]
        [Permission(MinimumPermission.Everyone)]
        public override Task NextTurnCmd()
            => GameInProgress == CurrentlyPlaying.ThisGame ? Game.NextTurn() : ReplyAsync("No game in progress.");

        [Command("endearly"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override Task EndGameCmd()
            => GameInProgress == CurrentlyPlaying.ThisGame ? Game.EndGame("Game ended early by moderator.") : ReplyAsync("No game in progress to end.");

        [Command("state"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild)]
        public override Task GameStateCmd()
            => GameInProgress == CurrentlyPlaying.ThisGame ? ReplyAsync(Game.GetGameState()) : ReplyAsync("No game in progress.");

        [Command("vote"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild), RequireGameState(GameState.Voting)]
        public Task VoteCmd(IUser target)
            => GameInProgress == CurrentlyPlaying.ThisGame ? Game.ProcessVote(voter: Context.User, target: target) : ReplyAsync("No game in progress.");

        [Command("pick"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.Fighter)]
        [RequireGameState(GameState.Choosing)]
        public Task ChooseCmd(int index)
         => GameInProgress == CurrentlyPlaying.ThisGame ? ReplyAsync(Game.ChooseInternal(Context.User, index)) : ReplyAsync("No game in progress.");

        [Command("confirm"), RequireGameState(GameState.Choosing)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.Fighter)]
        [Permission(MinimumPermission.Everyone)]
        public Task ConfirmCmd()
         => GameInProgress == CurrentlyPlaying.ThisGame ? Game.ConfirmInternal(Context.User) : ReplyAsync("No game in progress.");

        [Command("settimer"), RequireContext(ContextType.Guild), Permission(MinimumPermission.ModRole)]
        public Task SetTimerCmd(int minutes)
        {
            GameService.DiscussionTimer[Context.Channel.Id] = minutes;
            return ReplyAsync($"Discussion timer now set to {minutes} minutes.");
        }
    }
}
