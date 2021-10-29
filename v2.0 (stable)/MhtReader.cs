using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v2
{
    class MhtReader
    {
        /// <summary>
        ///     Get 3 parts from mht file：mht file head info, html content, image references data of html content.<br />
        ///     获取 mht 文件中三部分内容: mht 文件头信息，html 部分内容，html 中所引用的图片数据。
        /// </summary>
        /// <param name="mht_str">
        ///     The path of mht file.<br />
        ///     mht文件路径。
        /// </param>
        public static MhtData Read_Mht(string mht_path)
        {
            //read file || 读取文件
            var inStream = new System.IO.StreamReader(mht_path);
            var mht_str = inStream.ReadToEnd(); inStream.Close();

            //split file string || 分割文件字符串
            string[] data = mht_str.Split(new string[] { "<html ", "</html>" }, StringSplitOptions.RemoveEmptyEntries);
            string htmlStr = "<html " + data[1] + "</html>";

            //load html string as document || html部分 转为文档
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(htmlStr);

            //get img info || 读图片信息
            var imgs = new System.Collections.Generic.List<QMhtImg>();
            var imgStrs = data[2].Split("\r\n------=_", StringSplitOptions.RemoveEmptyEntries).ToList();
            imgStrs.RemoveAll(x => x.StartsWith("\r") || x.StartsWith("\n") || x.Length<=50 );
            foreach (string imgstr in imgStrs)
            {
                imgs.Add(new QMhtImg(imgstr));
            }
            return new MhtData(data[0], htmlDoc, imgs);
        }
    }
}
