using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace MechHisui.SymphoXDULib
{
    public partial class XduModule
    {
        [Group("memoria"), Alias("memo"), Permission(MinimumPermission.Everyone)]
        public class XduMemoria : ModuleBase<SocketCommandContext>
        {
            private readonly XduStatService _stats;

            public XduMemoria(XduStatService stats)
            {
                _stats = stats;
            }

            [Command, Alias("stats", "stat")]
            public Task CharaCmd(int id)
            {
                var memoria = _stats.Config.GetMemorias().SingleOrDefault(p => p.Id == id);
                return (memoria != null)
                    ? ReplyAsync("", embed: FormatMemoria(memoria))
                    : ReplyAsync("Unknown/Not a Memoria ID");
            }

            internal static Embed FormatMemoria(Memoria memoria)
            {
                return new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder { Name = $"Memoria Card #{memoria.Id}: {memoria.Rarity}☆" },
                    Title = memoria.Name,
                    Description = memoria.Effect,

                }.AddFieldWhen(memoria.HP > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max HP")
                        .WithValue(memoria.HP.ToString()))
                .AddFieldWhen(memoria.Atk > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max ATK")
                        .WithValue(memoria.Atk.ToString()))
                .AddFieldWhen(memoria.Def > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max DEF")
                        .WithValue(memoria.Def.ToString()))
                .Build();
            }
        }
    }
}
