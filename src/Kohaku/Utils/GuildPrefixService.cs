using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;

namespace Kohaku
{
    public class GuildPrefixService
    {
        private readonly Dictionary<ulong, string> _prefixes = new Dictionary<ulong, string>();

        public bool HasGuildSpecificPrefix(IUserMessage message, ref int pos)
        {
            if (message.Channel is IGuildChannel guildChannel && _prefixes.TryGetValue(guildChannel.GuildId, out var prefix))
            {
                return message.HasStringPrefix(prefix, ref pos);
            }
            else
            {
                return false;
            }
        }
    }

    public class ModuleGuildPrefixService
    {
        private readonly CommandService _commands;
        
        //(Guild.Id, ModuleName) => ModulePrefix
        private readonly Dictionary<(ulong, string), string> _prefixes = new Dictionary<(ulong, string), string>();

        public ModuleGuildPrefixService(CommandService commands)
        {
            _commands = commands;
        }

        public SearchResult Search(ICommandContext context, int argPos)
        {
            if (context.Guild != null)
            {
                foreach (var module in _commands.Modules)
                {
                    if (_prefixes.TryGetValue((context.Guild.Id, module.Name), out var prefix))
                    {
                        int prefixAdd = 0;
                        if (context.Message.HasStringPrefix(prefix, ref prefixAdd))
                        {
                            return _commands.Search(context, argPos + prefixAdd);
                        }
                    }
                }
            }

            return _commands.Search(context, argPos);
        }
    }
}
