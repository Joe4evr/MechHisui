using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class ServantAlias : IServantAlias
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Alias { get; set; }
        public ServantProfile Servant { get; set; }

        IServantProfile IServantAlias.Servant => Servant;

        public override string ToString() => $"{Alias} ({Servant.Name})";
    }
}
