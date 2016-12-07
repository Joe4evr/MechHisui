using System.Collections.Generic;
using Discord.Addons.SimplePermissions;
using MechHisui.FateGOLib;
using MechHisui.SecretHitler;

namespace MechHisui.Core
{
    public class MechHisuiConfig : IPermissionConfig
    {
        public ulong OwnerId { get; set; }

        public string LoginToken { get; set; }

        public string BankPath { get; set; }

        public string SuperfightBasePath { get; set; }

        public FgoConfig FgoConfig { get; set; }

        public Dictionary<ulong, ulong> GuildModRole { get; set; }

        public Dictionary<ulong, ulong> GuildAdminRole { get; set; }

        public Dictionary<ulong, HashSet<string>> ChannelModuleWhitelist { get; set; }

        public Dictionary<ulong, HashSet<ulong>> SpecialPermissionUsersList { get; set; }

        public Dictionary<string, SecretHitlerConfig> SHConfigs { get; set; }
    }
}
