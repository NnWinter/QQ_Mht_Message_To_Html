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
    // 在前20行内找到所有 header 定义
    var strb = new StringBuilder();
    for(int i = 0; i < 20; i++)
    {
        string? line = streamReader.ReadLine();
        if(line == null) { break; }
        strb.AppendLine(line);
    }
    var headerStr = strb.ToString();
    // 正则 Header 参数
    var matches = new Dictionary<MhtHeader.Attribute, Match>();
    matches.Add(MhtHeader.Attribute.From,
        Regex.Match(headerStr, "From: (<.+>)"));
    matches.Add(MhtHeader.Attribute.Subject, 
        Regex.Match(headerStr, "Subject:(?:\\s+)?(.[^\\s]+)(?:\\n)?(?:\\s+)?MIME"));
    matches.Add(MhtHeader.Attribute.MIME_Version,
        Regex.Match(headerStr, "MIME-Version:(?:\\s+)?(.[^\\s]+)(?:\\n)?(?:\\s+)?Content"));
    matches.Add(MhtHeader.Attribute.Content_Type, 
        Regex.Match(headerStr, "Content-Type:([^\\s+].[^\\s+]+?);"));
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
        mhtHeader.SetByAttribute(match.Key, match.Value.Groups[1].Value);
    }
    // 二次校验参数是否缺失
    if (mhtHeader.HasNull()) { Console.WriteLine("Header 缺失参数，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return null; }
    // 返回 Header
    return mhtHeader;
};


#endregion

#region 主程序
#if DEBUG
//文件流
Console.WriteLine("打开文件流");
var streamReader = GetStreamReader(new FileInfo("test.mht"));
#else
//文件流
var streamReader = GetStreamReader(new FileInfo(args[0]));
#endif
if (streamReader == null) { Console.WriteLine("文件读取流为空，按任意键退出"); Console.ReadKey(); Environment.Exit(1); return; }
Console.WriteLine("文件流已打开");
//获取 Header
Console.WriteLine("获取 .mht 的 Header 信息");
var mhtHeader = GetMhtHeader(streamReader);
#endregion
Console.WriteLine();


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