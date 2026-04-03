using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel
{
    /// <summary>
    ///Agent template information
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AgentTemplate))]//The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class AgentTemplate : EntityBase<int>
    {
        /// <summary>
        ///name
        /// </summary>
        [Required]
        public string Name { get; private set; }

        /// <summary>
        /// system message
        /// </summary>
        [Required]
        public string SystemMessage { get; private set; }

        /// <summary>
        /// Whether to enable
        /// </summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>
        ///Code name for PromptRange
        /// </summary>
        public string PromptCode { get; private set; }

        /// <summary>
        /// Third-party robot platform type
        /// </summary>
        public HookRobotType HookRobotType { get; private set; }

        /// <summary>
        /// Third-party robot platform parameters
        /// </summary>
        public string HookRobotParameter { get; set; }

        /// <summary>
        /// describe
        /// </summary>
        public string Description { get; private set; }

        public string Avastar { get; set; }

        /// <summary>
        /// Function Call name list, multiple separated by commas
        /// </summary>
        public string FunctionCallNames { get; private set; }

        /// <summary>
        /// McpEndpoints, multiple separated by commas
        /// </summary>
        public string McpEndpoints { get; private set; }

        /// <summary>
        /// Get the JSON object of McpEndpoints
        /// </summary>
        /// <returns>Dictionary object containing all Endpoints</returns>
        public Dictionary<string, Dictionary<string, string>> GetMcpEndpointsDict()
        {
            if (string.IsNullOrEmpty(McpEndpoints))
            {
                return new Dictionary<string, Dictionary<string, string>>();
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(McpEndpoints);
            }
            catch
            {
                return new Dictionary<string, Dictionary<string, string>>();
            }
        }

        /// <summary>
        /// Add an MCP Endpoint
        /// </summary>
        /// <param name="name">Endpoint name</param>
        /// <param name="url">Endpoint URL</param>
        /// <returns>Whether the addition was successful</returns>
        public bool AddMcpEndpoint(string name, string url)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                return false;
            }

            var endpointsDict = GetMcpEndpointsDict();
            
            // Add or update Endpoint
            endpointsDict[name] = new Dictionary<string, string>
            {
                { "url", url }
            };

            // serialize back to string
            McpEndpoints = JsonSerializer.Serialize(endpointsDict);
            return true;
        }

        /// <summary>
        /// Remove an MCP Endpoint
        /// </summary>
        /// <param name="name">Endpoint name</param>
        /// <returns>Whether the removal was successful</returns>
        public bool RemoveMcpEndpoint(string name)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(McpEndpoints))
            {
                return false;
            }

            var endpointsDict = GetMcpEndpointsDict();
            
            if (!endpointsDict.ContainsKey(name))
            {
                return false;
            }

            // Remove Endpoint
            endpointsDict.Remove(name);

            // Set to null if empty, otherwise serialize back to string
            McpEndpoints = endpointsDict.Count > 0 
                ? JsonSerializer.Serialize(endpointsDict)
                : null;
                
            return true;
        }

        //[InverseProperty(nameof(ChatGroupMember.AgentTemplate))]
        public ICollection<ChatGroupMember> ChatGroupMembers { get; private set; }

        //[InverseProperty(nameof(ChatGroup.AdminAgentTemplate))]
        public ICollection<ChatGroup> AdminChatGroups { get; private set; }

        //[InverseProperty(nameof(ChatGroup.EnterAgentTemplate))]
        public ICollection<ChatGroup> EnterAgentChatGroups { get; private set; }

        [InverseProperty(nameof(ChatGroupHistory.FromAgentTemplate))]
        public ICollection<ChatGroupHistory> FromChatGroupHistories { get; set; }

        [InverseProperty(nameof(ChatGroupHistory.ToAgentTemplate))]
        public ICollection<ChatGroupHistory> ToChatGroupHistoies { get; set; }

        private AgentTemplate() { }

        public AgentTemplate(string name, string systemMessage, bool enable, string description, string promptCode, HookRobotType hookRobotType, string hookRobotParameter, string avastar = null, string functionCallNames = null, string mcpEndpoints = null)
        {
            Name = name;
            SystemMessage = systemMessage;
            Enable = enable;
            Description = description;
            PromptCode = promptCode;
            HookRobotType = hookRobotType;
            HookRobotParameter = hookRobotParameter;
            Avastar = avastar;
            FunctionCallNames = functionCallNames;
            McpEndpoints = mcpEndpoints;
        }

        public bool EnableAgent()
        {
            Enable = true;
            return true;
        }

        public bool DisableAgent()
        {
            Enable = false;
            return true;
        }

        public void UpdateFromDto(AgentTemplateDto agentTemplateDto)
        {
            Name = agentTemplateDto.Name;
            SystemMessage = agentTemplateDto.SystemMessage;
            Enable = agentTemplateDto.Enable;
            Description = agentTemplateDto.Description;
            PromptCode = agentTemplateDto.PromptCode;
            HookRobotType = agentTemplateDto.HookRobotType;
            HookRobotParameter = agentTemplateDto.HookRobotParameter;
            FunctionCallNames = agentTemplateDto.FunctionCallNames;
            Avastar = agentTemplateDto.Avastar;
            McpEndpoints = agentTemplateDto.McpEndpoints;
        }
    }

    /// <summary>
    /// Third-party robot platform type
    /// </summary>
    public enum HookRobotType
    {
        None = 0,
        /// <summary>
        /// WeChat public account
        /// </summary>
        WeChatMp = 1,
        /// <summary>
        /// Enterprise WeChat robot
        /// </summary>
        WeChatWorkRobot = 2
    }
}
