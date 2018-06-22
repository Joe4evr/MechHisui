using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using MechHisui.HisuiBets;

namespace MechHisui.Core
{
    public sealed class BankAccount : IBankAccount
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int Balance { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get => unchecked((ulong)_uid);
            set => _uid = unchecked((long)value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        internal long _uid;
    }
}
