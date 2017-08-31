//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Discord.Commands;
//using Discord.Addons.SimplePermissions;

//namespace Kohaku
//{
//    /// <summary> Module for runtime evaluation of code. </summary>
//    public sealed class EvalModule : ModuleBase<ICommandContext>
//    {
//        private readonly EvalService _eval;
//        private readonly IServiceProvider _services;

//        /// <summary> Creates a new <see cref="EvalModule"/>. </summary>
//        /// <param name="eval"></param>
//        private EvalModule(EvalService eval, IServiceProvider services)
//        {
//            _eval = eval;
//            _services = services;
//        }

//        [Command("eval", RunMode = RunMode.Async), Permission(MinimumPermission.BotOwner)]
//        public async Task EvalCmd([Remainder] string code)
//        {
//            if (code.Contains('^'))
//            {
//                await ReplyAsync("**Note:** `^` is the Binary XOR operator. Use `Math.Pow(base, exponent)` if you wish to calculate an exponentiation.").ConfigureAwait(false);
//            }
//            using (Context.Channel.EnterTypingState())
//            {
//                await ReplyAsync(await _eval.Eval(code, Context, _services).ConfigureAwait(false)).ConfigureAwait(false);
//            }
//        }
//    }
//}
