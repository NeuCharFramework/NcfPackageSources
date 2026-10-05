using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.AppService;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Services;

namespace Senparc.Xncf.AgentsManagerTests.Application;

[TestClass]
[DoNotParallelize]
public class AgentStudioTests : AgentsManagerTestBase
{
    private ChatGroupAppService AppService => new(
        _serviceProvider,
        _serviceProvider.GetRequiredService<ChatGroupService>(),
        _serviceProvider.GetRequiredService<ChatGroupMemberService>(),
        _serviceProvider.GetRequiredService<ChatGroupRemoteMemberService>(),
        _serviceProvider.GetRequiredService<AgentsTemplateService>(),
        _serviceProvider.GetRequiredService<RemoteAgentService>(),
        _serviceProvider.GetRequiredService<PublishedA2AAgentService>(),
        _serviceProvider.GetRequiredService<AIModelService>(),
        _serviceProvider.GetRequiredService<ChatTaskService>(),
        _serviceProvider.GetRequiredService<PromptItemService>(),
        _serviceProvider.GetRequiredService<PromptRangeService>(),
        _serviceProvider.GetRequiredService<HumanInTheLoopRequestStore>());

    [TestMethod]
    public async Task Studio_AddParticipant_IsIdempotentAndPreservesConfiguration()
    {
        await WithFixture(async fixture =>
        {
            fixture.Group.DisableGroup();
            var groupDto = fixture.Groups.Mapping<ChatGroupDto>(fixture.Group);
            groupDto.ContextSharingMode = ChatGroupContextSharingMode.InstructionOnly;
            fixture.Group.Update(groupDto);
            await fixture.Groups.SaveObjectAsync(fixture.Group);

            var app = AppService;
            var first = await app.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Member.Id}");
            var duplicate = await app.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Member.Id}");
            var remote = await app.SetStudioParticipant(fixture.Group.Id, $"remote:{fixture.Remote.Id}");
            Assert.IsTrue(first.Success == true, first.ErrorMessage);
            Assert.IsTrue(duplicate.Success == true, duplicate.ErrorMessage);
            Assert.IsTrue(remote.Success == true, remote.ErrorMessage);

            var members = await fixture.Members.GetFullListAsync(item => item.ChatGroupId == fixture.Group.Id);
            Assert.AreEqual(1, members.Count(item => item.AgentTemplateId == fixture.Member.Id));
            var saved = await fixture.Groups.GetObjectAsync(item => item.Id == fixture.Group.Id);
            Assert.IsFalse(saved.Enable);
            Assert.AreEqual(fixture.Host.Id, saved.AdminAgentTemplateId);
            Assert.AreEqual(fixture.Host.Id, saved.EnterAgentTemplateId);
            Assert.AreEqual(ChatGroupContextSharingMode.InstructionOnly, saved.ContextSharingMode);
            var remoteMember = await fixture.RemoteMembers.GetObjectAsync(item => item.ChatGroupId == saved.Id);
            Assert.AreEqual(ChatGroupContextSharingMode.InstructionAndKeyReplies, remoteMember.ContextSharingMode);
        });
    }

    [TestMethod]
    public async Task Studio_RemoveParticipant_ProtectsRolesAndOtherMembers()
    {
        await WithFixture(async fixture =>
        {
            var app = AppService;
            await app.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Host.Id}");
            await app.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Member.Id}");
            await app.SetStudioParticipant(fixture.Group.Id, $"remote:{fixture.Remote.Id}");
            var protectedRole = await app.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Host.Id}", true);
            Assert.IsFalse(protectedRole.Success == true);
            var removed = await app.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Member.Id}", true);
            Assert.IsTrue(removed.Success == true, removed.ErrorMessage);
            Assert.IsNull(await fixture.Members.GetObjectAsync(item =>
                item.ChatGroupId == fixture.Group.Id && item.AgentTemplateId == fixture.Member.Id));
            Assert.IsNotNull(await fixture.Members.GetObjectAsync(item =>
                item.ChatGroupId == fixture.Group.Id && item.AgentTemplateId == fixture.Host.Id));
            Assert.IsNotNull(await fixture.RemoteMembers.GetObjectAsync(item => item.ChatGroupId == fixture.Group.Id));
        });
    }

    [TestMethod]
    public async Task Studio_ConcurrentAdds_DoNotDuplicateOrOverwriteMembers()
    {
        await WithFixture(async fixture =>
        {
            var responses = await Task.WhenAll(
                AppService.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Host.Id}"),
                AppService.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Member.Id}"),
                AppService.SetStudioParticipant(fixture.Group.Id, $"local:{fixture.Member.Id}"),
                AppService.SetStudioParticipant(fixture.Group.Id, $"remote:{fixture.Remote.Id}"));
            Assert.IsTrue(responses.All(response => response.Success == true),
                string.Join("; ", responses.Select(response => response.ErrorMessage)));
            Assert.AreEqual(2, (await fixture.Members.GetFullListAsync(item => item.ChatGroupId == fixture.Group.Id)).Count);
            Assert.AreEqual(1, (await fixture.RemoteMembers.GetFullListAsync(item => item.ChatGroupId == fixture.Group.Id)).Count);
        });
    }

    [TestMethod]
    public async Task Studio_CreateTeam_ValidatesBeforeSavingAndDeduplicatesParticipants()
    {
        await WithFixture(async fixture =>
        {
            var app = AppService;
            var name = "studio-validation-" + Guid.NewGuid().ToString("N");
            var invalid = await app.CreateStudioTeam(new StudioTeamRequest
            {
                Name = name,
                ParticipantKeys = [$"local:{fixture.Host.Id}", "remote:2147483647"],
                AdminAgentTemplateId = fixture.Host.Id,
                EnterAgentTemplateId = fixture.Host.Id
            });
            Assert.IsFalse(invalid.Success == true);
            Assert.IsNull(await fixture.Groups.GetObjectAsync(item => item.Name == name));

            var created = await app.CreateStudioTeam(new StudioTeamRequest
            {
                Name = name,
                ParticipantKeys = [$"local:{fixture.Host.Id}", $"local:{fixture.Host.Id}", $"remote:{fixture.Remote.Id}"],
                AdminAgentTemplateId = fixture.Host.Id,
                EnterAgentTemplateId = fixture.Host.Id
            });
            Assert.IsTrue(created.Success == true, created.ErrorMessage);
            fixture.AddedGroupIds.Add(created.Data.Id);
            Assert.AreEqual(1, (await fixture.Members.GetFullListAsync(item => item.ChatGroupId == created.Data.Id)).Count);
            Assert.AreEqual(1, (await fixture.RemoteMembers.GetFullListAsync(item => item.ChatGroupId == created.Data.Id)).Count);
        });
    }

    [TestMethod]
    public async Task Studio_InvalidOrDisabledParticipants_DoNotChangeMembership()
    {
        await WithFixture(async fixture =>
        {
            var app = AppService;
            foreach (var key in new[] { "", "local:0", "local:abc", "remote:-1", "other:1", $"local:{fixture.Disabled.Id}" })
            {
                var result = await app.SetStudioParticipant(fixture.Group.Id, key);
                Assert.IsFalse(result.Success == true, key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
            }
            Assert.AreEqual(0, (await fixture.Members.GetFullListAsync(item => item.ChatGroupId == fixture.Group.Id)).Count);
        });
    }

    [TestMethod]
    public async Task Studio_Snapshot_IncludesRoleOnlyGroupsAndSingleAgentTasksButNotArchivedTasks()
    {
        await WithFixture(async fixture =>
        {
            var taskService = _serviceProvider.GetRequiredService<ChatTaskService>();
            var active = new ChatTask(new ChatTaskDto("studio active", fixture.Group.Id, 0, ChatTask_Status.Waiting,
                "test", "", true, HookPlatform.None, "", false, DateTime.Now, DateTime.Now, null));
            var archived = new ChatTask(new ChatTaskDto("studio archived", fixture.Group.Id, 0, ChatTask_Status.Finished,
                "test", "", true, HookPlatform.None, "", false, DateTime.Now, DateTime.Now, null));
            archived.SetArchived(true);
            await taskService.SaveObjectAsync(active);
            await taskService.SaveObjectAsync(archived);
            try
            {
                var response = await AppService.GetAgentGraphSnapshot();
                Assert.IsTrue(response.Success == true, response.ErrorMessage);
                Assert.IsTrue(response.Data.Groups.Any(group => group.Id == fixture.Group.Id));
                Assert.IsTrue(response.Data.Tasks.Any(task => task.Id == active.Id));
                Assert.IsFalse(response.Data.Tasks.Any(task => task.Id == archived.Id));
                var group = response.Data.Groups.Single(group => group.Id == fixture.Group.Id);
                Assert.AreEqual(1, group.TaskStatusCounts[(int)ChatTask_Status.Waiting]);
                Assert.IsFalse(group.TaskStatusCounts.ContainsKey((int)ChatTask_Status.Finished));
                Assert.AreEqual(fixture.Host.Id, group.AdminAgentTemplateId);
                var collaboration = response.Data.Collaborations.Single(task => task.TaskId == active.Id);
                CollectionAssert.AreEqual(new[] { $"local:{fixture.Host.Id}" }, collaboration.ParticipantKeys);
            }
            finally
            {
                await taskService.DeleteObjectAsync(active);
                await taskService.DeleteObjectAsync(archived);
            }
        });
    }

    [TestMethod]
    public async Task Studio_RunGroup_RejectsDisabledGroupBeforeScheduling()
    {
        await WithFixture(async fixture =>
        {
            fixture.Group.DisableGroup();
            await fixture.Groups.SaveObjectAsync(fixture.Group);
            var response = await AppService.RunGroup(new ChatGroup_RunGroupRequest
            {
                ChatGroupId = fixture.Group.Id, Name = "must not run", PromptCommand = "test"
            });
            Assert.IsFalse(response.Success == true);
            var tasks = await _serviceProvider.GetRequiredService<ChatTaskService>()
                .GetFullListAsync(task => task.ChatGroupId == fixture.Group.Id);
            Assert.AreEqual(0, tasks.Count);
        });
    }

    [TestMethod]
    public async Task Studio_StartTask_PropagatesInitializationFailureBeforeTaskCreation()
    {
        var service = _serviceProvider.GetRequiredService<ChatGroupService>();
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(async () =>
            await service.StartChatGroupInThreadAsync(new ChatGroup_RunGroupRequest
            {
                ChatGroupId = int.MaxValue, Name = "missing studio", PromptCommand = "test"
            }).WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [TestMethod]
    public async Task Studio_LegacyBackgroundStart_RemainsNonBlocking()
    {
        var service = _serviceProvider.GetRequiredService<ChatGroupService>();
        var scheduled = service.RunChatGroupInThread(new ChatGroup_RunGroupRequest
        {
            ChatGroupId = int.MaxValue, Name = "legacy background", PromptCommand = "test"
        });
        Assert.IsTrue(scheduled.IsCompletedSuccessfully);
        await scheduled;
    }

    [TestMethod]
    public async Task Studio_StartTask_ReturnsPersistedIdBeforeExecutionEnds()
    {
        await WithFixture(async fixture =>
        {
            var service = _serviceProvider.GetRequiredService<ChatGroupService>();
            // A missing explicit model fails after persistence, before any provider request.
            var started = await service.StartChatGroupInThreadAsync(new ChatGroup_RunGroupRequest
            {
                ChatGroupId = fixture.Group.Id, Name = "studio persisted task", PromptCommand = "test",
                AiModelId = int.MaxValue
            }).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(started.ChatTaskId > 0);
            Assert.AreEqual(fixture.Group.Id, started.ChatGroupId);
            var taskService = _serviceProvider.GetRequiredService<ChatTaskService>();
            try
            {
                ChatTask? saved = null;
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    using var scope = _serviceProvider.CreateScope();
                    saved = await scope.ServiceProvider.GetRequiredService<ChatTaskService>()
                        .GetObjectAsync(task => task.Id == started.ChatTaskId);
                    if (saved != null && (int)saved.Status >= 3) break;
                    await Task.Delay(20);
                }
                Assert.IsNotNull(saved);
                Assert.IsTrue((int)saved.Status >= 3, "Execution failure must become a persisted terminal status.");
            }
            finally
            {
                var saved = await taskService.GetObjectAsync(task => task.Id == started.ChatTaskId);
                if (saved != null) await taskService.DeleteObjectAsync(saved);
            }
        });
    }

    [TestMethod]
    public async Task Studio_StartTask_RejectsInvalidModelWithoutCreatingTask()
    {
        await WithFixture(async fixture =>
        {
            var response = await AppService.StartStudioTask(new ChatGroup_RunGroupRequest
            {
                ChatGroupId = fixture.Group.Id, Name = "invalid model", PromptCommand = "test",
                AiModelId = int.MaxValue
            });
            Assert.IsFalse(response.Success == true);
            Assert.IsFalse(string.IsNullOrWhiteSpace(response.ErrorMessage));
            var tasks = await _serviceProvider.GetRequiredService<ChatTaskService>()
                .GetFullListAsync(task => task.ChatGroupId == fixture.Group.Id);
            Assert.AreEqual(0, tasks.Count);
        });
    }

    private async Task WithFixture(Func<StudioFixture, Task> test)
    {
        await using var fixture = new StudioFixture(_serviceProvider);
        await fixture.Initialize();
        await test(fixture);
    }

    private sealed class StudioFixture(IServiceProvider provider) : IAsyncDisposable
    {
        public ChatGroupService Groups { get; } = provider.GetRequiredService<ChatGroupService>();
        public ChatGroupMemberService Members { get; } = provider.GetRequiredService<ChatGroupMemberService>();
        public ChatGroupRemoteMemberService RemoteMembers { get; } = provider.GetRequiredService<ChatGroupRemoteMemberService>();
        private readonly AgentsTemplateService _agents = provider.GetRequiredService<AgentsTemplateService>();
        private readonly RemoteAgentService _remotes = provider.GetRequiredService<RemoteAgentService>();
        public AgentTemplate Host { get; } = new("studio host", "test", true, "", "test", HookRobotType.None, "");
        public AgentTemplate Member { get; } = new("studio member", "test", true, "", "test", HookRobotType.None, "");
        public AgentTemplate Disabled { get; } = new("studio disabled", "test", false, "", "test", HookRobotType.None, "");
        public RemoteAgent Remote { get; } = new(new RemoteAgentDto
        {
            Name = "studio remote", AgentCardUrl = "https://example.com/.well-known/agent-card.json", Enable = true
        });
        public ChatGroup Group { get; private set; } = null!;
        public List<int> AddedGroupIds { get; } = [];

        public async Task Initialize()
        {
            await _agents.SaveObjectListAsync([Host, Member, Disabled]);
            await _remotes.SaveObjectAsync(Remote);
            Group = new ChatGroup("studio-" + Guid.NewGuid().ToString("N"), true, ChatGroupState.Unstart, "", Host.Id, Host.Id);
            await Groups.SaveObjectAsync(Group);
            AddedGroupIds.Add(Group.Id);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var groupId in AddedGroupIds)
            {
                foreach (var member in await Members.GetFullListAsync(item => item.ChatGroupId == groupId))
                    await Members.DeleteObjectAsync(member);
                foreach (var member in await RemoteMembers.GetFullListAsync(item => item.ChatGroupId == groupId))
                    await RemoteMembers.DeleteObjectAsync(member);
                var group = await Groups.GetObjectAsync(item => item.Id == groupId);
                if (group != null) await Groups.DeleteObjectAsync(group);
            }
            if (Remote.Id > 0) await _remotes.DeleteObjectAsync(Remote);
            foreach (var agent in new[] { Host, Member, Disabled }.Where(agent => agent.Id > 0))
                await _agents.DeleteObjectAsync(agent);
        }
    }
}
