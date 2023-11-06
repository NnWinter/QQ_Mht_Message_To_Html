using System.Text.RegularExpressions;

namespace v3._1
{
    internal class 时间处理
    {
        const string SpanLeft = "<tr><td><div class=.+?><div class=.+?>.+?</div>";
        const string SpanRight = "</div><div class=.+?</div></td></tr>";
        //-- 不同小时制 --//
        // 24小时制_1 => 11:30:34
        const string Reg_24_1 = "(\\d{1,2}:\\d{2}:\\d{2})";
        // 12小时制_1 => 11:30:34 AM <=> 11:30:34 PM <=> 11:30:34&nbsp;AM <=> 11:30:34&nbsp;PM
        const string Reg_12_1 = "(\\d{1,2}:\\d{2}:\\d{2})(?:&nbsp;|\\s)+([AP]M)";
        // 12小时制_2 => 上午 11:30:34 <=> 下午 11:30:34 <=> 上午&nbsp;11:30:34 <=> 下午&nbsp;11:30:34
        const string Reg_12_2 = "([上下]午)(?:&nbsp;|\\s)+(\\d{1,2}:\\d{2}:\\d{2})";
        public static DateTime? GetDateTime(string tr, DateOnly? date, out string replaceOri, out string replacement)
        {
            // 初始化空值
            DateTime? dateTime = null;
            replaceOri = "";
            replacement = "";
            if (date == null) { return null; }

            // 先匹配整体结构
            var match = Regex.Match(tr, GetFullReg("(.+?)"));
            if (!match.Success) { return null; }
            replaceOri = match.Groups[1].Value;

            // 24小时制_1
            match = Regex.Match(replaceOri, Reg_24_1);
            if (match.Success)
            {
                dateTime = date.Value.ToDateTime(GetTime(match.Groups[1].Value));
                replacement = dateTime.Value.ToString(常量.NnTimeFormat);
                return dateTime;
            }
            // 12小时制_1
            match = Regex.Match(replaceOri, Reg_12_1);
            if (match.Success)
            {
                dateTime = AmPmToDateTime(match.Groups[2].Value == "AM", GetTime(match.Groups[1].Value), date.Value);
                replacement = dateTime.Value.ToString(常量.NnTimeFormat);
                return dateTime;
            }
            // 12小时制_2
            match = Regex.Match(replaceOri, Reg_12_2);
            if (match.Success)
            {
                dateTime = AmPmToDateTime(match.Groups[1].Value == "上午", GetTime(match.Groups[2].Value), date.Value);
                replacement = dateTime.Value.ToString(常量.NnTimeFormat);
                return dateTime;
            }
            replaceOri = ""; replacement = "";
            return null;
        }
        private static DateTime AmPmToDateTime(bool isAm, TimeOnly time, DateOnly date)
        {
            if (!isAm && time.Hour != 12) { time = new TimeOnly(time.Hour + 12, time.Minute, time.Second); }
            return date.ToDateTime(time);
        }
        private static string GetFullReg(string regex) { return SpanLeft + regex + SpanRight; }
        private static TimeOnly GetTime(string str)
        {
            { if (TimeOnly.TryParseExact(str, "HH:mm:ss", out TimeOnly result)) { return result; } }
            { if (TimeOnly.TryParseExact(str, "hh:mm:ss", out TimeOnly result)) { return result; } }
            { if (TimeOnly.TryParseExact(str, "H:mm:ss", out TimeOnly result)) { return result; } }
            { if (TimeOnly.TryParseExact(str, "h:mm:ss", out TimeOnly result)) { return result; } }
            throw new Exception($"无法转换时间信息为TimeOnly \"{str}\"");
        }
    }
}
