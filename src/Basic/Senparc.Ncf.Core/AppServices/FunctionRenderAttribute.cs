/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionRenderAttribute.cs
    文件功能描述：FunctionRenderAttribute 相关实现
    
    
    创建标识：Senparc - 20211012
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
