using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Models.VD
{
    public class BaseAdminUserInfoVD : BaseAdminVD
    {
    }

    public class AdminUserInfo_IndexVD : BaseAdminUserInfoVD
    {
        public PagedList<AdminUserInfo> AdminUserInfoList { get; set; }

    }
    public class AdminUserInfo_EditVD : BaseAdminUserInfoVD
    {    
      	/// <summary>
		/// Id
        /// </summary>		
        public int Id{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public string Password{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public string PasswordSalt{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public string RealName{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public string Phone{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public string Note{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public DateTime ThisLoginTime{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public string ThisLoginIP{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public DateTime LastLoginTime{get; set;}        
		/// <summary>
		///Legacy comment translated to English.
        /// </summary>		
        public string LastLoginIP{get; set;}        
		/// <summary>
		/// creation time
        /// </summary>		
        public DateTime AddTime{get; set;}  
        public bool IsEdit { get; set; }
	}
}

