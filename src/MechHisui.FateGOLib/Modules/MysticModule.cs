//using System;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Addons.SimplePermissions;
//using Discord.Commands;
//using SharedExtensions;

//namespace MechHisui.FateGOLib
//{
//    public partial class FgoModule
//    {
//        [Name("MysticCodes"), Group("mystic")]
//        public sealed class MysticModule : FgoModule
//        {

//            public MysticModule(FgoStatService service)
//            {
//                _service = service;
//            }

//            [Command]
//            public async Task MysticCmd(string name)
//            {
//                var codes = await _service.Config.FindMysticsAsync(name).ConfigureAwait(false);

//                if (codes.Count() == 1)
//                {
//                    await ReplyAsync("", embed: FormatMysticCodeProfile(codes.Single())).ConfigureAwait(false);
//                }
//                else if (codes.Count() > 1)
//                {
//                    var sb = new StringBuilder("Entry ambiguous. Did you mean one of the following?\n")
//                        .AppendSequence(codes, (s, m) => s.AppendLine($"**{m.Code}** *({String.Join(", ", m.Aliases)})*"));

//                    await ReplyAsync(sb.ToString()).ConfigureAwait(false);
//                }
//                else
//                {
//                    await ReplyAsync("Specified Mystic Code not found. Please use `.listmystic` for the list of available Mystic Codes.").ConfigureAwait(false);
//                }
//            }

//            [Command("list")]
//            public async Task ListMysticsCmd()
//            {
//                var ms = await _service.Config.GetAllMysticsAsync().ConfigureAwait(false);
//                await ReplyAsync(String.Join("\n", ms.Select(m => $"**{m.Code}** *({String.Join(", ", m.Aliases)})*"))).ConfigureAwait(false);
//            }

//            //[Command("alias"), Permission(MinimumPermission.ModRole)]
//            //public Task MysticAliasCmd(string code, string alias)
//            //{
//            //    if (!_service.Config.FindMystics(code).Select(c => c.Code).Contains(code))
//            //    {
//            //        return ReplyAsync("Could not find name to add alias for.");
//            //    }

//            //    if (_service.Config.AddMysticAlias(code, alias.ToLowerInvariant()))
//            //    {
//            //        return ReplyAsync($"Added alias `{alias}` for `{code}`.");
//            //    }
//            //    else
//            //    {
//            //        return ReplyAsync($"Alias `{alias}` already exists for CE `{_service.Config.AllMystics().Single(c => c.Aliases.Any(a => a.Alias == alias)).Code}`.");
//            //    }
//            //}

//            private static Embed FormatMysticCodeProfile(IMysticCode code)
//            {
//                var embed = new EmbedBuilder
//                {
//                    Title = code.Code,
//                    //Description = code.
//                    Fields =
//                    {
//                        new EmbedFieldBuilder
//                        {
//                            IsInline = false,
//                            Name = $"Skill 1: **{code.Skill1}**",
//                            Value = code.Skill1Effect
//                        },
//                        new EmbedFieldBuilder
//                        {
//                            IsInline = false,
//                            Name = $"Skill 2: **{code.Skill2}**",
//                            Value = code.Skill2Effect
//                        },
//                        new EmbedFieldBuilder
//                        {
//                            IsInline = false,
//                            Name = $"Skill 3: **{code.Skill3}**",
//                            Value = code.Skill3Effect
//                        }
//                    },
//                    ImageUrl = code.Image
//                };

//                return embed.Build();
//            }
//        }
//    }
//}
