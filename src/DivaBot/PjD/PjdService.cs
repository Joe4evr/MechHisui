using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace DivaBot
{
    public class PjdService
    {
        internal PjdService()
        {
        }

        internal string GetStrat()
        {
            return "";
        }
    }

    public static class PjdExtensions
    {
        public static Task AddPjdHelpers(this CommandService commands, IServiceCollection map)
        {
            map.AddSingleton(new PjdService());
            return commands.AddModuleAsync<FTHelperModule>();
        }

        internal const string Triangle = "<:pstriangle:269251203324575744>";
        internal const string Square   =   "<:pssquare:269251202900951040>";
        internal const string Cross    =    "<:pscross:269251202481389590>";
        internal const string Circle   =   "<:pscircle:269251202804482058>";

        internal static string FormatWithEmoji(this string input)
        {
            return input.Replace("🔺", Triangle)
                .Replace("⬜", Square)
                .Replace("❌", Cross)
                .Replace("⭕", Circle);
        }
    }
}
