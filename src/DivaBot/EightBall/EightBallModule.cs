using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace DivaBot
{
    [Name("Magic8Ball")]
    public class EightBallModule : ModuleBase<ICommandContext>
    {
        private readonly EightBallService _service;

        public EightBallModule(EightBallService service)
        {
            _service = service;
        }

        [Command("8ball"), Permission(MinimumPermission.Everyone)]
        [Summary("Ask the Magic 8-Ball.")]
        public async Task EightBallCmd([Remainder] string question)
        {
            var msg = await ReplyAsync(".").ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
            await msg.ModifyAsync(m => m.Content = "..").ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
            await msg.ModifyAsync(m => m.Content = "...").ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
            await msg.ModifyAsync(m => m.Content = _service.Options.Shuffle().ElementAt(_service.Rng.Next(maxValue: _service.Options.Count))).ConfigureAwait(false);
        }
    }
}
