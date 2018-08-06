using System;
using MechHisui.HisuiBets;

namespace MechHisui.Core
{
    public sealed partial class HisuiUser : IBankAccount
    {
        public int Balance { get; set; }
    }

    //public sealed class BankAccount : IBankAccount
    //{
    //    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    //    public int Id { get; set; }

    //    public int Balance { get; set; }

    //    [NotMapped]
    //    public ulong UserId
    //    {
    //        get => unchecked((ulong)_uid);
    //        set => _uid = unchecked((long)value);
    //    }

    //    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    //    [EditorBrowsable(EditorBrowsableState.Never)]
    //    [Browsable(false)]
    //    internal long _uid;
    //}
}
