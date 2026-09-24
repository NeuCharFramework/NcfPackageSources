using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Services;

public class WeixinClawMessageReceiptService : ServiceBase<WeixinClawMessageReceipt>, IServiceBase<WeixinClawMessageReceipt>
{
    private static readonly string[] DuplicateConstraintNames =
    [
        "IX_WeixinManager_WeixinClawMessageReceipt_WeixinClawAccountId_MessageId_Seq",
        "IX_WeixinManager_WeixinClawMessageReceipt_WeixinClawAccountId_M~",
        "IX_WeixinManager_WeixinClawMessageReceipt_WeixinClawAccountId_~"
    ];

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
            if (HasDuplicateErrorCode(current) || HasDuplicateConstraintName(current.Message))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDuplicateConstraintName(string message)
    {
        return !string.IsNullOrWhiteSpace(message) &&
               DuplicateConstraintNames.Any(name => message.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDuplicateErrorCode(Exception exception)
    {
        var number = GetIntProperty(exception, "Number");
        if (number is 1 or 1062 or 2601 or 2627)
        {
            return true;
        }

        var sqliteErrorCode = GetIntProperty(exception, "SqliteErrorCode");
        if (sqliteErrorCode == 19)
        {
            return true;
        }

        var sqlState = GetStringProperty(exception, "SqlState") ?? GetStringProperty(exception, "SQLState");
        return string.Equals(sqlState, "23505", StringComparison.OrdinalIgnoreCase);
    }

    private static int? GetIntProperty(object instance, string propertyName)
    {
        if (instance == null)
        {
            return null;
        }

        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (property == null)
        {
            return null;
        }

        var value = property.GetValue(instance);
        return value switch
        {
            int intValue => intValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            short shortValue => shortValue,
            byte byteValue => byteValue,
            _ => null
        };
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        return instance?
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(instance) as string;
    }
}
