using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    [Name("SecretHitler")]
    public sealed class SecretHitlerModule : MpGameModuleBase<SecretHitlerService, SecretHitlerGame, SecretHitlerPlayer>

    {
        private const int _maxPlayers = 10;
        private HouseRules _currentHouseRules;

        public SecretHitlerModule(SecretHitlerService service) : base(service)
        {
        }

        protected override void BeforeExecute()
        {
            base.BeforeExecute();
            _currentHouseRules = GameService.HouseRulesList.GetValueOrDefault(Context.Channel, defaultValue: HouseRules.None);
        }

        [Command("rules"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public async Task RulesCmd()
        {
            var sb = new StringBuilder("How to play:\n")
                .AppendLine("There are three roles: Liberal, Fascist, and Hitler.")
                .AppendLine("Hitler does not know who his fellow Fascists are, but the Fascists know who Hitler is (except in 5 or 6 player games).")
                .AppendLine("Liberals will always start off not knowing anything.")
                .AppendLine("If 6 Fascist Policies are enacted, or Hitler is chosen as Chancellor in the late-game, the Fascists win.")
                .AppendLine("If 5 Liberal Policies are enacted, or Hitler is successfully killed, the Liberals win.")
                .AppendWhen(() => GameService.Configs.Keys.Any(), b =>  b.AppendLine($"The following themes are available too: `{String.Join("`, `", GameService.Configs.Keys)}`"))
                .AppendLine("For more details: http://secrethitler.com/assets/Secret_Hitler_Rules.pdf ")
                .Append("Good luck, have fun.");

            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("open"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public override async Task OpenGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (OpenToJoin)
            {
                await ReplyAsync("There is already a game open to join.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel, newValue: true, comparisonValue: false))
                {
                    //GameService.MakeNewPlayerList(Context.Channel);
                    GameService.HouseRulesList[Context.Channel] = HouseRules.None;
                    await ReplyAsync("Opening for a game.").ConfigureAwait(false);
                }
            }
        }

        [Command("join"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public override async Task JoinGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot join a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to join.").ConfigureAwait(false);
            }
            else if (PlayerList.Count == _maxPlayers)
            {
                await ReplyAsync("Maximum number of players already joined.").ConfigureAwait(false);
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
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public override async Task LeaveGameCmd()
        {
            if (GameInProgress)
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
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public override async Task CancelGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot cancel a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to cancel.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel, newValue: false, comparisonValue: true))
                {
                    PlayerList.Clear();
                    await ReplyAsync("Game was cancelled.").ConfigureAwait(false);
                }
            }
        }

        [Command("start"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public override Task StartGameCmd()
            => StartInternal(SecretHitlerConfig.Default);

        [Command("start"), Permission(MinimumPermission.ModRole)] //rly?
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public Task StartGameCmd(string configName)
        {
            return GameService.Configs.TryGetValue(configName, out var config) ?
                 StartInternal(config) : ReplyAsync("Could not find that config.");
        }

        private async Task StartInternal(SecretHitlerConfig config)
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game has been opened at this time.").ConfigureAwait(false);
            }
            else if (PlayerList.Count < 5)
            {
                await ReplyAsync("Not enough players have joined.").ConfigureAwait(false);
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
                if (GameService.TryAddNewGame(Context.Channel, game))
                {
                    await game.SetupGame().ConfigureAwait(false);
                    await game.StartGame().ConfigureAwait(false);
                }
            }
        }

        [Command("turn"), RequireGameState(GameState.EndOfTurn)]
        [RequireContext(ContextType.Guild | ContextType.Group), RequirePlayerRole(PlayerRole.President)]
        public override Task NextTurnCmd()
            => GameInProgress ? Game.NextTurn() : ReplyAsync("No game in progress.");

        [Command("endearly"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public override Task EndGameCmd()
            => GameInProgress ? Game.EndGame("Game ended early by moderator.") : ReplyAsync("No game in progress to end.");

        [Command("state"), Permission(MinimumPermission.Everyone)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public override Task GameStateCmd()
            => GameInProgress ? ReplyAsync(Game.GetGameState()) : ReplyAsync("No game in progress.");

        [Command("enable"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public async Task EnableHouserule(string rule)
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("There is no game open to set rules for.").ConfigureAwait(false);
            }
            else
            {
                var r = GetRule(rule);
                if ((_currentHouseRules & r) == r)
                {
                    await ReplyAsync("Specified rule already enabled.").ConfigureAwait(false);
                    return;
                }
                var newRules = _currentHouseRules | r;
                if (newRules == _currentHouseRules)
                {
                    await ReplyAsync("Unknown parameter.").ConfigureAwait(false);
                }
                else if (GameService.HouseRulesList.TryUpdate(Context.Channel, newValue: newRules, comparisonValue: _currentHouseRules))
                {
                    await ReplyAsync($"Enabled house rule: {r}.").ConfigureAwait(false);
                }
            }
        }

        [Command("disable"), Permission(MinimumPermission.ModRole)]
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public async Task DisableHouserule(string rule)
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("There is no game open to set rules for.").ConfigureAwait(false);
            }
            else
            {
                var r = GetRule(rule);
                if ((_currentHouseRules & r) == r)
                {
                    await ReplyAsync("Specified rule already disabled.").ConfigureAwait(false);
                    return;
                }
                var newRules = _currentHouseRules ^ r;
                if (newRules == _currentHouseRules)
                {
                    await ReplyAsync("Unknown parameter.").ConfigureAwait(false);
                }
                else if (GameService.HouseRulesList.TryUpdate(Context.Channel, newValue: newRules, comparisonValue: _currentHouseRules))
                {
                    await ReplyAsync($"Disabled house rule: {r}.").ConfigureAwait(false);
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
        [RequireContext(ContextType.Guild | ContextType.Group), RequirePlayerRole(PlayerRole.President)]
        public Task NominatePlayer(IUser user) => Game.NominatedChancellor(user);

        [Command("elect"), RequireGameState(GameState.SpecialElection)]
        [RequireContext(ContextType.Guild | ContextType.Group), RequirePlayerRole(PlayerRole.President)]
        public Task ElectPlayer(IUser user) => Game.SpecialElection(user);

        [Command("investigate"), RequireGameState(GameState.Investigating)]
        [RequireContext(ContextType.Guild | ContextType.Group), RequirePlayerRole(PlayerRole.President)]
        public Task InvestigatePlayer(IUser user) => Game.InvestigatePlayer(user);

        [Command("kill"), RequireGameState(GameState.Kill)]
        [RequireContext(ContextType.Guild | ContextType.Group), RequirePlayerRole(PlayerRole.President)]
        public Task KillPlayer(IUser user) => Game.KillPlayer(user);

        [Command("veto"), RequireGameState(GameState.ChancellorVetod)]
        [RequireContext(ContextType.Guild | ContextType.Group), RequirePlayerRole(PlayerRole.President)]
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
        public Task Veto() => Game.ChancellorVetos((IDMChannel)Context.Channel);
    }
}
