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

        /// <summary>
        /// Initializes a source matching rule used by the namespace transformation.
        /// </summary>
        /// <param name="prefix">The syntax prefix preceding the namespace text.</param>
        /// <param name="orignalKeyword">The namespace text to replace.</param>
        /// <param name="replaceWord">The replacement namespace text.</param>
        /// <param name="fileType">Optional file glob restricting the rule.</param>
        public MeetRule(string prefix, string orignalKeyword, string replaceWord, string fileType = null)
        {
            Prefix = prefix;
            OrignalKeyword = orignalKeyword;
            ReplaceWord = replaceWord;
            FileType = fileType;
        }

        /// <summary>
        /// Gets or sets the syntax prefix that must precede the namespace keyword.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the namespace text that the rule searches for.
        /// </summary>
        public string OrignalKeyword { get; set; }

        /// <summary>
        /// Gets or sets the replacement namespace text.
        /// </summary>
        public string ReplaceWord { get; set; }

        /// <summary>
        /// Gets or sets the file glob to which the rule applies, or <see langword="null"/>
        /// when the rule is not restricted by file type.
        /// </summary>
        public string FileType { get; set; }


    }
}
