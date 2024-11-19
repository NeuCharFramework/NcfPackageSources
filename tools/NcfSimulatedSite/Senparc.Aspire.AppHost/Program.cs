using Aspire.Hosting;
using Projects;
using System.Text;

var builder = DistributedApplication.CreateBuilder(args);

//if (builder.ExecutionContext.IsRunMode)
//{
var installer = builder.AddProject<Projects.Senparc_Xncf_Installer>(nameof(Senparc_Xncf_Installer).Replace("_", "."))
         .WithExternalHttpEndpoints();

// "../Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj"

//builder.AddContainer("my-redis", "redis", "5.0.14");
//}


var ncfWeb = builder.AddProject<Projects.Senparc_Web>(nameof(Senparc_Web).Replace("_","."))
           .WithReference(installer)
           .WithExternalHttpEndpoints();


//支持中文字符
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.GetEncoding("GB2312");

builder.Build().Run();
