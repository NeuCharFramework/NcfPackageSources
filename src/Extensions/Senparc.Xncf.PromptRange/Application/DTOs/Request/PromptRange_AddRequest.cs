using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public class PromptRange_AddRequest
{
    /// <summary>
    /// Shooting range code (user-defined)
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    /// Shooting range name (from version number generation)
    /// </summary>
    [Required]
    public string RangeName { get; set; }

    /// <summary>
    ///Expected result Json
    /// </summary>
    public string ExpectedResultsJson { get; set; }
}