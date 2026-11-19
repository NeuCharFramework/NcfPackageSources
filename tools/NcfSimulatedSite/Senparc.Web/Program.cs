/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Program.cs
    File description: Program related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260705
    Change description: v0.21.6 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.21.7 Updated module behavior and related support.

    Modified by: Senparc - 20260724
    Change description: v0.34.0 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.34.1 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.35.0 Updated module behavior and related support.

    Modified by: Senparc - 20260812
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.36.0 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.36.1 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.39.8 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

//Legacy comment translated to English.
//Legacy comment translated to English.
//Legacy comment translated to English.
//Legacy comment translated to English.
//Legacy comment translated to English.
//Legacy comment translated to English.

using Senparc.CO2NET;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Database.SqlServer;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Senparc.Web.Controllers;
using Senparc.Ncf.XncfBase;
using Senparc.Web.Infrastructure.Database;
using Senparc.Web.Infrastructure.Security;

var databaseUpgradeOptions = DatabaseUpgradeCommandLineOptions.Parse(args);
void ReportDatabaseUpgradeProgress(string message)
{
    //Legacy comment translated to English.
    if (databaseUpgradeOptions.Enabled)
    {
        Console.WriteLine($"数据库升级：{message}");
    }
}

ReportDatabaseUpgradeProgress("正在创建应用宿主……");
var builder = WebApplication.CreateBuilder(databaseUpgradeOptions.HostArguments);
ReportDatabaseUpgradeProgress("应用构建器创建完成，正在注册 NCF 服务……");

//Legacy comment translated to English.
builder.AddNcf();
ReportDatabaseUpgradeProgress("NCF 服务注册完成，正在注册基础服务……");

//Legacy comment translated to English.
builder.AddServiceDefaults();

//Legacy comment translated to English.
builder.Services.Configure<CloudflareProtectOptions>(builder.Configuration.GetSection(CloudflareProtectOptions.SectionName));
builder.Services.AddSingleton<FixedWindowRateLimiter>();
ReportDatabaseUpgradeProgress("基础服务注册完成，正在注册 Dapr……");

// Keep the platform default TLS certificate validation. A global callback that
// accepts every certificate would make all outbound HTTPS requests vulnerable
// to man-in-the-middle attacks.

//Legacy comment translated to English.
builder.Services.AddDaprClient();
ReportDatabaseUpgradeProgress("服务注册完成，正在生成应用宿主……");

var app = builder.Build();
ReportDatabaseUpgradeProgress("应用宿主创建完成，正在注册 XNCF 模块……");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// A Sandbox NCF preview is exposed through /sandbox-preview/{sessionId}. The value is supplied
// only by the fixed Sandbox container workload; normal sites have no path-base at all. Applying
// it before NCF/static/routing middleware keeps links, redirects and WebSocket endpoints inside
// the authenticated reverse-proxy prefix instead of leaking to the main host root.
var xncfPreviewPathBase = Environment.GetEnvironmentVariable("NCF_XNCF_PREVIEW_PATH_BASE");
if (!string.IsNullOrWhiteSpace(xncfPreviewPathBase)
    && xncfPreviewPathBase.StartsWith('/')
    && !xncfPreviewPathBase.Contains("..", StringComparison.Ordinal)
    && !xncfPreviewPathBase.Contains('\\')
    && !xncfPreviewPathBase.Contains('\r')
    && !xncfPreviewPathBase.Contains('\n'))
{
    app.UsePathBase(xncfPreviewPathBase);
}

// Configure request localization (cookie first, then query/header providers)
var supportedCultures = LanguageController.SupportedCultures;
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures.ToArray())
    .AddSupportedUICultures(supportedCultures.ToArray())
    .AddInitialRequestCultureProvider(new CookieRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

//Legacy comment translated to English.
app.UseNcf<BySettingDatabaseConfiguration>(startBackgroundThreads: false);

//Legacy comment translated to English.
app.UseCloudflareProtect();
ReportDatabaseUpgradeProgress("XNCF 模块注册完成，正在检查数据库状态……");
/*  Legacy comment translated to English.

 *                

 *                  method                            |         description

 * -------------------------------------------------|-------------------------

 *  Legacy comment translated to English.

 *  UseNcf<SqlServerDatabaseConfiguration>()        |  use SQLServer database

 *  UseNcf<SqliteMemoryDatabaseConfiguration>()     |  use SQLite database

 *  UseNcf<MySqlDatabaseConfiguration>()            |  use MySQL database

 *  UseNcf<PostgreSQLDatabaseConfiguration>()       |  use PostgreSQL database

 *  UseNcf<OracleDatabaseConfiguration>()           |  use Oracle database（V12+）

 *  UseNcf<OracleDatabaseConfigurationForV11>()     |  use Oracle database（V11+）

 *  Legacy comment translated to English.

 *  Legacy comment translated to English.

 *  

 */

var databaseUpgradeCoordinator = app.Services.GetRequiredService<DatabaseUpgradeCoordinator>();
var databaseRuntimeState = await databaseUpgradeCoordinator.InspectAsync();
ReportDatabaseUpgradeProgress($"数据库状态检查完成：{databaseRuntimeState.Status}");
if (databaseUpgradeOptions.Enabled)
{
    ReportDatabaseUpgradeProgress("正在执行待处理的 Migration……");
    var result = await databaseUpgradeCoordinator.UpgradeAsync();
    Console.WriteLine(result.Message);
    foreach (var detail in result.Details)
    {
        Console.WriteLine(detail);
    }

    if (!result.Succeeded)
    {
        Console.Error.WriteLine($"数据库升级失败：{result.State.Message}");
        if (result.State.Exception != null)
        {
            Console.Error.WriteLine(result.State.Exception);
        }
    }

    Environment.ExitCode = result.Succeeded ? 0 : 2;
    await app.DisposeAsync();
    return;
}

if (databaseRuntimeState.Status == DatabaseRuntimeStatus.Ready)
{
    app.StartXncfThreads();
}
else
{
    Console.WriteLine($"数据库状态：{databaseRuntimeState.Status}；{databaseRuntimeState.Message}");
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseFileServer();//Legacy comment translated to English.

//Legacy comment translated to English.
app.UseDatabaseMaintenanceMode();

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapDefaultEndpoints();

if (databaseRuntimeState.Status == DatabaseRuntimeStatus.Ready)
{
    app.ShowSuccessTip();//Legacy comment translated to English.
}

string GetNcfApiClientPath(string xncfName, string appServiceName, string methodName, string showStaticApiState = null)
{
    var globalName = ApiBindAttribute.GetGlobalName(xncfName, $"{appServiceName}.{methodName}");

    var indexOfApiGroupDot = globalName.IndexOf(".");
    var apiName = globalName.Substring(indexOfApiGroupDot + 1, globalName.Length - indexOfApiGroupDot - 1);
    //var apiBindGlobalName = globalName.Split('.')[0];

    var apiPath = WebApiEngine.GetApiPath(xncfName, appServiceName, apiName, showStaticApiState);
    Console.WriteLine(apiPath);
    return apiPath;
}

/*
Console.WriteLine("============ logMsg =============");

Console.WriteLine("DatabaseName: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseName);

Console.WriteLine("DatabaseType: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseType);

Console.WriteLine("CacheType: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.CacheType);

Console.WriteLine("EnableMultiTenant: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant);

Console.WriteLine("TenantRule: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.TenantRule);

Console.WriteLine("RequestTempLogCacheMinutes: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes);

Console.WriteLine("PasswordSaltToken: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.PasswordSaltToken);

Console.WriteLine("McpAccessToken: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.McpAccessToken);

//output Database connection string

Console.WriteLine("Database connection string: " + string.Join(", ", Senparc.Ncf.Database.Helpers.NcfDatabaseHelper.GetCurrentConnectionInfo().Select(z => $"{z.Key}: {z.Value}")));

Console.WriteLine("Count of Database connection string: " + Senparc.Ncf.Database.Helpers.NcfDatabaseHelper.GetCurrentConnectionInfo().Count());

Console.WriteLine("============ logMsg END =============");

*/

// This route is a local development smoke test and must not be published by
// the deployed site. It calls an internal installer API and exposes its result.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test", async httpContext =>
    {
        //var senparcWebClient = httpContext.RequestServices.GetService<SenparcWebClient>();
        //var result = await senparcWebClient.GetHtml();
        //await httpContext.Response.WriteAsync(result);

        var apiClientHelper = httpContext.RequestServices.GetService<ApiClientHelper>();
        var apiClient = apiClientHelper.ConnectApiClient("installer");

        var xncfName = "Senparc.Xncf.Installer";//Assembly name / catalog
        var apiBindName = "InstallAppService";
        var methodName = "KeepAlive";
        var apiPath = GetNcfApiClientPath(xncfName, apiBindName, methodName, null);

        //var apiPath = $"/api/{keyName}/{apiBindGroupNamePath}/{apiNamePath}{showStaticApiState}";
        var url = apiPath; //"/api/Senparc.Xncf.Installer/InstallAppService/Xncf.Installer_InstallAppService.KeepAlive";
        var result2 = await RequestUtility.HttpGetAsync(null, url, Encoding.UTF8, apiClient);

        await httpContext.Response.WriteAsync(result2);
    });
}

await app.RunAsync();
