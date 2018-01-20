using System;
using System.Collections.Generic;

namespace MechHisui.SecretHitler
{
    public interface ISecretHitlerConfig
    {
        IEnumerable<string> GetKeys();

        ISecretHitlerTheme GetTheme(string key);
    }
}
