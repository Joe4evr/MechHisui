using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using Discord;
using Discord.Addons.MpGame;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using MechHisui.Superfight.Preconditions;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight
{
    public sealed class SuperfightModule : MpGameModuleBase<SuperfightService, SuperfightGame, SuperfightPlayer>
    {
        private readonly int _discusstimeout;
        public SuperfightModule(SuperfightService service) : base(service)
        {
            _discusstimeout = service.DiscussionTimer.TryGetValue(Context.Channel.Id, out _discusstimeout) ? _discusstimeout : 5;
        }

        [Command("open"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override async Task OpenGameCmd()
        {
            //Make sure to check if a game is already going...
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            //...or if it's looking for players but hasn't yet started...
            else if (OpenToJoin)
            {
                await ReplyAsync("There is already a game open to join.");
            }
            //...before deciding what needs to be done.
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: true, comparisonValue: false))
                {
                    GameService.MakeNewPlayerList(Context.Channel.Id);
                    await ReplyAsync("Opening for a game.");
                }
            }
        }

        [Command("join"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild)]
        public override async Task JoinGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot join a game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to join.");
            }
            else
            {
                if (PlayerList.Add(Context.User))
                {
                    await ReplyAsync($"**{Context.User.Username}** has joined.");
                }
            }
        }

        [Command("leave"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild)]
        public override async Task LeaveGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot leave a game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to leave.");
            }
            else
            {
                if (PlayerList.Remove(Context.User))
                {
                    await ReplyAsync($"**{Context.User.Username}** has left.");
                }
            }
        }

        [Command("cancel"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override async Task CancelGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot cancel a game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to cancel.");
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: false, comparisonValue: true))
                {
                    PlayerList.Clear();
                    await ReplyAsync("Game was canceled.");
                }
            }
        }

        [Command("start"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override async Task StartGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game has been opened at this time.");
            }
            else if (PlayerList.Count < 4)
            {
                await ReplyAsync("Not enough players have joined.");
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: false, comparisonValue: true))
                {
                    var players = PlayerList.Select(u => new SuperfightPlayer(u, Context.Channel)).Shuffle(28);

                    var game = new SuperfightGame(Context.Channel, players, GameService.Config, _discusstimeout);
                    if (GameService.TryAddNewGame(Context.Channel.Id, game))
                    {
                        await game.SetupGame();
                        await game.StartGame();
                    }
                }
            }
        }

        [Command("turn"), RequireGameState(GameState.EndOfTurn)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.Fighter)]
        [Permission(MinimumPermission.Everyone)]
        public override Task NextTurnCmd()
            => GameInProgress ? Game.NextTurn() : ReplyAsync("No game in progress.");

        [Command("endearly"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override Task EndGameCmd()
            => GameInProgress ? Game.EndGame("Game ended early by moderator.") : ReplyAsync("No game in progress to end.");

        [Command("state"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild)]
        public override Task GameStateCmd()
            => GameInProgress ? ReplyAsync(Game.GetGameState()) : ReplyAsync("No game in progress.");

        [Command("vote"), RequireGameState(GameState.Voting)]
        [RequireContext(ContextType.Guild), Permission(MinimumPermission.Everyone)]
        public Task VoteCmd(IUser target)
            => GameInProgress ? Game.ProcessVote(voter: Context.User, target: target) : ReplyAsync("No game in progress.");

        [Command("pick"), RequireGameState(GameState.Choosing)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.Fighter)]
        [Permission(MinimumPermission.Everyone)]
        public Task ChooseCmd(int index)
         => GameInProgress ? ReplyAsync(Game.ChooseInternal(Context.User, index)) : ReplyAsync("No game in progress.");

        [Command("confirm"), RequireGameState(GameState.Choosing)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.Fighter)]
        [Permission(MinimumPermission.Everyone)]
        public Task ConfirmCmd()
         => GameInProgress ? Game.ConfirmInternal(Context.User) : ReplyAsync("No game in progress.");

        [Command("settimer"), RequireContext(ContextType.Guild), Permission(MinimumPermission.ModRole)]
        public Task SetTimerCmd(int minutes)
        {
            GameService.DiscussionTimer[Context.Channel.Id] = minutes;
            return ReplyAsync($"Discussion timer now set to {minutes} minutes.");
        }
    }

    static class Ext
    {
        //Method for randomizing lists using a Fisher-Yates shuffle.
        //Taken from http://stackoverflow.com/questions/273313/
        /// <summary>
        /// Perform a Fisher-Yates shuffle on a collection implementing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="source">The list to shuffle.</param>
        /// <remarks>Adapted from http://stackoverflow.com/questions/273313/. </remarks>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, int iterations = 1)
        {
            var provider = RandomNumberGenerator.Create();
            var buffer = source.ToList();
            int n = buffer.Count;
            for (int i = 0; i < iterations; i++)
            {
                while (n > 1)
                {
                    byte[] box = new byte[(n / Byte.MaxValue) + 1];
                    int boxSum;
                    do
                    {
                        provider.GetBytes(box);
                        boxSum = box.Sum(b => b);
                    }
                    while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                    int k = (boxSum % n);
                    n--;
                    T value = buffer[k];
                    buffer[k] = buffer[n];
                    buffer[n] = value;
                }
            }

            return buffer;
        }

        /// <summary>
        /// Appends a <see cref="string"/> to a <see cref="StringBuilder"/> instance only if a condition is met.
        /// </summary>
        /// <param name="builder">A <see cref="StringBuilder"/> instanc</param>
        /// <param name="predicate">The condition to be met.</param>
        /// <param name="fn">The function to apply if predicate is true.</param>
        /// <returns>A <see cref="StringBuilder"/> instance with the specified
        /// <see cref="string"/> appended if predicate was true,
        /// or the unchanged <see cref="StringBuilder"/> instance otherwise.</returns>
        public static StringBuilder AppendWhen(this StringBuilder builder, Func<bool> predicate, Func<StringBuilder, StringBuilder> fn)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            return predicate() ? fn(builder) : builder;
        }

        /// <summary>
        /// Appends each element of an <see cref="IEnumerable{T}"/> to a <see cref="StringBuilder"/> instance.
        /// </summary>
        /// <param name="builder">A <see cref="StringBuilder"/> instance</param>
        /// <param name="seq">The sequence to append.</param>
        /// <param name="fn">The function to apply to each element of the sequence.</param>
        /// <returns>An instance of <see cref="StringBuilder"/> with all elements of seq appended.</returns>
        public static StringBuilder AppendSequence<T>(this StringBuilder builder, IEnumerable<T> seq, Func<StringBuilder, T, StringBuilder> fn)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (seq == null) throw new ArgumentNullException(nameof(seq));
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            return seq.Aggregate(builder, fn);
        }
    }
}
