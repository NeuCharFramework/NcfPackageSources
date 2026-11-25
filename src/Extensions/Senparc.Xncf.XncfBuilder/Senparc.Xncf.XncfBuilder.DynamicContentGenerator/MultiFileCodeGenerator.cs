/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MultiFileCodeGenerator.cs
    File description: MultiFileCodeGenerator related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260705
    Change description: v0.0.1 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.0.3 Localized XNCF Builder tools and template surfaces

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
/// <summary>
/// Generates a partial application-service class containing configured template files
/// and lookup helpers from Roslyn additional text inputs.
/// </summary>
public class MultiFileCodeGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Embedded fallback configuration used when the project does not provide a generation-config.json file.
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

    /// <inheritdoc/>
    /// <remarks>
    /// The generator collects all additional texts once per compilation and emits either
    /// the configured template class or a minimal fallback source when configuration is invalid.
    /// </remarks>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var allAdditional = context.AdditionalTextsProvider.Collect();
        context.RegisterSourceOutput(allAdditional, GenerateFromAllAdditionalTexts);
    }

    /// <summary>
    /// Deserializes a generation configuration without allowing malformed input to fail compilation.
    /// </summary>
    /// <param name="json">The JSON configuration text.</param>
    /// <returns>The parsed configuration, or <see langword="null"/> for blank or invalid JSON.</returns>
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

    /// <summary>
    /// Finds and parses the optional <c>generation-config.json</c> additional file.
    /// </summary>
    /// <param name="additionalFiles">The additional files supplied by the current compilation.</param>
    /// <returns>The parsed configuration, or <see langword="null"/> when no usable file exists.</returns>
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

    /// <summary>
    /// Selects the external or embedded configuration, loads configured files, and emits generated source.
    /// </summary>
    /// <param name="context">The Roslyn output context used to report diagnostics and add source.</param>
    /// <param name="additionalFiles">The additional text files available to the generator.</param>
    /// <remarks>
    /// Any configuration or generation failure is reported as a warning and produces a minimal
    /// partial class so downstream compilation can continue with a predictable fallback.
    /// </remarks>
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

    /// <summary>
    /// Emits the smallest valid partial class when configured generation cannot be completed.
    /// </summary>
    /// <param name="context">The Roslyn output context receiving the fallback source.</param>
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

    /// <summary>
    /// Resolves one configured file against the additional-text paths and reads its source text.
    /// </summary>
    /// <param name="filePath">The configured relative or file-name path.</param>
    /// <param name="additionalFiles">The additional files available to search.</param>
    /// <returns>The file text, or an empty string when no matching file is present.</returns>
    private string GetFileContent(string filePath, ImmutableArray<AdditionalText> additionalFiles)
    {
        // Match exact suffixes first, then fall back to the configured file name.
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

    /// <summary>
    /// Builds the configured partial class source, including constants, metadata, and helpers.
    /// </summary>
    /// <param name="context">The Roslyn output context receiving the generated source.</param>
    /// <param name="config">The output namespace, class, grouping, and feature configuration.</param>
    /// <param name="files">The configured files after content resolution.</param>
    private void GenerateMultiFileClass(SourceProductionContext context, FileGenerationConfig config, List<ProcessedFile> files)
    {
        var sb = new StringBuilder();

        // Emit the standard generated-file header before adding template content.
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

        // Keep the generated namespace aligned with the configured output namespace.
        sb.AppendLine($"namespace {config.OutputNamespace}");
        sb.AppendLine("{");

        // Document the generated partial class so its constants are discoverable in IDEs.
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Auto-generated NCF template constants containing all template file content.");
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

        // Select grouped or flat constant emission according to the configuration.
        if (config.Grouping.ByType)
        {
            GenerateGroupedConstants(sb, files, config);
        }
        else
        {
            // Flat mode preserves the configured file order.
            foreach (var file in files)
            {
                GenerateConstantForFile(sb, file);
            }
        }

        // Type constants are optional because older consumers only require file constants.
        if (config.Grouping.GenerateTypeConstants)
        {
            GenerateTypeConstants(sb, files);
        }

        // File metadata is emitted only when requested by the configuration.
        if (config.Options.IncludeFileInfo)
        {
            GenerateFileInfoArray(sb, files);
        }

        // Helper methods are optional for consumers that only need raw constants.
        if (config.Options.GenerateHelperMethods)
        {
            GenerateHelperMethods(sb, files);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource($"{config.OutputClassName}.Generated.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    /// <summary>
    /// Emits template constants grouped by their configured template type.
    /// </summary>
    /// <param name="sb">The generated-source builder.</param>
    /// <param name="files">The resolved template files.</param>
    /// <param name="config">The generation configuration.</param>
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

    /// <summary>
    /// Emits one XML-documented string constant containing a template file.
    /// </summary>
    /// <param name="sb">The generated-source builder.</param>
    /// <param name="file">The resolved file metadata and content.</param>
    private void GenerateConstantForFile(StringBuilder sb, ProcessedFile file)
    {
        var escapedContent = EscapeStringLiteral(file.Content);

        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// {file.Description}");
        sb.AppendLine($"        /// Type: {file.Type}");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine($"        public const string {file.ConstantName} = @\"{escapedContent}\";");
        sb.AppendLine();
    }

    /// <summary>
    /// Emits constants for the distinct template types represented by the input files.
    /// </summary>
    /// <param name="sb">The generated-source builder.</param>
    /// <param name="files">The resolved template files.</param>
    private void GenerateTypeConstants(StringBuilder sb, List<ProcessedFile> files)
    {
        var types = files.Select(f => f.Type).Distinct().ToList();

        sb.AppendLine("        #region Template Types");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Template type constants.");
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

    /// <summary>
    /// Emits the generated template metadata array and its record type.
    /// </summary>
    /// <param name="sb">The generated-source builder.</param>
    /// <param name="files">The resolved template files.</param>
    private void GenerateFileInfoArray(StringBuilder sb, List<ProcessedFile> files)
    {
        sb.AppendLine("        #region File Information");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Information for all template files.");
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
        sb.AppendLine("        /// Template file information structure.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public record TemplateFileInfo(string Name, string Description, string Type, string Path, string Content);");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
    }

    /// <summary>
    /// Emits helper methods for filtering, retrieving, and enumerating generated templates.
    /// </summary>
    /// <param name="sb">The generated-source builder.</param>
    /// <param name="files">The resolved template files; used to determine emitted metadata availability.</param>
    private void GenerateHelperMethods(StringBuilder sb, List<ProcessedFile> files)
    {
        sb.AppendLine("        #region Helper Methods");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Returns generated template metadata whose type exactly matches the requested value.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"templateType\">The configured template type to match.</param>");
        sb.AppendLine("        /// <returns>All matching template metadata entries, or an empty array when none match.</returns>");
        sb.AppendLine("        public static TemplateFileInfo[] GetTemplatesByType(string templateType)");
        sb.AppendLine("        {");
        sb.AppendLine("            return AllTemplateFiles.Where(f => f.Type == templateType).ToArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Returns the generated content for one template constant name.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"templateName\">The generated metadata name to locate.</param>");
        sb.AppendLine("        /// <returns>The template content, or an empty string when no name matches.</returns>");
        sb.AppendLine("        public static string GetTemplateContent(string templateName)");
        sb.AppendLine("        {");
        sb.AppendLine("            return AllTemplateFiles.FirstOrDefault(f => f.Name == templateName)?.Content ?? string.Empty;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Returns the distinct template types represented by generated metadata.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <returns>A de-duplicated array of configured template types.</returns>");
        sb.AppendLine("        public static string[] GetAllTemplateTypes()");
        sb.AppendLine("        {");
        sb.AppendLine("            return AllTemplateFiles.Select(f => f.Type).Distinct().ToArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    /// <summary>
    /// Emits the legacy default-code representation for all additional C# files.
    /// </summary>
    /// <param name="context">The Roslyn output context receiving the generated source.</param>
    /// <param name="additionalFiles">The additional files to inspect.</param>
    private void GenerateDefaultCode(SourceProductionContext context, ImmutableArray<AdditionalText> additionalFiles)
    {
        // Preserve the legacy default-code path for projects that still use it.
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

    /// <summary>
    /// Escapes quotation marks for inclusion in a verbatim C# string literal.
    /// </summary>
    /// <param name="input">The source text to escape.</param>
    /// <returns>The escaped text, or an empty string for null or empty input.</returns>
    private string EscapeStringLiteral(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input.Replace("\"", "\"\"");
    }

    /// <summary>
    /// Reports a non-fatal generator warning with the supplied diagnostic identifier and message.
    /// </summary>
    /// <param name="context">The Roslyn output context used to publish the diagnostic.</param>
    /// <param name="id">The stable diagnostic identifier.</param>
    /// <param name="message">The diagnostic text shown to the compiler consumer.</param>
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
