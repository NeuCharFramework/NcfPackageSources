/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SmsTemplate_Base.cs
    文件功能描述：SmsTemplate_Base 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.ComponentModel;

namespace Senparc.Ncf.Core.Models
{
    public interface ISmsTemplate_Base
    {
        string CompanyName { get; set; }

        string CompanyTel { get; set; }
    }


    public partial class SmsTemplate_Base : ISmsTemplate_Base
    {
        [Description("本公司名称")]
        public virtual string CompanyName { get; set; }

        [Description("本公司电话")]
        public virtual string CompanyTel { get; set; }
    }
}