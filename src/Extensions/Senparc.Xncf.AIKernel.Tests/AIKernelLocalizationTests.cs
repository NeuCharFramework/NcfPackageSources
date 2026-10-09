using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class AIKernelLocalizationTests
{
    [TestMethod]
    public async Task CompiledModulePartial_ExportsActualLocalizedFineTuningStrings()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AIKernelResource).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddLocalization();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AIKernelResource).Assembly);
        await using var application = builder.Build();
        using var scope = application.Services.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();
        var result = engine.GetView(null, "/Areas/Admin/Pages/Shared/_AIKernelLocalizationScripts.cshtml", false);
        Assert.IsTrue(result.Success, string.Join(", ", result.SearchedLocations ?? []));
        var original = CultureInfo.CurrentUICulture;
        try
        {
            foreach (var (culture, expected) in new[] { ("zh-CN", "本地模型微调"), ("en", "Local model fine-tuning") })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                using var writer = new StringWriter();
                var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
                var action = new ActionContext(context, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
                var viewContext = new ViewContext(action, result.View,
                    new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                    new TempDataDictionary(context, scope.ServiceProvider.GetRequiredService<ITempDataProvider>()),
                    writer, new HtmlHelperOptions());
                await result.View.RenderAsync(viewContext);
                var script = writer.ToString();
                const string prefix = "window.ncfI18n || {}, ";
                var start = script.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
                var end = script.IndexOf(");", start, StringComparison.Ordinal);
                using var translations = JsonDocument.Parse(script[start..end]);
                Assert.AreEqual(expected, translations.RootElement.GetProperty("AIKernel.FineTuning.Title").GetString());
                Assert.IsTrue(translations.RootElement.GetProperty("AIKernel.FineTuning.WorkerSetupEmpty").GetString()!.Length > 30);
                Assert.IsTrue(translations.RootElement.GetProperty("AIKernel.FineTuning.Guide.Title").GetString()!.Length > 0);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
