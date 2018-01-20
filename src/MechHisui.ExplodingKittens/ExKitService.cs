using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.WebSocket;

namespace MechHisui.ExplodingKittens
{
    public sealed class ExKitService : MpGameService<ExKitGame, ExKitPlayer>
    {
        public ExKitService(
            DiscordSocketClient socketClient,
            Func<LogMessage, Task> logger = null)
            : base(socketClient, logger)
        {
        }
    }
}
