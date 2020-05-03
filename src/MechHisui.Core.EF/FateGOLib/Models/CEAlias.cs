using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class CEAlias : ICEAlias
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Alias { get; set; }
        public CEProfile CE { get; set; }

        ICEProfile ICEAlias.CE => CE;
    }
}
