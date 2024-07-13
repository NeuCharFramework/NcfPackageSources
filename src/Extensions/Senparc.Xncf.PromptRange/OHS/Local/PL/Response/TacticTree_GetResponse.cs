using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class TacticTree_GetResponse
    {
        public List<TreeNode<PromptItem_GetIdAndNameResponse>> RootNodeList { get; set; }

        public DateTime QueryTime { get; set; }

        public TacticTree_GetResponse(List<TreeNode<PromptItem_GetIdAndNameResponse>> rootNodeList)
        {
            RootNodeList = rootNodeList;
            QueryTime = DateTime.Now;
        }
    }

    public class TreeNode<T>
    {
        public string Name { get; set; }
        public string NickName { get; set; }

        public List<TreeNode<T>> Children { get; set; } = new();

        public Dictionary<string, object> Attributes { get; set; } = new();

        public T Data { get; set; }

        public int Level { get; set; }

        public TreeNode()
        {
        }

        // public TreeNode(T data)
        // {
        //     Data = data;
        // }

        public TreeNode(string name, string nickName, T data, int level)
        {
            Name = name;
            NickName = nickName;
            Data = data;
            Level = level;
        }
    }
}