/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRangeItemHelper.cs
    文件功能描述：PromptRangeItemHelper 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        public static async Task LoadPromptRangeItemSelection(IServiceProvider serviceProvider, SelectionList SystemMessagePromptCodeSelection)
        {
            //载入 PromptRange
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
