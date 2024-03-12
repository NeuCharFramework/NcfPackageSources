
using Senparc.Ncf.Database.SqlServer;

using Senparc.IntegrationSample;
using Senparc.Xncf.DatabaseToolkit.Domain.Services;
using Senparc.Xncf.SystemCore.Domain.Database;
using Senparc.CO2NET.Extensions;

var builder = WebApplication.CreateBuilder(args);

//添加（注册） Ncf 服务（必须）
builder.AddNcf<SQLServerDatabaseConfiguration>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//Use NCF（必须）
app.UseNcf();
//var col = Senparc.Ncf.Core.Models.EntitySetKeys.GetAllEntitySetInfo();
//Console.WriteLine("================================EntitySetKeys:");
//Console.WriteLine(col.Where(z => z.Key.FullName.Contains("AI")).ToJson(true));

app.UseStaticFiles();

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.ShowSuccessTip();

app.Run();
