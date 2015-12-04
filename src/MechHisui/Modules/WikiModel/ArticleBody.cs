using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MechHisui.Modules.WikiModel
{
    public class ArticleBody
    {
        public IEnumerable<ArticleSection> Sections { get; set; }
    }

    public class ArticleSection
    {
        public string Title { get; set; }
        public int Level { get; set; }
        public IEnumerable<Content> Content { get; set; }
        public IEnumerable<Image> Images { get; set; }
    }

    public class Image
    {
        public string Src { get; set; }
        public string Caption { get; set; }
    }

    public class Content
    {
        public string Type { get; set; }
        public string Text { get; set; }
    }
}
