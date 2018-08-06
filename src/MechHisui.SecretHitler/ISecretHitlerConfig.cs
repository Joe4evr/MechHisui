using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MechHisui.SecretHitler
{
    public interface ISecretHitlerConfig
    {
        Task<IEnumerable<string>> GetThemeKeysAsync();

        Task<ISecretHitlerTheme> GetThemeAsync(string key);
    }
}
