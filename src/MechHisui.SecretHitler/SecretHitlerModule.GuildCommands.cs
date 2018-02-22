using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.MpGame;
using Discord.Addons.SimplePermissions;
using MechHisui.SecretHitler.Models;
using SharedExtensions;

namespace MechHisui.SecretHitler
{
    [Name("SecretHitler"), Group("sh"), Permission(MinimumPermission.Everyone)]
    public sealed partial class SecretHitlerModule : ModuleBase<SocketCommandContext>
    {
        [RequireContext(ContextType.Guild | ContextType.Group)]
        public sealed class GuildCommands : MpGameModuleBase<SecretHitlerService, SecretHitlerGame, SecretHitlerPlayer>
        {
            private const int _minPlayers = 5;
            private const int _maxPlayers = 10;
            private HouseRules _currentHouseRules;

            public GuildCommands(SecretHitlerService gameService)
                : base(gameService)
            {
            }

            protected override void BeforeExecute(CommandInfo command)
            {
                base.BeforeExecute(command);
                _currentHouseRules = GameService.HouseRulesList.GetValueOrDefault(Context.Channel, defaultValue: HouseRules.None);
            }

            [Command("rules")]
            public async Task RulesCmd()
            {
                var keys = GameService.GetKeys();
                var sb = new StringBuilder("How to play:\n")
                    .AppendLine("There are three roles: Liberal, Fascist, and Hitler.")
                    .AppendLine("Hitler does not know who his fellow Fascists are, but the Fascists know who Hitler is (except in 5 or 6 player games).")
                    .AppendLine("Liberals will always start off not knowing anything.")
                    .AppendLine("If 6 Fascist Policies are enacted, or Hitler is chosen as Chancellor in the late-game, the Fascists win.")
                    .AppendLine("If 5 Liberal Policies are enacted, or Hitler is successfully killed, the Liberals win.")
                    .AppendWhen(() => keys.Any(), b => b.AppendLine($"The following themes are available too: `{String.Join("`, `", keys)}`"))
                    .AppendLine("For more details: http://secrethitler.com/assets/Secret_Hitler_Rules.pdf ")
                    .Append("Good luck, have fun.");

                await ReplyAsync(sb.ToString()).ConfigureAwait(false);
            }

            [Command("open"), Permission(MinimumPermission.ModRole)]
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
                    if (GameService.TryUpdateOpenToJoin(Context.Channel, newValue: true, comparisonValue: false))
                    {
                        GameService.HouseRulesList[Context.Channel] = HouseRules.None;
                        await ReplyAsync("Opening for a game.").ConfigureAwait(false);
                    }
                }
            }

            [Command("join")]
            public override async Task JoinGameCmd()
            {
                if (GameInProgress != CurrentlyPlaying.None)
                {
                    await ReplyAsync("Cannot join a game already in progress.").ConfigureAwait(false);
                }
                else if (!OpenToJoin)
                {
                    await ReplyAsync("No game open to join.").ConfigureAwait(false);
                }
                else if (JoinedUsers.Count == _maxPlayers)
                {
                    await ReplyAsync("Maximum number of players already joined.").ConfigureAwait(false);
                }
                else
                {
                    if (await GameService.AddUser(Context.Channel, Context.User).ConfigureAwait(false))
                    {
                        await ReplyAsync($"**{Context.User.Username}** has joined.").ConfigureAwait(false);
                    }
                }
            }

            [Command("leave")]
            public override async Task LeaveGameCmd()
            {
                if (GameInProgress != CurrentlyPlaying.None)
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
            public override async Task CancelGameCmd()
            {
                if (GameInProgress != CurrentlyPlaying.None)
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
                        await ReplyAsync("Game was cancelled.").ConfigureAwait(false);
                    }
                }
            }

            [Command("start", RunMode = RunMode.Async)]
            [Permission(MinimumPermission.ModRole)] //:thinking:
            public override Task StartGameCmd()
                => StartInternal(DefaultSecretHitlerTheme.Instance);

            [Command("start", RunMode = RunMode.Async)]
            [Permission(MinimumPermission.ModRole)] //:thinking:
            public Task StartGameCmd(string themeName)
            {
                var theme = GameService.GetTheme(themeName);
                return theme != null
                    ? StartInternal(theme)
                    : ReplyAsync("Could not find that theme.");
            }

            private async Task StartInternal(ISecretHitlerTheme theme)
            {
                if (GameInProgress != CurrentlyPlaying.None)
                {
                    await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
                }
                else if (!OpenToJoin)
                {
                    await ReplyAsync("No game has been opened at this time.").ConfigureAwait(false);
                }
                else if (JoinedUsers.Count < _minPlayers)
                {
                    await ReplyAsync("Not enough players have joined.").ConfigureAwait(false);
                }
                else
                {
                    int ReqFas(int p)
                    {
                        switch (p)
                        {
                            case 5:
                            case 6:
                                return 2;
                            case 7:
                            case 8:
                                return 3;
                            case 9:
                            case 10:
                                return 4;
                            default:
                                throw new InvalidOperationException("Player count should be a minimum of 5 and a maximum of 10.");
                        }
                    }
                    int fascists = ReqFas(JoinedUsers.Count);

                    var players = JoinedUsers.Select((u, i) =>
                    {
                        if (i == 0)
                        {
                            return new SecretHitlerPlayer(u, Context.Channel, theme.FascistParty, theme.Hitler);
                        }
                        else if (i < fascists)
                        {
                            return new SecretHitlerPlayer(u, Context.Channel, theme.FascistParty, theme.Fascist);
                        }
                        else
                        {
                            return new SecretHitlerPlayer(u, Context.Channel, theme.LiberalParty, theme.Liberal);
                        }
                    }).Shuffle(32);

                    var game = new SecretHitlerGame(Context.Channel, players, theme, _currentHouseRules);
                    if (GameService.TryAddNewGame(Context.Channel, game))
                    {
                        await game.SetupGame().ConfigureAwait(false);
                        await game.StartGame().ConfigureAwait(false);
                    }
                }
            }

            [Command("turn"), RequireGameState(GameState.EndOfTurn)]
            [RequirePlayerRole(PlayerRole.President)]
            public override Task NextTurnCmd()
                => GameInProgress == CurrentlyPlaying.ThisGame ? Game.NextTurn() : ReplyAsync("No game in progress.");

            [Command("endearly"), Permission(MinimumPermission.ModRole)]
            public override Task EndGameCmd()
                => GameInProgress == CurrentlyPlaying.ThisGame ? Game.EndGame("Game ended early by moderator.") : ReplyAsync("No game in progress to end.");

            [Command("state")]
            public override Task GameStateCmd()
                => GameInProgress == CurrentlyPlaying.ThisGame ? ReplyAsync(Game.GetGameState()) : ReplyAsync("No game in progress.");

            [Command("enable"), Permission(MinimumPermission.ModRole)]
            public async Task EnableHouserule(string rule)
            {
                if (GameInProgress != CurrentlyPlaying.None)
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
            public async Task DisableHouserule(string rule)
            {
                if (GameInProgress != CurrentlyPlaying.None)
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
                    if ((_currentHouseRules | r) != _currentHouseRules)
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

            private static HouseRules GetRule(string rule)
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

            // Game-specific commands

            [Command("nominate"), RequireGameState(GameState.StartOfTurn)]
            [RequirePlayerRole(PlayerRole.President)]
            public Task NominatePlayer(SecretHitlerPlayer player)
                => Game.NominatedChancellor(player);

            [Command("elect"), RequireGameState(GameState.SpecialElection)]
            [RequirePlayerRole(PlayerRole.President)]
            public Task ElectPlayer(SecretHitlerPlayer player)
                => Game.SpecialElection(player);

            [Command("investigate"), RequireGameState(GameState.Investigating)]
            [RequirePlayerRole(PlayerRole.President)]
            public Task InvestigatePlayer(SecretHitlerPlayer player)
                => Game.InvestigatePlayer(player);

            [Command("kill"), RequireGameState(GameState.Kill)]
            [RequirePlayerRole(PlayerRole.President)]
            public Task KillPlayer(SecretHitlerPlayer player)
                => Game.KillPlayer(player);

            [Command("veto"), RequireGameState(GameState.ChancellorVetod)]
            [RequirePlayerRole(PlayerRole.President)]
            public Task Veto([LimitToStrings(StringComparison.OrdinalIgnoreCase, "approved", "denied")] string consent)
                => Game.PresidentConsentsVeto(consent.ToLowerInvariant());
        }
    }
}
