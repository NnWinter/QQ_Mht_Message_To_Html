using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.Design.AxImporter;

namespace v3._1
{
    internal class IO
    {
        public static StreamReader? GetReader(FileInfo fileInfo)
        {
            if (!fileInfo.Exists) { 控制台.错误("拖入的文件不存在，或使用了错误的参数"); }
            if (fileInfo.Extension != ".mht") { 控制台.错误("指定文件的格式不是mht"); }
            try
            {
                var streamReader = new StreamReader(fileInfo.FullName);
                if (streamReader.Peek() < 0) { 控制台.错误("文件内容为空"); }
                return streamReader;
            }
            catch { 控制台.错误("打开文件读取流失败"); }
            return null;
        }
        public static UserOptions 获取选项(FileInfo fileInfo)
        {
            return new UserOptions(fileInfo);
        }
        public static string HtmlHead()
        {
            return
        "<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n" +
        "<head>\r\n" +
        "    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />\r\n" +
        "    <title>QQ Message</title>\r\n" +
        "    <style type=\"text/css\">\r\n" +
        "        body {\r\n" +
        "            font-size: 12px;\r\n" +
        "            line-height: 22px;\r\n" +
        "            margin: 2px;\r\n" +
        "        }\r\n" +
        "\r\n" +
        "        td {\r\n" +
        "            font-size: 12px;\r\n" +
        "            line-height: 22px;\r\n" +
        "        }\r\n" +
        "    </style>\r\n" +
        "</head>\r\n" +
        "<body>\r\n" +
        "    <table width=\"100%\" cellspacing=\"0\">";
        }
        public static string HtmlFoot(Dictionary<string, int> styles)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("    </table>");
            sb.AppendLine("    <style tyle=\"text/css\">");
            foreach (var style in styles)
            {
                string style_key = style.Key;

                // 对比字典中样式 -> style=aaa:bbb;ccc:ddd;>
                Match matchA = Regex.Match(style_key, "style=(.+?;)>");
                // 对比字典中样式 -> style="aaa:bbb;ccc:ddd;" eee='fff' ggg=hhh>
                Match matchB = Regex.Match(style_key, "style=\"(.*?)\"(.+?)>");

                if (!(matchA.Success | matchB.Success))
                {
                    控制台.错误($"无法识别的样式 - {style_key}");
                }

                if (matchA.Success)
                {
                    int slen = "style=".Length;
                    style_key = style_key.Substring(slen, matchA.Value.Length - slen - 1);
                }
                else
                {
                    string partA = matchB.Groups[1].Value;
                    string partB = matchB.Groups[2].Value;
                    partB = partB.Replace("=", ":");
                    style_key = partA + partB;
                }

                string style_str = string.Format("        .n{0}{{{1}}}", style.Value, style_key);
                sb.AppendLine(style_str);
            }
            sb.AppendLine("    </style>\n</body>\n</html>");
            return sb.ToString(); ;
        }
    }

    class UserOptions
    {
        public bool cut { get; init; }         // 按时间截取
        public bool comp { get; init; }        // 合并样式
        public DateTime begin { get; init; }    // 截取开始时间
        public DateTime end { get; init; }      // 截取结束时间
        public FileInfo fileinfo { get; init; } // 输出文件
        public DirectoryInfo dir { get; init; } // 工作目录

        public UserOptions(FileInfo inputFile)
        {
            string inputFilePath = inputFile.FullName;
            // 是否裁剪
            Console.WriteLine("1.mht转html");
            Console.WriteLine("2.按日期裁剪mht后 转html\n");
            Console.Write(">");
            int input = Console.ReadKey().KeyChar - 49; Console.WriteLine("\n");
            if (input == 0) { cut = false; }
            else if (input == 1) { cut = true; }
            else { 控制台.错误("选项错误"); }

            // 是否合并样式 (新版总是合并样式)
            comp = true;

            // 若需剪切，问时间范围
            if (cut == true)
            {
                string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";
                Console.WriteLine("按 年年年年-月月-日日-时时-分分-秒秒 输入时间");
                Console.WriteLine();

                Console.WriteLine("输入起始时间 (含):");
                Console.Write(">");
                string? beginStr = Console.ReadLine();
                if (beginStr == null) { 控制台.错误("输入了无效的时间"); }
                DateTime temp;
                bool flag = DateTime.TryParseExact(beginStr, dateTimeFormat, null, System.Globalization.DateTimeStyles.None, out temp);
                if (!flag) { 控制台.错误("日期格式错误"); }
                begin = temp;

                Console.WriteLine();
                Console.WriteLine("输入结束时间 (含):");
                Console.Write(">");
                string? endStr = Console.ReadLine();
                if (endStr == null) { 控制台.错误("输入了无效的时间"); }
                flag = DateTime.TryParseExact(endStr, dateTimeFormat, null, System.Globalization.DateTimeStyles.None, out temp);
                if (!flag) { 控制台.错误("日期格式错误"); }
                end = temp;
            }
            // 若日期错误
            if (begin > end) { 控制台.错误("开始时间晚于结束时间"); }

            // 输出路径
            Console.WriteLine("设置输出路径");
            Console.WriteLine("为空则放入同目录");
            Console.WriteLine("若自定义路径，输入文件路径及扩展名 如 'C:\\test.html'");
            Console.WriteLine("注:输出文件所在文件夹必须存在\n");
            Console.Write(">");
            string? path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.ChangeExtension(inputFilePath, ".html");
            }
            else
            {
                path = path.Replace("'", "");
            }
            var fileinfo = new FileInfo(path);

            Console.WriteLine("文件将被保存到 - " + path);

            // 检查父目录是否合法
            DirectoryInfo? dir = fileinfo.Directory;
            if (dir == null || !dir.Exists)
            {
                控制台.错误("文件找不到所属文件夹路径 - \"" + fileinfo.FullName + "\"");
                this.fileinfo = new FileInfo("");
                this.dir = new DirectoryInfo("");
            }
            else
            {
                this.fileinfo = fileinfo;
                this.dir = dir;
            }
        }
        public DirectoryInfo ImgOutDir()
        {
            return new DirectoryInfo(Path.Combine(dir.FullName, Path.GetFileNameWithoutExtension(fileinfo.Name)));
        }
    }
}
