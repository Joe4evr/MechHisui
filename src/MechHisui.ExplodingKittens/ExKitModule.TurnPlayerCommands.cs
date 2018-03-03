using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.MpGame;

namespace MechHisui.ExplodingKittens
{
    public sealed partial class ExKitModule
    {
        [RequireTurnPlayer]
        public sealed class TurnPlayerCommands : MpGameModuleBase<ExKitService, ExKitGame, ExKitPlayer>
        {
            public TurnPlayerCommands(ExKitService gameService)
                : base(gameService)
            {
            }

            public override Task OpenGameCmd() => throw new NotImplementedException();
            public override Task JoinGameCmd() => throw new NotImplementedException();
            public override Task LeaveGameCmd() => throw new NotImplementedException();
            public override Task CancelGameCmd() => throw new NotImplementedException();
            public override Task StartGameCmd() => throw new NotImplementedException();
            public override Task NextTurnCmd() => throw new NotImplementedException();
            public override Task EndGameCmd() => throw new NotImplementedException();
            public override Task GameStateCmd() => throw new NotImplementedException();

            [Command("defuse"), RequireGameState(GameState.KittenExploding)]
            public Task Defuse()
            {
                var defuse = Player.TakeDefuse();
                return (defuse == null)
                    ? ReplyAsync("You do not have a Defuse card in your hand. Goodbye.")
                    : Game.DefuseExplodingKitten(defuse);
            }

            [Command("insert"), RequireContext(ContextType.DM)]
            [RequireGameState(GameState.KittenDefused)]
            public Task ReinsertExplodingKitten([RequireLessThanCurrentDeckSize] uint location)
            {
                Game.InsertExplodingKitten(location);
                return ReplyAsync("You inserted the Exploding Kitten back into the deck.");
            }
        }
    }
}
