using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

public class PromptRangeDto : DtoBase
{
    /// <summary>
    /// primary key ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Shooting range code (user-defined)
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    /// Shooting range name (from version number generation)
    /// </summary>
    public string RangeName { get; set; }

    // /// <summary>
    // /// Expected result Json
    // /// </summary>
    // public string ExpectedResultsJson { get;  set; }
}