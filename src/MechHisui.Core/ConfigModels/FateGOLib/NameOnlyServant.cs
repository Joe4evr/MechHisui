using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MechHisui.FateGOLib;

namespace MechHisui.Core
{
    public sealed class NameOnlyServant : INameOnlyServant
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Class { get; set; }
        public string Name { get; set; }
    }
}
