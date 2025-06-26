//using Microsoft.CodeAnalysis.Text;
//using Microsoft.CodeAnalysis;
//using System.Collections.Immutable;
//using System.Text;

//[Generator]
//public class RequestCodeGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {
//        //var compilation = GetFileContent(context, fileName);

//        try
//        {
//            var senparcEntitiesContent = GetFileContent(context, "Template_XncfNameSenparcEntities.cs");
//            var colorContent = GetFileContent(context, "Color.cs");
//            var colorDtoContent = GetFileContent(context, "ColorDto.cs");
//            var colorServiceContent = GetFileContent(context, "ColorService.cs");

//            #region Backend Template

//            var backendTemplate = @$"
//        ## Database EntityFramework DbContext class sample
//        File Name: Template_XncfNameSenparcEntities.cs
//        File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
//        Code:
//        ```csharp
//        {senparcEntitiesContent}
//        ```

//        ## Database Entity class sample
//        File Name: Color.cs
//        File Path: <ModuleRootPath>/Domain/Models/DatabaseModel
//        Code:
//        ```csharp
//        {colorContent}
//        ```

//        ## Database Entity DTO class sample
//        File Name: ColorDto.cs
//        File Path: <ModuleRootPath>/Domain/Models/DatabaseModel/Dto
//        Code:
//        ```csharp
//        {colorDtoContent}
//        ```

//        ## Service class sample
//        File Name: Template_XncfNameService.cs
//        File Path: <ModuleRootPath>/Domain/Services
//        Code:
//        ```csharp
//        {colorServiceContent}
//        ```
//        ";
//            // 转义字符串内容以便在 C# 代码中使用
//            //var escapedBackendContent = EscapeStringLiteral(backendTemplate);
//            #endregion

//            #region Frontend Template


//            string databaseSampleIndexPageContent = GetFileContent(context, "DatabaseSampleIndex.cshtml");
//            string databaseSampleIndexPageCsContent = GetFileContent(context, "DatabaseSampleIndex.cshtml.cs");
//            string databaseSampleIndexJsContent = GetFileContent(context, "databaseSampleIndex.js");
//            string databaseSampleIndexCssContent = GetFileContent(context, "databaseSampleIndex.css");

//            var frontendTemplate = @$"
//        ## Page UI sample (front-end)
//        File Name: DatabaseSampleIndex.cshtml
//        File Path: <ModuleRootPath>/Areas/Admin/Pages/Template_XncfName
//        Code:
//        ```razorpage
//        {databaseSampleIndexPageContent}
//        ```

//        ## Page UI sample (back-end)
//        File Name: DatabaseSampleIndex.cshtml.cs
//        File Path: <ModuleRootPath>/Areas/Admin/Pages/Template_XncfName
//        Code:
//        ```csharp
//        {databaseSampleIndexPageCsContent}
//        ```

//        ## Page JavaScript file sample
//        File Name: databaseSampleIndex.js
//        File Path: <ModuleRootPath>/wwwroot/js/Admin/Template_XncfName
//        Code:
//        ```javascript
//        {databaseSampleIndexJsContent}
//        ```

//        ## Page CSS file sample
//        File Name: databaseSampleIndex.css
//        File Path: <ModuleRootPath>/wwwroot/css/Admin/Template_XncfName
//        Code:
//        ```css
//        {databaseSampleIndexCssContent}
//        ```
//        ";

//            //var escapedFrontendContent = EscapeStringLiteral(frontendTemplate);

//            #endregion

//            var requestFiles = context.AdditionalTextsProvider
//          .Where(file => file.Path.EndsWith(".nothing"))
//          .Select((file, cancellationToken) => file.GetText(cancellationToken)?.ToString() ?? "");

//            // 组合编译信息和文件内容
//            IncrementalValueProvider<(Compilation compilation, ImmutableArray<string> requestContents)> compilation = context.CompilationProvider.Combine(requestFiles.Collect());

//            // 注册源代码生成
//            context.RegisterSourceOutput(compilation, (ctx, source) =>
//            {
//                GenerateResponsePartialClass(ctx, backendTemplate, frontendTemplate, "BuildXncfAppService.cs");
//            });


//            //GenerateResponsePartialClass(context, escapedBackendContent, escapedFrontendContent, "BuildXncfAppService.cs");
//        }
//        catch (System.Exception ex)
//        {
//            // 在编译时生成诊断信息
//            var diagnostic = Diagnostic.Create(
//                new DiagnosticDescriptor(
//                    "RCG001",
//                    "RequestCodeGenerator Error",
//                    "Error in RequestCodeGenerator: {0}",
//                    "RequestCodeGenerator",
//                    DiagnosticSeverity.Warning,
//                    true),
//                Location.None,
//                ex.Message);

//            //context.ReportDiagnostic(diagnostic);
//        }


//        //// 注册源代码生成
//        //context.RegisterSourceOutput(compilation, GenerateCode);
//    }

//    private string temp = "// run";

//    private string GetFileContent(IncrementalGeneratorInitializationContext context, string fileName)
//    {
//        string fileConent = null;


//        // 获取 AdditionalFiles 中的 Request.cs
//        var requestFiles = context.AdditionalTextsProvider
//            .Where(file => file.Path.EndsWith(fileName))
//            .Select((file, cancellationToken) =>
//            {
//                temp += "//" + fileName + " "
//;                fileConent = file.GetText(cancellationToken)?.ToString() ?? "";
//                return fileConent;
//            });

//        IncrementalValueProvider<(Compilation compilation, ImmutableArray<string> requestContents)> compilation = context.CompilationProvider.Combine(requestFiles.Collect());

//        return fileConent;


//        //// 组合编译信息和文件内容
//        //




//        //var results = compilation.Select(z => z.compilation);

//        //    // 注册源代码生成
//        //context.RegisterSourceOutput(compilation, (ctx, source) =>
//        //{
//        //    //temp += "in RegisterSourceOutput:";
//        //    //temp += fileName + "  ";

//        //    if (source.requestContents.Length > 0)
//        //    {
//        //        string requestContent = source.requestContents[0];


//        //        //temp += requestContent + " / ";


//        //        //TODO: encode

//        //        //fileConent += "// source.requestContents count:" + source.requestContents.Count();

//        //        fileConent += requestContent;
//        //        //Console.WriteLine($">>>>>>>>>>> {fileName}：{fileConent}");
//        //    }
//        //    else
//        //    {
//        //        //temp += "nothing";
//        //        fileConent += "nothing";
//        //        // TODO...
//        //    }
//        //GenerateCode(ctx, source, fileName);
//    //});

//        //Console.WriteLine($"<<<<<<<<<<<<");
            
//        return fileConent;
//    }



////private void GenerateCode(SourceProductionContext context, source, string fileName)
////{
////    var (compilation, requestContents) = input;

////    try
////    {
////        // 如果没有从 AdditionalFiles 找到，尝试从编译中查找
////        string requestContent = "";
////        if (requestContents.Length > 0)
////        {
////            requestContent = requestContents[0];
////        }
////        else
////        {
////            var requestSyntaxTree = compilation.SyntaxTrees
////                .FirstOrDefault(tree => tree.FilePath.EndsWith(fileName));

////            if (requestSyntaxTree != null)
////            {
////                requestContent = requestSyntaxTree.GetText().ToString();
////            }
////        }

////        if (!string.IsNullOrEmpty(requestContent))
////        {
////            GenerateResponsePartialClass(context, requestContent);
////        }
////    }
////    catch (System.Exception ex)
////    {
////        // 报告诊断信息
////        var diagnostic = Diagnostic.Create(
////            new DiagnosticDescriptor(
////                "RCG001",
////                "RequestCodeGenerator Error",
////                "Error in RequestCodeGenerator: {0}",
////                "RequestCodeGenerator",
////                DiagnosticSeverity.Warning,
////                true),
////            Location.None,
////            ex.Message);

////        context.ReportDiagnostic(diagnostic);
////    }
////}

//private void GenerateResponsePartialClass(SourceProductionContext context, string backendTemplate, string frontendTemplate, string fileName)
//{
//    // 转义字符串内容
//    var escapedBackendContent = EscapeStringLiteral(backendTemplate);
//    var escapedFrontendContent = EscapeStringLiteral(frontendTemplate);

//    var source = $@"
//        namespace Senparc.Xncf.XncfBuilder.OHS.Local
//        {{ 
//            public partial class BuildXncfAppService
//            {{
////{temp}
//                public const string BackendTemplate = @""{escapedBackendContent}"";
//                public const string FrontendTemplate = @""{escapedFrontendContent}"";
//            }}
//        }}";


//    // 添加生成的源代码
//    context.AddSource(fileName.Replace(".cs", ".Generated.cs"), SourceText.From(source, Encoding.UTF8));
//}

//private string EscapeStringLiteral(string input)
//{
//    if (string.IsNullOrEmpty(input))
//        return "";

//    // 对于 verbatim string literal (@"..."), 我们只需要转义双引号
//    return input.Replace("\"", "\"\"");
//}
//}


////using Microsoft.CodeAnalysis;
////using Microsoft.CodeAnalysis.Text;
////using System.Linq;
////using System.Text;

////[Generator]
////public class RequestCodeGenerator : ISourceGenerator
////{
////    public void Initialize(GeneratorInitializationContext context)
////    {
////        // 不需要特殊初始化
////    }

////    public void Execute(GeneratorExecutionContext context)
////    {
////       
////    }

////    /// <summary>
////    /// 获取文件内容
////    /// </summary>
////    /// <param name="context"></param>
////    /// <returns></returns>
////    private string GetFileContent(GeneratorExecutionContext context, string fileName)
////    {
////        // 查找 fileName 文件
////        var requestFile = context.AdditionalFiles
////            .FirstOrDefault(file => file.Path.EndsWith(fileName));//TODO: 需要考虑文件名只部分匹配的问题

////        if (requestFile == null)
////        {
////            // 如果没有找到 AdditionalFiles 中的 Request.cs，尝试从 SourceTexts 中查找
////            var requestSyntaxTree = context.Compilation.SyntaxTrees
////                .FirstOrDefault(tree => tree.FilePath.EndsWith(fileName));

////            if (requestSyntaxTree != null)
////            {
////                GenerateResponsePartialClass(context, requestSyntaxTree.GetText().ToString(), fileName);
////            }
////            return null;
////        }

////        // 读取 Request.cs 的内容
////        string requestContent = requestFile.GetText(context.CancellationToken)?.ToString() ?? "";
////        return requestContent;
////    }

////    private void GenerateResponsePartialClass(GeneratorExecutionContext context, string source, string fileName)
////    {
////        // 将生成的代码添加到编译中
////        context.AddSource(fileName.Replace(".cs", ".Generated.cs")/*"BuildXncfAppService.Generated.cs"*/, SourceText.From(source, Encoding.UTF8));
////    }

////    private string EscapeStringLiteral(string input)
////    {
////        if (string.IsNullOrEmpty(input))
////            return "";

////        // 对于 verbatim string literal (@"..."), 我们只需要转义双引号
////        return input.Replace("\"", "\"\"");
////    }
////}