//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Discord.Commands;
//using Discord.Addons.SimplePermissions;

//namespace MechHisui
//{
//    /// <summary> Module for runtime evaluation of code. </summary>
//    public class EvalModule : ModuleBase<ICommandContext>
//    {
//        private readonly EvalService _service;
//        private readonly IServiceProvider _services;

//        /// <summary> Creates a new <see cref="EvalModule"/>. </summary>
//        /// <param name="service"></param>
//        /// <param name="services"></param>
//        private EvalModule(EvalService service, IServiceProvider services)
//        {
//            _service = service;
//            _services = services;
//        }

//        [Command("eval"), Permission(MinimumPermission.Everyone)]
//        public async Task EvalCmd([Remainder] string code)
//        {
//            if (code.Contains('^'))
//            {
//                await ReplyAsync("**Note:** `^` is the Binary XOR operator. Use `Math.Pow(base, exponent)` if you wish to calculate an exponentiation.").ConfigureAwait(false);
//            }

//            await ReplyAsync(await _service.Eval(code, _services).ConfigureAwait(false)).ConfigureAwait(false);
//        }
//    }
//}
