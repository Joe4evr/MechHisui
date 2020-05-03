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
        internal IExKitConfig ExKitConfig { get; }

        public ExKitService(
            BaseSocketClient client,
            IExKitConfig exKitConfig,
            IMpGameServiceConfig? mpconfig = null,
            Func<LogMessage, Task>? logger = null)
            : base(client, mpconfig, logger)
        {
            ExKitConfig = exKitConfig ?? throw new ArgumentNullException(nameof(exKitConfig));
        }
    }
}
