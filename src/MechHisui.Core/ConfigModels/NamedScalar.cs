using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MechHisui.Core
{
    public sealed class NamedScalar
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Key { get; set; }

        public string StringValue { get; set; }

        public int IntValue { get; set; }
    }
}
