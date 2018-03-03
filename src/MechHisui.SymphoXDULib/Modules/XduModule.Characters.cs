using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace MechHisui.SymphoXDULib
{
    public partial class XduModule
    {
        [Group("gear"), Permission(MinimumPermission.Everyone)]
        public class XduCharacters : ModuleBase<SocketCommandContext>
        {
            private readonly XduStatService _stats;

            public XduCharacters(XduStatService stats)
            {
                _stats = stats;
            }

            [Command, Alias("stats", "stat")]
            public Task CharaCmd(int id)
            {
                var profile = _stats.Config.GetGear(id);
                return (profile != null)
                    ? ReplyAsync("", embed: FormatCharacter(profile)) //SendResults(, _stats, Context, listenForSelect: false)
                    : ReplyAsync("Unknown/Not a Gear ID");
            }

            [Command("search")]
            public Task SearchChara(string name)
            {
                var pages = _stats.Config.FindGears(name)
                    .ToEmbedPages()
                    .ToList();
                return SendResults(pages, _stats, Context, listenForSelect: true);
            }

            internal static Embed FormatCharacter(IXduProfile profile)
            {
                return new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder { Name = $"Symphogear #{profile.StartId}..{profile.StartId + 3}: {profile.Rarity}☆ Element: {profile.Element}" },
                    Title = $"{profile.CharacterName}: {profile.Skills[1].SkillName}",
                    Description = "Detailed stats not known yet."
                }
                .AddFieldWhen(profile.HP > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max HP")
                        .WithValue(profile.HP.ToString()))
                .AddFieldWhen(profile.Atk > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max ATK")
                        .WithValue(profile.Atk.ToString()))
                .AddFieldWhen(profile.Def > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max DEF")
                        .WithValue(profile.Def.ToString()))
                .AddField(field => field.WithIsInline(true)
                    .WithName("SPD")
                    .WithValue(profile.Spd.ToString()))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Critical Rate")
                    .WithValue($"{profile.Ctr}%"))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Critical Damage")
                    .WithValue($"{profile.Ctd}%"))
                .AddFieldWhen(!String.IsNullOrWhiteSpace(profile.LeaderSkill),
                    field => field.WithIsInline(false)
                        .WithName("Leader Skill")
                        .WithValue(profile.LeaderSkill))
                .AddFieldWhen(!String.IsNullOrWhiteSpace(profile.PassiveSkill),
                    field => field.WithIsInline(false)
                        .WithName("Passive Skill")
                        .WithValue(profile.PassiveSkill))
                .AddFieldSequence(profile.Skills,
                    (field, skill) => field.WithIsInline(false)
                        .WithName($"{skill.SkillName} ({skill.SkillType})")
                        .WithValue($"{skill.Range} {skill.Effect} CD:{skill.Cooldown}s"))
                .AddImageChecked(profile.Image)
                .Build();
            }
        }
    }
}
