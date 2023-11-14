using System;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptItem_AddRequest
    {
        //public int PromptGroupId { get; set; }

        #region Model Config
        /// <summary>
        /// TopP
        /// </summary>
        public float TopP { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// 最大 Token 数
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public float FrequencyPenalty { get; set; }


        public float PresencePenalty { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string StopSequences { get; set; }
        #endregion

        public int ModelId { get; set; }
        
        public string Content { get; set; }

        public string Version { get; set; }

        public int NumsOfResults { get; set; } = 0;

    }
}
