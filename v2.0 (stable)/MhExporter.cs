using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v2
{
    class MhExporter
    {
        public static void SaveFile(MhtData data, bool cut, bool comp, DateTime begin, DateTime end, string outpath)
        {
            //get basic data info || 读取基本内容
            var root = data.MhtDoc.DocumentNode;
            var html = root.ChildNodes[0];
            var head = html.ChildNodes["head"];
            var body = html.ChildNodes["body"];
            var table = body.ChildNodes["table"];
            //remove none tr tags || 移除非 tr 标签
            var select_1 = table.ChildNodes.Where(x => x.Name != "tr").ToArray();
            for (int i = 0; i < select_1.Count(); i++) { table.RemoveChild(select_1[i]); }

            var lines = table.ChildNodes;

            //if cut by datetime || 若按日期裁剪
            if (cut)
            {
                ReplaceTimeWithDatetime(table, lines);
                DateTimeFilter(table, lines, begin, end);
            }
            //if compress style || 若合并样式
            if (comp)
            {
                CompressStyles(table, head);
            }
            //implant imgs || 嵌入图片
            ImplantImages(table, data.Imgs);
            //insert mht info || 插入mht信息
            head.InnerHtml += string.Format("\n<!--\n{0}\n-->", data.Heading);
            //save file|| 保存
            data.MhtDoc.Save(outpath);
        }
        static void ReplaceTimeWithDatetime(HtmlAgilityPack.HtmlNode table, HtmlAgilityPack.HtmlNodeCollection lines)
        {
            string date_Pointer = "[unknown_date]";
            var remove_query = new List<HtmlAgilityPack.HtmlNode>();
            //replace time with datetime || 用完整日时替换时间
            for (int i = 1; i < lines.Count(); i++)
            {
                //get date pointer || 获取日期区块
                var td = lines.ElementAt(i).ChildNodes["td"];
                if (td != null)
                {
                    var style = td.Attributes["style"];
                    if (style != null)
                    {
                        if (style.Value == "border-bottom-width:1px;border-bottom-color:#8EC3EB;border-bottom-style:solid;color:#3568BB;font-weight:bold;height:24px;line-height:24px;padding-left:10px;margin-bottom:5px;")
                        {
                            date_Pointer = td.InnerText.Substring(4);
                            remove_query.Add(table.ChildNodes[i]);
                        }
                    }
                    //alter conversations || 修改对话信息
                    else
                    {
                        var divs = td.ChildNodes.Where(x => x.Name == "div");
                        if (divs.Count() == 2)
                        {
                            var div0 = divs.ElementAt(0);
                            var div1 = divs.ElementAt(1);
                            if ((div0.Attributes["style"].Value == "color:#42B475;padding-left:10px;" || div0.Attributes["style"].Value == "color:#006EFE;padding-left:10px;") && div1.Attributes["style"].Value == "padding-left:20px;")
                            {
                                string[] time_str = div0.ChildNodes["#text"].InnerText.Split(':');
                                for (int j = 0; j < time_str.Length; j++)
                                {
                                    time_str[j] = time_str[j].PadLeft(2, '0');
                                }
                                div0.ChildNodes["#text"].InnerHtml = string.Format("{0} | {1}:{2}:{3}", date_Pointer, time_str[0], time_str[1], time_str[2]);
                            }
                        }
                    }
                }
            }
            //remove date labels || 删除日期标签
            var remove_query_array = remove_query.ToArray();
            foreach (HtmlAgilityPack.HtmlNode item in remove_query_array) { table.RemoveChild(item); }
        }
        static void DateTimeFilter(HtmlAgilityPack.HtmlNode table, HtmlAgilityPack.HtmlNodeCollection lines, DateTime begin, DateTime end)
        {
            //filter by datetime || 按日期过滤
            bool first_flag = false, first_time_flag = true;
            int begin_index = -1, end_index = -1;

            for (int i = 1; i < lines.Count(); i++)
            {
                var td = lines.ElementAt(i).ChildNodes["td"];
                if (td != null)
                {
                    var style = td.Attributes["style"];
                    if (style == null)
                    {
                        var divs = td.ChildNodes.Where(x => x.Name == "div");
                        if (divs.Count() == 2)
                        {
                            var div0 = divs.ElementAt(0);
                            var div1 = divs.ElementAt(1);
                            if ((div0.Attributes["style"].Value == "color:#42B475;padding-left:10px;" || div0.Attributes["style"].Value == "color:#006EFE;padding-left:10px;") && div1.Attributes["style"].Value == "padding-left:20px;")
                            {
                                DateTime time = DateTime.ParseExact(div0.ChildNodes["#text"].InnerText, "yyyy-MM-dd | HH:mm:ss", null);
                                //get first begin index || 获取初始位置
                                if (!first_flag)
                                {
                                    if (time >= begin && time > end && first_time_flag)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        first_time_flag = false;
                                    }
                                    if (time >= begin && time <= end)
                                    {
                                        begin_index = i;
                                        first_flag = true;
                                    }
                                }
                                //get first end index || 获取结束位置
                                else
                                {
                                    if (time >= end)
                                    {
                                        end_index = i - 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //no end
            if (begin_index >= 0 && end_index == -1) { end_index = lines.Count() - 1; }
            //use index to cut || 用标记位置裁剪
            var remove_query = new List<HtmlAgilityPack.HtmlNode>();
            for (int i = 4; i < lines.Count(); i++)
            {
                if (i < begin_index || i > end_index || begin_index == -1) { remove_query.Add(lines.ElementAt(i)); }
            }
            var remove_query_array = remove_query.ToArray();
            foreach (HtmlAgilityPack.HtmlNode item in remove_query_array) { table.RemoveChild(item); }
        }
        static void CompressStyles(HtmlAgilityPack.HtmlNode table, HtmlAgilityPack.HtmlNode head)
        {
            Dictionary<string, string> style_dictionary = new Dictionary<string, string>();

            var styledTags = table.Descendants().Where(x => x.Attributes["style"] != null);
            int style_pointer = 0;
            foreach (var sTag in styledTags)
            {
                var style_value = sTag.Attributes["style"].Value;
                //creat new key || 建立新键
                if (!style_dictionary.ContainsKey(style_value))
                {
                    style_dictionary.Add(style_value, "NS_" + style_pointer);
                    sTag.Attributes.Remove("style");
                    sTag.AddClass("NS_" + style_pointer++);
                }
                //have key || 已有键
                else
                {
                    sTag.Attributes.Remove("style");
                    sTag.AddClass(style_dictionary[style_value]);
                }
            }

            var NnStyle = HtmlAgilityPack.HtmlNode.CreateNode("<style type=\"text/css\">\n</style>");

            foreach (var clas in style_dictionary)
            {
                NnStyle.InnerHtml += string.Format(".{0}{{{1}}}\n", clas.Value, clas.Key);
            }
            head.ChildNodes.Add(NnStyle);
        }
        static void ImplantImages(HtmlAgilityPack.HtmlNode table, List<QMhtImg> Imgs)
        {
            var imgTags = table.Descendants().Where(x => x.Name == "img");
            foreach (var imgtag in imgTags)
            {
                if (imgtag.Attributes["src"] != null)
                {
                    if (Imgs.Any(x => x.ContentLocation == imgtag.Attributes["src"].Value))
                    {
                        var imgdata = Imgs.First(x => x.ContentLocation == imgtag.Attributes["src"].Value);

                        string imgSrc = string.Format("data:{0};{1},{2}", imgdata.ContentType, imgdata.ContentTransferEncoding, imgdata.Data);
                        imgtag.Attributes["src"].Value = imgSrc;
                        imgtag.Attributes.Add("alt", string.Format("boundary:{0};content-location:{1}", imgdata.Heading, imgdata.ContentLocation));
                    }
                    else
                    {
                        var srcstr = imgtag.Attributes["src"].Value;
                        imgtag.Attributes.Add("alt", string.Format("<dataless_img:{0}>", srcstr));

                        Console.WriteLine(string.Format("Image {0} does not exist, skip.", srcstr));
                    }
                }
            }
        }
    }
}
