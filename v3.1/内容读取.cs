using System;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace v3._1
{
    internal class 内容读取
    {
        public static void 读写(StreamReader mhtStream, Dictionary<string, string> imgExtMap, UserOptions options, ref long lineCount)
        {
            Console.WriteLine("正在写为 html 文件");
            string tableLine = 寻找表头(mhtStream, ref lineCount);

            // 输出流
            string outFilePath = Path.Combine(options.dir.FullName, Path.GetFileNameWithoutExtension(options.fileinfo.Name) + ".html");
            StreamWriter writer = new StreamWriter(outFilePath);

            // 写入html头
            writer.WriteLine(IO.HtmlHead());
            writer.Flush();

            // 时间记录
            DateOnly? currentDate = null;

            // 样式记录
            var styles = new Dictionary<string, int>();
            int styleCount = 0; // 记录下一个写入的新 style 编号

            // 图片记录
            HashSet<string> imgs = new HashSet<string>();

            // 拆分第一行数据 (获取对话对象 和 第一有效数据)
            (string, string) th = 拆分表头行(tableLine);
            // 写入表头
            string th1 = 替换样式(th.Item1, ref styles, ref styleCount);
            writer.WriteLine("        " + th1);

            // 逐行读取
            string? line;
            bool isFirst = true; // 用于标记第一表头数据是否被读取
            while (true)
            {
                // 使用第一表头数据或顺位数据
                if (isFirst)
                {
                    line = th.Item2;
                    isFirst = false;
                }
                else
                {
                    // 读取新行
                    line = mhtStream.ReadLine(); lineCount++;
                    if (line == null || line.StartsWith("</table>")) { break; }
                }

                // 按行写入
                try
                {
                    line = 替换样式(line, ref styles, ref styleCount);
                    写入新行(writer, line, imgExtMap, ref currentDate, lineCount, ref imgs, options);
                }
                catch (Exception ex)
                {
                    控制台.错误($"读取行内容时发生错误 位于行: {lineCount} - {ex.Message}");
                }
            }

            // 写入 结尾 + 样式
            writer.Write(IO.HtmlFoot(styles));
            writer.Flush();
            writer.Close();

            Console.WriteLine("html 文件写入完毕\n");

            // 移动未被使用的图片
            Console.WriteLine("正在移动未使用图片到新目录");
            if (imgs.Count > 0) // 记录中可能没有图像的情况则不需要移动
            {
                Console.WriteLine("读取到的图像数量为 0, 跳过");
                IO.移动多余图片(options, imgs);
            }
            Console.WriteLine("移动完毕\n");

        }
        private static string 寻找表头(StreamReader stream, ref long lineCount)
        {
            string? line;
            while ((line = stream.ReadLine()) != null)
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
        private static (string, string) 拆分表头行(string th)
        {
            Match match = Regex.Match(th, "(<tr><td><div.+?><br><b>.+?<\\/b><\\/div><\\/td><\\/tr><tr><td><div.+?>消息分组:.+?<\\/div><\\/td><\\/tr><tr><td><div.+?>消息对象:.+?<\\/div><\\/td><\\/tr><tr><td><div.+?>&nbsp;<\\/div><\\/td><\\/tr>)(.+)");
            if (!match.Success) { 控制台.错误("表头格式不匹配"); return new("", ""); }
            var header = match.Groups[1].Value;
            var first = match.Groups[2].Value;
            return (header, first);
        }
        private static void 写入新行(StreamWriter writer, string line, Dictionary<string, string> imgExtMap, ref DateOnly? date, long lineCount, ref HashSet<string> imgs, UserOptions options)
        {
            // 匹配所有 tr 标签
            var matches_tr = Regex.Matches(line, "<tr>.+?</tr>");

            foreach (Match tr in matches_tr)
            {
                // 尝试读取日期区块
                var match_date = Regex.Match(tr.Value, "<tr><td class=.+?>日期: (\\d\\d\\d\\d-\\d\\d-\\d\\d)</td></tr>");
                if (match_date.Success)
                {
                    // 将日期更新
                    date = DateOnly.ParseExact(match_date.Groups[1].Value, "yyyy-MM-dd");
                }
                // 非日期消息
                else
                {
                    // 当前时间字符串
                    string mtr = tr.Value;
                    // 如果日期为空则为错误
                    if (date == null)
                    {
                        控制台.错误($"读取第 {lineCount} 行的消息前没有读取到该消息所属日期，按任意键退出"); return;
                    }
                    // 日期处理
                    {
                        const string NnTimeFormat = "yyyy/MM/dd  HH:mm:ss";

                        // 不同小时制
                        DateTime? GetDateTime(DateOnly? date, out string replaceOri, out string replacement)
                        {
                            // 24小时制
                            var match = Regex.Match(tr.Value, "<tr><td><div class=.+?><div class=.+?>.+?</div>(\\d?\\d:\\d\\d:\\d\\d)</div><div class=.+?</div></td></tr>");
                            if (match.Success)
                            {
                                var dateTime = date.Value.ToDateTime(TimeOnly.ParseExact(match.Groups[1].Value, "H:mm:ss"));
                                string timeStr = dateTime.ToString(NnTimeFormat);
                                replaceOri = match.Groups[1].Value;
                                replacement = timeStr;
                                return dateTime;
                            }
                            // 12小时制 (AM PM 可能导致错误的时间)
                            match = Regex.Match(tr.Value, "<tr><td><div class=.+?><div class=.+?>.+?</div>(\\d?\\d:\\d\\d:\\d\\d) ([AP]M)</div><div class=.+?</div></td></tr>");
                            if (match.Success)
                            {
                                var timeonly = TimeOnly.ParseExact(match.Groups[1].Value, "h:mm:ss");
                                if (match.Groups[2].Value == "PM" && timeonly.Hour != 12) { timeonly = new TimeOnly(timeonly.Hour + 12, timeonly.Minute, timeonly.Second); }
                                var dateTime = date.Value.ToDateTime(timeonly);
                                string timeStr = dateTime.ToString(NnTimeFormat);
                                replaceOri = match.Groups[1].Value + " " + match.Groups[2].Value;
                                replacement = timeStr;
                                return dateTime;
                            }
                            // 12小时制含nbsp (AM PM 可能导致错误的时间)
                            match = Regex.Match(tr.Value, "<tr><td><div class=.+?><div class=.+?>.+?</div>(\\d?\\d:\\d\\d:\\d\\d)&nbsp;([AP]M)</div><div class=.+?</div></td></tr>");
                            if (match.Success)
                            {
                                var timeonly = TimeOnly.ParseExact(match.Groups[1].Value, "h:mm:ss");
                                if (match.Groups[2].Value == "PM" && timeonly.Hour != 12) { timeonly = new TimeOnly(timeonly.Hour + 12, timeonly.Minute, timeonly.Second); }
                                var dateTime = date.Value.ToDateTime(timeonly);
                                string timeStr = dateTime.ToString(NnTimeFormat);
                                replaceOri = match.Groups[1].Value + "&nbsp;" + match.Groups[2].Value;
                                replacement = timeStr;
                                return dateTime;
                            }
                            replaceOri = ""; replacement = "";
                            return null;
                        }

                        // 获取时间
                        var dateTime = GetDateTime(date, out string replaceOri, out string timeStr);

                        // 获取时间失败除错
                        if (dateTime == null)
                        {
                            var theTime = Regex.Match(tr.Value, "<tr><td><div class=.+?><div class=.+?>.+?</div>(.+?)</div><div class=.+?</div></td></tr>");
                            if (theTime.Success)
                            {
                                控制台.错误($"在处理 {lineCount} 行时，消息的时间未能成功读取, 错误的日期：{theTime.Groups[1]}");
                            }
                            else
                            {
                                控制台.错误($"{lineCount} 行没能读取时间，且数据格式可能错误");
                            }
                        }

                        if ((dateTime < options.begin || dateTime > options.end) && options.cut) { continue; }

                        // 替换时间文本
                        mtr = mtr.Replace($"</div>{replaceOri}</div>", $"</div>{timeStr}</div>");

                        // 替换图片
                        string? imgName;
                        mtr = 替换图片(mtr, imgExtMap, options, lineCount, out imgName);
                        if (imgName != null)
                        {
                            imgs.Add(imgName);
                        }

                        // 输出结果
                        writer.WriteLine("        " + mtr);
                    }
                }
            }
            writer.Flush();
        }
        private static string 替换样式(string line, ref Dictionary<string, int> styles, ref int styleCount)
        {
            var matches = Regex.Matches(line, "(style=.+?)>");
            var m_styles = matches.DistinctBy(m => m.Value);
            // 对比字典中样式
            foreach (Match match in m_styles)
            {
                // 替换为已有的 style
                if (styles.TryGetValue(match.Value, out int value))
                {
                    line = line.Replace(match.Value, $"class=\"n{value}\">");
                }
                // 使用新 style
                else
                {
                    line = line.Replace(match.Value, $"class=\"n{styleCount}\">");
                    styles.Add(match.Value, styleCount++);
                }
            }
            return line;
        }
        private static string 替换图片(string line, Dictionary<string, string> imgExtMap, UserOptions options, long lineCount, out string? newName)
        {
            Match match = Regex.Match(line, "<IMG src=\"({.+?}.dat)\">");
            string? imgName = null;
            if (match.Success)
            {
                var outDir = options.ImgOutDir();

                var foundImg = imgExtMap.TryGetValue(match.Groups[1].Value, out imgName);

                if (foundImg)
                {
                    var outPath = Path.Combine(outDir.FullName, imgName);
                    var relativePath = Path.GetRelativePath(options.dir.FullName, outPath);
                    line = Regex.Replace(line, "<IMG src=\"({.+?}.dat)\">", "<IMG src=\"" + relativePath + "\">");
                }
                else
                {
                    控制台.警告($"警告: 在 {lineCount} 行 读取图像时未在mht下方找到图像数据 - {match.Groups[1].Value}");
                    line = Regex.Replace(line, "<IMG src=\"({.+?}.dat)\">", $"<div>[未找到的图片: {match.Groups[1].Value}]</div>");
                }
            }
            newName = imgName;
            return line;
        }
    }
}
