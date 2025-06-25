using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

[Generator]
public class RequestCodeGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // 不需要特殊初始化
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            string senparcEntitiesContent = GetFileContent(context, "Template_XncfNameSenparcEntities.cs");
            string colorContent = GetFileContent(context, "Color.cs");
            string colorDtoContent = GetFileContent(context, "ColorDto.cs");
            string colorServiceContent = GetFileContent(context, "ColorService.cs");

            var template = @$"
## Database EntityFramework DbContext class sample
File Name: Template_XncfNameSenparcEntities.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
{senparcEntitiesContent}
```

## Database Entity class sample
File Name: Color.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
Code:
```csharp
{colorContent}
```

## Database Entity DTO class sample
File Name: ColorDto.cs
File Path: <ModuleRootPath>/Domain/Models/DatabaseModel/Dto
Code:
```csharp
{colorDtoContent}
```

## Service class sample
File Name: Template_XncfNameService.cs
File Path: <ModuleRootPath>/Domain/Services
Code:
```csharp
{colorServiceContent}
```
";

            GenerateResponsePartialClass(context, template);
        }
        catch (System.Exception ex)
        {
            // 在编译时生成诊断信息
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    "RCG001",
                    "RequestCodeGenerator Error",
                    "Error in RequestCodeGenerator: {0}",
                    "RequestCodeGenerator",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None,
                ex.Message);

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 获取文件内容
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private string GetFileContent(GeneratorExecutionContext context, string fileName)
    {
        // 查找 fileName 文件
        var requestFile = context.AdditionalFiles
            .FirstOrDefault(file => file.Path.EndsWith(fileName));

        if (requestFile == null)
        {
            // 如果没有找到 AdditionalFiles 中的 Request.cs，尝试从 SourceTexts 中查找
            var requestSyntaxTree = context.Compilation.SyntaxTrees
                .FirstOrDefault(tree => tree.FilePath.EndsWith(fileName));

            if (requestSyntaxTree != null)
            {
                GenerateResponsePartialClass(context, requestSyntaxTree.GetText().ToString());
            }
            return null;
        }

        // 读取 Request.cs 的内容
        string requestContent = requestFile.GetText(context.CancellationToken)?.ToString() ?? "";
        return requestContent;
    }

    private void GenerateResponsePartialClass(GeneratorExecutionContext context, string requestContent)
    {
        // 转义字符串内容以便在 C# 代码中使用
        var escapedContent = EscapeStringLiteral(requestContent);

        var source = $@"
namespace Senparc.Xncf.XncfBuilder.OHS.Local
{{ 
public partial class BuildXncfAppService
{{
    public const string BackendTemplate = @""{escapedContent}"";
}}
}}";

        // 将生成的代码添加到编译中
        context.AddSource("BuildXncfAppService.Generated.cs", SourceText.From(source, Encoding.UTF8));
    }

    private string EscapeStringLiteral(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        // 对于 verbatim string literal (@"..."), 我们只需要转义双引号
        return input.Replace("\"", "\"\"");
    }
}