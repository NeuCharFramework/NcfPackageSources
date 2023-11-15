namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
  public static class LlmModelHelper
  {
    public static string GetAzureModelName(string llmModelName)
    {
      if (string.IsNullOrWhiteSpace(llmModelName)) return "text-davinci-003";

      if (llmModelName.Contains("azure"))
      {
        return llmModelName.Substring("azure-".Length);
      }

      return llmModelName;
    }
    
  }
}