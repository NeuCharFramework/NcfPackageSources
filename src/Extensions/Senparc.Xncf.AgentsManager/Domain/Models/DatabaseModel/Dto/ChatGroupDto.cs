/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupDto.cs
    File description: ChatGroupDto related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// Transfers Agent chat-group identity, state, and administrator/entry-agent configuration.
    /// </summary>
    public class ChatGroupDto : DtoBase<int>
    {
        /// <summary>
        /// Gets or sets the group display name.
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
        /// Gets or sets the context-sharing policy.
        /// </summary>
        public ChatGroupContextSharingMode? ContextSharingMode { get; set; }

        /// <summary>
        /// Gets or sets the administrator Agent-template identifier.
        /// </summary>
        public int AdminAgentTemplateId { get; set; }

        //public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>
        /// Gets or sets the entry Agent-template identifier.
        /// </summary>

        public int EnterAgentTemplateId { get; set; }

        //public AgentTemplate EnterAgentTemplate { get; set; }

        /// <summary>Initializes an empty chat-group DTO.</summary>
        public ChatGroupDto() { }

        /// <summary>Initializes a chat-group DTO from its core configuration.</summary>
        public ChatGroupDto(string name, bool enable, ChatGroupState state, string description, int adminAgentTemplateId, int enterAgentTemplateId)
        {
            Name = name;
            Enable = enable;
            State = state;
            Description = description;
            AdminAgentTemplateId = adminAgentTemplateId;
            EnterAgentTemplateId = enterAgentTemplateId;
        }

        /// <summary>Maps a persisted chat group to a DTO.</summary>
        /// <param name="chatGroup">The source chat group.</param>
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

        /// <summary>Changes the group state to running.</summary>
        public void Start()
        {
            State = ChatGroupState.Running;
        }

        /// <summary>Changes the group state to finished.</summary>
        public void Finish()
        {
            State = ChatGroupState.Finished;
        }
    }
}
