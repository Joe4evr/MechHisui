//using System;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;

//namespace MechHisui.Superfight.Preconditions
//{
//    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
//    internal sealed class RequireNoGameAttribute : PreconditionAttribute
//    {
//        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
//        {
//            var sfservice = services.GetService<SuperfightService>();
//            if (sfservice != null)
//            {
//                var game = sfservice.GetGameFromChannel(context.Channel);
//                if (game == null)
//                {
//                    return Task.FromResult(PreconditionResult.FromSuccess());
//                }
//                return Task.FromResult(PreconditionResult.FromError("Command cannot be used during game."));
//            }
//            return Task.FromResult(PreconditionResult.FromError("No service."));
//        }
//    }
//}
//}
