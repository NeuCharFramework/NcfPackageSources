using Senparc.CO2NET.Extensions;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;

namespace Senparc.Xncf.Application.Functions
{
    /// <summary>
    /// 启动应用程序
    /// </summary>
    public class LaunchApp : FunctionBase
    {
        public class LaunchApp_Parameters : IFunctionParameter
        {
        //    /// <summary>
        //    /// 提供选项
        //    /// <para>注意：string[]类型的默认值为选项的备选值，如果没有提供备选值，此参数将别忽略</para>
        //    /// </summary>
        //    [Required]
        //    [Description("启动路径,如果是自定义路径，需要输入程序的安装路径;如果是系统则只需要输入执行程序的文件名即可")]
        //    public string[] LaunchPath { get; set; } = new[] {
        //    Parameters_Path.System.ToString(),
        //    Parameters_Path.Custom.ToString()
        //};

        //    public enum Parameters_Path
        //    {
        //        System,
        //        Custom
        //    }

            [Required]
            [MaxLength(300)]
            [Description("自定义路径||文件名")]
            public string FilePath { get; set; }
        }

        //注意：Name 必须在单个 Xscf 模块中唯一！
        public override string Name => "应用程序";

        public override string Description => "启动所有的程序";

        public override Type FunctionParameterType => typeof(LaunchApp_Parameters);

        public LaunchApp(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            var typeParam = param as LaunchApp_Parameters;

            FunctionResult result = new FunctionResult()
            {
                Success = true
            };

            StringBuilder sb = new StringBuilder();
            base.RecordLog(sb, "开始运行 LaunchApp");

            StartApp(typeParam.FilePath);

            //sb.AppendLine($"LaunchPath{typeParam.LaunchPath}");
            sb.AppendLine($"FilePath{typeParam.FilePath}");

            result.Log = sb.ToString();
            result.Message = "操作成功！";
            return result;
        }

        #region 启动程序
        private bool StartApp(string appName)
        {
            return StartApp(appName, ProcessWindowStyle.Hidden);
        }

        private bool StartApp(string appName, ProcessWindowStyle style)
        {
            return StartApp(appName, null, style);
        }

        private bool StartApp(string appName, string arguments)
        {
            return StartApp(appName, arguments, ProcessWindowStyle.Hidden);
        }

        private void StartAppointApp(string filePath, string fileName)
        {
            Process exep = Process.Start(filePath + fileName);
        }

        private void StartAppointApp(string fileNamePath)
        {
            Process exep = Process.Start(fileNamePath);
        }

        private bool StartApp(string appName, string arguments, ProcessWindowStyle style)
        {
            bool blnRst = false;
            Process p = new Process();
            p.StartInfo.FileName = appName;//exe,bat and so on
            p.StartInfo.WindowStyle = style;
            p.StartInfo.Arguments = arguments;
            try
            {
                p.Start();
                p.WaitForExit();
                p.Close();
                blnRst = true;
            }
            catch
            {
            }
            return blnRst;
        }
        #endregion

    }
}
