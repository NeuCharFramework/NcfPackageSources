/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IntegrationEvent.cs
    文件功能描述：IntegrationEvent 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Xncf.Dapr.Blocks.PubSub;
public record IntegrationEvent
{
    public Guid Id { get; }

    public DateTime CreationDate { get; }

    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }
}
