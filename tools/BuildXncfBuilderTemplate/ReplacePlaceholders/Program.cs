using System;
using System.IO;

string directoryPath;
// 检查传入的参数  
if (args.Length != 1)
{
    Console.WriteLine("Usage: ReplacePlaceholders <directory>");
    directoryPath = "X:\\Senparc 项目\\NeuCharFramework\\NcfPackageSources\\src\\Extensions\\Senparc.Xncf.XncfBuilder\\Senparc.Xncf.XncfBuilder.Template";
}

directoryPath = args[0];

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
    string content = File.ReadAllText(filePath);
    content = content.Replace("ORGPLACEHOLDER", "Template_OrgName")
                     .Replace("MODPLACEHOLDER", "Template_XncfName");
    File.WriteAllText(filePath, content);
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
        }
    }

    foreach (var subDirectoryPath in Directory.GetDirectories(directoryPath))
    {
        string newSubDirectoryPath = subDirectoryPath.Replace("ORGPLACEHOLDER", "Template_OrgName")
                                                     .Replace("MODPLACEHOLDER", "Template_XncfName");

        if (newSubDirectoryPath != subDirectoryPath)
        {
            Directory.Move(subDirectoryPath, newSubDirectoryPath);
        }

        RenameFilesAndDirectories(newSubDirectoryPath);
    }
}
