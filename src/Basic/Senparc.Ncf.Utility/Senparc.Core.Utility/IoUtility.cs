/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IoUtility.cs
    文件功能描述：IoUtility 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Utility
{
    public class IoUtility
    {
        //COCONET .net core不支持FileSystemRights，需要继续改进

        //// Adds an ACL entry on the specified directory for the specified account.
        //public static void AddDirectorySecurity(string FileName, string User, FileSystemRights Rights, AccessControlType ControlType)
        //{
        //    // Create a new DirectoryInfo object.
        //    DirectoryInfo dInfo = new DirectoryInfo(FileName);

        //    // Get a DirectorySecurity object that represents the 
        //    // current security settings.
        //    DirectorySecurity dSecurity = dInfo.GetAccessControl();

        //    // Add the FileSystemAccessRule to the security settings. 
        //    dSecurity.AddAccessRule(new FileSystemAccessRule(User,
        //                                                    Rights,
        //                                                    ControlType));

        //    // Set the new access settings.
        //    dInfo.SetAccessControl(dSecurity);

        //}

        //// Removes an ACL entry on the specified directory for the specified account.
        //public static void RemoveDirectorySecurity(string FileName, string User, FileSystemRights Rights, AccessControlType ControlType)
        //{
        //    // Create a new DirectoryInfo object.
        //    DirectoryInfo dInfo = new DirectoryInfo(FileName);

        //    // Get a DirectorySecurity object that represents the 
        //    // current security settings.
        //    DirectorySecurity dSecurity = dInfo.GetAccessControl();

        //    // Add the FileSystemAccessRule to the security settings. 
        //    dSecurity.RemoveAccessRule(new FileSystemAccessRule(User,
        //                                                    Rights,
        //                                                    ControlType));

        //    // Set the new access settings.
        //    dInfo.SetAccessControl(dSecurity);

        //}

    }
}
