namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class LlmModel_ModifyRequest
    {
        public int Id { get; set; }
        public string Alias { get; set; }

        public string DeploymentName { get; set; }
        public bool Show { get; set; }
    }
}