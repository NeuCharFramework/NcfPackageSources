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
