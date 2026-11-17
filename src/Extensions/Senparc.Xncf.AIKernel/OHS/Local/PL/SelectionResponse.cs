/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SelectionResponse.cs
    File description: Related implementation.

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

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

    public AIModelSelectionResponse()
    {
    }

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

    public AIVectorSelectionResponse()
    {
    }

    public AIVectorSelectionResponse(AIVector vector)
    {
        Id = vector.Id;
        Alias = vector.Alias;
        Show = vector.Show;
    }
}
