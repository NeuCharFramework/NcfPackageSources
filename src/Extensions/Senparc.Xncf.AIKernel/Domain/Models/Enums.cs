using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Models
{
    /// <summary>
    /// 和 Senaprc.AI 中的 ConfigModel 匹配（包括值），是其子集
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
