using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Net;

namespace Discord
{
    public static class DiscordExt
    {
        public static async Task<Message> SendWithRetry(this Channel channel, string text)
        {
            Message result = null;
            while (true)
            {
                try
                {
                    result = await channel.SendMessage(text.Length >= 2000 ? new String(text.Take(1950).ToArray()) + "..." : text);
                    break;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("502")) continue;
                    throw;
                }
            }
            return result;
        }
    }
}
