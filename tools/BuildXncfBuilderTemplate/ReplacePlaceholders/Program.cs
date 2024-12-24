using System;
using System.IO;
using System.Text;

string directoryPath;

// 检查传入的参数  
if (args.Length != 1)
{
    Console.WriteLine("Usage: ReplacePlaceholders <directory>");
    directoryPath = "X:\\Senparc 项目\\NeuCharFramework\\NcfPackageSources\\src\\Extensions\\Senparc.Xncf.XncfBuilder\\Senparc.Xncf.XncfBuilder.Template";
}
else
{
    directoryPath = args[0];
}

if (!Directory.Exists(directoryPath))
{
    Console.WriteLine($"The directory '{directoryPath}' does not exist.");
    return;
}

// 开始替换占位符  
ReplacePlaceholdersInDirectory(directoryPath);
Console.WriteLine("Replacement completed.");

void ReplacePlaceholdersInDirectory(string directoryPath)
{
    foreach (var file in Directory.GetFiles(directoryPath))
    {
        ReplacePlaceholdersInFile(file);
    }

    foreach (var subDirectory in Directory.GetDirectories(directoryPath))
    {
        ReplacePlaceholdersInDirectory(subDirectory);
    }

    RenameFilesAndDirectories(directoryPath);
}

void ReplacePlaceholdersInFile(string filePath)
{
    // 使用默认编码读取文件内容  
    string content;
    string newContent;
    Encoding encoding;

    // 读取文件内容和编码  
    using (var reader = new StreamReader(filePath, true))
    {
        content = reader.ReadToEnd();
        encoding = reader.CurrentEncoding;
    }

    newContent = content.Replace("ORGPLACEHOLDER", "Template_OrgName")
                        .Replace("MODPLACEHOLDER", "Template_XncfName");

    // 只有当内容发生变化时才写入文件  
    if (!content.Equals(newContent))
    {
        // 使用读取时的编码写入文件内容  
        using (var writer = new StreamWriter(filePath, false, encoding))
        {
            writer.Write(newContent);
        }
    }
}

void RenameFilesAndDirectories(string directoryPath)
{
    foreach (var filePath in Directory.GetFiles(directoryPath))
    {
        string newFilePath = filePath.Replace("ORGPLACEHOLDER", "Template_OrgName")
                                     .Replace("MODPLACEHOLDER", "Template_XncfName");

        if (newFilePath != filePath)
        {
            File.Move(filePath, newFilePath);
            Console.WriteLine($"文件已更新：{filePath} -> {newFilePath}");
        }
    }

    foreach (var subDirectoryPath in Directory.GetDirectories(directoryPath))
    {
        string newSubDirectoryPath = subDirectoryPath.Replace("ORGPLACEHOLDER", "Template_OrgName")
                                                     .Replace("MODPLACEHOLDER", "Template_XncfName");

        if (newSubDirectoryPath != subDirectoryPath)
        {
            Directory.Move(subDirectoryPath, newSubDirectoryPath);
            Console.WriteLine($"目录已更新：{subDirectoryPath} -> {newSubDirectoryPath}");
        }

        RenameFilesAndDirectories(newSubDirectoryPath);
    }
}
