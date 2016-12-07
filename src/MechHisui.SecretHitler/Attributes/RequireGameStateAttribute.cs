using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MechHisui.SecretHitler.Models;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RequireGameStateAttribute : PreconditionAttribute
    {
        private GameState RequiredState { get; }
        public RequireGameStateAttribute(GameState state)
        {
            RequiredState = state;
        }

        public override Task<PreconditionResult> CheckPermissions(CommandContext context, CommandInfo command, IDependencyMap map)
        {
            var service = map.Get<SecretHitlerService>();
            if (service != null)
            {
                var game = service.GameList.Values
                    .Where(g => g.PlayerChannels.Select(c => c.Id).Contains(context.Channel.Id))
                    .FirstOrDefault();
                if (game != null || service.GameList.TryGetValue(context.Channel.Id, out game))
                {
                    return (game.State == RequiredState && !(command.Name == "veto" && !game.VetoUnlocked))
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
                }
                return Task.FromResult(PreconditionResult.FromError("No game."));
            }
            return Task.FromResult(PreconditionResult.FromError("No service."));
        }
    }
}
