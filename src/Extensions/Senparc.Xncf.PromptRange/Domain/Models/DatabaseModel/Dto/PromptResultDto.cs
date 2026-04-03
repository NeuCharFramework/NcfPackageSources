using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class PromptResultDto : DtoBase
    {
        /// <summary>
        ///ID primary key
        /// </summary>
        public int Id { get; set; }

        #region LlmModel

        /// <summary>
        ///Id of LlmModel
        /// </summary>
        public int LlmModelId { get; set; }

        /// <summary>
        /// LlmModel type LlmModel
        /// </summary>
        public LlModel LlModel { get; set; }

        #endregion

        #region Prompt Item

        /// <summary>
        ///PromptItem of type PromptItem
        /// </summary>
        public int PromptItemId { get; set; }

        /// <summary>
        ///PromptItemVersion of type string
        /// </summary>
        public string PromptItemVersion { get; set; }

        #endregion


        /// <summary>
        ///result string
        /// </summary>
        public string ResultString { get; set; }

        /// <summary>
        /// Time spent, unit: milliseconds
        /// </summary>
        public double CostTime { get; set; }

        /// <summary>
        /// Robot scoring, 0-100 points
        /// </summary>
        public decimal RobotScore { get; set; }

        /// <summary>
        /// Human scoring, 0-100 points
        /// </summary>
        public decimal HumanScore { get; set; }

        /// <summary>
        /// final score
        /// </summary>
        public decimal FinalScore { get; set; }


        /// <summary>
        /// Robot test expected results
        /// </summary>
        public string RobotTestExceptedResult { get; set; }

        /// <summary>
        /// Whether the robot test results are exactly equal
        /// </summary>
        public bool IsRobotTestExactlyEquat { get; set; }

        /// <summary>
        /// Test type, the enumeration includes: text, graphics, sound
        /// </summary>
        public TestType TestType { get; set; }

        /// <summary>
        /// Prompt the number of Tokens spent
        /// </summary>
        public int PromptCostToken { get; set; }

        /// <summary>
        /// The number of Tokens spent as a result
        /// </summary>
        public int ResultCostToken { get; set; }

        /// <summary>
        ///Total number of Tokens spent
        /// </summary>
        public int TotalCostToken { get; set; }

        /// <summary>
        /// Target practice mode: Chat (chat mode) or Single (single test mode), can be empty (compatible with old data)
        /// </summary>
        public ResultMode? Mode { get; set; }

        /// <summary>
        /// SystemMessage (Prompt content, final content after completing parameter replacement)
        /// Used in conversation mode to ensure that even if the Prompt content or parameters change, the historically used SystemMessage can be traced
        /// </summary>
        public string SystemMessage { get; set; }
    }
}