using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace v3._1
{
    internal class MhtHeader
    {
        public string From { get; init; }
        public string Subject { get; init; }
        public string MIME_Version { get; init; }
        public string Content_Type { get; init; }
        public string Charset { get; init; }
        public string Type { get; init; }
        public string Boundary { get; init; }

        public MhtHeader(string from, string subject, string mIME_Version, string content_Type, string charset, string type, string boundary)
        {
            From = from;
            Subject = subject;
            MIME_Version = mIME_Version;
            Content_Type = content_Type;
            Charset = charset;
            Type = type;
            Boundary = "--" + boundary;
        }
        public MhtHeader(StreamReader mhtStream, ref long lineCount)
        {
            From = "";
            Subject = "";
            MIME_Version = "";
            Content_Type = "";
            Charset = "";
            Type = "";
            Boundary = "";

            string? line;
            while ((line = mhtStream.ReadLine()) != null)
            {
                lineCount++;
                if (line.Trim().StartsWith("From:"))
                {
                    Match match_from = Regex.Match(line, "From: (.+)");
                    if (!match_from.Success) { 控制台.错误("Mht文件头缺失 From 位于行: " + lineCount); return; }
                    From = match_from.Groups[1].Value;

                    Func<string, long, string> GetAttribute = (attribute, localLineCount) =>
                    {
                        line = mhtStream.ReadLine(); localLineCount++;
                        string att = attribute.Substring(0, attribute.Length - 1);
                        if (line == null) 
                        { 
                            控制台.错误("Mht文件头缺失 " + att + " 位于行: " + localLineCount); return ""; 
                        }
                        Match match = Regex.Match(line, attribute+"(.+)");
                        if (!match.Success) 
                        { 
                            控制台.错误("Mht文件头缺失 " + att + " 位于行: " + localLineCount); return ""; 
                        }
                        return match.Groups[1].Value;
                    };

                    Subject = GetAttribute("Subject:", lineCount++);
                    MIME_Version = GetAttribute("MIME-Version:", lineCount++);
                    Content_Type = GetAttribute("Content-Type:", lineCount++);
                    Charset = GetAttribute("charset=", lineCount++);
                    Type = GetAttribute("type=", lineCount++);

                    string bound = GetAttribute("boundary=", lineCount++);
                    if(!(bound.StartsWith("\"") && bound.EndsWith("\"")))
                    { 
                        控制台.错误("Mht文件头内容错误 - boundary 位于行: " + lineCount); return; 
                    }
                    bound = bound.Replace("\"", "");
                    Boundary = "--" + bound;

                    return;
                }
            }
        }
    }
}
