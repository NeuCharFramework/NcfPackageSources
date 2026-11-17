/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SmsTemplate_Base.cs

    File description: SmsTemplate_Base related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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