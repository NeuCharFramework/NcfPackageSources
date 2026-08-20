/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupDto.cs
    文件功能描述：ChatGroupDto 相关实现


    创建标识：Senparc - 20240616

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// ChatGroup 数据库实体 DTO
    /// </summary>
    public class ChatGroupDto : DtoBase<int>
    {
        /// <summary>
        /// 群名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 状态
        /// </summary>
        public ChatGroupState State { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 群组上下文分发策略。null 保持旧群组原有行为。
        /// </summary>
        public ChatGroupContextSharingMode? ContextSharingMode { get; set; }

        /// <summary>
        /// 管理员代理模板Id
        /// </summary>
        public int AdminAgentTemplateId { get; set; }

        //public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>
        /// 对接人代理模板Id
        /// </summary>

        public int EnterAgentTemplateId { get; set; }

        //public AgentTemplate EnterAgentTemplate { get; set; }

        public ChatGroupDto() { }

        public ChatGroupDto(string name, bool enable, ChatGroupState state, string description, int adminAgentTemplateId, int enterAgentTemplateId)
        {
            Name = name;
            Enable = enable;
            State = state;
            Description = description;
            AdminAgentTemplateId = adminAgentTemplateId;
            EnterAgentTemplateId = enterAgentTemplateId;
        }

        public ChatGroupDto(ChatGroup chatGroup)
        {
            Name = chatGroup.Name;
            Enable = chatGroup.Enable;
            State = chatGroup.State;
            Description = chatGroup.Description;
            ContextSharingMode = chatGroup.ContextSharingMode;
            AdminAgentTemplateId = chatGroup.AdminAgentTemplateId;
            EnterAgentTemplateId = chatGroup.EnterAgentTemplateId;
        }

        public void Start()
        {
            State = ChatGroupState.Running;
        }

        public void Finish()
        {
            State = ChatGroupState.Finished;
        }
    }
}
