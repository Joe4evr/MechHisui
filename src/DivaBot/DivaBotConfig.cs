using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
//#if !ARM
//using Discord.Addons.SimpleAudio;
//#endif
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace DivaBot
{
    //    internal class DivaBotConfig : JsonConfigBase
    //    {
    //        public string LoginToken { get; set; }

    //#if !ARM
    //        public AudioConfig AudioConfig { get; set; }
    //#endif

    //        public Dictionary<string, string[]> AutoResponses { get; set; }

    //        public Dictionary<string, string> TagResponses { get; set; }

    //        public Dictionary<ulong, ScoreAttackChallenge> CurrentChallenges { get; set; }

    //        public List<string> Additional8BallOptions { get; set; }
    //    }

    public sealed class DivaBotConfig : EFBaseConfigContext<DivaGuild, DivaChannel, DivaUser>
        //: DbContext
    {
        public DbSet<StringKeyValuePair> Strings { get; set; }

        public DivaBotConfig(DbContextOptions options, CommandService commandService)
            : base(options, commandService)
            //: base(options)
        {
        }
    }

    public class StringKeyValuePair
    {
        [Key]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }
    }
}