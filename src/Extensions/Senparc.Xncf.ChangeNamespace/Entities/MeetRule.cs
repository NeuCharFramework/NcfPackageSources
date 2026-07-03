/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MeetRule.cs
    文件功能描述：MeetRule 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.ChangeNamespace
{
    public class MeetRule
    {

        public MeetRule(string prefix, string orignalKeyword, string replaceWord, string fileType = null)
        {
            Prefix = prefix;
            OrignalKeyword = orignalKeyword;
            ReplaceWord = replaceWord;
            FileType = fileType;
        }

        public string Prefix { get; set; }
        public string OrignalKeyword { get; set; }
        public string ReplaceWord { get; set; }
        public string FileType { get; set; }


    }
}
