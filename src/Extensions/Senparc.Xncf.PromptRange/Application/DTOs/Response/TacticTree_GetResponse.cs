/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TacticTree_GetResponse.cs
    文件功能描述：TacticTree_GetResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Nodes;
using Senparc.CO2NET.WebApi;
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