/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Prompt.cshtml.cs
    文件功能描述：Prompt.cshtml 相关实现
    
    
    创建标识：Senparc - 20231021
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        /// 根据 PromptCode（支持靶场/靶道/完整版本）解析当前实际运行 Prompt 并跳转。
        /// </summary>
        /// <param name="promptCode">PromptCode，可为 RangeName、RangeName-T1 或 RangeName-T1-A1</param>
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
        /// 将 Alias 开头的 PromptCode 归一化为 RangeName 开头，避免历史数据中的 Alias 造成解析失败。
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
