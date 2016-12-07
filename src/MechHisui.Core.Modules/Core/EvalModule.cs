using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.SimplePermissions;

namespace MechHisui.Modules
{
    /// <summary>
    /// Module for runtime evaluation of code.
    /// Use <see cref="EvalModule.Builder"/> to create an instance of this class.
    /// </summary>
    public class EvalModule : ModuleBase
    {
        private readonly EvalService _service;

        /// <summary>
        /// Creates a new <see cref="EvalModule"/>.
        /// </summary>
        /// <param name="service"></param>
        private EvalModule(EvalService service)
        {
            _service = service;
        }

        [Command("eval"), Permission(MinimumPermission.Everyone)]
        public async Task EvalCmd([Remainder] string code)
        {
            if (code.Contains('^'))
            {
                await ReplyAsync("**Note:** `^` is the Binary XOR operator. Use `Math.Pow(base, exponent)` if you wish to calculate an exponentiation.");
            }

            await ReplyAsync(await _service.Eval(code));
        }
    }
}
