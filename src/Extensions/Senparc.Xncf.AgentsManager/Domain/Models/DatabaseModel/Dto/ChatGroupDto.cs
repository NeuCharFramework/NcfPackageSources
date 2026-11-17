/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupDto.cs
    File description: ChatGroupDto related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class ChatGroupDto : DtoBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// whether enabled
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// status
        /// </summary>
        public ChatGroupState State { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public ChatGroupContextSharingMode? ContextSharingMode { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int AdminAgentTemplateId { get; set; }

        //public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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
