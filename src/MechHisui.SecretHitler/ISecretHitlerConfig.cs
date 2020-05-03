using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Addons.MpGame;

namespace MechHisui.SecretHitler
{
    public interface ISecretHitlerConfig : IMpGameServiceConfig
    {
        Task<IEnumerable<string>> GetThemeKeysAsync();

        Task<ISecretHitlerTheme?> GetThemeAsync(string key);
    }
}
