/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Prompt.cshtml.cs
    File description: Prompt.cshtml related implementation

    Created by: Senparc - 20231021

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Areas.Admin.Pages.PromptRange
{
    public class PromptModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private const string PromptPageUid = "C6175B8E-9F79-4053-9523-F8E4AC0C3E18";
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;

        /// <summary>Initializes the PromptRange prompt page.</summary>
        /// <param name="xncfModuleService">The lazy XNCF module service.</param>
        /// <param name="promptItemService">The prompt-item service.</param>
        /// <param name="promptRangeService">The prompt-range service.</param>
        public PromptModel(
            Lazy<XncfModuleService> xncfModuleService,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService) : base(xncfModuleService)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
        }

        /// <summary>Initializes the prompt page request.</summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Resolves a prompt code and redirects to its prompt editor location.
        /// </summary>
        /// <param name="promptCode">The prompt code or range alias/version.</param>
        /// <returns>A redirect to the resolved prompt or a query-preserving fallback.</returns>
        public async Task<IActionResult> OnGetResolveAsync(string promptCode)
        {
            if (string.IsNullOrWhiteSpace(promptCode))
            {
                return Redirect($"/Admin/PromptRange/Prompt?uid={PromptPageUid}");
            }

            var normalizedPromptCode = await NormalizePromptCodeAsync(promptCode.Trim());
            try
            {
                var promptItem = await _promptItemService.GetBestPromptAsync(normalizedPromptCode, true);
                return Redirect($"/Admin/PromptRange/Prompt?uid={PromptPageUid}#rangeId={promptItem.RangeId}&promptId={promptItem.Id}");
            }
            catch
            {
                return Redirect($"/Admin/PromptRange/Prompt?uid={PromptPageUid}&promptCode={Uri.EscapeDataString(normalizedPromptCode)}");
            }
        }

        /// <summary>
        /// Normalizes a range alias prefix to its canonical range name.
        /// </summary>
        /// <param name="promptCode">The prompt code to normalize.</param>
        /// <returns>The canonicalized prompt code.</returns>
        private async Task<string> NormalizePromptCodeAsync(string promptCode)
        {
            if (string.IsNullOrWhiteSpace(promptCode))
            {
                return promptCode;
            }

            var splitIndex = promptCode.IndexOf('-');
            var rangePrefix = splitIndex >= 0 ? promptCode.Substring(0, splitIndex) : promptCode;
            var suffix = splitIndex >= 0 ? promptCode.Substring(splitIndex) : string.Empty;

            var promptRange = await _promptRangeService.GetObjectAsync(
                z => z.RangeName == rangePrefix || z.Alias == rangePrefix);

            if (promptRange == null)
            {
                return promptCode;
            }

            return $"{promptRange.RangeName}{suffix}";
        }
    }
}
