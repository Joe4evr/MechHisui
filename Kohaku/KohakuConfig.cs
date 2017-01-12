using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.SimplePermissions;

namespace Kohaku
{
    class KohakuConfig : JsonConfigBase
    {
        public string LoginToken { get; set; }
    }
}
