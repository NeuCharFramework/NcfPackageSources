import { createHash, randomBytes } from "node:crypto";
import * as vscode from "vscode";
import { NcfApiClient } from "./apiClient";
import {
  abortError,
  errorMessage,
  HttpError,
  isAbortError,
  normalizeSiteUrl,
} from "./errors";
import { WorkspaceCapabilities } from "./types";

const DESKTOP_TOKEN_KEY = "ncfWorkspace.desktopSessionToken";
const ADMIN_TOKEN_KEY = "ncfWorkspace.adminAccessToken";
const SITE_URL_KEY = "ncfWorkspace.siteUrl";
const USER_NAME_KEY = "ncfWorkspace.userName";
const LAST_WORKSPACE_ID_KEY = "ncfWorkspace.lastWorkspaceId";
const LAST_WORKSPACE_NAME_KEY = "ncfWorkspace.lastWorkspaceName";

export type ConnectionState = "disconnected" | "connecting" | "connected" | "error";

export interface ConnectionSnapshot {
  state: ConnectionState;
  siteUrl?: string;
  userName?: string;
  message?: string;
}

export class ConnectionManager implements vscode.Disposable {
  private desktopToken?: string;
  private accessToken?: string;
  private siteUrl?: string;
  private userName?: string;
  private operationController = new AbortController();
  private snapshot: ConnectionSnapshot = { state: "disconnected" };
  private readonly stateEmitter = new vscode.EventEmitter<ConnectionSnapshot>();

  public readonly onDidChangeState = this.stateEmitter.event;

  public constructor(private readonly context: vscode.ExtensionContext) {}

  public get state(): ConnectionState {
    return this.snapshot.state;
  }

  public get connection(): ConnectionSnapshot {
    return this.snapshot;
  }

  public get client(): NcfApiClient {
    if (!this.siteUrl) {
      throw new HttpError(401, "尚未设置 NCF 站点，请先连接。");
    }
    return this.createClient(this.siteUrl);
  }

  public async restore(): Promise<boolean> {
    const storedSite = this.context.globalState.get<string>(SITE_URL_KEY);
    this.siteUrl = storedSite ? normalizeSiteUrl(storedSite) : undefined;
    this.userName = this.context.globalState.get<string>(USER_NAME_KEY);
    this.desktopToken = await this.context.secrets.get(DESKTOP_TOKEN_KEY);
    this.accessToken = await this.context.secrets.get(ADMIN_TOKEN_KEY);
    if (!this.siteUrl || !this.desktopToken || !this.accessToken) {
      this.setState("disconnected");
      return false;
    }

    this.resetOperations();
    this.setState("connecting", "正在恢复安全会话…");
    try {
      await this.validateCapabilities(this.client);
      this.setState("connected");
      return true;
    } catch (error) {
      if (!isAbortError(error)) {
        this.setState("disconnected", "会话需要重新连接");
      }
      return false;
    }
  }

  public async connect(): Promise<void> {
    const value = await vscode.window.showInputBox({
      title: "连接 NCF Workspace",
      prompt: "请输入启用了 DesktopBridge 与 Workspace 模块的 NCF 站点地址",
      placeHolder: "https://ncf.example.com",
      value: this.siteUrl ?? this.context.globalState.get<string>(SITE_URL_KEY) ?? "",
      ignoreFocusOut: true,
      validateInput: (input) => {
        try {
          normalizeSiteUrl(input);
          return undefined;
        } catch (error) {
          return errorMessage(error);
        }
      },
    });
    if (value === undefined) {
      throw abortError();
    }

    const siteUrl = normalizeSiteUrl(value);
    this.resetOperations();
    this.setState("connecting", "等待 DesktopBridge 配对…");
    this.siteUrl = siteUrl;
    this.userName = undefined;
    await this.context.globalState.update(SITE_URL_KEY, siteUrl);
    await this.clearSecrets();

    try {
      const client = this.createClient(siteUrl);
      const desktopToken = await this.performPairing(client);
      this.desktopToken = desktopToken;
      await this.context.secrets.store(DESKTOP_TOKEN_KEY, desktopToken);

      this.setState("connecting", "等待管理员授权…");
      await this.performAdminHandoff(client);
      await this.validateCapabilities(client);
      this.setState("connected");
    } catch (error) {
      this.setState(isAbortError(error) ? "disconnected" : "error", errorMessage(error));
      throw error;
    }
  }

  public async reconnect(): Promise<void> {
    this.resetOperations();
    this.setState("connecting", "正在重新连接…");
    this.siteUrl = this.context.globalState.get<string>(SITE_URL_KEY);
    this.userName = this.context.globalState.get<string>(USER_NAME_KEY);
    this.desktopToken = await this.context.secrets.get(DESKTOP_TOKEN_KEY);
    this.accessToken = await this.context.secrets.get(ADMIN_TOKEN_KEY);

    if (!this.siteUrl || !this.desktopToken) {
      this.setState("disconnected");
      await this.connect();
      return;
    }

    try {
      this.siteUrl = normalizeSiteUrl(this.siteUrl);
      const client = this.createClient(this.siteUrl);
      if (this.accessToken) {
        try {
          await this.validateCapabilities(client);
          this.setState("connected");
          return;
        } catch (error) {
          if (!(error instanceof HttpError) || ![401, 403].includes(error.status)) {
            throw error;
          }
        }
      }

      this.setState("connecting", "管理员票据已过期，请重新授权…");
      try {
        await this.performAdminHandoff(client);
      } catch (error) {
        if (error instanceof HttpError && [401, 403].includes(error.status)) {
          await this.connect();
          return;
        }
        throw error;
      }
      await this.validateCapabilities(client);
      this.setState("connected");
    } catch (error) {
      this.setState(isAbortError(error) ? "disconnected" : "error", errorMessage(error));
      throw error;
    }
  }

  public async disconnect(): Promise<void> {
    this.operationController.abort(abortError());
    this.operationController = new AbortController();
    await this.clearSecrets();
    this.desktopToken = undefined;
    this.accessToken = undefined;
    this.userName = undefined;
    await Promise.all([
      this.context.globalState.update(USER_NAME_KEY, undefined),
      this.context.globalState.update(LAST_WORKSPACE_ID_KEY, undefined),
      this.context.globalState.update(LAST_WORKSPACE_NAME_KEY, undefined),
    ]);
    this.setState("disconnected");
  }

  public async rememberWorkspace(id: string, name: string): Promise<void> {
    await Promise.all([
      this.context.globalState.update(LAST_WORKSPACE_ID_KEY, id),
      this.context.globalState.update(LAST_WORKSPACE_NAME_KEY, name),
    ]);
    this.setState(this.snapshot.state);
  }

  public get rememberedWorkspace(): { id?: string; name?: string } {
    return {
      id: this.context.globalState.get<string>(LAST_WORKSPACE_ID_KEY),
      name: this.context.globalState.get<string>(LAST_WORKSPACE_NAME_KEY),
    };
  }

  public dispose(): void {
    this.operationController.abort(abortError());
    this.stateEmitter.dispose();
  }

  private async performPairing(client: NcfApiClient): Promise<string> {
    return vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: "NCF DesktopBridge 配对",
        cancellable: true,
      },
      async (progress, cancellationToken) => {
        cancellationToken.onCancellationRequested(() =>
          this.operationController.abort(abortError()),
        );
        const pairing = await client.pairingRequest(
          `${vscode.env.appName} / NCF Workspace`,
        );
        const verificationUrl = new URL(pairing.verificationPath, `${this.siteUrl}/`);
        await vscode.env.openExternal(vscode.Uri.parse(verificationUrl.toString()));
        void vscode.window.showInformationMessage(
          `请在浏览器确认配对码：${pairing.deviceCode}`,
          { modal: false },
        );

        const interval = Math.max(1, pairing.pollIntervalSeconds) * 1000;
        while (Date.now() < Date.parse(pairing.expiresAt)) {
          progress.report({ message: `请在浏览器确认配对码 ${pairing.deviceCode}` });
          await delay(interval, this.operationController.signal);
          const result = await client.pairingPoll(pairing.requestId, pairing.pollSecret);
          const status = result.status.toLowerCase();
          if (status === "approved" && result.sessionToken) {
            return result.sessionToken;
          }
          if (status !== "pending") {
            throw new Error(result.message ?? `DesktopBridge 配对未完成：${result.status}`);
          }
        }
        throw new Error("DesktopBridge 配对已超时，请重新连接。");
      },
    );
  }

  private async performAdminHandoff(client: NcfApiClient): Promise<void> {
    const codeVerifier = randomBytes(32).toString("base64url");
    const codeChallenge = createHash("sha256")
      .update(codeVerifier, "ascii")
      .digest("base64url");
    const handoff = await client.createAdminHandoff(codeChallenge, "/Admin/Index");
    const approvalUrl = new URL(handoff.approvalPath, `${this.siteUrl}/`);
    await vscode.env.openExternal(vscode.Uri.parse(approvalUrl.toString()));

    await vscode.window.withProgress(
      {
        location: vscode.ProgressLocation.Notification,
        title: "NCF 管理员授权",
        cancellable: true,
      },
      async (progress, cancellationToken) => {
        cancellationToken.onCancellationRequested(() =>
          this.operationController.abort(abortError()),
        );
        const interval = Math.max(250, handoff.pollIntervalMilliseconds);
        while (Date.now() < Date.parse(handoff.expiresAt)) {
          progress.report({ message: "请在浏览器批准本次 Workspace 访问" });
          await delay(interval, this.operationController.signal);
          const result = await client.redeemAdminHandoff(
            handoff.requestId,
            codeVerifier,
          );
          const status = result.status.toLowerCase();
          if (status === "approved" && result.accessToken) {
            this.accessToken = result.accessToken;
            this.userName = result.userName;
            await this.context.secrets.store(ADMIN_TOKEN_KEY, result.accessToken);
            await this.context.globalState.update(USER_NAME_KEY, result.userName);
            return;
          }
          if (status !== "pending") {
            throw new Error(result.message ?? `管理员授权未完成：${result.status}`);
          }
        }
        throw new Error("管理员授权已超时，请重新连接。");
      },
    );
  }

  private async validateCapabilities(client: NcfApiClient): Promise<WorkspaceCapabilities> {
    const capabilities = await client.capabilities();
    if (capabilities.protocolVersion !== 1) {
      throw new Error(
        `Workspace 协议版本不兼容：服务器为 ${capabilities.protocolVersion}，扩展需要 1。`,
      );
    }
    if (
      !capabilities.supportsBinaryFiles ||
      !capabilities.supportsEtags ||
      !capabilities.supportsEvents
    ) {
      throw new Error("服务器 Workspace 能力不完整，需要二进制、ETag 和事件支持。");
    }
    return capabilities;
  }

  private createClient(siteUrl: string): NcfApiClient {
    return new NcfApiClient(
      siteUrl,
      () => ({
        desktopToken: this.desktopToken,
        accessToken: this.accessToken,
      }),
      () => this.operationController.signal,
    );
  }

  private resetOperations(): void {
    this.operationController.abort(abortError());
    this.operationController = new AbortController();
  }

  private async clearSecrets(): Promise<void> {
    await Promise.all([
      this.context.secrets.delete(DESKTOP_TOKEN_KEY),
      this.context.secrets.delete(ADMIN_TOKEN_KEY),
    ]);
    this.desktopToken = undefined;
    this.accessToken = undefined;
  }

  private setState(state: ConnectionState, message?: string): void {
    this.snapshot = {
      state,
      siteUrl: this.siteUrl,
      userName: this.userName,
      message,
    };
    this.stateEmitter.fire(this.snapshot);
  }
}

function delay(milliseconds: number, signal: AbortSignal): Promise<void> {
  if (signal.aborted) {
    return Promise.reject(signal.reason instanceof Error ? signal.reason : abortError());
  }
  return new Promise((resolve, reject) => {
    const timer = setTimeout(resolve, milliseconds);
    signal.addEventListener(
      "abort",
      () => {
        clearTimeout(timer);
        reject(signal.reason instanceof Error ? signal.reason : abortError());
      },
      { once: true },
    );
  });
}
