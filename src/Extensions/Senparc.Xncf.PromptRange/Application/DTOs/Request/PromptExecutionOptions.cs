/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptExecutionOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705
    Change description: v0.16.4-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.20.0 execution options carry Skill policies

    Modified by: Senparc - 20261103
    Change description: v0.20.8 maintain the PromptExecutionOptions data-transfer-object definition

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptExecutionOptions
    {
        public bool PersistResult { get; set; } = true;

        public PromptTextToSpeechExecutionOptions TextToSpeech { get; set; }

        public PromptSpeechToTextExecutionOptions SpeechToText { get; set; }

        public PromptTextEmbeddingExecutionOptions TextEmbedding { get; set; }
    }

    public class PromptTextToSpeechExecutionOptions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Voice { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public float? SpeedRatio { get; set; }
    }

    public class PromptSpeechToTextExecutionOptions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AudioFileName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AudioBase64 { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AudioLocalRelativePath { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Language { get; set; }
    }

    public class PromptTextEmbeddingExecutionOptions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? VectorDbId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SearchQuery { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? SearchTopK { get; set; }
    }
}
