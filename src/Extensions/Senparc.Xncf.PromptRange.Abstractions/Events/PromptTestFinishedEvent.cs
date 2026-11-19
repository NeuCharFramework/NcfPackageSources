/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptTestFinishedEvent.cs
    File description: PromptTestFinishedEvent related implementation

    Created by: Senparc - 20260218

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.3-preview8 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.PromptRange.Abstractions.Events
{
    //Legacy comment translated to English.
    public record PromptTestFinishedEvent(
        string PromptCode,
        string OriginalPrompt,
        string UserInput,
        string ModelOutput,
        double Score,
        string EvaluationReason,
        string ModelId,
        double Temperature
    ) : IntegrationEvent;


}