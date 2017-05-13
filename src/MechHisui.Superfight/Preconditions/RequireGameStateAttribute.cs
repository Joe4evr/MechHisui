using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MechHisui.Superfight.Models;

namespace MechHisui.Superfight.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class RequireGameStateAttribute : PreconditionAttribute
    {
        private GameState RequiredState { get; }
        internal RequireGameStateAttribute(GameState state)
        {
            RequiredState = state;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var sfservice = services.GetService<SuperfightService>();
            if (sfservice != null)
            {
                var game = sfservice.GameList.Values
                    .FirstOrDefault(g => g.PlayerChannels.Select(c => c.Id).Contains(context.Channel.Id))
;
                if (game != null || sfservice.GameList.TryGetValue(context.Channel, out game))
                {
                    return (game.State == RequiredState /*&& !(executingCommand.Name == "veto" && !game.VetoUnlocked)*/)
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                }
                return Task.FromResult(PreconditionResult.FromError("No game."));
            }
            return Task.FromResult(PreconditionResult.FromError("No service."));
        }
    }
}
