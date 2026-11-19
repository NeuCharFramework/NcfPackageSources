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
    public class Module_StatResponse
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int InstalledXncfCount { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int UpdateVersionXncfCount { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int NewXncfCount { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MissingXncfCount { get; set; }
    }

    public class Module_GetItemResponse
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool MustUpdate { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public XncfModuleDto XncfModule { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Response_XncfRegister XncfRegister { get; set; }

        public List<Response_FunctionParameterInfoCollection> FunctionParameterInfoCollection { get; set; }

        public class Response_XncfRegister
        {
            /// <summary>
            ///Legacy comment translated to English.
            /// </summary>
            public string AreaHomeUrl { get; set; }
            /// <summary>
            ///Legacy comment translated to English.
            /// </summary>
            public string MenuName { get; set; }
            /// <summary>
            ///Legacy comment translated to English.
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
            ///Legacy comment translated to English.
            /// </summary>
            public string Uid { get; set; }

            /// <summary>
            ///Legacy comment translated to English.
            /// </summary>
            public List<Ncf.Core.Areas.AreaPageMenuItem> AreaPageMenuItems { get; set; }

            /// <summary>
            /// interface
            /// </summary>
            public List<string> Interfaces { get; set; }

            /// <summary>
            ///Legacy comment translated to English.
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
                ///Legacy comment translated to English.
                /// </summary>
                public RegisteredThreadInfo_Value Value { get; set; }

                public class RegisteredThreadInfo_Key
                {
                    /// <summary>
                    /// threadname
                    /// </summary>
                    public string Name { get; set; }
                    /// <summary>
                    ///Legacy comment translated to English.
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

        public class Response_FunctionParameterInfoCollection
        {
            public Response_FunctionParameterInfoCollection_Key Key { get; set; }
            public List<FunctionParameterInfo> Value { get; set; }

        }

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

    public class Module_RunFunctionResponse
    {
        public string Msg { get; set; }
        public string Log { get; set; }
        public string Exception { get; set; }
        public string TempId { get; set; }
    }
}
