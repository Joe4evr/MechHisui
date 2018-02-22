using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.MpGame;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    public sealed partial class SecretHitlerModule
    {
        [RequireContext(ContextType.DM)]
        public sealed class DMCommands : MpGameModuleBase<SecretHitlerService, SecretHitlerGame, SecretHitlerPlayer>
        {
            public DMCommands(SecretHitlerService gameService)
                : base(gameService)
            {
            }

            #region MUDA
            public override Task OpenGameCmd() => throw new NotImplementedException();
            public override Task JoinGameCmd() => throw new NotImplementedException();
            public override Task LeaveGameCmd() => throw new NotImplementedException();
            public override Task CancelGameCmd() => throw new NotImplementedException();
            public override Task StartGameCmd() => throw new NotImplementedException();
            public override Task NextTurnCmd() => throw new NotImplementedException();
            public override Task EndGameCmd() => throw new NotImplementedException();
            public override Task GameStateCmd() => throw new NotImplementedException();
            #endregion

            [Command("vote"), RequireGameState(GameState.VoteForGovernment)]
            public Task Vote(string vote)
                => Game.ProcessVote((IDMChannel)Context.Channel, Context.User, vote);

            [Command("discard"), RequireGameState(GameState.PresidentPicks)]
            [RequirePlayerRole(PlayerRole.President)]
            public Task PickDiscard([LimitRange(1, 3)] int pick)
                => Game.PresidentDiscards((IDMChannel)Context.Channel, pick);

            [Command("play"), RequireGameState(GameState.ChancellorPicks)]
            [RequirePlayerRole(PlayerRole.Chancellor)]
            public Task PickPlay([LimitRange(1, 2)] int pick)
                => Game.ChancellorPlays((IDMChannel)Context.Channel, pick);

            [Command("veto"), RequireGameState(GameState.ChancellorPicks)]
            [RequirePlayerRole(PlayerRole.Chancellor), RequireVetoUnlocked]
            public Task Veto()
                => Game.ChancellorVetos((IDMChannel)Context.Channel);
        }
    }
}
