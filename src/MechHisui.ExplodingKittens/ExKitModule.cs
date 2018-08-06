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
                => (Game != null)
                    ? ReplyAsync("", embed: Game.GetGameStateEmbed()) //ReplyAsync(Game.GetGameState())
                    : ReplyAsync("No game in progress.");

        [Command("nope"), RequireGameState(GameState.ActionPlayed)]
        public Task Nope()
        {
            var nope = Player.TakeCard<NopeCard>();
            return (nope == null)
                ? ReplyAsync("")
                : Game.ActionNoped(Player, nope);
        }
    }
}
