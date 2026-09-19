using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Services;

public class WeixinClawMessageReceiptService : ServiceBase<WeixinClawMessageReceipt>, IServiceBase<WeixinClawMessageReceipt>
{
    public WeixinClawMessageReceiptService(
        IRepositoryBase<WeixinClawMessageReceipt> repo,
        System.IServiceProvider serviceProvider) : base(repo, serviceProvider)
    {
    }

    public async Task<bool> ExistsAsync(int accountId, string messageId, long seq)
    {
        var count = await GetCountAsync(z =>
            z.WeixinClawAccountId == accountId &&
            z.MessageId == messageId &&
            z.Seq == seq).ConfigureAwait(false);
        return count > 0;
    }
}
