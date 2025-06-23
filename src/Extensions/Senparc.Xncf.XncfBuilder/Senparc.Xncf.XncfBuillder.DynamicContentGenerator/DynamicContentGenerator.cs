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
            var fileName = "ColorService.cs";
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
                return;
            }

            // 读取 Request.cs 的内容
            var requestContent = requestFile.GetText(context.CancellationToken)?.ToString() ?? "";

            GenerateResponsePartialClass(context, requestContent);
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