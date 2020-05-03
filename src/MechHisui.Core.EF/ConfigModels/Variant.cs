using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings
namespace MechHisui.Core
{
    public sealed class Variant
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Key { get; set; }

        public string StringValue { get; set; }

        public int IntValue { get; set; }
    }

    public enum VariantType
    {
        Int,
        String
    }
}
