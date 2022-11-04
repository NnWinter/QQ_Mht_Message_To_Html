using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace v3._1
{
    internal class 保存图片
    {
        public static Dictionary<string, string> 获取图片字典(StreamReader? stream, UserOptions options)
        {
            //图片名映射表
            var extMapping = new Dictionary<string, string>();

            //跳过正文并返回行号
            if (stream == null) { 控制台.错误("未能成功打开文件读取流"); return extMapping; }
            long skiped = 跳过正文内容(stream);

            //函数
            #region ==== 函数 ====
            //获取扩展名
            Func<string> GetExt = () =>
            {
                string? ext_line = stream.ReadLine();
                if (ext_line == null)
                {
                    控制台.错误("读取图片错误 - 位于行: " + skiped);
                    return "";
                }
                return ext_line.Split('/')[1];
            };
            //检查编码
            Action CheckBase64 = () =>
            {
                string? encoding = stream.ReadLine();
                if (encoding == null)
                {
                    控制台.错误("图片不包含编码信息 - 位于行: " + skiped);
                    return;
                }
                if (encoding.Trim() != "Content-Transfer-Encoding:base64")
                {
                    控制台.错误("图片不是Base64编码 - 位于行: " + skiped);
                }
            };
            //获取图片名并写入到字典
            Func<string, Dictionary<string, string>, string> GetImgName = (ext, dic) =>
            {
                string? line = stream.ReadLine();
                if (line == null) { 控制台.错误("图片文件名丢失 - 位于行: " + skiped); return ""; }

                string oldname = line.Split(':')[1];
                string newname = oldname.Substring(0, oldname.LastIndexOf('.')) + "." + ext;

                dic.Add(oldname, newname);

                return newname;
            };
            //保存图片数据
            Action<StreamReader, string, UserOptions> SaveImg = (stream, imgName, options) =>
            {
                try
                {
                    StringBuilder base64 = new StringBuilder();

                    // 读取图片数据(遇到空行结束)
                    string? line;
                    while ((line = stream.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
                    {
                        base64.Append(line);
                    }
                    byte[] bytes = Convert.FromBase64String(base64.ToString());

                    // 用 MemoryStream 保存图片
                    MemoryStream ms = new MemoryStream(bytes);
                    Image img = Image.FromStream(ms);

                    //  获取图片路径
                    var outDir = options.ImgOutDir();
                    if (!outDir.Exists) { outDir.Create(); }
                    var outPath = Path.Combine(outDir.FullName, imgName);
                    img.Save(outPath);
                }
                catch { 控制台.错误("读取并保存图片数据时发生了一个错误 - 位于行: " + skiped); }
            };
            #endregion

            //控制台进度提示
            long imgCount = 0;
            Console.Write($"已保存 {imgCount} 个图片".PadRight(20));

            //查找分割线
            string? line;
            while ((line = stream.ReadLine()) != null)
            {
                skiped++;
                if (line.StartsWith("------=_NextPart_") && !line.EndsWith("--"))
                {
                    // 获取扩展名
                    string ext = GetExt();
                    skiped++;

                    // 检查编码方式
                    CheckBase64();
                    skiped++;

                    // 获取文件名并写入字典
                    string imgName = GetImgName(ext, extMapping);
                    skiped++;

                    // 读一个空行
                    stream.ReadLine();

                    //保存图片
                    SaveImg(stream, imgName, options);

                    //输出提示
                    Console.CursorLeft = 0;
                    Console.Write($"已保存 {++imgCount} 个图片".PadRight(20));
                }
            }
            Console.WriteLine();
            //返回映射表
            return extMapping;
        }
        /// <summary>
        /// 返回跳过正文后跳过的行数
        /// </summary>
        /// <param name="streamReader">读取流</param>
        /// <returns>行数</returns>
        private static long 跳过正文内容(StreamReader streamReader)
        {
            long count = 0;
            string? line;
            while ((line = streamReader.ReadLine()) != null && !line.Contains("</html>")) {
                count++;
            }
            return count;
        }
    }
}
