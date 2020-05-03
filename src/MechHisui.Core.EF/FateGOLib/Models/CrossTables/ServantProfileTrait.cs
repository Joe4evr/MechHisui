﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class ServantProfileTrait
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public ServantProfile Servant { get; set; }
        public ServantTrait Trait { get; set; }

        public override string ToString() => $"{Servant}: {Trait}";
    }
}
