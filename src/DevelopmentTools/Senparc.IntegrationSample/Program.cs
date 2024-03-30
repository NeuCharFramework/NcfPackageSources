
using Senparc.Ncf.Database.SqlServer;

using Senparc.IntegrationSample;


var builder = WebApplication.CreateBuilder(args);

//添加（注册） Ncf 服务（必须）
builder.AddNcf/*<BySettingDatabaseConfiguration>*/();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//Use NCF（必须）
app.UseNcf<BySettingDatabaseConfiguration>();
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
