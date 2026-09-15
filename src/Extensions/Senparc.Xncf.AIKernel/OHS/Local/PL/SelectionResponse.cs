/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SelectionResponse.cs
    文件功能描述：AIKernel 选择器安全响应模型

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

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
