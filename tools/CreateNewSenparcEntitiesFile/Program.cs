using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string baseDirectory = @"X:\Senparc 项目\NeuCharFramework\NcfPackageSources\src"; // 替换为你的目录路径  
        string replacementText = "Dm"; // 替换为你指定的字符  

        ProcessFiles(baseDirectory, replacementText);
    }

    static void ProcessFiles(string baseDir, string replacement)
    {
        // 获取所有.cs文件  
        var files = Directory.GetFiles(baseDir, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            // 检查文件名是否以SenparcEntities_SqlServer.cs结尾  
            if (fileName.EndsWith("SenparcEntities_SqlServer.cs"))
            {
                var newFileName = fileName.Replace("SqlServer", replacement);
                var newFilePath = Path.Combine(Path.GetDirectoryName(file), newFileName);

                // 读取原文件内容  
                var content = File.ReadAllText(file);

                // 替换内容中的SqlServer  
                var newContent = content.Replace("SqlServer", replacement);

                // 写入新文件  
                File.WriteAllText(newFilePath, newContent);

                // 输出新建文件的路径  
                Console.WriteLine($"New file created: {newFilePath}");
            }
        }
    }
}
