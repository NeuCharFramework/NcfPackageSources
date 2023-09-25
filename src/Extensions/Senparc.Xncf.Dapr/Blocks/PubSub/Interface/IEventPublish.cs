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
