using System.IO;
using System.Text;
using System.Text.RegularExpressions;
#region 获取文件流
Func<FileInfo, StreamReader?> GetStreamReader = (fileInfo) => {
    
    if (!fileInfo.Exists) { Console.WriteLine("拖入的文件不存在，或使用了错误的参数，按任意键退出。"); Console.ReadKey(); Environment.Exit(1); }
    if (fileInfo.Extension != ".mht") { Console.WriteLine("指定文件的格式不是mht，按任意键退出"); Console.ReadKey(); Environment.Exit(1); }
    try
    {
        var streamReader = new StreamReader(fileInfo.FullName);
        if (streamReader.Peek() < 0) { Console.WriteLine("文件内容为空，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return null; }
        return streamReader;
    }
    catch { Console.WriteLine("打开文件读取流失败，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return null; }
};
#endregion
#region 解析文件头
Func<StreamReader, MhtHeader> GetMhtHeader = (streamReader) =>
{
    // 在前8行内找到所有 header 定义
    var strb = new StringBuilder();
    for(int i = 0; i < 8; i++)
    {
        string? line = streamReader.ReadLine();
        if(line == null) { Console.WriteLine("文件格式有误，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return null; }
        strb.AppendLine(line);
    }
    var headerStr = strb.ToString();
    // 正则 Header 参数
    var matches = new Dictionary<MhtHeader.Attribute, Match>();
    matches.Add(MhtHeader.Attribute.From,
        Regex.Match(headerStr, "From:[\\s+?]?(.+)[\\s+?]?Subject"));
    matches.Add(MhtHeader.Attribute.Subject, 
        Regex.Match(headerStr, "Subject:[\\s+?]?(.+)[\\s+?]?MIME"));
    matches.Add(MhtHeader.Attribute.MIME_Version,
        Regex.Match(headerStr, "MIME-Version:[\\s+?]?(.+)[\\s+?]?Content"));
    matches.Add(MhtHeader.Attribute.Content_Type, 
        Regex.Match(headerStr, "Content-Type:[\\s+?]?(.+)[\\s+?]?;"));
    matches.Add(MhtHeader.Attribute.Charset, 
        Regex.Match(headerStr, "charset=\"(.+?)\""));
    matches.Add(MhtHeader.Attribute.Type,
        Regex.Match(headerStr, "type=\"(.+?)\""));
    matches.Add(MhtHeader.Attribute.Boundary, 
        Regex.Match(headerStr, "boundary=\"(.+?)\""));
    // 匹配正则 添加到 Header
    var mhtHeader = new MhtHeader();
    foreach (var match in matches)
    {
        if (!match.Value.Success) { Console.WriteLine($"未找到参数 {match.Key:g} ，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return null; }
        mhtHeader.SetByAttribute(match.Key, match.Value.Groups[1].Value.Replace("\r","").Replace("\n",""));
    }
    // 二次校验参数是否缺失
    if (mhtHeader.HasNull()) { Console.WriteLine("Header 缺失参数，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return null; }
    // 返回 Header
    return mhtHeader;
};
#endregion
#region 验证文件头
Func<MhtHeader, bool> IsHeaderValid = (header) =>
{
    if(header.GetByAttribute(MhtHeader.Attribute.From) != "<Save by Tencent MsgMgr>") {
        Console.WriteLine("mht 文件 From 不是 Tencent MsgMgr 格式，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return false;
    }
    if(header.GetByAttribute(MhtHeader.Attribute.Subject) != "Tencent IM Message"){
        Console.WriteLine("mht 文件 Subject 不是 Tencent IM Message 格式，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return false;
    }
    if (header.GetByAttribute(MhtHeader.Attribute.Content_Type) != "multipart/related"){
        Console.WriteLine("mht 文件 Content-Type 不是 multipart/related 格式，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return false;
    }
    if (header.GetByAttribute(MhtHeader.Attribute.MIME_Version) != "1.0"){
        Console.WriteLine("mht 文件 MIME_Version 不是 1.0，按任意键忽略警告 或关闭窗口退出"); Console.ReadKey();
    }
    return true;
};
#endregion
#region 移动到表头
Func<StreamReader, bool> MoveStreamToTabel = (streamReader) => {
    char[] buffer = new char[12];
    bool found = false;
    // 找 <table
    while (!streamReader.EndOfStream)
    {
        streamReader.ReadBlock(buffer, 6, 6);
        string str = new string(buffer);
        if (str.Contains("<table"))
        {
            found = true;
            break;
        }
        else { Array.Copy(buffer, 6, buffer, 0, 6); }
    }
    if (!found) { return false; }
    // 找 >
    while (!streamReader.EndOfStream)
    {
        streamReader.ReadBlock(buffer, 0, 1);
        if (buffer[0] == '>')
        {
            return true;
        }
    }
    return false;
};
#endregion
#region 转换表格内容
Action<UserOptions, StreamReader> ConvertTable = (options, streamReader) => {
    // 输出流
    var streamWriter = new StreamWriter(options.path);
    try
    {
        // 清空流（覆盖文件）
        streamWriter.BaseStream.Position = 0;
        streamWriter.BaseStream.SetLength(0);
        // 写入头
        WriteHtmlHead(streamWriter);
    }
    catch { }
    var styles = new Dictionary<string, int>();
    int styleCount = 0; // 记录下一个写入的新 style 编号
    DateOnly? currentDate = null;
    if (!options.cut.Value)
    {
        // 逐行读取
        string? line; long line_num = 0;
        while((line = streamReader.ReadLine())!=null)
        {
            // 行号
            line_num++;
            // 读取样式
            var matches = Regex.Matches(line, "style=.[^>]+");
            var m_styles = matches.DistinctBy(m=>m.Value);
            // 对比字典中样式
            foreach(Match match in m_styles)
            {
                // 替换为已有的 style
                if (styles.TryGetValue(match.Value, out int value))
                {
                    line = line.Replace(match.Value, $"class=\"n{value}\"");
                }
                // 使用新 style
                else
                {
                    line = line.Replace(match.Value, $"class=\"n{styleCount}\"");
                    styles.Add(match.Value, styleCount++);
                }
            }
            // 读取每一行中的 tr 标签
            var matches_tr = Regex.Matches(line, "<tr>.+?</tr>").Skip(0);
            
            // 第一行要单独读取前四个 tr 以输出对话信息
            if(line_num == 1) {
                var mhd = matches_tr.Take(4);
                foreach(var hdm in mhd) { streamWriter.WriteLine(hdm.Value); }
                streamWriter.Flush();
                matches_tr = matches_tr.Skip(4);
            }
            foreach(Match tr in matches_tr)
            {
                // 尝试读取日期区块
                var match_date = Regex.Match(tr.Value, "<tr><td class=.+?>日期: (\\d\\d\\d\\d-\\d\\d-\\d\\d)</td></tr>");
                if (match_date.Success)
                {
                    // 将日期更新
                    currentDate = DateOnly.ParseExact(match_date.Groups[1].Value, "yyyy-MM-dd");
                }
                // 非日期消息
                else
                {
                    // 当前时间字符串
                    string mtr = tr.Value;
                    // 如果日期为空则为错误
                    if (currentDate == null) { Console.WriteLine($"读取第 {line_num} 行的消息前没有读取到该消息所属日期，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return; }
                    // 读取日期
                    var match = Regex.Match(tr.Value, "<tr><td><div class=.+?><div class=.+?>.+?</div>(\\d?\\d:\\d\\d:\\d\\d)</div><div class=.+?</div></td></tr>");
                    // 修改日期 如果没找到指定的日期标签则不做修改
                    if (match.Success)
                    {
                        TimeOnly time = TimeOnly.ParseExact(match.Groups[1].Value.PadLeft(8, '0'), "HH:mm:ss");
                        DateTime dateTime = currentDate.Value.ToDateTime(time);
                        string timeStr = dateTime.ToString("yyyy/MM/dd  HH:mm:ss");
                        mtr = mtr.Replace($"</div>{match.Groups[1].Value}</div>", $"</div>{timeStr}</div>");
                        streamWriter.WriteLine(mtr);
                    }
                }
            }
            streamWriter.Flush();
            if (line.Contains("</table>")) 
            { 
                break; 
            }
        }
    }
};
#endregion
#region 主程序
#if DEBUG
// 文件流
Console.WriteLine("打开文件流");
var fileInfo = new FileInfo("test.mht");
var streamReader = GetStreamReader(fileInfo);
#else
//文件流
var streamReader = GetStreamReader(new FileInfo(args[0]));
#endif
if (streamReader == null) { Console.WriteLine("文件读取流为空，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return; }
Console.WriteLine("文件流已打开");
// 获取 Header
Console.WriteLine("获取 .mht 的 Header 信息");
var mhtHeader = GetMhtHeader(streamReader);
// 验证 Header
if (!IsHeaderValid(mhtHeader)) { Console.WriteLine("mht 文件 Header 有误，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return; }
// 移动到表头
MoveStreamToTabel(streamReader);
// 用户输入参数
var options = new UserOptions(fileInfo.FullName);
if (!options.isValid) { Console.WriteLine("输入的选项有误，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return; }
// 转换
ConvertTable(options, streamReader);
#endregion


static void WriteHtmlHead(StreamWriter streamWriter)
{
    streamWriter.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n" +
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
        "    <table width=\"100%\" cellspacing=\"0\">");
    streamWriter.Flush();
}
class MhtHeader
{
    public enum Attribute
    {
        From,
        Subject,
        MIME_Version,
        Content_Type,
        Charset,
        Type,
        Boundary
        
    }
    private string? From;
    private string? Subject;
    private string? MIME_Version;
    private string? Content_Type;
    private string? Charset;
    private string? Type;
    private string? Boundary;
    public string? GetByAttribute(Attribute attribute)
    {
        switch (attribute)
        {
            case Attribute.From:
                return From;
            case Attribute.Subject:
                return Subject;
            case Attribute.MIME_Version:
                return MIME_Version;
            case Attribute.Content_Type:
                return Content_Type;
            case Attribute.Charset:
                return Charset;
            case Attribute.Type:
                return Type;
            case Attribute.Boundary:
                return Boundary;
        }
        return null;
    }
    public MhtHeader(string from, string subject, string mIME_Version, string content_Type, string charset, string type, string boundary)
    {
        From = from;
        Subject = subject;
        MIME_Version = mIME_Version;
        Content_Type = content_Type;
        Charset = charset;
        Type = type;
        Boundary = boundary;
    }
    public MhtHeader()
    {

    }
    public void SetByAttribute(Attribute attribute, string value)
    {
        switch (attribute)
        {
            case Attribute.From:
                From = value;
                return;
            case Attribute.Subject:
                Subject = value;
                return;
            case Attribute.MIME_Version:
                MIME_Version = value;
                return;
            case Attribute.Content_Type:
                Content_Type = value;
                return;
            case Attribute.Charset:
                Charset = value;
                return;
            case Attribute.Type:
                Type = value;
                return;
            case Attribute.Boundary:
                Boundary = value;
                return;
        }
    }
    public bool HasNull()
    {
        return
            From            == null || 
            Subject         == null || 
            MIME_Version    == null || 
            Content_Type    == null ||
            Charset         == null || 
            Type            == null ||
            Boundary        == null;
    }
}
class UserOptions
{
    public bool isValid { get; init; }
    public bool? cut { get; init; }         // 按时间截取
    public bool? comp { get; init; }        // 合并样式
    public DateTime begin { get; init; }    // 截取开始时间
    public DateTime end { get; init; }      // 截取结束时间
    public string path { get; init; }       // 输出路径

    public UserOptions(string inputFilePath)
    {
        // 是否裁剪
        Console.WriteLine("1.mht转html");
        Console.WriteLine("2.按日期裁剪mht后 转html\n");
        Console.Write(">"); 
        int input = Console.ReadKey().KeyChar - 49; Console.WriteLine("\n");
        if (input == 0) { cut = false; }
        else if (input == 1) { cut = true; }
        else { Console.WriteLine("选项错误"); return; }
        // 是否合并样式
        Console.WriteLine("1.默认");
        Console.WriteLine("2.合并样式为 css classes\n");
        Console.Write(">"); 
        input = Console.ReadKey().KeyChar - 49; Console.WriteLine("\n");
        if (input == 0) { comp = false; }
        else if (input == 1) { comp = true; }
        else { Console.WriteLine("选项错误"); return; }

        // 若需剪切，问时间范围
        if (cut == true)
        {
            string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";
            Console.WriteLine("按 年年年年-月月-日日-时时-分分-秒秒 输入时间");
            Console.WriteLine();
            Console.WriteLine("输入起始时间 (含):");
            Console.Write(">"); 
            string beginStr = Console.ReadLine();
            DateTime temp = new DateTime();
            bool flag = DateTime.TryParseExact(beginStr, dateTimeFormat, null, System.Globalization.DateTimeStyles.None, out temp);
            if (!flag) { Console.WriteLine("日期格式错误"); return; }
            begin = temp;

            Console.WriteLine();
            Console.WriteLine("输入结束时间 (含):");
            Console.Write(">"); 
            string endStr = Console.ReadLine();
            flag = DateTime.TryParseExact(endStr, dateTimeFormat, null, System.Globalization.DateTimeStyles.None, out temp);
            if (!flag) { Console.WriteLine("日期格式错误"); return; }
            end = temp;
        }
        // 若日期错误
        if (begin > end) { Console.WriteLine("开始时间晚于结束时间"); return; }

        // 输出路径
        Console.WriteLine("设置输出路径");
        Console.WriteLine("为空则放入同目录");
        Console.WriteLine("若自定义路径，输入文件路径及扩展名 如 'C:\\test.html'");
        Console.WriteLine("注:输出文件所在文件夹必须存在\n");
        Console.Write(">");
        string path = Console.ReadLine().Replace("'", ""); Console.WriteLine();

        if (string.IsNullOrWhiteSpace(path))
        {
            path = System.IO.Path.GetFileNameWithoutExtension(inputFilePath) + ".html";
        }
        var fileinfo = new System.IO.FileInfo(path);

        // 若文件夹不存在
        if (!fileinfo.Directory.Exists) { Console.WriteLine("文件夹不存在：\"" + fileinfo.FullName + "\""); return; }

        // 一切正常
        this.path = path;
        isValid = true;
    }
}