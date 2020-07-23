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
