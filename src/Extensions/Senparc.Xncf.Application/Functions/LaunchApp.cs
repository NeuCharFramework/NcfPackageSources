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
    /// Start application
    /// </summary>
    public class LaunchApp : FunctionBase
    {
        public class LaunchApp_Parameters : IFunctionParameter
        {
        //    /// <summary>
        //    /// Provide options
        //    /// <para>Note: string[]The default value of the type is the alternative value of the option. If no alternative value is provided, this parameter will not be ignored.</para>
        //    /// </summary>
        //    [Required]
        //    [Description("Startup path,If it is a custom path, you need to enter the installation path of the program;If it is a system, you only need to enter the file name of the executing program.")]
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
            [Description("Custom path||file name")]
            public string FilePath { get; set; }
        }

        //Note: Name must be unique within a single Xncf module!
        public override string Name => "app";

        public override string Description => "Start all programs";

        public override Type FunctionParameterType => typeof(LaunchApp_Parameters);

        public LaunchApp(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// run
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
            base.RecordLog(sb, "Start running LaunchApp");

            StartApp(typeParam.FilePath);

            //sb.AppendLine($"LaunchPath{typeParam.LaunchPath}");
            sb.AppendLine($"FilePath{typeParam.FilePath}");

            result.Log = sb.ToString();
            result.Message = "Operation successful!";
            return result;
        }

        #region Start the program
        private bool StartApp(string appName){
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
