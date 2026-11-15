using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.Tests.Domain.Services;

[TestClass]
public class AdminChatSystemMessagePromptProviderTests
{
    [TestMethod]
    public void SelectPublishedPrompt_UsesFirstPublishedWhenNothingIsScored()
    {
        var first = Create("first", draft: false);
        var second = Create("second", draft: false);
        var draft = Create("draft", draft: true);

        Assert.AreSame(first, AdminChatSystemMessagePromptProvider.SelectPublishedPrompt([first, second, draft]));
    }

    [TestMethod]
    public void SelectPublishedPrompt_UsesHighestCurrentScoreAndIgnoresDrafts()
    {
        var lower = Create("lower", draft: false);
        lower.SetReferenceScore(7.5m, false);
        var higher = Create("higher", draft: false);
        higher.SetReferenceScore(9.0m, false);
        var draft = Create("draft", draft: true);
        draft.SetReferenceScore(10m, false);

        Assert.AreSame(higher, AdminChatSystemMessagePromptProvider.SelectPublishedPrompt([lower, higher, draft]));
    }

    private static PromptItem Create(string content, bool draft) =>
        new(1, "2026.11.15.1", "1", 1, string.Empty, new PromptItem_AddRequest
        {
            Content = content,
            ModelId = 0,
            IsDraft = draft
        });
}
