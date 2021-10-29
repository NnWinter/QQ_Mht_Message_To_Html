using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v2
{
    class MhtData
    {
        private string heading;
        private HtmlAgilityPack.HtmlDocument mhtDoc;
        private System.Collections.Generic.List<QMhtImg> imgs;

        public string Heading { get => heading; }
        public HtmlAgilityPack.HtmlDocument MhtDoc { get => mhtDoc; }
        public System.Collections.Generic.List<QMhtImg> Imgs { get => imgs; }
        public MhtData(string heading, HtmlAgilityPack.HtmlDocument mhtDoc, System.Collections.Generic.List<QMhtImg> imgs)
        {
            this.heading = heading;
            this.mhtDoc = mhtDoc;
            this.imgs = imgs;
        }
    }
}
