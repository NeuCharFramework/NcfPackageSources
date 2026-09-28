using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public interface IWeixinClawBindingProfileResolver
{
    Task<WeixinClawBindingProfileResolution> ResolveAsync(
        int accountId,
        string bindingCode,
        CancellationToken cancellationToken = default);
}

public sealed class WeixinClawBindingProfileResolution
{
    public int Id { get; init; }
    public int AccountId { get; init; }
    public string Name { get; init; }
    public int AdminUserId { get; init; }
    public int? WorkflowId { get; init; }
    public int AiModelId { get; init; }
    public int Mode { get; init; }
    public bool EnableNeuBell { get; init; }
    public bool EnableWorkflow { get; init; }
}
