using Microsoft.Extensions.Localization;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Localization
{
    public static class AdminDbDisplayStrings
    {
        public const string SystemInitNoteZh = "系统初始化账号";
        public const string SuperAdminRoleNameZh = "超级管理员";

        public static string LocalizeNote(IStringLocalizer<AdminResource> ar, string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return note;
            }
            if (note == SystemInitNoteZh)
            {
                return ar["AdminUserInfo.Db.SystemInitNote"];
            }
            return note;
        }

        public static string LocalizeRoleName(IStringLocalizer<AdminResource> ar, string roleCode, string roleName)
        {
            if (roleCode == Config.SYSROLE_ADMINISTRATOR_ROLE_CODE || roleName == SuperAdminRoleNameZh)
            {
                return ar["Menu.Db.超级管理员"];
            }
            return roleName ?? "";
        }
    }
}
