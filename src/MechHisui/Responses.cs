using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MechHisui
{
    public partial class Program
    {
        static ConcurrentDictionary<string[], DateTime> lastResponses = new ConcurrentDictionary<string[], DateTime>();
        static IDictionary<string[], string> responseDict = new Dictionary<string[], string>()
        {
            { new[] { "osu", "Osu" }, "Greetings" },
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
        };
		
		static IDictionary<string[], string> spammableResponses = new Dictionary<string[], string>()
		{
			{ new[] { "(╯°□°）╯︵ ┻━┻", "(ノಠ益ಠ)ノ彡┻━┻", "┻━┻ ︵ヽ(`Д´)ﾉ︵﻿ ┻━┻" }, "┬─┬ノ( º _ ºノ)" }
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
            //{ new[] { "", "" }, "" },
		};
    }
}
