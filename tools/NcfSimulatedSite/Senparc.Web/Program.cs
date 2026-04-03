//Namespaces for the following database modules are added or removed as needed
//using Senparc.Ncf.Database.MySql; //Use requires reference package: Senparc.Ncf.Database.MySql
//using Senparc.Ncf.Database.Sqlite; //Use requires reference package: Senparc.Ncf.Database.Sqlite
//using Senparc.Ncf.Database.PostgreSQL; //Use requires reference package: Senparc.Ncf.Database.PostgreSQL
//using Senparc.Ncf.Database.Oracle; //Use requires reference package: Senparc.Ncf.Database.Oracle
//using Senparc.Ncf.Database.SqlServer; //Use requires reference package: Senparc.Ncf.Database.SqlServer

using Senparc.CO2NET;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Database.SqlServer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Add (register) NCF service (required)
builder.AddNcf();

//Add ServiceDefaults
//builder.AddServiceDefaults();

System.Net.ServicePointManager.ServerCertificateValidationCallback =
    ((sender, certificate, chain, sslPolicyErrors) => true);

//Add Dapr
builder.Services.AddDaprClient();

var app = builder.Build();

//app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//Use NCF (required)
app.UseNcf<BySettingDatabaseConfiguration>();
/*  UseNcf<TDatabaseConfiguration>() generic type description
 *                
 * Method | Description
 * -------------------------------------------------|-------------------------
 * UseNcf<BySettingDatabaseConfiguration>() | The configuration is determined by appsettings.json
 * UseNcf<SqlServerDatabaseConfiguration>() | Use SQLServer database
 * UseNcf<SqliteMemoryDatabaseConfiguration>() | Use SQLite database
 * UseNcf<MySqlDatabaseConfiguration>() | Use MySQL database
 * UseNcf<PostgreSQLDatabaseConfiguration>() | Use PostgreSQL database
 * UseNcf<OracleDatabaseConfiguration>() | Use Oracle database (V12+)
 * UseNcf<OracleDatabaseConfigurationForV11>() | Use Oracle database (V11+)
 * UseNcf<DmDatabaseConfiguration>() | Use DM (Dameng) database
 * More databases can be expanded, and so on...
 *  
 */

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseFileServer();//Not necessary

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.ShowSuccessTip();//Display system preparation success prompt

string GetNcfApiClientPath(string xncfName,string appServiceName, string methodName,string showStaticApiState=null)
{
    var globalName = ApiBindAttribute.GetGlobalName(xncfName,$"{appServiceName}.{methodName}");

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

await app.RunAsync();
