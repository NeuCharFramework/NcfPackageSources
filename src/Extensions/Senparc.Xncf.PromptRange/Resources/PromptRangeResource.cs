/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeResource.cs
    File description: PromptRangeResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.17.0-preview5 integrate shared resource localization and improve feature text for the PromptRange module

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// Localization catalog owned and packaged by the PromptRange module.
    /// </summary>
    public sealed class PromptRangeResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(PromptRangeResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(PromptRangeResource), key, fallback, arguments);
        }
    }
}
