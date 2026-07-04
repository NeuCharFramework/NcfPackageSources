/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MultiFileCodeGenerator.cs
    文件功能描述：MultiFileCodeGenerator 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System;
using System.Threading;

public class FileGenerationConfig
{
    public string OutputNamespace { get; set; } = "Senparc.Xncf.XncfBuilder.OHS.Local";
    public string OutputClassName { get; set; } = "BuildXncfAppService";
    public FileItem[] Files { get; set; } = Array.Empty<FileItem>();
    public GroupingOptions Grouping { get; set; } = new GroupingOptions();
    public GenerationOptions Options { get; set; } = new GenerationOptions();
}

public class FileItem
{
    public string Path { get; set; } = "";
    public string ConstantName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "code";
}

public class GroupingOptions
{
    public bool ByType { get; set; } = false;
    public bool GenerateTypeConstants { get; set; } = false;
}

public class GenerationOptions
{
    public bool IncludeFileInfo { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
    public bool GenerateHelperMethods { get; set; } = true;
}

[Generator]
public class MultiFileCodeGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 与 generation-config.json 保持一致；AdditionalFiles 未带上配置或反序列化失败时使用，
    /// 避免 RegisterSourceOutput 不执行导致缺少 FrontendTemplate / BackendTemplate。
    /// </summary>
    private const string EmbeddedDefaultGenerationConfigJson = """
{
  "outputNamespace": "Senparc.Xncf.XncfBuilder.OHS.Local",
  "outputClassName": "BuildXncfAppService",
  "files": [
    {
      "path": "Request.cs",
      "constantName": "RequestCode",
      "description": "请求类代码",
      "type": "code"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/Domain/Models/DatabaseModel/Template_XncfNameSenparcEntities.cs",
      "constantName": "SenparcEntitiesTemplate",
      "description": "Senparc实体类模板",
      "type": "backend_template"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/Domain/Models/DatabaseModel/Color.cs",
      "constantName": "ColorModelTemplate",
      "description": "颜色模型模板",
      "type": "backend_template"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/Domain/Models/DatabaseModel/Dto/ColorDto.cs",
      "constantName": "ColorDtoTemplate",
      "description": "颜色DTO模板",
      "type": "backend_template"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/Domain/Services/ColorService.cs",
      "constantName": "ColorServiceTemplate",
      "description": "颜色服务模板",
      "type": "backend_template"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/Areas/Admin/Pages/Template_XncfName/DatabaseSampleIndex.cshtml",
      "constantName": "DatabaseSampleIndexViewTemplate",
      "description": "数据库示例索引页面视图模板",
      "type": "frontend_template"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/Areas/Admin/Pages/Template_XncfName/DatabaseSampleIndex.cshtml.cs",
      "constantName": "DatabaseSampleIndexCodeBehindTemplate",
      "description": "数据库示例索引页面代码后置模板",
      "type": "frontend_template"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/wwwroot/js/Admin/Template_XncfName/databaseSampleIndex.js",
      "constantName": "DatabaseSampleIndexJsTemplate",
      "description": "数据库示例索引页面JavaScript模板",
      "type": "frontend_script"
    },
    {
      "path": "../Senparc.Xncf.XncfBuilder.Template/templates/template1/wwwroot/css/Admin/Template_XncfName/databaseSampleIndex.css",
      "constantName": "DatabaseSampleIndexCssTemplate",
      "description": "数据库示例索引页面CSS模板",
      "type": "frontend_style"
    }
  ],
  "grouping": {
    "byType": true,
    "generateTypeConstants": true
  },
  "options": {
    "includeFileInfo": true,
    "includeMetadata": true,
    "generateHelperMethods": true
  }
}
""";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var allAdditional = context.AdditionalTextsProvider.Collect();
        context.RegisterSourceOutput(allAdditional, GenerateFromAllAdditionalTexts);
    }

    private static FileGenerationConfig? DeserializeConfig(string? json)
    {
        if (json is null || string.IsNullOrWhiteSpace(json))
            return null;

        var normalizedJson = json;
        try
        {
            return JsonSerializer.Deserialize<FileGenerationConfig>(normalizedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static FileGenerationConfig? TryLoadConfigFromAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles)
    {
        var configFile = additionalFiles.FirstOrDefault(f =>
            f.Path.EndsWith("generation-config.json", StringComparison.OrdinalIgnoreCase));
        if (configFile == null)
            return null;
        try
        {
            return DeserializeConfig(configFile.GetText(CancellationToken.None)?.ToString());
        }
        catch
        {
            return null;
        }
    }

    private void GenerateFromAllAdditionalTexts(SourceProductionContext context, ImmutableArray<AdditionalText> additionalFiles)
    {
        var config = TryLoadConfigFromAdditionalFiles(additionalFiles)
                     ?? DeserializeConfig(EmbeddedDefaultGenerationConfigJson);
        if (config == null)
        {
            ReportError(context, "MFG002", "MultiFileCodeGenerator: could not deserialize embedded default generation config.");
            EmitMinimalBuildXncfAppServicePartial(context);
            return;
        }

        try
        {
            var processedFiles = new List<ProcessedFile>();
            foreach (var fileItem in config.Files ?? Array.Empty<FileItem>())
            {
                var content = GetFileContent(fileItem.Path, additionalFiles) ?? "";
                processedFiles.Add(new ProcessedFile
                {
                    ConstantName = fileItem.ConstantName,
                    Content = content,
                    Description = fileItem.Description,
                    Type = fileItem.Type,
                    Path = fileItem.Path
                });
            }

            GenerateMultiFileClass(context, config, processedFiles);
        }
        catch (Exception ex)
        {
            ReportError(context, "MFG001", $"Error in MultiFileCodeGenerator: {ex.Message}");
            EmitMinimalBuildXncfAppServicePartial(context);
        }
    }

    /// <summary>仅保证编译通过；模板内容为空。</summary>
    private static void EmitMinimalBuildXncfAppServicePartial(SourceProductionContext context)
    {
        const string minimal = """
// <auto-generated />
using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    public partial class BuildXncfAppService
    {
        public const string BackendTemplate = "";
        public const string FrontendTemplate = "";
    }
}
""";
        context.AddSource("BuildXncfAppService.Generated.cs", SourceText.From(minimal, Encoding.UTF8));
    }

    private string GetFileContent(string filePath, ImmutableArray<AdditionalText> additionalFiles)
    {
        // 首先尝试从 AdditionalFiles 中查找
        var file = additionalFiles.FirstOrDefault(f =>
            f.Path.EndsWith(filePath) ||
            f.Path.Replace('\\', '/').EndsWith(filePath.Replace('\\', '/')) ||
            Path.GetFileName(f.Path).Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase)
        );

        if (file != null)
        {
            return file.GetText()?.ToString() ?? "";
        }

        return "";
    }

    private void GenerateMultiFileClass(SourceProductionContext context, FileGenerationConfig config, List<ProcessedFile> files)
    {
        var sb = new StringBuilder();

        // 添加文件头注释
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This file was generated by MultiFileCodeGenerator");
        sb.AppendLine("// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("#nullable enable annotations");
        sb.AppendLine();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();

        // 添加命名空间
        sb.AppendLine($"namespace {config.OutputNamespace}");
        sb.AppendLine("{");

        // 添加类定义
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// 自动生成的NCF模板常量类，包含所有模板文件的内容");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public partial class {config.OutputClassName}");
        sb.AppendLine("    {");


        sb.AppendLine(@"public const string BackendTemplate =  @$""
## Database EntityFramework DbContext class sample
File Name: Template_XncfNameSenparcEntities.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
{SenparcEntitiesTemplate}
```

## Database Entity class sample
File Name: Color.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
{ColorModelTemplate}
```

## Database Entity DTO class sample
File Name: ColorDto.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel/Dto
Code:
```csharp
{ColorDtoTemplate}
```

## Service class sample
File Name: Template_XncfNameService.cs
File Path: <ModuleRootPath>/Domain/Services
Code:
```csharp
{ColorServiceTemplate}
```
"";
");


        sb.AppendLine(@"public const string FrontendTemplate = @$""
## Page UI sample (front-end)
File Name: DatabaseSampleIndex.cshtml
File Path: < ModuleRootPath >/ Areas / Admin / Pages / Template_XncfName
Code:
```razorpage
{DatabaseSampleIndexViewTemplate}
```

## Page UI sample (back-end)
File Name: DatabaseSampleIndex.cshtml.cs
File Path: < ModuleRootPath >/ Areas / Admin / Pages / Template_XncfName
Code:
```csharp
{DatabaseSampleIndexCodeBehindTemplate}
```

## Page JavaScript file sample
File Name: databaseSampleIndex.js
File Path: < ModuleRootPath >/ wwwroot / js / Admin / Template_XncfName
Code:
```javascript
{DatabaseSampleIndexJsTemplate}
```

## Page CSS file sample
File Name: databaseSampleIndex.css
File Path: < ModuleRootPath >/ wwwroot / css / Admin / Template_XncfName
Code:
```css
{DatabaseSampleIndexCssTemplate}
```
"";
");

        // 按类型分组生成常量
        if (config.Grouping.ByType)
        {
            GenerateGroupedConstants(sb, files, config);
        }
        else
        {
            // 生成每个文件的常量
            foreach (var file in files)
            {
                GenerateConstantForFile(sb, file);
            }
        }

        // 生成类型常量
        if (config.Grouping.GenerateTypeConstants)
        {
            GenerateTypeConstants(sb, files);
        }

        // 生成文件信息数组
        if (config.Options.IncludeFileInfo)
        {
            GenerateFileInfoArray(sb, files);
        }

        // 生成辅助方法
        if (config.Options.GenerateHelperMethods)
        {
            GenerateHelperMethods(sb, files);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource($"{config.OutputClassName}.Generated.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private void GenerateGroupedConstants(StringBuilder sb, List<ProcessedFile> files, FileGenerationConfig config)
    {
        var groupedFiles = files.GroupBy(f => f.Type).ToList();

        foreach (var group in groupedFiles)
        {
            sb.AppendLine();
            sb.AppendLine($"        #region {group.Key.ToUpperInvariant()} Templates");
            sb.AppendLine();

            foreach (var file in group)
            {
                GenerateConstantForFile(sb, file);
            }

            sb.AppendLine($"        #endregion");
        }
    }

    private void GenerateConstantForFile(StringBuilder sb, ProcessedFile file)
    {
        var escapedContent = EscapeStringLiteral(file.Content);

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// {file.Description}");
        sb.AppendLine($"        /// 类型: {file.Type}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public const string {file.ConstantName} = @\"{escapedContent}\";");
        sb.AppendLine();
    }

    private void GenerateTypeConstants(StringBuilder sb, List<ProcessedFile> files)
    {
        var types = files.Select(f => f.Type).Distinct().ToList();

        sb.AppendLine("        #region Template Types");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 模板类型常量");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static class TemplateTypes");
        sb.AppendLine("        {");

        foreach (var type in types)
        {
            var constantName = type.ToUpperInvariant().Replace("_", "");
            sb.AppendLine($"            public const string {constantName} = \"{type}\";");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    private void GenerateFileInfoArray(StringBuilder sb, List<ProcessedFile> files)
    {
        sb.AppendLine("        #region File Information");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 所有模板文件信息");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static readonly TemplateFileInfo[] AllTemplateFiles = new TemplateFileInfo[]");
        sb.AppendLine("        {");

        foreach (var file in files)
        {
            sb.AppendLine($"            new TemplateFileInfo(\"{file.ConstantName}\", \"{file.Description}\", \"{file.Type}\", \"{file.Path}\", {file.ConstantName}),");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 模板文件信息结构");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public record TemplateFileInfo(string Name, string Description, string Type, string Path, string Content);");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    private void GenerateHelperMethods(StringBuilder sb, List<ProcessedFile> files)
    {
        sb.AppendLine("        #region Helper Methods");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 根据类型获取模板文件");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static TemplateFileInfo[] GetTemplatesByType(string templateType)");
        sb.AppendLine("        {");
        sb.AppendLine("            return AllTemplateFiles.Where(f => f.Type == templateType).ToArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 根据名称获取模板内容");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static string GetTemplateContent(string templateName)");
        sb.AppendLine("        {");
        sb.AppendLine("            return AllTemplateFiles.FirstOrDefault(f => f.Name == templateName)?.Content ?? string.Empty;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 获取所有模板类型");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static string[] GetAllTemplateTypes()");
        sb.AppendLine("        {");
        sb.AppendLine("            return AllTemplateFiles.Select(f => f.Type).Distinct().ToArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    private void GenerateDefaultCode(SourceProductionContext context, ImmutableArray<AdditionalText> additionalFiles)
    {
        // 默认行为：处理所有 .cs 文件
        var csFiles = additionalFiles.Where(f => f.Path.EndsWith(".cs")).ToList();

        if (!csFiles.Any())
            return;

        var sb = new StringBuilder();
        sb.AppendLine("namespace Senparc.Xncf.XncfBuilder.OHS.Local");
        sb.AppendLine("{");
        sb.AppendLine("    public partial class BuildXncfAppService");
        sb.AppendLine("    {");

        foreach (var file in csFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.Path);
            var content = file.GetText()?.ToString() ?? "";
            var escapedContent = EscapeStringLiteral(content);

            sb.AppendLine($"        public const string {fileName}Code = @\"{escapedContent}\";");
        }

        sb.AppendLine("public const string BackendTemplate = \"{escapedBackendContent}\";");
        sb.AppendLine("public const string FrontendTemplate = \"{escapedFrontendContent}\";");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("BuildXncfAppService.Default.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private string EscapeStringLiteral(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input.Replace("\"", "\"\"");
    }

    private void ReportError(SourceProductionContext context, string id, string message)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                "MultiFileCodeGenerator Error",
                message,
                "MultiFileCodeGenerator",
                DiagnosticSeverity.Warning,
                true),
            Location.None);

        context.ReportDiagnostic(diagnostic);
    }

    private class ProcessedFile
    {
        public string ConstantName { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
