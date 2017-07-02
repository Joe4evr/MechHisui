using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace DivaBot
{
    [Name("PjDFT")]
    public class FTHelperModule : ModuleBase<ICommandContext>
    {
        private readonly PjdService _service;

        public FTHelperModule(PjdService service)
        {
            _service = service;
        }

        public Task Strat()
        {
            return Task.CompletedTask;
        }
    }
}
