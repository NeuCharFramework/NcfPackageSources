/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Enums.cs
    File description: Enums related implementation

    Created by: Senparc - 20231228

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum ConfigModelType
    {
        TextCompletion = 1,
        Chat = 2,
        TextEmbedding = 3,
        TextToImage = 4,
        ImageToText = 5,
        TextToSpeech = 6,
        SpeechToText = 7,
        SpeechRecognition = 8
    }
}
