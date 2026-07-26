/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ApiRequest.cs
    文件功能描述：XNCF 模板示例实现
    
    
    创建标识：Senparc - 20211226
    
    修改标识：Senparc - 20260726
    修改描述：v1.1.0 补充示例模板 EventBus 请求-响应回环与多语言能力

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;

namespace Template_OrgName.Xncf.Template_XncfName.Application.DTOs.Request
{
    public class Api_MyCustomApiRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
