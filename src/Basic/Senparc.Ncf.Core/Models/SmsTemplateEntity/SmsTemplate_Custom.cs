/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SmsTemplate_Custom.cs
    文件功能描述：SmsTemplate_Custom 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.ComponentModel;

namespace Senparc.Ncf.Core.Models
{
    public partial class SmsTemplate_Custom : SmsTemplate_Base
    {
        [Description("联系人姓名")]
        public virtual string PersonName { get; set; }

        [Description("联系人头衔")]
        public virtual string PersonTitle { get; set; }
    }
}