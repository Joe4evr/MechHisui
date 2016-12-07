using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    /// <summary>
    /// 
    /// </summary>
    [Name("SecretHitler")]
    public sealed class SecretHitlerModule : MpGameModuleBase<SecretHitlerService, SecretHitlerGame, SecretHitlerPlayer>

    {
        private const int MaxPlayers = 10;
        private readonly HouseRules _currentHouseRules;

        public SecretHitlerModule(SecretHitlerService service) : base(service)
        {
            service.HouseRulesList.TryGetValue(Context.Channel.Id, out _currentHouseRules);
        }

        [Command("rules"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild)]
        public async Task RulesCmd()
        {
            var sb = new StringBuilder("How to play:\n")
                .AppendLine("There are three roles: Liberal, Fascist, and Hitler.")
                .AppendLine("Hitler does not know who his fellow Fascists are, but the Fascists know who Hitler is (except in 5 or 6 player games).")
                .AppendLine("Liberals will always start off not knowing anything.")
                .AppendLine("If 6 Fascist Policies are enacted, or Hitler is chosen as Chancellor in the late-game, the Fascists win.")
                .AppendLine("If 5 Liberal Policies are enacted, or Hitler is successfully killed, the Liberals win.")
                .AppendLine($"The following themes are available too: `{String.Join("`, `", GameService.Configs.Keys)}`")
                .AppendLine("For more details: https://dl.dropboxusercontent.com/u/502769/Secret_Hitler_Rules.pdf ")
                .Append("Good luck, have fun.");

            await ReplyAsync(sb.ToString());
        }

        [Command("open"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override async Task OpenGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            else if (OpenToJoin)
            {
                await ReplyAsync("There is already a game open to join.");
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: true, comparisonValue: false))
                {
                    GameService.MakeNewPlayerList(Context.Channel.Id);
                    GameService.HouseRulesList[Context.Channel.Id] = HouseRules.None;
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
            else if (PlayerList.Count == MaxPlayers)
            {
                await ReplyAsync("Maximum number of players already joined.");
            }
            else
            {
                var author = Context.User as IGuildUser;
                if (author != null)
                {
                    if (PlayerList.Add(author))
                        await ReplyAsync($"**{author.Username}** has joined.");
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
                var author = Context.User as IGuildUser;
                if (author != null && PlayerList.Remove(author))
                {
                    await ReplyAsync($"**{author.Username}** has left.");
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
                    await ReplyAsync("Game was cancelled.");
                }
            }
        }

        [Command("start"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public override Task StartGameCmd()
            => StartInternal(SecretHitlerConfig.Default);

        [Command("start"), Permission(MinimumPermission.ModRole)] //rly?
        [RequireContext(ContextType.Guild)]
        public Task StartGameCmd(string configName)
        {
            SecretHitlerConfig config;
            return GameService.Configs.TryGetValue(configName, out config) ?
                 StartInternal(config) : ReplyAsync("Could not find that config.");
        }

        private async Task StartInternal(SecretHitlerConfig config)
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game has been opened at this time.");
            }
            else if (PlayerList.Count < 5)
            {
                await ReplyAsync("Not enough players have joined.");
            }
            else
            {
                int fascists = 0;
                switch (PlayerList.Count)
                {
                    case 5:
                    case 6:
                        fascists = 2;
                        break;
                    case 7:
                    case 8:
                        fascists = 3;
                        break;
                    case 9:
                    case 10:
                        fascists = 4;
                        break;
                    default:
                        break;
                }

                var players = new List<SecretHitlerPlayer>();

                for (int i = 0; i < PlayerList.Count; i++)
                {
                    if (players.Count == 0)
                    {
                        players.Add(new SecretHitlerPlayer(PlayerList.ElementAt(i), Context.Channel,
                            config.FascistParty, config.Hitler));
                    }
                    else if (players.Count(p => p.Party == config.FascistParty) < fascists)
                    {
                        players.Add(new SecretHitlerPlayer(PlayerList.ElementAt(i), Context.Channel,
                            config.FascistParty, config.Fascist));
                    }
                    else
                    {
                        players.Add(new SecretHitlerPlayer(PlayerList.ElementAt(i), Context.Channel,
                            config.LiberalParty, config.Liberal));
                    }
                }

                players = players.Shuffle(32).ToList();

                var game = new SecretHitlerGame(Context.Channel, players, config, _currentHouseRules);
                if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: false, comparisonValue: true) &&
                    GameService.TryAddNewGame(Context.Channel.Id, game))
                {
                    await game.SetupGame();
                    await game.StartGame();
                }
            }
        }

        [Command("turn"), RequireGameState(GameState.EndOfTurn)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.President)]
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

        [Command("enable"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public async Task EnableHouserule(string rule)
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("There is no game open to set rules for.");
            }
            else
            {
                var r = GetRule(rule);
                if ((_currentHouseRules & r) == r)
                {
                    await ReplyAsync("Specified rule already enabled.");
                    return;
                }
                var newRules = _currentHouseRules | r;
                if (newRules == _currentHouseRules)
                {
                    await ReplyAsync("Unknown parameter.");
                }
                else if (GameService.HouseRulesList.TryUpdate(Context.Channel.Id, newValue: newRules, comparisonValue: _currentHouseRules))
                {
                    await ReplyAsync($"Enabled house rule: {r.ToString()}.");
                }
            }
        }

        [Command("disable"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild)]
        public async Task DisableHouserule(string rule)
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("There is no game open to set rules for.");
            }
            else
            {
                var r = GetRule(rule);
                if ((_currentHouseRules & r) == r)
                {
                    await ReplyAsync("Specified rule already disabled.");
                    return;
                }
                var newRules = _currentHouseRules ^ r;
                if (newRules == _currentHouseRules)
                {
                    await ReplyAsync("Unknown parameter.");
                }
                else if (GameService.HouseRulesList.TryUpdate(Context.Channel.Id, newValue: newRules, comparisonValue: _currentHouseRules))
                {
                    await ReplyAsync($"Disabled house rule: {r.ToString()}.");
                }
            }
        }

        private HouseRules GetRule(string rule)
        {
            switch (rule)
            {
                case "skip1":
                case "skipfirst":
                    return HouseRules.SkipFirstElection;
                default:
                    return HouseRules.None;
            }
        }

        //Guild commands

        [Command("nominate"), RequireGameState(GameState.StartOfTurn)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.President)]
        public Task NominatePlayer(IGuildUser user) => Game.NominatedChancellor(user);

        [Command("elect"), RequireGameState(GameState.SpecialElection)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.President)]
        public Task ElectPlayer(IGuildUser user) => Game.SpecialElection(user);

        [Command("investigate"), RequireGameState(GameState.Investigating)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.President)]
        public Task InvestigatePlayer(IGuildUser user) => Game.InvestigatePlayer(user);

        [Command("kill"), RequireGameState(GameState.Kill)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.President)]
        public Task KillPlayer(IGuildUser user) => Game.KillPlayer(user);

        [Command("veto"), RequireGameState(GameState.ChancellorVetod)]
        [RequireContext(ContextType.Guild), RequirePlayerRole(PlayerRole.President)]
        public Task Veto(string consent) => Game.PresidentConsentsVeto(consent.ToLowerInvariant());

        //DM commands

        [Command("vote"), RequireGameState(GameState.VoteForGovernment)]
        [RequireContext(ContextType.DM)]
        public Task Vote(string vote)
            => Game.ProcessVote((IDMChannel)Context.Channel, Context.User, vote);

        [Command("discard"), RequireGameState(GameState.PresidentPicks)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.President)]
        public Task PickDiscard(int pick)
        {
            switch (pick)
            {
                case 1:
                case 2:
                case 3:
                    return Game.PresidentDiscards((IDMChannel)Context.Channel, pick);
                default:
                    return ReplyAsync("Out of range.");
            }
        }

        [Command("play"), RequireGameState(GameState.ChancellorPicks)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.Chancellor)]
        public Task PickPlay(int pick)
        {
            switch (pick)
            {
                case 1:
                case 2:
                    return Game.ChancellorPlays((IDMChannel)Context.Channel, pick);
                default:
                    return ReplyAsync("Out of range.");
            }
        }

        [Command("veto"), RequireGameState(GameState.ChancellorPicks)]
        [RequireContext(ContextType.DM), RequirePlayerRole(PlayerRole.Chancellor)]
        public Task Veto()
            => Game.ChancellorVetos((IDMChannel)Context.Channel);
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
