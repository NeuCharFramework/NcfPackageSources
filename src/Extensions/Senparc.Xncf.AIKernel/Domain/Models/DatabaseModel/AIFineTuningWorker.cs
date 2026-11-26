/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIFineTuningWorker.cs
    File description: Related implementation.

    Created by: Senparc - 20261009

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 enhance Agent Studio, group configuration, and task management interactions

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIKernel.Models;

/// <summary>
/// A locally managed fine-tuning worker. Authentication material remains outside the database.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AIFineTuningWorker))]
[Serializable]
public class AIFineTuningWorker : EntityBase<int>
{
    [Required, MaxLength(50)]
    public string Alias { get; private set; }

    [Required, MaxLength(100)]
    public string Name { get; private set; }

    [Required, MaxLength(250)]
    public string Endpoint { get; private set; }

    [Required, DefaultValue(30)]
    public int RequestTimeoutSeconds { get; private set; }

    [Required, DefaultValue(false)]
    public bool Enabled { get; private set; }

    [MaxLength(500)]
    public string Note { get; private set; }

    /// <summary>
    /// JSON launch profile for a worker started by NCF. Secrets are stored outside the database.
    /// </summary>
    [MaxLength(2000)]
    public string LaunchProfileJson { get; private set; }

    /// <summary>
    /// Initializes a fine-tuning worker configuration and normalizes its optional launch profile.
    /// </summary>
    /// <param name="alias">The stable worker alias.</param>
    /// <param name="name">The display name.</param>
    /// <param name="endpoint">The worker endpoint.</param>
    /// <param name="requestTimeoutSeconds">The request timeout.</param>
    /// <param name="enabled">Whether the worker is enabled.</param>
    /// <param name="note">The administrator note.</param>
    /// <param name="launchProfileJson">Optional serialized launch settings.</param>
    public AIFineTuningWorker(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note, string launchProfileJson = null)
    {
        Apply(alias, name, endpoint, requestTimeoutSeconds, enabled, note, launchProfileJson);
    }

    /// <summary>
    /// Replaces the worker configuration and returns the same entity instance.
    /// </summary>
    /// <param name="alias">The stable worker alias.</param>
    /// <param name="name">The display name.</param>
    /// <param name="endpoint">The worker endpoint.</param>
    /// <param name="requestTimeoutSeconds">The request timeout.</param>
    /// <param name="enabled">Whether the worker is enabled.</param>
    /// <param name="note">The administrator note.</param>
    /// <param name="launchProfileJson">Optional serialized launch settings.</param>
    /// <returns>This worker entity after the update.</returns>
    public AIFineTuningWorker Update(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note, string launchProfileJson = null)
    {
        Apply(alias, name, endpoint, requestTimeoutSeconds, enabled, note, launchProfileJson);
        return this;
    }

    /// <summary>
    /// Applies the mutable worker fields and clears blank launch-profile JSON.
    /// </summary>
    /// <param name="alias">The stable worker alias.</param>
    /// <param name="name">The display name.</param>
    /// <param name="endpoint">The worker endpoint.</param>
    /// <param name="requestTimeoutSeconds">The request timeout.</param>
    /// <param name="enabled">Whether the worker is enabled.</param>
    /// <param name="note">The administrator note.</param>
    /// <param name="launchProfileJson">Optional serialized launch settings.</param>
    private void Apply(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note, string launchProfileJson)
    {
        Alias = alias;
        Name = name;
        Endpoint = endpoint;
        RequestTimeoutSeconds = requestTimeoutSeconds;
        Enabled = enabled;
        Note = note;
        LaunchProfileJson = string.IsNullOrWhiteSpace(launchProfileJson) ? null : launchProfileJson;
    }
}
