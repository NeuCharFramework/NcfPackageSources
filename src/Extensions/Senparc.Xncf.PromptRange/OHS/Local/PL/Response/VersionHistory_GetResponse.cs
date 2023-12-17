using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class VersionHistory_GetResponse
    {
        public List<TreeNode<PromptItem>> VersionHistory { get; set; }

        public DateTime QueryTime { get; set; }

        public VersionHistory_GetResponse(List<TreeNode<PromptItem>> versionHistory)
        {
            VersionHistory = versionHistory;
            QueryTime = DateTime.Now;
        }

        public VersionHistory_GetResponse(TreeNode<PromptItem> versionHistory)
        {
            VersionHistory = new List<TreeNode<PromptItem>> { versionHistory };

            QueryTime = DateTime.Now;
        }
    }

    public class TreeNode<T>
    {
        public string Name { get; set; }

        public List<TreeNode<T>> Children { get; set; } = new();

        public Dictionary<string, object> Extensions { get; set; } = new();

        public T Data { get; set; }

        public TreeNode()
        {
        }

        public TreeNode(T data)
        {
            Data = data;
        }

        public TreeNode(string name, T data)
        {
            Name = name;
            Data = data;
        }
    }
}