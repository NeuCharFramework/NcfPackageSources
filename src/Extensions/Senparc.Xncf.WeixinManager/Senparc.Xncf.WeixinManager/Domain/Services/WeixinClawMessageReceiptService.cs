using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using System;
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

    public async Task<bool> TryCreateAsync(int accountId, string messageId, long seq)
    {
        try
        {
            await SaveObjectAsync(new WeixinClawMessageReceipt(accountId, messageId, seq)).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex) when (IsDuplicateReceiptException(ex))
        {
            return false;
        }
    }

    private static bool IsDuplicateReceiptException(DbUpdateException exception)
    {
        for (Exception current = exception; current != null; current = current.InnerException)
        {
            var message = current.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            if (message.Contains("WeixinClawMessageReceipt", StringComparison.OrdinalIgnoreCase) &&
                (message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("unique", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
