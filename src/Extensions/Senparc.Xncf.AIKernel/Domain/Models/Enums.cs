/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Enums.cs
    文件功能描述：Enums 相关实现
    
    
    创建标识：Senparc - 20231228
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
