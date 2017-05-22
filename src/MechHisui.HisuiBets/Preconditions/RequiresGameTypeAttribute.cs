using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.HisuiBets
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RequiresGameTypeAttribute : PreconditionAttribute
    {
        private readonly GameType _requiredType;

        public RequiresGameTypeAttribute(GameType requiredType)
        {
            _requiredType = requiredType;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var hbservice = services.GetService<HisuiBankService>();
            if (hbservice != null && hbservice.Games.TryGetValue(context.Channel.Id, out var game))
            {
                if (_requiredType == GameType.Any || game.GameType == _requiredType)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("Cannot be used for this game."));
                }
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
                //return Task.FromResult(PreconditionResult.FromError("No game going on."));
            }
        }
    }
}
