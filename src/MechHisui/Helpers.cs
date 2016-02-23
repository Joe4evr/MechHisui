using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Modules;
using MechHisui.Modules;

namespace MechHisui
{
    internal static class Helpers
    {
        internal static bool IsWhilested(Channel channel, DiscordClient client) => client.Modules().Modules
            .SingleOrDefault(m => m.Id == nameof(ChannelWhitelistModule).ToLowerInvariant())?
            .EnabledChannels
            .Contains(channel) ?? false;

        internal static ulong[] ConvertStringArrayToULongArray(params string[] strings)
        {
            var ulongs = new List<ulong>();
            foreach (var s in strings)
            {
                ulong temp;
                if (UInt64.TryParse(s, out temp))
                {
                    ulongs.Add(temp);
                }
            }

            return ulongs.ToArray();
        }
        
        internal static IEnumerable<Channel> IterateChannels(IEnumerable<Server> servers, bool printServerNames = false, bool printChannelNames = false)
        {
            foreach (var server in servers)
            {
                if (printServerNames)
                {
                    Console.WriteLine("\n" + server?.Name);
                }
                foreach (var channel in server.AllChannels)
                {
                    if (printChannelNames)
                    {
                        Console.WriteLine($"{channel.Name}:  {channel.Id}");
                    }
                    yield return channel;
                }
            }
        }

    }
}
