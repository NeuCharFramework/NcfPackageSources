/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptExecutionOptions.cs
    文件功能描述：PromptExecutionOptions 数据传输对象定义
    
    
    创建标识：Senparc - 20260705

----------------------------------------------------------------*/

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptExecutionOptions
    {
        public PromptTextToSpeechExecutionOptions TextToSpeech { get; set; }

        public PromptSpeechToTextExecutionOptions SpeechToText { get; set; }

        public PromptTextEmbeddingExecutionOptions TextEmbedding { get; set; }
    }

    public class PromptTextToSpeechExecutionOptions
    {
        /// <summary>
        /// 音色（alloy / ash / ballad / coral / echo / fable / onyx / nova / sage / shimmer / verse）
        /// </summary>
        public string Voice { get; set; }

        /// <summary>
        /// 输出格式（mp3 / opus / aac / flac / wav / pcm）
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 语速（建议 0.5 - 2.0）
        /// </summary>
        public float? SpeedRatio { get; set; }
    }

    public class PromptSpeechToTextExecutionOptions
    {
        /// <summary>
        /// 文件名（用于推断扩展名）
        /// </summary>
        public string AudioFileName { get; set; }

        /// <summary>
        /// 音频 Base64（支持 data:*;base64, 前缀）
        /// </summary>
        public string AudioBase64 { get; set; }

        /// <summary>
        /// 已存储在 App_Data 下的相对路径（用于重复打靶 / 连发）
        /// </summary>
        public string AudioLocalRelativePath { get; set; }

        /// <summary>
        /// 识别语言（可空，如：zh / en）
        /// </summary>
        public string Language { get; set; }
    }

    public class PromptTextEmbeddingExecutionOptions
    {
        /// <summary>
        /// 向量库 ID（来自 AIKernel 的 AIVector）
        /// </summary>
        public int? VectorDbId { get; set; }

        /// <summary>
        /// Collection 名称（可空，空则按规则自动生成）
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// 首次打靶后可直接执行一次查询测试（可空）
        /// </summary>
        public string SearchQuery { get; set; }

        /// <summary>
        /// 查询测试 TopK（默认 3）
        /// </summary>
        public int? SearchTopK { get; set; }
    }
}
