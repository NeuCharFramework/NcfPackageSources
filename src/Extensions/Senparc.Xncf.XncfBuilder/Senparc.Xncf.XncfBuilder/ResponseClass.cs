/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ResponseClass.cs
    文件功能描述：ResponseClass 相关实现
    
    
    创建标识：Senparc - 20250624
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

public partial class ResponseClass
{
    public int StatusCode { get; set; }

    // 新增属性测试 Response.cs 独立修改
    public string? Message { get; set; }
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}
