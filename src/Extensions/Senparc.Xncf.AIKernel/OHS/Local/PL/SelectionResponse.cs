/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SelectionResponse.cs
    File description: Related implementation.

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL;

/// <summary>
/// AI model fields required by selectors. Credentials and provider details are
/// intentionally excluded.
/// </summary>
public sealed class AIModelSelectionResponse
{
    public int Id { get; set; }

    public string Alias { get; set; }

    public bool Show { get; set; }

    /// <summary>
    /// Initializes an empty model-selector response.
    /// </summary>
    public AIModelSelectionResponse()
    {
    }

    /// <summary>
    /// Projects non-sensitive selector fields from an AI model.
    /// </summary>
    /// <param name="model">The model to project.</param>
    public AIModelSelectionResponse(AIModel model)
    {
        Id = model.Id;
        Alias = model.Alias;
        Show = model.Show;
    }
}

/// <summary>
/// Vector database fields required by selectors. Connection strings and notes
/// are intentionally excluded.
/// </summary>
public sealed class AIVectorSelectionResponse
{
    public int Id { get; set; }

    public string Alias { get; set; }

    public bool Show { get; set; }

    /// <summary>
    /// Initializes an empty vector-selector response.
    /// </summary>
    public AIVectorSelectionResponse()
    {
    }

    /// <summary>
    /// Projects non-sensitive selector fields from a vector database configuration.
    /// </summary>
    /// <param name="vector">The vector configuration to project.</param>
    public AIVectorSelectionResponse(AIVector vector)
    {
        Id = vector.Id;
        Alias = vector.Alias;
        Show = vector.Show;
    }
}
