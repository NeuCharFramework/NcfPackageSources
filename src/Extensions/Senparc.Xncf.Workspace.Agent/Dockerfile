FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Senparc.Xncf.Workspace.Abstractions/ Senparc.Xncf.Workspace.Abstractions/
COPY Senparc.Xncf.Workspace.Agent/ Senparc.Xncf.Workspace.Agent/
RUN dotnet publish Senparc.Xncf.Workspace.Agent/Senparc.Xncf.Workspace.Agent.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /workspace
COPY --from=build /app /agent
USER app
ENTRYPOINT ["dotnet", "/agent/Senparc.Xncf.Workspace.Agent.dll"]
