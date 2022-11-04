using v3._1;

#region 函数
var 获取mht路径 = FileInfo () =>
{
    Console.WriteLine("打开文件流");
#if DEBUG
    var fileInfo = new FileInfo("test.mht");
#else
    var fileInfo = new FileInfo(args[0]);
#endif
    if (!fileInfo.Exists) 
    { 
        控制台.错误("未找到文件 - " + fileInfo.FullName); 
    }
    if (fileInfo.Extension.ToLower() != ".mht") 
    { 
        控制台.错误("文件不是 mht 格式 - " + fileInfo.FullName); 
    }
    return fileInfo;
};
#endregion

#region 主程序

// 获取mht文件路径
FileInfo mhtFileInfo = 获取mht路径();

// 获取选项
UserOptions userOptions = IO.获取选项(mhtFileInfo);

// 保存文件中所有图片并取得字典
Console.WriteLine("保存图片中...");
var imgDic = 保存图片.获取图片字典(IO.GetReader(mhtFileInfo), userOptions);
Console.WriteLine("图片保存完毕");

// 读取正文
long lineCount = 0;
StreamReader? mhtStream = IO.GetReader(mhtFileInfo);

//   读取 MHT 文件头
if (mhtStream == null) { 控制台.错误("打开文件流失败"); return; }
MhtHeader header = new MhtHeader(mhtStream, ref lineCount);

//   寻找 table 起始位置 (传递 lineCount 用于报告行数)
内容读取.读写(mhtStream, imgDic, userOptions, ref lineCount);

#endregion
Console.WriteLine();