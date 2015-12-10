using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        internal static long[] ConvertStringArrayToLongArray(params string[] strings)
        {
            var longs = new List<long>();
            foreach (var s in strings)
            {
                long temp;
                if (Int64.TryParse(s, out temp))
                {
                    longs.Add(temp);
                }
            }

            return longs.ToArray();
        }
        
        internal static IEnumerable<Channel> IterateChannels(IEnumerable<Server> servers, bool printServerNames = false, bool printChannelNames = false)
        {
            foreach (var server in servers)
            {
                if (printServerNames)
                {
                    Console.WriteLine(server.Name + "\n");
                }
                foreach (var channel in server.Channels)
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
