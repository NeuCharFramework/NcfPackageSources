/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IEventBus.cs
    文件功能描述：IEventBus 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Xncf.Dapr.Blocks.PubSub.Interface;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent);
}
