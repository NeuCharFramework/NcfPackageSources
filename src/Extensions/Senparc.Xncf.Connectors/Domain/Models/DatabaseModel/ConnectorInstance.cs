/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorInstance.cs
    File description: persisted connector account entity

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 store connector accounts and protected configuration

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.Connectors.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(ConnectorInstance))]
[Index(nameof(PublicId), IsUnique = true)]
public class ConnectorInstance : EntityBase<int>
{
    [Required, MaxLength(64)]
    public string PublicId { get; private set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required, MaxLength(80)]
    public string ConnectorKey { get; private set; } = string.Empty;

    public bool Enabled { get; private set; } = true;

    [MaxLength(2000)]
    public string? ConfigJson { get; private set; }

    [MaxLength(1000)]
    public string? EnabledActionsJson { get; private set; }

    [MaxLength(500)]
    public string? Description { get; private set; }

    public bool? LastTestSucceeded { get; private set; }

    [MaxLength(1000)]
    public string? LastTestMessage { get; private set; }

    public DateTime? LastTestedAt { get; private set; }

    /// <summary>
    /// Initializes an empty connector instance for ORM materialization.
    /// </summary>
    private ConnectorInstance()
    {
    }

    /// <summary>
    /// Creates a new connector instance with a generated public identifier.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <param name="connectorKey">The registered provider key.</param>
    /// <param name="description">The optional display description.</param>
    public ConnectorInstance(string name, string connectorKey, string? description)
    {
        PublicId = Guid.NewGuid().ToString("N");
        Rename(name, description);
        ConnectorKey = connectorKey.Trim();
        Enabled = true;
    }

    /// <summary>
    /// Updates the display name and optional description.
    /// </summary>
    /// <param name="name">The new display name.</param>
    /// <param name="description">The optional new description.</param>
    public void Rename(string name, string? description)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Stores protected configuration, allowed action keys, and enabled state.
    /// </summary>
    /// <param name="connectorKey">The registered provider key.</param>
    /// <param name="configJson">The serialized protected field configuration.</param>
    /// <param name="enabledActionsJson">The serialized allowed action-key list.</param>
    /// <param name="enabled">Whether the instance may be invoked.</param>
    public void SetConfiguration(string connectorKey, string? configJson, string? enabledActionsJson, bool enabled)
    {
        ConnectorKey = connectorKey.Trim();
        ConfigJson = configJson;
        EnabledActionsJson = enabledActionsJson;
        Enabled = enabled;
        SetUpdateTime();
    }

    /// <summary>
    /// Records the latest provider test result with a bounded message and current UTC timestamp.
    /// </summary>
    /// <param name="succeeded">Whether the provider test succeeded.</param>
    /// <param name="message">The test result message.</param>
    public void RecordTest(bool succeeded, string message)
    {
        LastTestSucceeded = succeeded;
        LastTestMessage = message.Length <= 1000 ? message : message[..1000];
        LastTestedAt = DateTime.UtcNow;
        SetUpdateTime();
    }
}
