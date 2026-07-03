/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WeixinTemplateBase.cs
    文件功能描述：WeixinTemplateBase 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Xncf.WeixinManager.WeixinTemplate
{
    public interface IWeixinTemplateBase
    {
        string TemplateId { get; set; }

        string TemplateName { get; set; }
    }


    public class WeixinTemplateBase : IWeixinTemplateBase
    {
        public string TemplateId { get; set; }

        public string TemplateName { get; set; }

        public WeixinTemplateBase(string templateId, string templateName)
        {
            TemplateId = templateId;
            TemplateName = templateName;
        }
    }
}