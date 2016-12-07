using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace MechHisui.HisuiBets
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequiresGameTypeAttribute : PreconditionAttribute
    {
        private readonly GameType _requiredType;

        public RequiresGameTypeAttribute(GameType requiredType)
        {
            _requiredType = requiredType;
        }

        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            BetGame game;
            if (map.Get<HisuiBankService>().Games.TryGetValue(context.Channel.Id, out game))
            {
                if (_requiredType == GameType.Any || game.GameType == _requiredType)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError("Cannot be used for this game."));
            }
            else
                return Task.FromResult(PreconditionResult.FromError("No game going on."));
        }
    }
}
