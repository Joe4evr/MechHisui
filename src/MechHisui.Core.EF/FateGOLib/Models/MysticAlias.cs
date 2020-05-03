using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class MysticAlias : IMysticAlias
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Alias { get; set; }
        public MysticCode Code { get; set; }

        IMysticCode IMysticAlias.Code => Code;

        public override string ToString() => $"{Alias} ({Code.Code})";
    }
}
