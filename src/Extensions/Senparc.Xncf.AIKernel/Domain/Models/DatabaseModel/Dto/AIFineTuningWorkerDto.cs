using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;

public sealed class AIFineTuningWorkerDto : DtoBase
{
    public int Id { get; set; }
    public string Alias { get; set; }
    public string Name { get; set; }
    public string Endpoint { get; set; }
    public int RequestTimeoutSeconds { get; set; }
    public bool Enabled { get; set; }
    public string Note { get; set; }
    public bool HasConfiguredSecret { get; set; }

    public AIFineTuningWorkerDto()
    {
    }

    public AIFineTuningWorkerDto(AIFineTuningWorker worker, bool hasConfiguredSecret)
    {
        Id = worker.Id;
        Alias = worker.Alias;
        Name = worker.Name;
        Endpoint = worker.Endpoint;
        RequestTimeoutSeconds = worker.RequestTimeoutSeconds;
        Enabled = worker.Enabled;
        Note = worker.Note;
        HasConfiguredSecret = hasConfiguredSecret;
        AddTime = worker.AddTime;
        LastUpdateTime = worker.LastUpdateTime;
        TenantId = worker.TenantId;
    }
}
