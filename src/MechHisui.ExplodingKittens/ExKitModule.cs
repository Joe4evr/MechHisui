using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.MpGame;

namespace MechHisui.ExplodingKittens
{
    [Name("Exploding Kittens"), Group("exk")]
    public sealed partial class ExKitModule : MpGameModuleBase<ExKitService, ExKitGame, ExKitPlayer>
    {
        private const int _min = 2;

        public ExKitModule(ExKitService gameService)
            : base(gameService)
        {
        }

        public override Task OpenGameCmd()
        {
            throw new NotImplementedException();
        }

        public override Task JoinGameCmd()
        {
            throw new NotImplementedException();
        }

        public override Task LeaveGameCmd()
        {
            throw new NotImplementedException();
        }

        public override Task CancelGameCmd()
        {
            throw new NotImplementedException();
        }

        public override Task StartGameCmd()
        {
            throw new NotImplementedException();
        }

        public override Task NextTurnCmd()
        {
            throw new NotImplementedException();
        }

        public override Task EndGameCmd()
        {
            throw new NotImplementedException();
        }

        public override Task GameStateCmd()
        {
            throw new NotImplementedException();
        }
    }
}
