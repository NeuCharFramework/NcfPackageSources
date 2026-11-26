/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawMessageReceiptService.cs
    File description: WeixinClawMessageReceiptService.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 harden WeixinClaw merge integration

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

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

    /// <summary>
    /// Initializes the inbound message receipt service.
    /// </summary>
    /// <param name="repo">Receipt repository.</param>
    /// <param name="serviceProvider">Dependency-injection provider.</param>
    public WeixinClawMessageReceiptService(
        IRepositoryBase<WeixinClawMessageReceipt> repo,
        System.IServiceProvider serviceProvider) : base(repo, serviceProvider)
    {
    }

    /// <summary>
    /// Checks whether an inbound message receipt already exists for the account/message/sequence tuple.
    /// </summary>
    /// <param name="accountId">The Weixin account identifier.</param>
    /// <param name="messageId">The protocol message identifier.</param>
    /// <param name="seq">The protocol sequence number.</param>
    /// <returns><see langword="true"/> when the receipt exists.</returns>
    public async Task<bool> ExistsAsync(int accountId, string messageId, long seq)
    {
        var count = await GetCountAsync(z =>
            z.WeixinClawAccountId == accountId &&
            z.MessageId == messageId &&
            z.Seq == seq).ConfigureAwait(false);
        return count > 0;
    }

    /// <summary>
    /// Creates a receipt atomically and treats supported unique-constraint violations as duplicates.
    /// </summary>
    /// <param name="accountId">The Weixin account identifier.</param>
    /// <param name="messageId">The protocol message identifier.</param>
    /// <param name="seq">The protocol sequence number.</param>
    /// <returns><see langword="true"/> when a new receipt was created; otherwise, <see langword="false"/> for duplicates.</returns>
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

    /// <summary>
    /// Determines whether a database update exception represents a duplicate receipt.
    /// </summary>
    /// <param name="exception">The database update exception.</param>
    /// <returns><see langword="true"/> when provider codes, constraints, or messages indicate duplication.</returns>
    private static bool IsDuplicateReceiptException(DbUpdateException exception)
    {
        for (Exception current = exception; current != null; current = current.InnerException)
        {
            if (HasDuplicateErrorCode(current)
                || HasDuplicateConstraintName(current.Message)
                || HasGenericDuplicateReceiptMessage(current.Message))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detects a generic duplicate message mentioning the receipt entity.
    /// </summary>
    /// <param name="message">The provider exception message.</param>
    /// <returns><see langword="true"/> when the message matches the duplicate pattern.</returns>
    private static bool HasGenericDuplicateReceiptMessage(string message)
    {
        return !string.IsNullOrWhiteSpace(message)
            && message.Contains("WeixinClawMessageReceipt", StringComparison.OrdinalIgnoreCase)
            && (message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks whether an error message contains a known receipt uniqueness constraint.
    /// </summary>
    /// <param name="message">The provider exception message.</param>
    /// <returns><see langword="true"/> when a known constraint name is present.</returns>
    private static bool HasDuplicateConstraintName(string message)
    {
        return !string.IsNullOrWhiteSpace(message) &&
               DuplicateConstraintNames.Any(name => message.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reads common provider-specific duplicate error codes from an exception.
    /// </summary>
    /// <param name="exception">The provider exception to inspect.</param>
    /// <returns><see langword="true"/> when a known duplicate code is found.</returns>
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

    /// <summary>
    /// Reads a public numeric provider property and converts it to a nullable integer.
    /// </summary>
    /// <param name="instance">The provider exception object.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The converted value, or <see langword="null"/> when unavailable.</returns>
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

    /// <summary>
    /// Reads a public string provider property by reflection.
    /// </summary>
    /// <param name="instance">The provider exception object.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The property value, or <see langword="null"/> when unavailable.</returns>
    private static string GetStringProperty(object instance, string propertyName)
    {
        return instance?
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?
            .GetValue(instance) as string;
    }
}
