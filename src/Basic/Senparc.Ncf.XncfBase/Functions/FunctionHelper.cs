using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// Function 帮助类
    /// </summary>
    public class FunctionHelper
    {
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        public static void RecordLog(StringBuilder sb, string msg)
        {
            sb.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
        }

        /// <summary>
        /// 获取所有参数的信息列表
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="functionRenderBag">FunctionRenderBag</param>
        /// <param name="tryLoadData">是否尝试载入数据（参数必须实现 IFunctionParameterLoadDataBase 接口）</param>
        /// <returns></returns>
        public static async Task<List<FunctionParameterInfo>> GetFunctionParameterInfoAsync(IServiceProvider serviceProvider, FunctionRenderBag functionRenderBag, bool tryLoadData = true)
        {
            var functionParameterType = functionRenderBag.FunctionParameterType;

            var paraObj = functionParameterType.Assembly.CreateInstance(functionParameterType.FullName) as IAppRequest;//TODO:通过 DI 生成

            if (tryLoadData && paraObj is FunctionAppRequestBase functionRequestPara)
            {
                await functionRequestPara.LoadData(serviceProvider);//载入原有信息
            }

            var props = functionParameterType.GetProperties();
            ParameterType parameterType = ParameterType.Text;
            List<FunctionParameterInfo> result = new List<FunctionParameterInfo>();
            foreach (var prop in props)
            {
                SelectionList selectionList = null;
                parameterType = ParameterType.Text;//默认为文本内容
                                                   //判断是否存在选项
                if (prop.PropertyType == typeof(SelectionList))
                {
                    var selections = prop.GetValue(paraObj, null) as SelectionList;
                    switch (selections.SelectionType)
                    {
                        case SelectionType.DropDownList:
                            parameterType = ParameterType.DropDownList;
                            break;
                        case SelectionType.CheckBoxList:
                            parameterType = ParameterType.CheckBoxList;
                            break;
                        default:
                            //TODO: throw
                            break;
                    }
                    selectionList = selections;
                }

                var name = prop.Name;
                string title = null;
                string description = null;
                var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttr != null && descriptionAttr.Description != null)
                {
                    //分割：名称||说明
                    var descriptionAttrArr = descriptionAttr.Description.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    title = descriptionAttrArr[0];
                    if (descriptionAttrArr.Length > 1)
                    {
                        description = descriptionAttrArr[1];
                    }
                }
                var systemType = prop.PropertyType.Name;

                object value = null;
                try
                {
                    value = prop.GetValue(paraObj);
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }

                var functionParamInfo = new FunctionParameterInfo(name, title, description, isRequired, systemType, parameterType,
                                                selectionList ?? new SelectionList(SelectionType.Unknown), value);
                result.Add(functionParamInfo);
            }
            return result;
        }

        /// <summary>
        /// 给 SelectionList 对象添加当前解决方案中的 XNCF 项目
        /// （扫描当前解决方案包含的所有领域项目）
        /// </summary>
        /// <param name="mustHaveXncfModule">当前解决方案是否必须包含 XNCF 项目</param>
        public static List<SelectionItem> LoadXncfProjects(bool mustHaveXncfModule = false)
        {
            var selectList = new List<SelectionItem>();

            var currentDir = System.IO.Directory.GetCurrentDirectory();

            while (currentDir != null)
            {
                var slnFile = Directory.GetFiles(currentDir, "*.sln");
                if (slnFile.Length > 0)
                {
                    break;
                }
                currentDir = Directory.GetParent(currentDir).FullName;
            }

            if (currentDir != null)
            {
                //找到了 SLN 文件，开始展开地毯式搜索

                //第一步：查找 XNCF

                var projectFolders = Directory.GetDirectories(currentDir, "*.XNCF.*", SearchOption.AllDirectories);

                foreach (var projectFolder in projectFolders)
                {
                    //第二步：查看 Register 文件是否存在
                    var registerFilePath = Path.Combine(projectFolder, "Register.cs");
                    if (!File.Exists(registerFilePath))
                    {
                        continue;//不存在则跳过
                    }

                    //第三步：检查 Register 文件是否合格

                    var registerContent = File.ReadAllText(registerFilePath);
                    if (registerContent.Contains("[XncfRegister]") &&
                        registerContent.Contains("IXncfRegister") &&
                        registerContent.Contains("Uid"))
                    {
                        selectList.Add(
                            new SelectionItem(
                                projectFolder,
                                projectFolder, /*Path.GetFileName(projectFolder)*/
                                projectFolder/*Path.GetDirectoryName(projectFolder)*/));
                    }
                }
            }

            if (mustHaveXncfModule && (currentDir == null || selectList.Count == 0))
            {
                selectList.Add(
                             new SelectionItem(
                                 "N/A",
                                 "没有发现任何可用的 XNCF 项目，请确保你正在一个标准的 NCF 开发环境中！"));
            }

            return selectList;
        }

        /// <summary>
        /// 获取 xxxSenparcEntities.cs 数据库文件
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="dbType">数据库类型</param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public static string GetSenparcEntitiesFilePath(string projectPath, string dbType)
        {
            var databaseModelPath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel");
            var files = Directory.GetFiles(databaseModelPath, "*SenparcEntities.cs");
            if (files.Length == 0)
            {
                throw new NcfExceptionBase($"目录 {databaseModelPath} 下没有找到 SenparcEntities.cs 结尾的文件");
            }

            var databaseFile = files[0].Replace(".cs", "");

            if (!dbType.IsNullOrEmpty())
            {
                databaseFile += "_" + dbType;
            }

            return databaseFile;
        }
    }
}
