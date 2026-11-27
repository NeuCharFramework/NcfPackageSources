/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionRenderAttribute.cs
    File description: FunctionRenderAttribute related implementation

    Created by: Senparc - 20211012

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.25.0-preview2 add unify resource localization base facilities and support FunctionRender dynamic text

    Modified by: Senparc - 20260815
    Change description: v0.29.0-preview8 add AI automaticinvoke control for FunctionRender

    Modified by: Senparc - 20260828
    Change description: Added global NeuCharPivot floating invoke mapping and access policy metadata

    Modified by: Senparc - 20260829
    Change description: v0.30.0-preview9 Add FunctionRender function name matching and global invoke control capabilities

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.Core.AppServices
{
    [AttributeUsage(/*AttributeTargets.Class |*/ AttributeTargets.Method)]
    public class FunctionRenderAttribute : Attribute
    {
        /// <summary>
        /// name
        /// </summary>
        private string _name;

        public string Name
        {
            get => ResourceType == null
                ? FunctionRenderTextResolver.Resolve(RegisterType, _name)
                : ResourceStringLocalizer.Get(ResourceType, NameResourceKey, _name);
            set => _name = value;
        }
        /// <summary>
        /// description
        /// </summary>
        private string _description;

        public string Description
        {
            get => ResourceType == null
                ? FunctionRenderTextResolver.Resolve(RegisterType, _description)
                : ResourceStringLocalizer.Get(ResourceType, DescriptionResourceKey, _description);
            set => _description = value;
        }
        /// <summary>
        /// Classification to XNCF Module Register Type
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

        /// <summary>
        /// Initializes FunctionRender metadata from literal name and description values.
        /// </summary>
        /// <param name="name">The function display name or localization key.</param>
        /// <param name="description">The function description or localization key.</param>
        /// <param name="registerType">The module registration type owning the function.</param>
        public FunctionRenderAttribute(string name, string description, Type registerType/*TODO: Can provide default values for system modules*/)
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
        /// <summary>
        /// Initializes FunctionRender metadata backed by a resource catalog.
        /// </summary>
        /// <param name="resourceType">The resource type containing localized strings.</param>
        /// <param name="nameResourceKey">The name resource key.</param>
        /// <param name="descriptionResourceKey">The description resource key.</param>
        /// <param name="registerType">The module registration type owning the function.</param>
        /// <param name="nameFallback">Fallback name when localization is unavailable.</param>
        /// <param name="descriptionFallback">Fallback description when localization is unavailable.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourceType"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when a resource key is blank.</exception>
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
