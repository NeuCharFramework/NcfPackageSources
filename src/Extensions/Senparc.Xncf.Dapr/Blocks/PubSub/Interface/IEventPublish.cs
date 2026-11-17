/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IEventPublish.cs
    File description: IEventPublish related implementation

    Created by: Senparc - 20230925

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr.Blocks.PubSub.Interface
{
    internal interface IEventPublish
    {
        Task PublishEventAsync(string pubSubName, string topicName, object data);
    }
}
