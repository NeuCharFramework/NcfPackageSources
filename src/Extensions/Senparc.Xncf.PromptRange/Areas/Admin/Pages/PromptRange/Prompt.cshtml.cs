/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Prompt.cshtml.cs
    File description: Prompt.cshtml related implementation
    
    
    Created by: Senparc - 20231021
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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

        public PromptModel(
            Lazy<XncfModuleService> xncfModuleService,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService) : base(xncfModuleService)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
        }

        public void OnGet()
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
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
        ///Legacy comment translated to English.
        /// </summary>
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
