namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public static class LlmModelHelper
    {
        public static string GetRealModelName(string llmModelName)
        {
            if (string.IsNullOrWhiteSpace(llmModelName)) return "text-davinci-003";

            if (llmModelName.Contains("_"))
            {
                return llmModelName.Split("_")[1];
            }

            return llmModelName;
        }
    }
}