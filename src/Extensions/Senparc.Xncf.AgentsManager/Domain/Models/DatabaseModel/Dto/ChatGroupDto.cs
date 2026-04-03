using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// ChatGroup Database Entity DTO
    /// </summary>
    public class ChatGroupDto : DtoBase<int>
    {
        /// <summary>
        ///group name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether to enable
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// state
        /// </summary>
        public ChatGroupState State { get; set; }

        /// <summary>
        /// describe
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Administrator agent template ID
        /// </summary>
        public int AdminAgentTemplateId { get; set; }

        //public AgentTemplate AdminAgentTemplate { get; set; }

        /// <summary>
        /// Connector agent template ID
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
