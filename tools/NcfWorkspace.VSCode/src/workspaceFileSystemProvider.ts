import { posix } from "node:path";
import * as vscode from "vscode";
import { ConnectionManager } from "./connectionManager";
import { HttpError, isAbortError } from "./errors";
import {
  WorkspaceChangeEvent,
  WorkspaceFileMetadata,
  WorkspaceWriteFileResult,
} from "./types";

export const WORKSPACE_SCHEME = "ncf-workspace";

interface WatchState {
  references: number;
  controller?: AbortController;
}

export class WorkspaceFileSystemProvider
  implements vscode.FileSystemProvider, vscode.Disposable
{
  private readonly changeEmitter = new vscode.EventEmitter<vscode.FileChangeEvent[]>();
  private readonly etags = new Map<string, string>();
  private readonly watchers = new Map<string, WatchState>();
  private readonly connectionSubscription: vscode.Disposable;

  public readonly onDidChangeFile = this.changeEmitter.event;

  public constructor(private readonly connection: ConnectionManager) {
    this.connectionSubscription = connection.onDidChangeState(({ state }) => {
      if (state === "connected") {
        for (const workspaceId of this.watchers.keys()) {
          this.startEventStream(workspaceId);
        }
      } else {
        this.stopEventStreams();
      }
    });
  }

  public watch(
    uri: vscode.Uri,
    _options: { recursive: boolean; excludes: string[] },
  ): vscode.Disposable {
    this.validateUri(uri);
    const current = this.watchers.get(uri.authority) ?? { references: 0 };
    current.references += 1;
    this.watchers.set(uri.authority, current);
    if (this.connection.state === "connected") {
      this.startEventStream(uri.authority);
    }

    return new vscode.Disposable(() => {
      const state = this.watchers.get(uri.authority);
      if (!state) {
        return;
      }
      state.references -= 1;
      if (state.references <= 0) {
        state.controller?.abort();
        this.watchers.delete(uri.authority);
      }
    });
  }

  public async stat(uri: vscode.Uri): Promise<vscode.FileStat> {
    const { workspaceId, path } = this.parts(uri);
    try {
      const metadata = await this.connection.client.stat(workspaceId, path);
      this.rememberETag(workspaceId, path, metadata.eTag);
      return metadataToStat(metadata);
    } catch (error) {
      throw this.fileSystemError(error, uri, "读取属性");
    }
  }

  public async readDirectory(uri: vscode.Uri): Promise<[string, vscode.FileType][]> {
    const { workspaceId, path } = this.parts(uri);
    try {
      const entries = await this.connection.client.listEntries(workspaceId, path);
      return entries.map((entry) => [
        posix.basename(entry.relativePath),
        entry.isDirectory ? vscode.FileType.Directory : vscode.FileType.File,
      ]);
    } catch (error) {
      throw this.fileSystemError(error, uri, "读取目录");
    }
  }

  public async readFile(uri: vscode.Uri): Promise<Uint8Array> {
    const { workspaceId, path } = this.parts(uri);
    try {
      const file = await this.connection.client.readFile(workspaceId, path);
      this.rememberETag(workspaceId, path, file.eTag);
      return file.content;
    } catch (error) {
      throw this.fileSystemError(error, uri, "读取文件");
    }
  }

  public async writeFile(
    uri: vscode.Uri,
    content: Uint8Array,
    options: { create: boolean; overwrite: boolean },
  ): Promise<void> {
    const { workspaceId, path } = this.parts(uri);
    try {
      const known = await this.existingEntry(workspaceId, path);
      if (known.exists && !options.overwrite) {
        throw vscode.FileSystemError.FileExists(uri);
      }
      if (!known.exists && !options.create) {
        throw vscode.FileSystemError.FileNotFound(uri);
      }

      const result = await this.connection.client.writeFile(
        workspaceId,
        path,
        content,
        options.create,
        known.exists ? options.overwrite : false,
        known.eTag,
      );
      this.rememberETag(workspaceId, path, result.metadata.eTag);
      this.fireWrite(uri, result);
    } catch (error) {
      if (error instanceof vscode.FileSystemError) {
        throw error;
      }
      throw this.fileSystemError(error, uri, "保存文件");
    }
  }

  public async createDirectory(uri: vscode.Uri): Promise<void> {
    const { workspaceId, path } = this.parts(uri);
    try {
      await this.connection.client.createDirectory(workspaceId, path);
      this.changeEmitter.fire([{ type: vscode.FileChangeType.Created, uri }]);
    } catch (error) {
      throw this.fileSystemError(error, uri, "创建目录");
    }
  }

  public async delete(
    uri: vscode.Uri,
    options: { recursive: boolean },
  ): Promise<void> {
    const { workspaceId, path } = this.parts(uri);
    try {
      const known = await this.existingEntry(workspaceId, path);
      if (!known.exists) {
        throw vscode.FileSystemError.FileNotFound(uri);
      }
      await this.connection.client.delete(
        workspaceId,
        path,
        options.recursive,
        known.eTag,
      );
      this.forgetETags(workspaceId, path);
      this.changeEmitter.fire([{ type: vscode.FileChangeType.Deleted, uri }]);
    } catch (error) {
      if (error instanceof vscode.FileSystemError) {
        throw error;
      }
      throw this.fileSystemError(error, uri, "删除条目");
    }
  }

  public async rename(
    oldUri: vscode.Uri,
    newUri: vscode.Uri,
    options: { overwrite: boolean },
  ): Promise<void> {
    const source = this.parts(oldUri);
    const target = this.parts(newUri);
    if (source.workspaceId !== target.workspaceId) {
      throw vscode.FileSystemError.NoPermissions("不能跨 Workspace 移动条目。");
    }

    try {
      const known = await this.existingEntry(source.workspaceId, source.path);
      if (!known.exists) {
        throw vscode.FileSystemError.FileNotFound(oldUri);
      }
      await this.connection.client.move(
        source.workspaceId,
        source.path,
        target.path,
        options.overwrite,
        known.eTag ?? "",
      );
      this.forgetETags(source.workspaceId, source.path);
      this.changeEmitter.fire([
        { type: vscode.FileChangeType.Deleted, uri: oldUri },
        { type: vscode.FileChangeType.Created, uri: newUri },
      ]);
    } catch (error) {
      if (error instanceof vscode.FileSystemError) {
        throw error;
      }
      throw this.fileSystemError(error, oldUri, "重命名条目");
    }
  }

  public refresh(uri: vscode.Uri): void {
    const { workspaceId, path } = this.parts(uri);
    this.forgetETags(workspaceId, path);
    this.changeEmitter.fire([{ type: vscode.FileChangeType.Changed, uri }]);
  }

  public dispose(): void {
    this.stopEventStreams();
    this.watchers.clear();
    this.etags.clear();
    this.connectionSubscription.dispose();
    this.changeEmitter.dispose();
  }

  private async existingEntry(
    workspaceId: string,
    path: string,
  ): Promise<{ exists: boolean; eTag?: string }> {
    const cached = this.etags.get(etagKey(workspaceId, path));
    if (cached) {
      return { exists: true, eTag: cached };
    }
    try {
      const metadata = await this.connection.client.stat(workspaceId, path);
      this.rememberETag(workspaceId, path, metadata.eTag);
      return { exists: true, eTag: metadata.eTag };
    } catch (error) {
      if (error instanceof HttpError && error.status === 404) {
        return { exists: false };
      }
      throw error;
    }
  }

  private startEventStream(workspaceId: string): void {
    const state = this.watchers.get(workspaceId);
    if (!state || state.controller) {
      return;
    }
    const controller = new AbortController();
    state.controller = controller;
    void this.runEventStream(workspaceId, controller).finally(() => {
      const current = this.watchers.get(workspaceId);
      if (current && current.controller === controller) {
        current.controller = undefined;
      }
    });
  }

  private async runEventStream(
    workspaceId: string,
    controller: AbortController,
  ): Promise<void> {
    let retryDelay = 1000;
    while (!controller.signal.aborted && this.connection.state === "connected") {
      try {
        const response = await this.connection.client.openEvents(
          workspaceId,
          controller.signal,
        );
        if (!response.body) {
          throw new Error("服务器没有返回事件流。");
        }
        retryDelay = 1000;
        await consumeSse(response.body, (event) =>
          this.applyExternalChange(workspaceId, event),
        );
      } catch (error) {
        if (controller.signal.aborted || isAbortError(error)) {
          return;
        }
        if (error instanceof HttpError && [401, 403].includes(error.status)) {
          const action = await vscode.window.showWarningMessage(
            "NCF Workspace 实时刷新已停止：授权失效，请重新连接。",
            "重新连接",
          );
          if (action === "重新连接") {
            void vscode.commands.executeCommand("ncfWorkspace.reconnect");
          }
          return;
        }
      }

      await cancellableDelay(retryDelay, controller.signal);
      retryDelay = Math.min(retryDelay * 2, 15_000);
    }
  }

  private applyExternalChange(
    expectedWorkspaceId: string,
    event: WorkspaceChangeEvent,
  ): void {
    if (event.workspaceId !== expectedWorkspaceId) {
      return;
    }

    this.forgetETags(expectedWorkspaceId, event.relativePath);
    const uri = workspaceUri(expectedWorkspaceId, event.relativePath);
    switch (event.changeType.toLowerCase()) {
      case "created":
        this.changeEmitter.fire([{ type: vscode.FileChangeType.Created, uri }]);
        break;
      case "deleted":
        this.changeEmitter.fire([{ type: vscode.FileChangeType.Deleted, uri }]);
        break;
      case "renamed": {
        const changes: vscode.FileChangeEvent[] = [];
        if (event.previousRelativePath) {
          this.forgetETags(expectedWorkspaceId, event.previousRelativePath);
          changes.push({
            type: vscode.FileChangeType.Deleted,
            uri: workspaceUri(expectedWorkspaceId, event.previousRelativePath),
          });
        }
        changes.push({ type: vscode.FileChangeType.Created, uri });
        this.changeEmitter.fire(changes);
        break;
      }
      default:
        this.changeEmitter.fire([{ type: vscode.FileChangeType.Changed, uri }]);
        break;
    }
  }

  private stopEventStreams(): void {
    for (const state of this.watchers.values()) {
      state.controller?.abort();
      state.controller = undefined;
    }
  }

  private parts(uri: vscode.Uri): { workspaceId: string; path: string } {
    this.validateUri(uri);
    if (this.connection.state !== "connected") {
      throw vscode.FileSystemError.Unavailable("NCF Workspace 尚未连接，请运行“重新连接”。");
    }

    const raw = uri.path.replace(/^\/+/, "");
    if (raw.includes("\\") || raw.includes("\0")) {
      throw vscode.FileSystemError.NoPermissions("Workspace 路径无效。");
    }
    const normalized = posix.normalize(raw);
    if (normalized === ".") {
      return { workspaceId: uri.authority, path: "" };
    }
    if (
      normalized === ".." ||
      normalized.startsWith("../") ||
      posix.isAbsolute(normalized)
    ) {
      throw vscode.FileSystemError.NoPermissions("Workspace 路径不能离开根目录。");
    }
    return { workspaceId: uri.authority, path: normalized };
  }

  private validateUri(uri: vscode.Uri): void {
    if (uri.scheme !== WORKSPACE_SCHEME || !uri.authority) {
      throw vscode.FileSystemError.Unavailable("无效的 NCF Workspace 地址。");
    }
  }

  private rememberETag(workspaceId: string, path: string, eTag?: string): void {
    if (eTag) {
      this.etags.set(etagKey(workspaceId, path), eTag);
    }
  }

  private forgetETags(workspaceId: string, path: string): void {
    const exact = etagKey(workspaceId, path);
    const prefix = `${exact}/`;
    for (const key of this.etags.keys()) {
      if (key === exact || key.startsWith(prefix)) {
        this.etags.delete(key);
      }
    }
  }

  private fireWrite(uri: vscode.Uri, result: WorkspaceWriteFileResult): void {
    this.changeEmitter.fire([
      {
        type: result.created
          ? vscode.FileChangeType.Created
          : vscode.FileChangeType.Changed,
        uri,
      },
    ]);
  }

  private fileSystemError(
    error: unknown,
    uri: vscode.Uri,
    operation: string,
  ): vscode.FileSystemError {
    if (error instanceof vscode.FileSystemError) {
      return error;
    }
    if (error instanceof HttpError) {
      switch (error.status) {
        case 404:
          return vscode.FileSystemError.FileNotFound(uri);
        case 401:
        case 403:
          void vscode.window
            .showWarningMessage(
              `NCF Workspace ${operation}失败：授权已失效。`,
              "重新连接",
            )
            .then((action) => {
              if (action === "重新连接") {
                void vscode.commands.executeCommand("ncfWorkspace.reconnect");
              }
            });
          return vscode.FileSystemError.NoPermissions(error.message);
        case 409:
        case 412:
          void vscode.window
            .showErrorMessage(
              `NCF Workspace 保存冲突（HTTP ${error.status}）：远程内容已改变，请刷新后重试。`,
              "刷新",
            )
            .then((action) => {
              if (action === "刷新") {
                this.refresh(uri);
              }
            });
          return vscode.FileSystemError.Unavailable(
            `远程内容已改变，${operation}被安全阻止。请刷新后重试。`,
          );
      }
    }
    return vscode.FileSystemError.Unavailable(
      `${operation}失败：${error instanceof Error ? error.message : String(error)}`,
    );
  }
}

function metadataToStat(metadata: WorkspaceFileMetadata): vscode.FileStat {
  return {
    type: metadata.isDirectory ? vscode.FileType.Directory : vscode.FileType.File,
    ctime: parseTime(metadata.creationTimeUtc),
    mtime: parseTime(metadata.lastWriteTimeUtc),
    size: metadata.length,
  };
}

function parseTime(value: string): number {
  const result = Date.parse(value);
  return Number.isFinite(result) ? result : 0;
}

function etagKey(workspaceId: string, path: string): string {
  return `${workspaceId}\0${path}`;
}

function workspaceUri(workspaceId: string, relativePath: string): vscode.Uri {
  return vscode.Uri.from({
    scheme: WORKSPACE_SCHEME,
    authority: workspaceId,
    path: `/${relativePath.replace(/^\/+/, "")}`,
  });
}

async function consumeSse(
  stream: ReadableStream<Uint8Array>,
  onEvent: (event: WorkspaceChangeEvent) => void,
): Promise<void> {
  const reader = stream.getReader();
  const decoder = new TextDecoder();
  let buffer = "";
  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        buffer += decoder.decode();
        break;
      }
      buffer += decoder.decode(value, { stream: true });
      buffer = consumeFrames(buffer, onEvent);
    }
    consumeFrames(`${buffer}\n\n`, onEvent);
  } finally {
    reader.releaseLock();
  }
}

function consumeFrames(
  input: string,
  onEvent: (event: WorkspaceChangeEvent) => void,
): string {
  let buffer = input;
  while (true) {
    const separator = /\r?\n\r?\n/.exec(buffer);
    if (!separator || separator.index === undefined) {
      return buffer;
    }
    const frame = buffer.slice(0, separator.index);
    buffer = buffer.slice(separator.index + separator[0].length);
    const lines = frame.split(/\r?\n/);
    const eventName = lines
      .find((line) => line.startsWith("event:"))
      ?.slice("event:".length)
      .trim();
    if (eventName && eventName !== "workspace-change") {
      continue;
    }
    const data = lines
      .filter((line) => line.startsWith("data:"))
      .map((line) => line.slice("data:".length).trimStart())
      .join("\n");
    if (!data) {
      continue;
    }
    try {
      onEvent(JSON.parse(data) as WorkspaceChangeEvent);
    } catch {
      // 忽略损坏的单条事件，保持后续事件流可用。
    }
  }
}

function cancellableDelay(milliseconds: number, signal: AbortSignal): Promise<void> {
  if (signal.aborted) {
    return Promise.resolve();
  }
  return new Promise((resolve) => {
    const timer = setTimeout(resolve, milliseconds);
    signal.addEventListener(
      "abort",
      () => {
        clearTimeout(timer);
        resolve();
      },
      { once: true },
    );
  });
}
