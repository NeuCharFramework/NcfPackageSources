namespace Senparc.Xncf.Installer.Domain.Dto
{
    /* Module information data transfer object*/
    public class XncfRegisterDto
    {
        public bool IgnoreInstall { get; set; }
        public string Name { get; set; }
        public string Uid { get; set; }
        public string Version { get; set; }
        public string MenuName { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
}
