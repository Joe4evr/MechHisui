using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.MpGame;
using Discord.Addons.SimplePermissions;
using MechHisui.SecretHitler.Models;
using SharedExtensions;

namespace MechHisui.SecretHitler
{
    [Name("SecretHitler"), Group("sh"), Permission(MinimumPermission.Everyone)]
    public abstract partial class SecretHitlerModule : MpGameModuleBase<SecretHitlerService, SecretHitlerGame, SecretHitlerPlayer>
    {
        private const int _minPlayers = 5;
        private const int _maxPlayers = 10;

        private protected HouseRules CurrentHouseRules { get; private set; }

        private SecretHitlerModule(SecretHitlerService gameService)
            : base(gameService)
        {
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            CurrentHouseRules = GameService.HouseRulesList.GetValueOrDefault(Context.Channel, defaultValue: HouseRules.None);
        }
        
        public override Task OpenGameCmd() => Task.CompletedTask;
        public override Task JoinGameCmd() => Task.CompletedTask;
        public override Task LeaveGameCmd() => Task.CompletedTask;
        public override Task CancelGameCmd() => Task.CompletedTask;
        public override Task StartGameCmd() => Task.CompletedTask;
        public override Task NextTurnCmd() => Task.CompletedTask;
        public override Task EndGameCmd() => Task.CompletedTask;
        public override Task GameStateCmd() => Task.CompletedTask;
    }
}
