using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class PromptResultDto : DtoBase
    {
        /// <summary>
        /// ID 主键
        /// </summary>
        public int Id { get; set; }

        #region LlmModel

        /// <summary>
        /// LlmModel 的 Id
        /// </summary>
        public int LlmModelId { get; set; }

        /// <summary>
        /// LlmModel 类型的 LlmModel
        /// </summary>
        public LlModel LlModel { get; set; }

        #endregion

        #region Prompt Item

        /// <summary>
        /// PromptItem类型的PromptItem
        /// </summary>
        public int PromptItemId { get; set; }

        /// <summary>
        /// string类型的PromptItemVersion
        /// </summary>
        public string PromptItemVersion { get; set; }

        #endregion


        /// <summary>
        /// 结果字符串
        /// </summary>
        public string ResultString { get; set; }

        /// <summary>
        /// 花费时间，单位：毫秒
        /// </summary>
        public double CostTime { get; set; }

        /// <summary>
        /// 机器人打分，0-100分
        /// </summary>
        public decimal RobotScore { get; set; }

        /// <summary>
        /// 人类打分，0-100分
        /// </summary>
        public decimal HumanScore { get; set; }

        /// <summary>
        /// 最终得分
        /// </summary>
        public decimal FinalScore { get; set; }


        /// <summary>
        /// 机器人测试期望结果
        /// </summary>
        public string RobotTestExceptedResult { get; set; }

        /// <summary>
        /// 是否机器人测试结果完全相等
        /// </summary>
        public bool IsRobotTestExactlyEquat { get; set; }

        /// <summary>
        /// 测试类型，枚举中包含：文字、图形、声音
        /// </summary>
        public TestType TestType { get; set; }

        /// <summary>
        /// 提示花费的 Token 数量
        /// </summary>
        public int PromptCostToken { get; set; }

        /// <summary>
        /// 结果花费的 Token 数量
        /// </summary>
        public int ResultCostToken { get; set; }

        /// <summary>
        /// 总共花费的 Token 数量
        /// </summary>
        public int TotalCostToken { get; set; }
    }
}