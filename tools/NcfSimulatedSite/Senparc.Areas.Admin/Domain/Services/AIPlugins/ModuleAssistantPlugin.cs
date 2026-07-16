using Microsoft.SemanticKernel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Services.AIPlugins
{
    /// <summary>
    /// ModuleAssistantPlugin：针对会话关联模块的 AI Function Calling 插件。
    /// <para>当用户提问涉及模块信息、数据库结构、已安装功能等时，AI 将自动调用对应函数。</para>
    /// </summary>
    public class ModuleAssistantPlugin
    {
        private readonly List<AdminChatSessionModule> _sessionModules;

        /// <summary>
        /// 初始化模块助手插件。
        /// </summary>
        /// <param name="sessionModules">当前会话关联模块列表。</param>
        public ModuleAssistantPlugin(List<AdminChatSessionModule> sessionModules)
        {
            _sessionModules = sessionModules ?? new List<AdminChatSessionModule>();
        }

        /// <summary>
        /// 列出当前会话关联的所有 XNCF 模块
        /// </summary>
        [KernelFunction, LocalizedDescription(typeof(AdminResource), "Admin.ModuleAssistant.Tool.SessionModules")]
        public string GetSessionModuleList()
        {
            if (!_sessionModules.Any())
                return AdminResource.Get("Admin.ModuleAssistant.NoSessionModules");

            var sb = new StringBuilder();
            sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.SessionModuleCount", "当前会话已关联 {0} 个模块：", _sessionModules.Count));
            foreach (var m in _sessionModules)
            {
                var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == m.XncfModuleUid);
                sb.AppendLine($"\n- **{m.ModuleName}**");
                sb.AppendLine($"  UID: {m.XncfModuleUid}");
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.Version", "  版本：{0}", m.ModuleVersion));
                if (register != null)
                {
                    sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.Description", "  描述：{0}", register.Description));
                    sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.MenuName", "  菜单名：{0}", register.MenuName));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取指定模块的详细信息（含 Functions 列表）
        /// </summary>
        [KernelFunction, LocalizedDescription(typeof(AdminResource), "Admin.ModuleAssistant.Tool.ModuleDetail")]
        public string GetModuleDetail(
            [LocalizedDescription(typeof(AdminResource), "Admin.ModuleAssistant.Parameter.Module")] string moduleUidOrName)
        {
            var module = FindModule(moduleUidOrName);
            if (module == null)
                return AdminResource.Format("Admin.ModuleAssistant.ModuleNotFound", "未找到匹配的模块：“{0}”。请先调用 GetSessionModuleList 确认模块列表。", moduleUidOrName);

            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == module.XncfModuleUid);
            var sb = new StringBuilder();
            sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DetailTitle", "## 模块详情：{0}", module.ModuleName));
            sb.AppendLine($"- **UID**: {module.XncfModuleUid}");
            sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DetailVersion", "- **版本**：{0}", module.ModuleVersion));

            if (register != null)
            {
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DetailAssembly", "- **程序集名称**：{0}", register.Name));
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DetailMenu", "- **菜单名**：{0}", register.MenuName));
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DetailDescription", "- **描述**：{0}", register.Description));
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DetailIcon", "- **图标**：{0}", register.Icon));
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DetailMcp", "- **支持 MCP**：{0}", register.EnableMcpServer ? AdminResource.Get("Common.Yes") : AdminResource.Get("Common.No")));

                // 获取 FunctionRender 注册的功能列表
                if (Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(register.GetType(), out var functionGroup) && functionGroup.Any())
                {
                    sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.FunctionCount", "\n### 可用功能（FunctionRender，共 {0} 个）：", functionGroup.Count));
                    foreach (var f in functionGroup.Values)
                    {
                        sb.AppendLine($"- **{f.FunctionRenderAttribute.Name}**：{f.FunctionRenderAttribute.Description}");
                    }
                }
                else
                {
                    sb.AppendLine(AdminResource.Get("Admin.ModuleAssistant.NoFunctions"));
                }
            }
            else
            {
                sb.AppendLine(AdminResource.Get("Admin.ModuleAssistant.RuntimeRegistrationMissing"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取指定模块的数据库结构信息
        /// </summary>
        [KernelFunction, LocalizedDescription(typeof(AdminResource), "Admin.ModuleAssistant.Tool.DatabaseInfo")]
        public string GetModuleDatabaseInfo(
            [LocalizedDescription(typeof(AdminResource), "Admin.ModuleAssistant.Parameter.Module")]
            string moduleUidOrName)
        {
            var module = FindModule(moduleUidOrName);
            if (module == null)
                return AdminResource.Format("Admin.ModuleAssistant.DatabaseModuleNotFound", "未找到模块：“{0}”。请先调用 GetSessionModuleList 确认可用模块。", moduleUidOrName);

            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == module.XncfModuleUid);
            if (register == null)
                return AdminResource.Format("Admin.ModuleAssistant.DatabaseRuntimeMissing", "未找到模块 {0} 的运行时注册，无法读取数据库信息。", module.ModuleName);

            var sb = new StringBuilder();
            sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DatabaseTitle", "## {0} 数据库信息", module.ModuleName));

            if (register is IXncfDatabase dbRegister)
            {
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DatabasePrefix", "- **数据库表前缀**：{0}", dbRegister.DatabaseUniquePrefix));
                var ctxType = dbRegister.TryGetXncfDatabaseDbContextType;
                sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DatabaseContext", "- **DbContext 类型**：{0}", ctxType?.FullName ?? AdminResource.Get("Common.NotAvailable")));

                if (ctxType != null)
                {
                    var dbSetProps = ctxType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.PropertyType.IsGenericType &&
                                    p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("DbSet"))
                        .ToList();

                    if (dbSetProps.Any())
                    {
                        sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DatabaseTableCount", "- **数据表（DbSet，共 {0} 张）**：", dbSetProps.Count));
                        foreach (var prop in dbSetProps)
                        {
                            var entityType = prop.PropertyType.GetGenericArguments().FirstOrDefault();
                            sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DatabaseEntity", "  * **{0}** → 实体类型：{1}", prop.Name, entityType?.Name ?? AdminResource.Get("Common.Unknown")));

                            // 列出实体的公开属性（简要字段清单）
                            if (entityType != null)
                            {
                                var fields = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(p => !p.PropertyType.IsGenericType || p.PropertyType.GetGenericTypeDefinition() != typeof(Lazy<>))
                                    .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                                    .Select(p => $"{p.Name}（{p.PropertyType.Name}）")
                                    .Take(12)
                                    .ToList();
                                if (fields.Any())
                                    sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.DatabaseFields", "    字段：{0}", string.Join(", ", fields)));
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine(AdminResource.Get("Admin.ModuleAssistant.NoDbSets"));
                    }
                }
            }
            else
            {
                sb.AppendLine(AdminResource.Get("Admin.ModuleAssistant.NoIndependentDatabase"));
                sb.AppendLine(AdminResource.Get("Admin.ModuleAssistant.SharedOrNoStorage"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 列出系统中所有已注册模块
        /// </summary>
        [KernelFunction, LocalizedDescription(typeof(AdminResource), "Admin.ModuleAssistant.Tool.AllModules")]
        public string ListAllInstalledModules()
        {
            var allModules = XncfRegisterManager.RegisterList
                .Where(z => !z.IgnoreInstall)
                .OrderBy(z => z.MenuName)
                .ToList();

            if (!allModules.Any())
                return AdminResource.Get("Admin.ModuleAssistant.NoInstalledModules");

            var sb = new StringBuilder();
            sb.AppendLine(AdminResource.Format("Admin.ModuleAssistant.InstalledModuleCount", "系统共注册 {0} 个模块：", allModules.Count));
            foreach (var r in allModules)
            {
                var inSession = _sessionModules.Any(m => m.XncfModuleUid == r.Uid);
                sb.AppendLine($"- **{r.MenuName}** ({r.Name}) v{r.Version}{(inSession ? AdminResource.Get("Admin.ModuleAssistant.InSessionTag") : "")}");
                if (!string.IsNullOrWhiteSpace(r.Description))
                    sb.AppendLine($"  {r.Description}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 根据 UID 或名称关键字从会话模块中查找
        /// </summary>
        private AdminChatSessionModule FindModule(string moduleUidOrName)
        {
            if (string.IsNullOrWhiteSpace(moduleUidOrName)) return null;
            return _sessionModules.FirstOrDefault(m =>
                m.XncfModuleUid.Equals(moduleUidOrName, StringComparison.OrdinalIgnoreCase) ||
                m.ModuleName.Contains(moduleUidOrName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
