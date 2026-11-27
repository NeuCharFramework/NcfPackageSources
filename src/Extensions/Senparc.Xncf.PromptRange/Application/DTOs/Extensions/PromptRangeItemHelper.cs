/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeItemHelper.cs
    File description: PromptRangeItemHelper related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Extensions
{
    public static class PromptRangeItemHelper
    {
        /// <summary>
        /// Adds a manual-entry option and the currently available PromptRange items to a selection list.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve <see cref="PromptItemService"/>.</param>
        /// <param name="SystemMessagePromptCodeSelection">The selection list receiving prompt-code options.</param>
        /// <returns>A task that completes after the prompt tree has been loaded.</returns>
        public static async Task LoadPromptRangeItemSelection(IServiceProvider serviceProvider, SelectionList SystemMessagePromptCodeSelection)
        {
            SystemMessagePromptCodeSelection.Items.Add(new SelectionItem()
            {
                Value = "0",
                Text = "手动输入 SystemMessage",
            });

            var promptItemService = serviceProvider.GetService<PromptItemService>();
            var items = await promptItemService.GetPromptRangeTreeList(true, true);
            foreach (var item in items)
            {
                SystemMessagePromptCodeSelection.Items.Add(new SelectionItem()
                {
                    Text = item.Text,
                    Value = item.Value
                });
            }
        }
    }
}
