using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace MechHisui.SymphoXDULib
{
    public class Profile
    {
        public int Rarity { get; set; }
        public Dictionary<string, CharacterVariation> Variations { get; set; }
        //public string Element { get; set; }
        public string CharacterName { get; set; }
        public string Image { get; set; }
        public ICollection<string> Aliases { get; set; }

        public List<Embed> ToEmbedPages()
        {
            return Variations.DictionarySelect((element, variation) => new EmbedBuilder()
                .WithAuthor(auth => auth.WithName($"Symphogear #{variation.Id}: {Rarity}☆ Element: {element}"))
                .WithTitle($"{CharacterName}: {variation.Skills[1].SkillName}")
                .WithDescription("Detailed stats not known yet.")
                .AddFieldWhen(variation.HP > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max HP")
                        .WithValue(variation.HP.ToString()))
                .AddFieldWhen(variation.Atk > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max ATK")
                        .WithValue(variation.Atk.ToString()))
                .AddFieldWhen(variation.Def > 0,
                    field => field.WithIsInline(true)
                        .WithName("Max DEF")
                        .WithValue(variation.Def.ToString()))
                .AddField(field => field.WithIsInline(true)
                    .WithName("SPD")
                    .WithValue(variation.Spd.ToString()))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Critical Rate")
                    .WithValue($"{variation.Ctr}%"))
                .AddField(field => field.WithIsInline(true)
                    .WithName("Critical Damage")
                    .WithValue($"{variation.Ctd}%"))
                .AddFieldWhen(!String.IsNullOrWhiteSpace(variation.LeaderSkill),
                    field => field.WithIsInline(false)
                        .WithName("Leader Skill")
                        .WithValue(variation.LeaderSkill))
                .AddFieldWhen(!String.IsNullOrWhiteSpace(variation.PassiveSkill),
                    field => field.WithIsInline(false)
                        .WithName("Passive Skill")
                        .WithValue(variation.PassiveSkill))
                .AddFieldSequence(variation.Skills,
                    (field, skill) => field.WithIsInline(false)
                        .WithName($"{skill.SkillName} ({skill.SkillType})")
                        .WithValue($"{skill.Range} {skill.Effect} CD:{skill.Cooldown}s"))
                .WithImageUrl(new Uri(Image))
                .Build()).ToList();
        }
    }
}