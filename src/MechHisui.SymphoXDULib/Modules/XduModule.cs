using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace MechHisui.SymphoXDULib
{
    [Group("xdu")]
    [Name("SymphogearXDU"), Permission(MinimumPermission.Everyone)]
    public partial class XduModule : ModuleBase<ICommandContext>
    {
        [Command("elements"), Alias("classes")]
        public Task ClassesCmd()
        {
            return ReplyAsync(@"In short: ```
(力) STR (Red)     > DEX
(知) INT (Blue)    > STR
(体) PHY (Pink)    > INT
(技) TEQ (Yellow)  > PHY
(心) SPR (Orange)  > TEQ
(巧) DEX (Green)   > SPR
(怒) EXT (Silver) <> any (Berserker class)
(全) Omni (only on Music Sheets (EXP))
```");
        }

        private static readonly AppearanceOptions _options = new AppearanceOptions
        {
            EmoteBack = new Emoji(XduStatService.EmoteBack),
            EmoteNext = new Emoji(XduStatService.EmoteNext),
            EmoteStop = new Emoji(XduStatService.EmoteStop),
            EmoteFirst = null,
            EmoteLast = null
        };

        private static async Task SendResults(List<Embed> pages, XduStatService stats, ICommandContext context, bool listenForSelect)
        {
            if (pages.Count == 0)
            {
                await context.Channel.SendMessageAsync("No results found").ConfigureAwait(false);
                return;
            }

            var pmsg = new PaginatedMessage(pages,
                //embedColor: new Color(),
                //title: $"Matches for '{String.Join(", ", terms)}'",
                user: context.User,
                options: _options,
                listenForSelect: listenForSelect);

            await stats.AddNewPagedAsync(await pmsg.SendMessage(context.Channel).ConfigureAwait(false)).ConfigureAwait(false);
            if (listenForSelect)
                await pmsg.Msg!.AddReactionAsync(new Emoji(XduStatService.EmoteSelect)).ConfigureAwait(false);
        }

        private static bool RegexMatchOneWord(string hay, string needle)
                => Regex.Match(hay, String.Concat(_b, needle, _b), RegexOptions.IgnoreCase).Success;

        private const string _b = @"\b";
    }
}
