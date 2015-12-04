using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MechHisui.Modules.WikiModel
{
    public class ArticlesDetails
    {
        public string Basepath { get; set; }
        public InnerObject Items { get; set; }
    }

    public class InnerObject
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
