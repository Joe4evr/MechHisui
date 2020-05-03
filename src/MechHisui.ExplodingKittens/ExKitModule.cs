using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.MpGame;
using MechHisui.ExplodingKittens.Cards;

namespace MechHisui.ExplodingKittens
{
    [Name("Exploding Kittens"), Group("exk")]
    public partial class ExKitModule : MpGameModuleBase<ExKitService, ExKitGame, ExKitPlayer>
    {
        private const int _min = 2;

        public ExKitModule(ExKitService gameService)
            : base(gameService)
        {
        }

        public override Task OpenGameCmd()   => Task.CompletedTask;
        public override Task JoinGameCmd()   => Task.CompletedTask;
        public override Task LeaveGameCmd()  => Task.CompletedTask;
        public override Task CancelGameCmd() => Task.CompletedTask;
        public override Task StartGameCmd()  => Task.CompletedTask;
        public override Task NextTurnCmd()   => Task.CompletedTask;
        public override Task EndGameCmd()    => Task.CompletedTask;

        [Command("state")]
        public override Task GameStateCmd()
                => (Game is null)
                    ? ReplyAsync("No game in progress.")
                    : ReplyAsync("", embed: Game.GetGameStateEmbed());

        [Command("nope"), RequireGameState(GameState.ActionPlayed)]
        [RequireTurnPlayer(false)]
        public Task Nope()
        {
            var nope = Player!.TakeCard<NopeCard>();
            return (nope is null)
                ? ReplyAsync("You do not have a Nope card in your hand.")
                : Game!.ActionNoped(Player, nope);
        }
    }
}
