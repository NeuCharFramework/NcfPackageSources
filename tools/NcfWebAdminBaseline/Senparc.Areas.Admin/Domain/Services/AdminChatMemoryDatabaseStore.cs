/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatMemoryDatabaseStore.cs
    File description: AdminChatMemoryDatabaseStore related definitions and implementation

    Created by: Senparc - 20261103

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class AdminChatMemoryDatabaseStore : IAdminChatMemoryStore
{
    private readonly DbContext _db;
    private readonly AdminChatMemoryStore _legacy;
    private readonly IDataProtectionProvider _protection;

    /// <summary>
    /// Initializes the database-backed memory store and its legacy-file fallback.
    /// </summary>
    /// <param name="db">The database context containing memory state rows.</param>
    /// <param name="legacy">The legacy memory store used during migration.</param>
    /// <param name="protection">The data-protection provider for memory payloads.</param>
    public AdminChatMemoryDatabaseStore(DbContext db, AdminChatMemoryStore legacy, IDataProtectionProvider protection)
    {
        _db = db;
        _legacy = legacy;
        _protection = protection;
    }

    /// <summary>
    /// Builds a tenant- and user-scoped query for the memory row.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <returns>The unmaterialized memory-state query.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either identifier is negative.</exception>
    private IQueryable<AdminChatMemoryState> Query(int tenantId, int userId)
    {
        if (tenantId < 0 || userId < 0) throw new ArgumentOutOfRangeException(nameof(userId));
        return _db.Set<AdminChatMemoryState>().Where(row => row.TenantId == tenantId && row.UserId == userId);
    }

    /// <summary>
    /// Creates the versioned data protector scoped to one tenant and administrator.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <returns>The scoped data protector.</returns>
    private IDataProtector Protector(int tenantId, int userId) =>
        _protection.CreateProtector("NCF.AdminChat.Memory.Database.v1", tenantId.ToString(), userId.ToString());

    /// <summary>
    /// Reads protected database memory, migrating the legacy profile when no row exists.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="cancellationToken">The database-operation cancellation token.</param>
    /// <returns>The current memory profile.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when stored memory payload data is invalid.</exception>
    public async Task<AdminChatMemoryProfile> ReadAsync(int tenantId, int userId, CancellationToken cancellationToken = default)
    {
        var row = await Query(tenantId, userId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (row != null)
            return JsonSerializer.Deserialize<AdminChatMemoryProfile>(Protector(tenantId, userId).Unprotect(row.Payload))
                ?? throw new NcfExceptionBase("Invalid Memory data.");
        var profile = await _legacy.ReadAsync(tenantId, userId, cancellationToken);
        if (profile.Revision.Length == 0) return profile;
        // A database row, including an empty/deleted profile, always wins over a legacy file.
        await InsertAsync(tenantId, userId, profile, cancellationToken);
        return await ReadAsync(tenantId, userId, cancellationToken);
    }

    /// <summary>
    /// Applies an optimistic-concurrency update to one memory profile and persists a new revision.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="revision">The revision read by the caller.</param>
    /// <param name="update">The mutation applied to the current profile.</param>
    /// <param name="cancellationToken">The database-operation cancellation token.</param>
    /// <returns>The updated memory profile.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the profile exceeds the payload limit.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the revision no longer matches the stored profile.</exception>
    public async Task<AdminChatMemoryProfile> UpdateAsync(int tenantId, int userId, string revision,
        Action<AdminChatMemoryProfile> update, CancellationToken cancellationToken = default)
    {
        var profile = await ReadAsync(tenantId, userId, cancellationToken);
        if (revision == null || revision != profile.Revision) throw Conflict();
        update(profile);
        if (profile.Sessions.Count + profile.SessionModes.Count > 2000)
            throw new NcfExceptionBase("Memory session limit exceeded.");
        profile.Revision = Guid.NewGuid().ToString("N");
        var payload = Protect(tenantId, userId, profile);
        var count = await Query(tenantId, userId).Where(row => row.Revision == revision)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Revision, profile.Revision)
                .SetProperty(row => row.Payload, payload).SetProperty(row => row.LastUpdateTime, DateTime.Now), cancellationToken);
        if (count == 0 && (revision.Length != 0 || !await InsertAsync(tenantId, userId, profile, cancellationToken)))
            throw Conflict();
        return profile;
    }

    /// <summary>
    /// Serializes and protects a memory profile, enforcing the one-megabyte payload limit.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="profile">The profile to serialize.</param>
    /// <returns>The protected payload bytes.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the serialized profile exceeds one MiB.</exception>
    private byte[] Protect(int tenantId, int userId, AdminChatMemoryProfile profile)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(profile);
        if (json.Length > 1024 * 1024) throw new NcfExceptionBase("Memory data exceeds 1 MiB.");
        return Protector(tenantId, userId).Protect(json);
    }

    /// <summary>
    /// Inserts a new protected memory row and reports whether the insert won a concurrent race.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="profile">The profile to insert.</param>
    /// <param name="ct">The database-operation cancellation token.</param>
    /// <returns><see langword="true"/> when the row is inserted; otherwise <see langword="false"/> when it already exists.</returns>
    private async Task<bool> InsertAsync(int tenantId, int userId, AdminChatMemoryProfile profile, CancellationToken ct)
    {
        var row = new AdminChatMemoryState
        {
            TenantId = tenantId, UserId = userId, Revision = profile.Revision, Payload = Protect(tenantId, userId, profile)
        };
        _db.Add(row);
        try { await _db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateException)
        {
            _db.Entry(row).State = EntityState.Detached;
            if (await Query(tenantId, userId).AnyAsync(ct)) return false;
            throw;
        }
        finally { _db.Entry(row).State = EntityState.Detached; }
    }

    /// <summary>
    /// Creates the standardized optimistic-concurrency conflict exception.
    /// </summary>
    /// <returns>The conflict exception describing the required reload.</returns>
    private static NcfExceptionBase Conflict() =>
        new("Memory changed. Reload before saving; the newer version was preserved.");
}
