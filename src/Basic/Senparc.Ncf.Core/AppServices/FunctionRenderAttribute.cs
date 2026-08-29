/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionRenderAttribute.cs
    文件功能描述：FunctionRenderAttribute 相关实现
    
    
    创建标识：Senparc - 20211012
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.25.0-preview2 新增统一资源本地化基础设施并支持 FunctionRender 动态文案

    修改标识：Senparc - 20260815
    修改描述：v0.29.0-preview8 新增 FunctionRender 的 AI 自动调用控制

    修改标识：Senparc - 20260828
    修改描述：增加全局 NeuCharPivot 浮动调用映射及访问策略元数据

    修改标识：Senparc - 20260829
    修改描述：v0.30.0-preview9 新增 FunctionRender 函数名匹配与全局调用控制能力

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.Core.AppServices
{
    [AttributeUsage(/*AttributeTargets.Class |*/ AttributeTargets.Method)]
    public class FunctionRenderAttribute : Attribute
    {
        /// <summary>
        /// 名称
        /// </summary>
        private string _name;

        public string Name
        {
            get => ResourceType == null
                ? _name
                : ResourceStringLocalizer.Get(ResourceType, NameResourceKey, _name);
            set => _name = value;
        }
        /// <summary>
        /// 说明
        /// </summary>
        private string _description;

        public string Description
        {
            get => ResourceType == null
                ? _description
                : ResourceStringLocalizer.Get(ResourceType, DescriptionResourceKey, _description);
            set => _description = value;
        }
        /// <summary>
        /// 分类到 XNCF 模块的 Regster 类型
        /// </summary>
        public Type RegisterType { get; set; }

        /// <summary>
        /// Marker type associated with the resource set used by localized metadata.
        /// </summary>
        public Type ResourceType { get; }

        public string NameResourceKey { get; }

        public string DescriptionResourceKey { get; }

        /// <summary>
        /// Gets or sets whether this function may be imported automatically by an AI conversation.
        /// <para>
        /// This only controls automatic AI tool exposure. It deliberately does not affect the
        /// normal FunctionRender page or a caller that invokes the AppService explicitly.
        /// Mutating, host-level operations should opt out and expose a constrained workflow tool
        /// instead.
        /// </para>
        /// </summary>
        public bool AllowAiInvocation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the Function may be opened from the global
        /// NeuCharPivot floating invocation surface.
        /// </summary>
        public bool AllowGlobalPivot { get; set; }

        /// <summary>
        /// Comma-separated role codes allowed to open the global Pivot mapping.
        /// An empty value means any authenticated admin may use it.
        /// </summary>
        public string GlobalPivotRoleCodes { get; set; }

        /// <summary>
        /// Comma-separated permission resource codes allowed to open the global
        /// Pivot mapping. Role and permission rules are alternative grants.
        /// </summary>
        public string GlobalPivotPermissionCodes { get; set; }

        public FunctionRenderAttribute(string name, string description, Type registerType/*TODO：可提供系统模块的默认值*/)
        {
            _name = name;
            _description = description;
            RegisterType = registerType;
        }

        /// <summary>
        /// Creates localized FunctionRender metadata. The resource values are
        /// resolved whenever the properties are read, so a cached attribute still
        /// follows the current request culture.
        /// </summary>
        public FunctionRenderAttribute(
            Type resourceType,
            string nameResourceKey,
            string descriptionResourceKey,
            Type registerType,
            string nameFallback = null,
            string descriptionFallback = null)
        {
            ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
            NameResourceKey = string.IsNullOrWhiteSpace(nameResourceKey)
                ? throw new ArgumentException("A resource key is required.", nameof(nameResourceKey))
                : nameResourceKey;
            DescriptionResourceKey = string.IsNullOrWhiteSpace(descriptionResourceKey)
                ? throw new ArgumentException("A resource key is required.", nameof(descriptionResourceKey))
                : descriptionResourceKey;
            RegisterType = registerType;
            _name = nameFallback ?? nameResourceKey;
            _description = descriptionFallback ?? descriptionResourceKey;
        }

    }
}
