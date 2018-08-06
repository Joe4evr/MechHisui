using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    public abstract partial class SecretHitlerModule
    {
        [RequireContext(ContextType.DM)]
        public sealed class DMCommands : SecretHitlerModule
        {
            public DMCommands(SecretHitlerService gameService)
                : base(gameService)
            {
            }

            // Precondition attributes guarantee that 'Game'
            // is always non-null in methods below

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
