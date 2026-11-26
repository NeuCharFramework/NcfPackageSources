/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Module_Response.cs
    File description: Module_Response related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    /// <summary>Contains aggregate counts for installed and available XNCF modules.</summary>
    public class Module_StatResponse
    {
        /// <summary>
        /// Gets or sets the number of persisted modules.
        /// </summary>
        public int InstalledXncfCount { get; set; }
        /// <summary>
        /// Gets or sets the number of installed modules with newer versions.
        /// </summary>
        public int UpdateVersionXncfCount { get; set; }
        /// <summary>
        /// Gets or sets the number of modules available but not installed.
        /// </summary>
        public int NewXncfCount { get; set; }
        /// <summary>
        /// Gets or sets the number of persisted modules whose registrations are missing.
        /// </summary>
        public int MissingXncfCount { get; set; }
    }

    /// <summary>Contains detailed metadata for one installed XNCF module.</summary>
    public class Module_GetItemResponse
    {
        /// <summary>
        /// Gets or sets whether the module should be updated before inspection.
        /// </summary>
        public bool MustUpdate { get; set; }
        /// <summary>
        /// Gets or sets the persisted module DTO.
        /// </summary>
        public XncfModuleDto XncfModule { get; set; }
        /// <summary>
        /// Gets or sets the runtime registration metadata.
        /// </summary>
        public Response_XncfRegister XncfRegister { get; set; }

        /// <summary>Gets or sets the discovered function parameter metadata.</summary>
        public List<Response_FunctionParameterInfoCollection> FunctionParameterInfoCollection { get; set; }

        public class Response_XncfRegister
        {
            /// <summary>
            /// Gets or sets the module area's home URL.
            /// </summary>
            public string AreaHomeUrl { get; set; }
            /// <summary>
            /// Gets or sets the administration menu name.
            /// </summary>
            public string MenuName { get; set; }
            /// <summary>
            /// Gets or sets the module description.
            /// </summary>
            public string Description { get; set; }
            /// <summary>
            /// icon
            /// </summary>
            public string Icon { get; set; }
            /// <summary>
            /// version
            /// </summary>
            public string Version { get; set; }
            /// <summary>
            /// Gets or sets the stable module UID.
            /// </summary>
            public string Uid { get; set; }

            /// <summary>
            /// Gets or sets the module area page menu items.
            /// </summary>
            public List<Ncf.Core.Areas.AreaPageMenuItem> AreaPageMenuItems { get; set; }

            /// <summary>
            /// interface
            /// </summary>
            public List<string> Interfaces { get; set; }

            /// <summary>
            /// Gets or sets the number of discovered functions.
            /// </summary>
            public int FunctionCount { get; set; }

            /// <summary>
            /// threadinformation
            /// </summary>
            public IEnumerable<Response_XncfRegister_RegisteredThreadInfo> RegisteredThreadInfo { get; set; }


            public class Response_XncfRegister_RegisteredThreadInfo
            {
                /// <summary>
                /// threadinformation
                /// </summary>
                public RegisteredThreadInfo_Key Key { get; set; }
                /// <summary>
                /// Gets or sets the runtime thread state details.
                /// </summary>
                public RegisteredThreadInfo_Value Value { get; set; }

                public class RegisteredThreadInfo_Key
                {
                    /// <summary>
                    /// threadname
                    /// </summary>
                    public string Name { get; set; }
                    /// <summary>
                    /// Gets or sets the human-readable thread description.
                    /// </summary>
                    public string StoryHtml { get; set; }
                }
                public class RegisteredThreadInfo_Value
                {
                    public bool IsAlive { get; set; }
                    public bool? IsBackground { get; set; }
                    public ThreadState? ThreadState { get; set; }
                    public string ThreadStateStr { get; set; }

                }
            }
        }

        /// <summary>Associates a function identity with its parameter metadata.</summary>
        public class Response_FunctionParameterInfoCollection
        {
            public Response_FunctionParameterInfoCollection_Key Key { get; set; }
            public List<FunctionParameterInfo> Value { get; set; }

        }

        /// <summary>Identifies a discovered function by name and description.</summary>
        public class Response_FunctionParameterInfoCollection_Key
        {
            public string Name { get; set; }
            public string Description { get; set; }

            public Response_FunctionParameterInfoCollection_Key(string name, string description)
            {
                Name = name;
                Description = description;
            }

        }
    }

    /// <summary>Contains the rendered output and diagnostics from a module function invocation.</summary>
    public class Module_RunFunctionResponse
    {
        public string Msg { get; set; }
        public string Log { get; set; }
        public string Exception { get; set; }
        public string TempId { get; set; }
    }
}
