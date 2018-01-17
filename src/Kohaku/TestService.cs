using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Addons.SimplePermissions;

namespace Kohaku
{
    public sealed class TestService
    {
        private readonly IConfigStore<KohakuConfig> _store;

        public TestService(IConfigStore<KohakuConfig> store)
        {
            _store = store;
        }

        internal void SetFgoCode(IUser user, string code)
        {
            using (var config = _store.Load())
            {
                var cUser = config.Users.SingleOrDefault(u => u.UserId == user.Id);
                if (cUser != null)
                {
                    cUser.FgoFriendCode = code;
                    config.Save();
                }
            }
        }
    }
}
