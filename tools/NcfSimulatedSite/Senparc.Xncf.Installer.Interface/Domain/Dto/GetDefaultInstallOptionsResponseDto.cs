namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class GetDefaultInstallOptionsResponseDto
    {
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string DbConnectionString { get; set; }
        public List<XncfRegisterDto> NeedModelList { get; set; } //List of modules that need to be installed
    }
}
