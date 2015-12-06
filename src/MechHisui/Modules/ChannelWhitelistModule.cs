using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Modules;

namespace MechHisui.Modules
{
    public class ChannelWhitelistModule : IModule
    {
        private ModuleManager _manager;

        private readonly long[] _whitelistedChannels;

        public void Install(ModuleManager manager)
        {
            foreach (var c in _whitelistedChannels)
            {
                var channel = manager.Client.GetChannel(c);
                if (channel != null)
                {
                    manager.EnableChannel(channel);
                }
            }
            
            _manager = manager;
        }

        public ChannelWhitelistModule(params long[] channels)
        {
            _whitelistedChannels = channels;
        }
    }
}
