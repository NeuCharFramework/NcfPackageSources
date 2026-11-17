/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItemTreeList.cs
    File description: PromptItemTreeList related implementation

    Created by: Senparc - 20240713

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Models.Entities
{
    public class PromptItemTreeList : List<PromptItemTreeNode>
    {
        public PromptItemTreeList() { }

        public PromptItemTreeNode this[string key]
        {
            get
            {
                return this.FirstOrDefault(z => z.Key == key);
            }
        }

        public PromptItemTreeNode AddNode(string key, string text, string value, int level, int subNodeCount)
        {
            PromptItemTreeNode node = new PromptItemTreeNode(key, text, value, level, subNodeCount);
            this.Add(node);
            return node;
        }
    }
}
