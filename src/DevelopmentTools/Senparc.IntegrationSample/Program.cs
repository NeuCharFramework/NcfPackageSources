/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Program.cs
    文件功能描述：Program 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/


using Senparc.Ncf.Database.SqlServer;

using Senparc.IntegrationSample;


var builder = WebApplication.CreateBuilder(args);

//添加（注册） Ncf 服务（必须）
builder.AddNcf();

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
