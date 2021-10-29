using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v2
{
    class QMhtImg
    {
        //args || 参数
        private string heading = null;
        private string contentType = null;
        private string contentTransferEncoding = null;
        private string contentLocation = null;
        private string data = null;
        //gets || 调用
        public string Heading { get => heading; }
        public string ContentType { get => contentType; }
        public string ContentTransferEncoding { get => contentTransferEncoding; }
        public string ContentLocation { get => contentLocation; }
        public string Data { get => data; }
        //init || 初始化
        public QMhtImg(string imgStr)
        {
            //get sector end pos || 获取区域结尾位置
            int heading_end = imgStr.IndexOf("\r\n");
            int contentType_end = imgStr.IndexOf("\r\n", heading_end + 2);
            int contentTransferEncoding_end = imgStr.IndexOf("\r\n", contentType_end + 2);
            int contentLocation_end = imgStr.IndexOf("\r\n", contentTransferEncoding_end + 2);
            //split strings || 定义分割字符串
            string contentType_splitStr = "Content-Type:";
            string contentTransferEncoding_splitStr = "Content-Transfer-Encoding:";
            string contentLocation_splitStr = "Content-Location:";
            //get string by index || 根据位置得到片段
            this.heading =
                "------=_" +
                imgStr.Substring(0, heading_end);
            this.contentType =
                imgStr.Substring(heading_end + 2, contentType_end - (heading_end + 2))
                .Split(contentType_splitStr, StringSplitOptions.RemoveEmptyEntries)[0];
            this.contentTransferEncoding =
                imgStr.Substring(contentType_end + 2, contentTransferEncoding_end - (contentType_end + 2))
                .Split(contentTransferEncoding_splitStr, StringSplitOptions.RemoveEmptyEntries)[0];
            this.contentLocation =
                imgStr.Substring(contentTransferEncoding_end + 2, contentLocation_end - (contentTransferEncoding_end + 2))
                .Split(contentLocation_splitStr, StringSplitOptions.RemoveEmptyEntries)[0];
            this.data =
                imgStr.Substring(contentLocation_end + 2)
                .Replace("\r", "").Replace("\n", "");
        }
    }
}
