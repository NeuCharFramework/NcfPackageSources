namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class InstallRequestDto
    {
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string DbConnectionString { get; set; }
        public List<string> NeedModelList { get; set; }//List of modules that need to be installed
    }
}
