/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TacticTree_GetResponse.cs
    File description: TacticTree_GetResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>Initializes a tactic-tree response.</summary>
        /// <param name="rootNodeList">The root tree nodes.</param>
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

        /// <summary>Initializes an empty tree node for model binding.</summary>
        public TreeNode()
        {
        }

        // public TreeNode(T data)
        // {
        //     Data = data;
        // }

        /// <summary>Initializes a tree node with display and level metadata.</summary>
        /// <param name="name">The node name.</param>
        /// <param name="nickName">The display nickname.</param>
        /// <param name="data">The node payload.</param>
        /// <param name="level">The tree depth.</param>
        public TreeNode(string name, string nickName, T data, int level)
        {
            Name = name;
            NickName = nickName;
            Data = data;
            Level = level;
        }
    }
}
