using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace v3._1
{
    internal class 内容读取
    {
        public static void 读写(StreamReader mhtStream, Dictionary<string, string> imgExtMap, UserOptions options, ref long lineCount)
        {
            string tableLine = 寻找表头(mhtStream, ref lineCount);

            //   拆分第一行数据 (获取对话对象 和 第一有效数据)
            (表头, (string, string)) th = 拆分表头行(tableLine);

            // 逐行读取
            while (true)
            {
                string? line;
                // 用于标记第一第二表头数据是否被读取
                bool first = false, second = false;
                if (!first)
                {
                    line = th.Item2.Item1;
                    first = true;
                }
                else if (!second)
                {
                    line = th.Item2.Item2;
                    second = true;
                }
                // 读取新行
                line = mhtStream.ReadLine();
                if(line == null) { return; }
            }
        }
        private static string 寻找表头(StreamReader stream, ref long lineCount)
        {
            string? line;
            while((line = stream.ReadLine()) != null)
            {
                lineCount++;
                Match match = Regex.Match(line, "<table.+?>(.+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            控制台.错误("未找到 <table> 标签");
            return "";
        }
        public static (表头, (string, string)) 拆分表头行(string th)
        {
            Match match = Regex.Match(th, "<tr><td><div.+?><br><b>.+?<\\/b><\\/div><\\/td><\\/tr><tr><td><div.+?>消息分组:(.+?)<\\/div><\\/td><\\/tr><tr><td><div.+?>消息对象:(.+?)<\\/div><\\/td><\\/tr><tr><td><div.+?>&nbsp;<\\/div><\\/td><\\/tr>(<tr>.+?<\\/tr>)(<tr>.+?<\\/tr>)");
            if (!match.Success) { 控制台.错误("表头格式不匹配"); return (new 表头("",""), ("","")); }
            var header = new 表头(match.Groups[1].Value, match.Groups[2].Value);
            var first = match.Groups[3].Value;
            var second = match.Groups[4].Value;
            return (header, (first, second));
        }
        public static void 写入新行()
        {

        }
    }
    class 表头
    {
        public string 消息分组 { get; init; }
        public string 消息对象 { get; init; }
        public 表头(string 消息分组, string 消息对象)
        {
            this.消息分组 = 消息分组;
            this.消息对象 = 消息对象;
        }
    }
}
