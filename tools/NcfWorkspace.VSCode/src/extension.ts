import * as vscode from "vscode";
import { ConnectionManager } from "./connectionManager";
import { errorMessage, isAbortError } from "./errors";
import { WorkspaceDescriptor } from "./types";
import {
  WORKSPACE_SCHEME,
  WorkspaceFileSystemProvider,
} from "./workspaceFileSystemProvider";

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const connection = new ConnectionManager(context);
  const fileSystem = new WorkspaceFileSystemProvider(connection);
  const statusBar = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Left,
    50,
  );
  statusBar.name = "NCF Workspace";
  statusBar.show();

  const updateStatus = (): void => {
    const snapshot = connection.connection;
    const remembered = connection.rememberedWorkspace;
    switch (snapshot.state) {
      case "connected":
        statusBar.text = `$(remote) NCF${remembered.name ? `: ${remembered.name}` : ""}`;
        statusBar.tooltip = [
          "NCF Workspace 已连接",
          snapshot.siteUrl,
          snapshot.userName ? `管理员：${snapshot.userName}` : undefined,
        ]
          .filter(Boolean)
          .join("\n");
        statusBar.command = "ncfWorkspace.openWorkspace";
        statusBar.backgroundColor = undefined;
        break;
      case "connecting":
        statusBar.text = "$(sync~spin) NCF 正在连接…";
        statusBar.tooltip = snapshot.message ?? "正在建立安全连接";
        statusBar.command = undefined;
        statusBar.backgroundColor = undefined;
        break;
      case "error":
        statusBar.text = "$(warning) NCF 连接异常";
        statusBar.tooltip = snapshot.message ?? "单击重新连接";
        statusBar.command = "ncfWorkspace.reconnect";
        statusBar.backgroundColor = new vscode.ThemeColor(
          "statusBarItem.warningBackground",
        );
        break;
      default:
        statusBar.text = "$(debug-disconnect) NCF 未连接";
        statusBar.tooltip = snapshot.message ?? "单击连接 NCF Workspace";
        statusBar.command = "ncfWorkspace.connect";
        statusBar.backgroundColor = undefined;
        break;
    }
  };

  context.subscriptions.push(
    connection,
    fileSystem,
    statusBar,
    connection.onDidChangeState(updateStatus),
    vscode.workspace.registerFileSystemProvider(WORKSPACE_SCHEME, fileSystem, {
      isCaseSensitive: true,
      isReadonly: false,
    }),
    vscode.commands.registerCommand(
      "ncfWorkspace.connect",
      command(async () => {
        await connection.connect();
        await chooseAndOpenWorkspace(connection);
      }),
    ),
    vscode.commands.registerCommand(
      "ncfWorkspace.reconnect",
      command(async () => {
        await connection.reconnect();
        const current = currentWorkspaceFolder();
        if (current) {
          fileSystem.refresh(current.uri);
          void vscode.window.showInformationMessage("NCF Workspace 已重新连接。");
        } else {
          await chooseAndOpenWorkspace(connection);
        }
      }),
    ),
    vscode.commands.registerCommand(
      "ncfWorkspace.disconnect",
      command(async () => {
        await connection.disconnect();
        void vscode.window.showInformationMessage(
          "已断开 NCF Workspace，并清除本机安全票据。",
        );
      }),
    ),
    vscode.commands.registerCommand(
      "ncfWorkspace.openWorkspace",
      command(async () => {
        await ensureConnected(connection);
        await chooseAndOpenWorkspace(connection);
      }),
    ),
    vscode.commands.registerCommand(
      "ncfWorkspace.createWorkspace",
      command(async () => {
        await ensureConnected(connection);
        const workspace = await createWorkspace(connection);
        if (workspace) {
          await openWorkspace(connection, workspace);
        }
      }),
    ),
  );

  updateStatus();
  const restored = await connection.restore();
  if (!restored && currentWorkspaceFolder()) {
    const action = await vscode.window.showWarningMessage(
      "当前窗口是 NCF Workspace，但安全会话尚未恢复。",
      "重新连接",
    );
    if (action === "重新连接") {
      void vscode.commands.executeCommand("ncfWorkspace.reconnect");
    }
  }
}

export function deactivate(): void {}

async function ensureConnected(connection: ConnectionManager): Promise<void> {
  if (connection.state !== "connected") {
    await connection.reconnect();
  }
}

async function chooseAndOpenWorkspace(connection: ConnectionManager): Promise<void> {
  const workspaces = await connection.client.listWorkspaces();
  if (workspaces.length === 0) {
    const action = await vscode.window.showInformationMessage(
      "此管理员还没有 Workspace，现在创建一个吗？",
      "新建 Workspace",
    );
    if (action === "新建 Workspace") {
      const workspace = await createWorkspace(connection);
      if (workspace) {
        await openWorkspace(connection, workspace);
      }
    }
    return;
  }

  const remembered = connection.rememberedWorkspace.id;
  const ordered = [...workspaces].sort((left, right) => {
    if (left.id === remembered) {
      return -1;
    }
    if (right.id === remembered) {
      return 1;
    }
    return left.name.localeCompare(right.name, "zh-CN");
  });
  const picked = await vscode.window.showQuickPick(
    ordered.map((workspace) => ({
      label: `$(folder) ${workspace.name}`,
      description: workspace.id === remembered ? "上次打开" : workspace.storageProvider,
      detail: `Workspace ID: ${workspace.id}`,
      workspace,
    })),
    {
      title: "选择要打开的 NCF Workspace",
      placeHolder: "选择后将在当前窗口打开",
      matchOnDescription: true,
      matchOnDetail: true,
      ignoreFocusOut: true,
    },
  );
  if (picked) {
    await openWorkspace(connection, picked.workspace);
  }
}

async function createWorkspace(
  connection: ConnectionManager,
): Promise<WorkspaceDescriptor | undefined> {
  const name = await vscode.window.showInputBox({
    title: "新建 NCF Workspace",
    prompt: "输入一个便于识别的 Workspace 名称",
    placeHolder: "例如：官网重构",
    ignoreFocusOut: true,
    validateInput: (value) =>
      value.trim().length === 0 ? "Workspace 名称不能为空。" : undefined,
  });
  if (name === undefined) {
    return undefined;
  }
  return connection.client.createWorkspace(name.trim());
}

async function openWorkspace(
  connection: ConnectionManager,
  workspace: WorkspaceDescriptor,
): Promise<void> {
  await connection.rememberWorkspace(workspace.id, workspace.name);
  const uri = vscode.Uri.from({
    scheme: WORKSPACE_SCHEME,
    authority: workspace.id,
    path: "/",
  });
  await vscode.commands.executeCommand("vscode.openFolder", uri, false);
}

function currentWorkspaceFolder(): vscode.WorkspaceFolder | undefined {
  return vscode.workspace.workspaceFolders?.find(
    (folder) => folder.uri.scheme === WORKSPACE_SCHEME,
  );
}

function command(action: () => Promise<void>): () => Promise<void> {
  return async () => {
    try {
      await action();
    } catch (error) {
      if (!isAbortError(error)) {
        void vscode.window.showErrorMessage(`NCF Workspace：${errorMessage(error)}`);
      }
    }
  };
}
