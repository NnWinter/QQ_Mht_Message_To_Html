using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v3._1
{
    internal class 控制台
    {
        public static void 错误(string error)
        {
            ConsoleColor ori_fore_color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = ori_fore_color;
            Console.WriteLine("按任意键退出");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }
}
