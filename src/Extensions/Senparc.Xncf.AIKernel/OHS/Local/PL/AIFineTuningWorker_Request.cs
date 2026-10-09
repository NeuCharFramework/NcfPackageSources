using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL;

public sealed class AIFineTuningWorker_CreateOrEditRequest
{
    public int Id { get; set; }

    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,49}$")]
    public string Alias { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(250)]
    public string Endpoint { get; set; }

    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    public bool Enabled { get; set; }

    [StringLength(500)]
    public string Note { get; set; }
}
