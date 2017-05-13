using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace DivaBot
{
    [Name("Test"), DontAutoLoad]
    public class TestModule : ModuleBase<ICommandContext>
    {
        [Command("test"), Permission(MinimumPermission.BotOwner)]
        public Task TestCmd()
        {
            var emb = new EmbedBuilder()
                .WithDescription("Test: ")
                .AddField(field => field.WithIsInline(false)
                    .WithName("Test")
                    .WithValue("<:pstriangle:269251203324575744> <:pssquare:269251202900951040> <:pscross:269251202481389590> <:pscircle:269251202804482058>"))
                .Build();
            return ReplyAsync("", embed: emb);
        }
    }
}
