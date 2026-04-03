using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    ///ChatGroup database entity
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroup))]//The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class ChatGroup : EntityBase<int>
    {
        /// <summary>
        ///group name
        /// </summary>
        [Required]
        public string Name { get; private set; }

        /// <summary>
        /// Whether to enable
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        /// state
        /// </summary>
        [Required]
        public ChatGroupState State { get; private set; }

        /// <summary>
        /// describe
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Administrator agent template ID
        /// </summary>
        [Required]
        //[ForeignKey(nameof(AdminAgentTemplate))]
        public int AdminAgentTemplateId { get; private set; }
        //[InverseProperty(nameof(AgentTemplate.AdminChatGroups))]
        public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>
        /// Connector agent template ID
        /// </summary>
        [Required]
        //[ForeignKey(nameof(EnterAgentTemplate))]
        public int EnterAgentTemplateId { get; private set; }

        //[InverseProperty(nameof(AgentTemplate.EnterAgentChatGroups))]
        public AgentTemplate EnterAgentTemplate { get; set; }

        //public ICollection<ChatGroupMember> ChatGroupMembers { get; set; }


        private ChatGroup() { }

        public ChatGroup(string name, bool enable, ChatGroupState state, string description, int adminAgentTemplateId, int enterAgentTemplateId)
        {
            Name = name;
            Enable = enable;
            State = state;
            Description = description;
            AdminAgentTemplateId = adminAgentTemplateId;
            EnterAgentTemplateId = enterAgentTemplateId;
        }

        public ChatGroup(ChatGroupDto chatGroupDto)
        {
            Update(chatGroupDto);
        }

        public void Update(ChatGroupDto chatGroupDto)
        {
            Name = chatGroupDto.Name;
            Enable = chatGroupDto.Enable;
            State = chatGroupDto.State;
            Description = chatGroupDto.Description;
            AdminAgentTemplateId = chatGroupDto.AdminAgentTemplateId;
            EnterAgentTemplateId = chatGroupDto.EnterAgentTemplateId;
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

    /// <summary>
    ///group status
    /// </summary>
    public enum ChatGroupState
    {
        /// <summary>
        /// not started
        /// </summary>
        Unstart = 0,
        /// <summary>
        /// running
        /// </summary>
        Running = 1,
        /// <summary>
        /// ended
        /// </summary>
        Finished = 2,
    }
}
