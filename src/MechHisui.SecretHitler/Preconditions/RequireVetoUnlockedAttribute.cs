using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace MechHisui.SecretHitler
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal class RequireVetoUnlockedAttribute : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var shservice = services.GetService<SecretHitlerService>();
            if (shservice != null)
            {
                var game = await shservice.GetGameFromChannelAsync(context.Channel).ConfigureAwait(false);

                if (game != null)
                {
                    return game.VetoUnlocked
                        ? PreconditionResult.FromSuccess()
                        : PreconditionResult.FromError("Cannot use command at this time.");
                }
                return PreconditionResult.FromError("No game.");
            }
            return PreconditionResult.FromError("No service.");
        }
    }
}
