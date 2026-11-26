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
        /// <summary>Gets or sets whether the generated result is persisted.</summary>
        public bool PersistResult { get; set; } = true;

        /// <summary>Gets or sets text-to-speech execution options.</summary>
        public PromptTextToSpeechExecutionOptions TextToSpeech { get; set; }

        /// <summary>Gets or sets speech-to-text execution options.</summary>
        public PromptSpeechToTextExecutionOptions SpeechToText { get; set; }

        /// <summary>Gets or sets text-embedding search options.</summary>
        public PromptTextEmbeddingExecutionOptions TextEmbedding { get; set; }
    }

    public class PromptTextToSpeechExecutionOptions
    {
        /// <summary>
        /// Gets or sets the requested voice identifier.
        /// </summary>
        public string Voice { get; set; }

        /// <summary>
        /// Gets or sets the output audio format.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the playback speed multiplier.
        /// </summary>
        public float? SpeedRatio { get; set; }
    }

    public class PromptSpeechToTextExecutionOptions
    {
        /// <summary>
        /// Gets or sets the uploaded audio file name.
        /// </summary>
        public string AudioFileName { get; set; }

        /// <summary>
        /// Gets or sets the base64-encoded audio content.
        /// </summary>
        public string AudioBase64 { get; set; }

        /// <summary>
        /// Gets or sets a server-local relative audio path.
        /// </summary>
        public string AudioLocalRelativePath { get; set; }

        /// <summary>
        /// Gets or sets the language hint for speech recognition.
        /// </summary>
        public string Language { get; set; }
    }

    public class PromptTextEmbeddingExecutionOptions
    {
        /// <summary>
        /// Gets or sets the vector database identifier.
        /// </summary>
        public int? VectorDbId { get; set; }

        /// <summary>
        /// Gets or sets the vector collection name.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Gets or sets the semantic-search query.
        /// </summary>
        public string SearchQuery { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of vector results.
        /// </summary>
        public int? SearchTopK { get; set; }
    }
}
