using System;

namespace v2
{
    class Program
    {
        static void Main(string[] args)
        {

            var inputFileInfo = args.Length > 0 ? new System.IO.FileInfo(args[0]) : null;
            if (inputFileInfo != null && inputFileInfo.Exists)
            {
                Console.WriteLine("Input file name (文件名):\n" + inputFileInfo.FullName + "\n");
                var oArgs = Options(inputFileInfo.FullName);
                if(oArgs.Item1!=null && oArgs.Item2 != null && oArgs.Item6 == 0)
                {
                    var data = MhtReader.Read_Mht(inputFileInfo.FullName);
                    MhExporter.SaveFile(data,(bool)oArgs.Item1, (bool)oArgs.Item2, oArgs.Item3, oArgs.Item4,oArgs.Item5);
                    //MhExporter.SaveFile(data, true, true, DateTime.ParseExact("2020-03-22-08-42-52", "yyyy-MM-dd-HH-mm-ss", null), DateTime.ParseExact("2020-03-22-08-44-16", "yyyy-MM-dd-HH-mm-ss", null), "output.html");
                }
                else
                {
                    Console.WriteLine("!!Failed(失败)Error(错误): " + oArgs.Item5);
                }
            }
            else
            {
                Console.WriteLine("File args invalid (文件参数错误)");
            }
            Console.WriteLine("Program End, press any key to exit. (结束，任意键退出)");
        }
        /// <summary>
        ///     Get user input<br />
        ///     获取用户输入。
        /// </summary>
        /// <returns>
        ///     1.if file need a cut || 是否裁剪文件<br/>
        ///     2.if compress style || 是否合并样式<br/>
        ///     3.begin datetime || 起始时间<br/>
        ///     4.end datetime || 结束时间<br/>
        ///     5.output path || 输出路径<br/>
        ///     6.return value(code) || 返回值(代码)<br/>
        /// </returns>
        static (bool?, bool?, DateTime, DateTime, string, int) Options(string inputFilePath)
        {
            int input = -1;
            //Basic args || 基础参数
            bool? cut = null;
            bool? comp = null;
            DateTime begin = new DateTime();
            DateTime end = new DateTime();

            Console.WriteLine("1.Conver mht to html only (mht转html)");
            Console.WriteLine("2.Cut mht with datetime then convert to html (按日期裁剪mht后 转html)");
            Console.Write(">"); input = Console.ReadKey().KeyChar - 49; Console.Write("\n");
            if (input == 0) { cut = false; }
            else if (input == 1) { cut = true; }
            else { Console.WriteLine("Invalid option (选项错误)"); return (cut, comp, begin, end, null, 1); }

            Console.WriteLine("1.Default convert (默认)");
            Console.WriteLine("2.Compress style to css classes (合并样式为css classes)");
            Console.Write(">"); input = Console.ReadKey().KeyChar - 49; Console.Write("\n");
            if (input == 0) { comp = false; }
            else if (input == 1) { comp = true; }
            else { Console.WriteLine("Invalid option (选项错误)"); return (cut, comp, begin, end, null, 2); }

            //If Cut is required, ask for datetime range || 若需剪切，问时间范围
            if(cut == true)
            {
                string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";
                Console.WriteLine("Enter Datetime in format of yyyy-MM-dd-HH-mm-ss");
                Console.WriteLine("按 年年年年-月月-日日-时时-分分-秒秒 输入时间");

                Console.WriteLine("Enter Begin time inclusive(输入起始时间 含):");
                Console.Write(">"); string beginStr = Console.ReadLine();
                DateTime temp = new DateTime();
                bool flag = DateTime.TryParseExact(beginStr, dateTimeFormat, null,System.Globalization.DateTimeStyles.None, out temp);
                if (!flag) { Console.WriteLine("DateTime format invalid (日期格式错误)"); return (cut, comp, begin, end, null, 3); }
                begin = temp;

                Console.WriteLine("Enter End time inclusive(输入结束时间 含):");
                Console.Write(">"); string endStr = Console.ReadLine();
                flag = DateTime.TryParseExact(endStr, dateTimeFormat, null, System.Globalization.DateTimeStyles.None, out temp);
                if (!flag) { Console.WriteLine("DateTime format invalid (日期格式错误)"); return (cut, comp, begin, end, null, 4); }
                end = temp;
            }

            //If Datetime error || 若日期错误
            if (begin > end) { Console.WriteLine("Begin time is after End time (开始时间晚于结束时间)"); return (cut, comp, begin, end, null, 5); }

            //Get Output path || 问输出路径
            Console.WriteLine("Select output path (输入输出路径)");
            Console.WriteLine("If blank, output will be in the same folder as input (为空则放入同目录)");
            Console.WriteLine("If custom path, enter file path with extension like 'C:\\test.html'");
            Console.WriteLine("若自定义路径，输入文件路径及扩展名 如 'C:\\test.html'");
            Console.WriteLine("Note, directory must exist（注:文件夹必须存在)");
            Console.Write(">");
            string path = Console.ReadLine().Replace("'", "");

            if (string.IsNullOrWhiteSpace(path)) 
            {
                path = System.IO.Path.GetFileNameWithoutExtension(inputFilePath) +".html";
            }
            var fileinfo = new System.IO.FileInfo(path);

            //If dirctory invalid || 若文件夹不存在
            if (!fileinfo.Directory.Exists) { Console.WriteLine("Dirctory invalid 文件夹不存在：\""+ fileinfo.FullName + "\""); return (cut, comp, begin, end, fileinfo.FullName, 6); }

            return (cut, comp, begin, end, fileinfo.FullName, 0);
        }
    }
}
