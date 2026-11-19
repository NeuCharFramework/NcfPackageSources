/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MeetRule.cs
    File description: MeetRule related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.14 Updated module behavior, localization metadata, and English source comments

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
