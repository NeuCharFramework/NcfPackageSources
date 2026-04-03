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
        ///Number of installed modules
        /// </summary>
        public int InstalledXncfCount { get; set; }
        /// <summary>
        ///Number of modules to be updated
        /// </summary>
        public int UpdateVersionXncfCount { get; set; }
        /// <summary>
        ///Number of new modules
        /// </summary>
        public int NewXncfCount { get; set; }
        /// <summary>
        ///Number of exception modules
        /// </summary>
        public int MissingXncfCount { get; set; }
    }

    public class Module_GetItemResponse
    {
        /// <summary>
        /// The page must be refreshed. When MustUpdate is true, there must be exception information
        /// </summary>
        public bool MustUpdate { get; set; }
        /// <summary>
        ///module information
        /// </summary>
        public XncfModuleDto XncfModule { get; set; }
        /// <summary>
        ///XNCF module registration information
        /// </summary>
        public Response_XncfRegister XncfRegister { get; set; }

        public List<Response_FunctionParameterInfoCollection> FunctionParameterInfoCollection { get; set; }

        public class Response_XncfRegister
        {
            /// <summary>
            ///Homepage URL
            /// </summary>
            public string AreaHomeUrl { get; set; }
            /// <summary>
            ///Menu display name
            /// </summary>
            public string MenuName { get; set; }
            /// <summary>
            /// icon
            /// </summary>
            public string Icon { get; set; }
            /// <summary>
            /// Version
            /// </summary>
            public string Version { get; set; }
            /// <summary>
            /// unique number
            /// </summary>
            public string Uid { get; set; }

            /// <summary>
            /// List of submenu items
            /// </summary>
            public List<Ncf.Core.Areas.AreaPageMenuItem> AreaPageMenuItems { get; set; }

            /// <summary>
            ///interface
            /// </summary>
            public List<string> Interfaces { get; set; }

            /// <summary>
            /// "Execution method" quantity statistics
            /// </summary>
            public int FunctionCount { get; set; }

            /// <summary>
            /// thread information
            /// </summary>
            public IEnumerable<Response_XncfRegister_RegisteredThreadInfo> RegisteredThreadInfo { get; set; }


            public class Response_XncfRegister_RegisteredThreadInfo
            {
                /// <summary>
                /// thread information
                /// </summary>
                public RegisteredThreadInfo_Key Key { get; set; }
                /// <summary>
                ///Thread status details
                /// </summary>
                public RegisteredThreadInfo_Value Value { get; set; }

                public class RegisteredThreadInfo_Key
                {
                    /// <summary>
                    ///thread name
                    /// </summary>
                    public string Name { get; set; }
                    /// <summary>
                    ///thread story
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
