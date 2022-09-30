﻿using System.IO;
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
#region 读取至 table 区域的第一行
static List<Message> MoveStreamToTabel(StreamReader streamReader, out string? titleMessage){
    titleMessage = null;
    var titleBuilder = new StringBuilder();
    var messages = new List<Message>();
    string? line;
    while((line = streamReader.ReadLine())!= null)
    {
        var tHead_match = Regex.Match(line, "<table .+?>");
        if (!tHead_match.Success) { continue; }
        // 读取 table 标签之后的内容
        var after_tHead = line.Substring(tHead_match.Index + tHead_match.Length);
        // 判断是否在本行有 table 终止标签
        var tEnd_match = Regex.Match(after_tHead, "</table>");
        if(tEnd_match.Success)
        {
            after_tHead = after_tHead.Substring(0, tEnd_match.Index);
        }
        // 读取文本区间中的 tr 行 并转换为 Message
        var matches = Regex.Matches(after_tHead, "<tr>.+?</tr>");
        if(matches.Count < 3) { Console.WriteLine("文件缺少消息说明信息，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return null; }
        // out title
        for(int i=0; i < 3; i++) 
        {
            titleBuilder.AppendLine(matches.ElementAt(i).Value);
        }
        titleMessage = titleBuilder.ToString();
        // 写入第一行消息
        for (int i = 3; i < matches.Count; i++)
        {
            string value = matches[i].Value;
            //过滤器 - 去掉无效空行
            var filter = Regex.Match(value, "<tr><td><div .[^>]+>&nbsp;</div></td></tr>");
            if (filter.Success) { continue; }
            //添加到消息列表
            messages.Add(new Message(value));
        }
    }
    return messages;
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
// 用户输入参数
var options = new UserOptions(fileInfo.FullName);
// 消息记录表格第一行
string? title = "";
var messages = MoveStreamToTabel(streamReader, out title);
if (messages == null || title == null) { Console.WriteLine("未找到消息记录表，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return; }
// 
#endregion
Console.WriteLine(streamReader.ReadLine());



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
class Message
{
    public DateOnly? date { get; init; }
    public string? style { get; init; }
    public enum Type
    {
        Date,
        PlainMessage
    }
    public Message(string line)
    {
        // 日期
        var regex_date = Regex.Match(line, "<tr><td style=(.+)>日期: (\\d\\d\\d\\d-\\d\\d-\\d\\d)</td></tr>");
        if(regex_date.Success)
        {
            style = regex_date.Groups[1].Value;
            date = DateOnly.ParseExact(regex_date.Groups[2].Value,"yyyy-MM-dd");
        }
        else
        {
            // "<tr><td><div style=(.[^>]+)><div style=(.[^>]+)>(.[^<]+)</div>(\d\d:\d\d:\d\d)</div><div style=(.[^>]+)><font style=(.[^>]+)>(.[^<]+)</font></div></td></tr>"
            var regex_plain = Regex.Matches(line, "style=.[^>]+");
        }
        
        Console.WriteLine(line);
    }
}
class UserOptions
{
    public bool isValid { get; init; }
    public bool? cut { get; init; }         // 按时间截取
    public bool? comp { get; init; }        // 合并样式
    public DateTime begin { get; init; }    // 截取开始时间
    public DateTime end { get; init; }      // 截取结束时间

    public UserOptions(string inputFilePath)
    {
        // 是否裁剪
        Console.WriteLine("1.mht转html");
        Console.WriteLine("2.按日期裁剪mht后 转html");
        Console.Write(">"); 
        int input = Console.ReadKey().KeyChar - 49; Console.Write("\n");
        if (input == 0) { cut = false; }
        else if (input == 1) { cut = true; }
        else { Console.WriteLine("选项错误"); return; }
        // 是否合并样式
        Console.WriteLine("1.默认");
        Console.WriteLine("2.合并样式为 css classes");
        Console.Write(">"); 
        input = Console.ReadKey().KeyChar - 49; Console.Write("\n");
        if (input == 0) { comp = false; }
        else if (input == 1) { comp = true; }
        else { Console.WriteLine("选项错误"); return; }

        // 若需剪切，问时间范围
        if (cut == true)
        {
            string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";
            Console.WriteLine("按 年年年年-月月-日日-时时-分分-秒秒 输入时间");

            Console.WriteLine("输入起始时间 (含):");
            Console.Write(">"); 
            string beginStr = Console.ReadLine();
            DateTime temp = new DateTime();
            bool flag = DateTime.TryParseExact(beginStr, dateTimeFormat, null, System.Globalization.DateTimeStyles.None, out temp);
            if (!flag) { Console.WriteLine("日期格式错误"); return; }
            begin = temp;

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
        Console.WriteLine("注:输出文件所在文件夹必须存在");
        Console.Write(">");
        string path = Console.ReadLine().Replace("'", "");

        if (string.IsNullOrWhiteSpace(path))
        {
            path = System.IO.Path.GetFileNameWithoutExtension(inputFilePath) + ".html";
        }
        var fileinfo = new System.IO.FileInfo(path);

        // 若文件夹不存在
        if (!fileinfo.Directory.Exists) { Console.WriteLine("文件夹不存在：\"" + fileinfo.FullName + "\""); return; }

        // 一切正常
        isValid = true;
    }
}