using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.XncfBuidler.Templates;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Functions
{

    public class BuildXncf : FunctionBase
    {
        public BuildXncf(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public class Parameters : IFunctionParameter
        {
            [Required]
            [MaxLength(50)]
            [Description("组织名称||用于作为模块命名空间（及名称）的前缀")]
            public string OrgName { get; set; }

            [Required]
            [MaxLength(50)]
            [Description("模块名称||同时将作为类名（请注意类名规范），支持连续英文大小写和数字，不能以数字开头，不能带有空格和.,/*等特殊符号")]
            public string XncfName { get; set; }

            //[Required]
            //[MaxLength(36)]
            //[Description("Uid||必须确保全局唯一，生成后必须固定")]
            //public string Uid { get; set; }

            [Required]
            [MaxLength(50)]
            [Description("版本号||如：1.0、2.0-beta1")]
            public string Version { get; set; }

            [Required]
            [MaxLength(50)]
            [Description("菜单名称||如“NCF 生成器”")]
            public string MenuName { get; set; }

            [Required]
            [MaxLength(50)]
            [Description("图标||支持 Font Awesome 图标集，如：fa fa-ofbuilding")]
            public string Icon { get; set; }

            [Required]
            [MaxLength(400)]
            [Description("说明||模块的说明")]
            public string Description { get; set; }

            [Description("使用函数||")]
            public SelectionList UseFunction { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用函数模块（Function）",false),
            });

            [Description("使用数据库||")]
            public SelectionList UseDatabase { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用数据库模块（Database）",false),
            });

            [Description("使用 Web 页面||")]
            public SelectionList UseWeb { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用 Web 页面模块（Database）",false),
            });

            [Description("使用中间件||")]
            public SelectionList UseMiddleware { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[] {
                 new SelectionItem("1","使用","是否需要使用中间件模块（Middleware）",false),
            });

            [Required]
            [MaxLength(250)]
            [Description("解决方案文件||输入解决方案文件物理路径，将在其并列位置生成模块目录，如：E:\\Senparc\\Ncf\\ncf.sln")]
            public string SlnFilePath { get; set; }
        }


        public override string Name => "生成 XNCF";

        public override string Description => "根据配置条件生成 XNCF";

        public override Type FunctionParameterType => typeof(Parameters);


        private string _outPutBaseDir;
        /// <summary>
        /// 输出内容
        /// </summary>
        /// <param name="page"></param>
        private void WriteContent(IXncfTemplatePage page, StringBuilder sb)
        {
            String pageContent = page.TransformText();
            System.IO.File.WriteAllText(Path.Combine(_outPutBaseDir, page.RelativeFilePath), pageContent, Encoding.UTF8);
            sb.AppendLine($"已添加文件：{page.RelativeFilePath}");
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        /// <param name="dirName"></param>
        private string AddDir(string dirName)
        {
            var path = Path.Combine(_outPutBaseDir, dirName);
            Directory.CreateDirectory(path);
            return path;
        }

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                var projectName = $"{typeParam.OrgName}.Xncf.{typeParam.XncfName}";
                _outPutBaseDir = Path.Combine(Senparc.CO2NET.Config.RootDictionaryPath, "..", $"{projectName}");
                _outPutBaseDir = Path.GetFullPath(_outPutBaseDir);
                if (!Directory.Exists(_outPutBaseDir))
                {
                    Directory.CreateDirectory(_outPutBaseDir);
                }
                Senparc.Xncf.XncfBuidler.Templates.Register registerPage = new Senparc.Xncf.XncfBuidler.Templates.Register()
                {
                    OrgName = typeParam.OrgName,
                    XncfName = typeParam.XncfName,
                    Uid = Guid.NewGuid().ToString(),
                    Version = typeParam.Version,
                    MenuName = typeParam.MenuName,
                    Icon = typeParam.Icon,
                    Description = typeParam.Description,
                };

                //判断是否使用函数（方法）
                var functionTypes = "";
                if (typeParam.UseFunction.SelectedValues.Contains("1"))
                {
                    functionTypes = "typeof(MyFunction)";

                    //添加文件夹
                    var dir = AddDir("Functions");
                    Senparc.Xncf.XncfBuidler.Templates.Functions.MyFunction functionPage = new XncfBuidler.Templates.Functions.MyFunction()
                    {
                        OrgName = typeParam.OrgName,
                        XncfName = typeParam.XncfName
                    };
                    WriteContent(functionPage, sb);
                }
                registerPage.FunctionTypes = functionTypes;
                WriteContent(registerPage, sb);

                //生成 .csproj
                Senparc.Xncf.XncfBuidler.Templates.csproj csprojPage = new csproj()
                {
                    OrgName = typeParam.OrgName,
                    XncfName = typeParam.XncfName,
                    Version = typeParam.Version,
                    MenuName = typeParam.MenuName,
                    Description = typeParam.Description,
                };
                WriteContent(csprojPage, sb);

                //生成 .sln
                if (!typeParam.SlnFilePath.ToUpper().EndsWith(".SLN"))
                {
                    result.Success = false;
                    result.Message = $"解决方案文件未找到，请手动引用项目 {csprojPage.RelativeFilePath}";
                    sb.AppendLine($"操作未全部完成：{result.Message}");
                }
                else if (File.Exists(typeParam.SlnFilePath))
                {
                    var slnFileName = Path.GetFileName(typeParam.SlnFilePath);
                    var newSlnFileName = $"{slnFileName}-new-{SystemTime.Now.DateTime.ToString("yyyyMMdd_HHmmss")}.sln";
                    var newSlnFilePath = Path.Combine(Path.GetDirectoryName(typeParam.SlnFilePath), newSlnFileName);
                    File.Copy(typeParam.SlnFilePath, newSlnFilePath);
                    result.Message = $"项目生成成功！请打开  {newSlnFilePath} 解决方案文件查看已附加的项目！。";

                    //修改 new Sln
                    string slnContent = null;
                    using (FileStream fs = new FileStream(newSlnFilePath, FileMode.Open))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            slnContent = sr.ReadToEnd();
                            sr.Close();
                        }
                    }

                    var projGuid = Guid.NewGuid().ToString("D");
                    slnContent = slnContent.Replace(@"Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Senparc.Core"", ""Senparc.Core\Senparc.Core.csproj"", ""{D0EF2816-B99A-4554-964A-6EA6814B3A36}""
EndProject", @$"Project(""{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}"") = ""Senparc.Core"", ""Senparc.Core\Senparc.Core.csproj"", ""{{D0EF2816-B99A-4554-964A-6EA6814B3A36}}""
EndProject
Project(""{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}"") = ""{projectName}"", ""{projectName}\{csprojPage.RelativeFilePath}"", ""{projGuid}""
EndProject
").Replace(@"		{D0EF2816-B99A-4554-964A-6EA6814B3A36}.Test|Any CPU.Build.0 = Release|Any CPU
", @$"		{{D0EF2816-B99A-4554-964A-6EA6814B3A36}}.Test|Any CPU.Build.0 = Release|Any CPU
		{projGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{projGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{projGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{projGuid}.Release|Any CPU.Build.0 = Release|Any CPU
		{projGuid}.Test|Any CPU.ActiveCfg = Release|Any CPU
		{projGuid}.Test|Any CPU.Build.0 = Release|Any CPU
");
                    System.IO.File.WriteAllText(newSlnFilePath, slnContent, Encoding.UTF8);
                    sb.AppendLine($"已创建新的解决方案文件：{newSlnFilePath}");
                    result.Message = $"项目生成成功！请打开  {newSlnFilePath} 解决方案文件查看已附加的项目！。";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"解决方案文件未找到，请手动引用项目 {csprojPage.RelativeFilePath}";
                    sb.AppendLine($"操作未全部完成：{result.Message}");
                }
            });
        }
    }
}
