namespace Senparc.Xncf.PromptRange.Domain.Models.Entities
{
    public class PromptItemTreeNode
    {
        public string Key { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public int Level { get; set; }
        public int SubNodeCount { get; set; }

        public PromptItemTreeNode(string key, string text, string value, int level, int subNodeCount)
        {
            Key = key;
            Text = text;
            Value = value;
            Level = level;
            SubNodeCount = subNodeCount;
        }

    }
}