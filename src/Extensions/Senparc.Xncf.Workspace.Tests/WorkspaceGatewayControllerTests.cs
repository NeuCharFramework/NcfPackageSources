using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Shared.Abstractions.Security;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Xncf.Workspace.OHS.Local.Controllers;

namespace Senparc.Xncf.Workspace.Tests;

[TestClass]
public sealed class WorkspaceGatewayControllerTests
{
    [TestMethod]
    public void Capabilities_RequiresHttpsForRemoteClients()
    {
        var controller = CreateController(new TestDesktopValidator());
        controller.ControllerContext.HttpContext.Connection.RemoteIpAddress =
            IPAddress.Parse("203.0.113.10");
        controller.ControllerContext.HttpContext.Request.Scheme = "http";
        controller.ControllerContext.HttpContext.Request.Headers["X-Ncf-Desktop-Token"] = "valid";

        var result = controller.GetCapabilities();

        Assert.IsInstanceOfType<ObjectResult>(result.Result);
        Assert.AreEqual(
            StatusCodes.Status403Forbidden,
            ((ObjectResult)result.Result).StatusCode);
    }

    [TestMethod]
    public void Capabilities_RequiresDesktopSession()
    {
        var controller = CreateController(new TestDesktopValidator());

        var result = controller.GetCapabilities();

        Assert.IsInstanceOfType<UnauthorizedResult>(result.Result);
    }

    [TestMethod]
    public void Capabilities_RequiresSingleDesktopValidator()
    {
        var controller = CreateController(new TestDesktopValidator(), new TestDesktopValidator());
        controller.ControllerContext.HttpContext.Request.Headers["X-Ncf-Desktop-Token"] = "valid";

        var result = controller.GetCapabilities();

        Assert.IsInstanceOfType<ObjectResult>(result.Result);
        Assert.AreEqual(
            StatusCodes.Status503ServiceUnavailable,
            ((ObjectResult)result.Result).StatusCode);
    }

    [TestMethod]
    public void Capabilities_ReturnsSafeRuntimeMetadata()
    {
        var controller = CreateController(new TestDesktopValidator());
        controller.ControllerContext.HttpContext.Request.Headers["X-Ncf-Desktop-Token"] = "valid";

        var result = controller.GetCapabilities();

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        var capabilities = ok.Value as WorkspaceGatewayCapabilities;
        Assert.IsNotNull(capabilities);
        Assert.AreEqual("NativeAgent", capabilities.RuntimeProvider);
        Assert.IsFalse(capabilities.ExecutionEnabled);
        Assert.IsTrue(capabilities.SupportsEtags);
    }

    private static WorkspaceGatewayController CreateController(
        params IDesktopBridgeSessionValidator[] validators)
    {
        var options = Options.Create(new WorkspaceOptions
        {
            RuntimeProvider = "NativeAgent",
            AllowNativeExecution = false
        });
        var processFactory = new WorkspaceAgentProcessFactory(
            options,
            NullLogger<WorkspaceAgentProcessFactory>.Instance);
        var controller = new WorkspaceGatewayController(
            new UnsupportedStorage(),
            new UnsupportedClientFactory(),
            new WorkspaceChangeHub(),
            processFactory,
            options,
            validators);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "42")],
            "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private sealed class TestDesktopValidator : IDesktopBridgeSessionValidator
    {
        public string HeaderName => "X-Ncf-Desktop-Token";

        public bool IsConfigured => true;

        public bool TryAuthorize(string? suppliedToken, out CancellationToken sessionRevoked)
        {
            sessionRevoked = CancellationToken.None;
            return string.Equals(suppliedToken, "valid", StringComparison.Ordinal);
        }
    }

    private sealed class UnsupportedClientFactory : IWorkspaceClientFactory
    {
        public IWorkspaceClient Create(WorkspaceAccessContext context) =>
            throw new NotSupportedException();
    }

    private sealed class UnsupportedStorage : IWorkspaceStorage
    {
        public Task<WorkspaceDescriptor> CreateAsync(
            WorkspaceCreateRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<WorkspaceDescriptor?> GetAsync(
            string workspaceId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<WorkspaceDescriptor>> ListWorkspacesAsync(
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
            string workspaceId,
            string relativePath = "",
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<string> ResolveFilePathAsync(
            string workspaceId,
            string relativePath,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<string> ReadFileAsync(
            string workspaceId,
            string relativePath,
            int maxBytes = 2097152,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<WorkspaceFileMetadata> GetFileInfoAsync(
            string workspaceId,
            string relativePath,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<WorkspaceFileContent> ReadFileBytesAsync(
            string workspaceId,
            string relativePath,
            int maxBytes = 2097152,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task WriteFileAsync(
            string workspaceId,
            string relativePath,
            string content,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<WorkspaceWriteFileResult> WriteFileBytesAsync(
            string workspaceId,
            WorkspaceWriteFileRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task CreateDirectoryAsync(
            string workspaceId,
            string relativePath,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteEntryAsync(
            string workspaceId,
            WorkspaceDeleteEntryRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task MoveEntryAsync(
            string workspaceId,
            WorkspaceMoveEntryRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
            string workspaceId,
            WorkspaceSearchRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<WorkspacePatchResult> ApplyPatchAsync(
            string workspaceId,
            WorkspacePatchRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<WorkspaceCommandResult> ExecuteCommandAsync(
            string workspaceId,
            WorkspaceCommandRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<WorkspaceCommandResult> ExecutePythonAsync(
            string workspaceId,
            WorkspacePythonRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(
            string workspaceId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
