import { Buffer } from "node:buffer";
import { HttpError } from "./errors";
import {
  AdminHandoffCreateResponse,
  AdminHandoffRedeemResponse,
  PairingCreateResponse,
  PairingPollResponse,
  WorkspaceCapabilities,
  WorkspaceDescriptor,
  WorkspaceEntry,
  WorkspaceFileMetadata,
  WorkspaceWriteFileResult,
} from "./types";

export interface Credentials {
  desktopToken?: string;
  accessToken?: string;
}

type SecurityLevel = "none" | "desktop" | "full";

interface RequestOptions extends RequestInit {
  security?: SecurityLevel;
  signal?: AbortSignal;
}

export class NcfApiClient {
  public constructor(
    private readonly baseUrl: string,
    private readonly getCredentials: () => Credentials,
    private readonly getConnectionSignal: () => AbortSignal,
  ) {}

  public pairingRequest(clientName: string): Promise<PairingCreateResponse> {
    return this.json<PairingCreateResponse>(
      "/api/Senparc.Xncf.DesktopBridge/pairing/requests",
      this.jsonPost({ clientName }, "none"),
    );
  }

  public pairingPoll(requestId: string, pollSecret: string): Promise<PairingPollResponse> {
    return this.json<PairingPollResponse>(
      "/api/Senparc.Xncf.DesktopBridge/pairing/poll",
      this.jsonPost({ requestId, pollSecret }, "none"),
    );
  }

  public createAdminHandoff(
    codeChallenge: string,
    returnPath: string,
  ): Promise<AdminHandoffCreateResponse> {
    return this.json<AdminHandoffCreateResponse>(
      "/api/Senparc.Xncf.DesktopBridge/admin-auth-handoff/requests",
      this.jsonPost({ codeChallenge, returnPath }, "desktop"),
    );
  }

  public redeemAdminHandoff(
    requestId: string,
    codeVerifier: string,
  ): Promise<AdminHandoffRedeemResponse> {
    return this.json<AdminHandoffRedeemResponse>(
      "/api/Senparc.Xncf.DesktopBridge/admin-auth-handoff/redeem",
      this.jsonPost({ requestId, codeVerifier }, "desktop"),
    );
  }

  public capabilities(): Promise<WorkspaceCapabilities> {
    return this.json<WorkspaceCapabilities>(
      "/api/Senparc.Xncf.Workspace/capabilities",
      { security: "full" },
    );
  }

  public listWorkspaces(): Promise<WorkspaceDescriptor[]> {
    return this.json<WorkspaceDescriptor[]>(
      "/api/Senparc.Xncf.Workspace/workspaces",
      { security: "full" },
    );
  }

  public createWorkspace(name: string): Promise<WorkspaceDescriptor> {
    return this.json<WorkspaceDescriptor>(
      "/api/Senparc.Xncf.Workspace/workspaces",
      this.jsonPost({ name }, "full"),
    );
  }

  public listEntries(workspaceId: string, path: string): Promise<WorkspaceEntry[]> {
    return this.json<WorkspaceEntry[]>(
      `${this.workspacePath(workspaceId, "entries")}?${query({ path })}`,
      { security: "full" },
    );
  }

  public stat(workspaceId: string, path: string): Promise<WorkspaceFileMetadata> {
    return this.json<WorkspaceFileMetadata>(
      `${this.workspacePath(workspaceId, "stat")}?${query({ path })}`,
      { security: "full" },
    );
  }

  public async readFile(
    workspaceId: string,
    path: string,
  ): Promise<{ content: Uint8Array; eTag?: string }> {
    const response = await this.raw(
      `${this.workspacePath(workspaceId, "file")}?${query({ path })}`,
      { security: "full" },
    );
    return {
      content: new Uint8Array(await response.arrayBuffer()),
      eTag: response.headers.get("etag") ?? undefined,
    };
  }

  public writeFile(
    workspaceId: string,
    path: string,
    content: Uint8Array,
    create: boolean,
    overwrite: boolean,
    expectedETag?: string,
  ): Promise<WorkspaceWriteFileResult> {
    const headers = new Headers({ "Content-Type": "application/octet-stream" });
    if (expectedETag) {
      headers.set("If-Match", expectedETag);
    }
    return this.json<WorkspaceWriteFileResult>(
      `${this.workspacePath(workspaceId, "file")}?${query({
        path,
        create: String(create),
        overwrite: String(overwrite),
      })}`,
      {
        method: "PUT",
        headers,
        body: Buffer.from(content),
        security: "full",
      },
    );
  }

  public async createDirectory(workspaceId: string, path: string): Promise<void> {
    await this.raw(
      this.workspacePath(workspaceId, "directories"),
      this.jsonPost({ path }, "full"),
    );
  }

  public async delete(
    workspaceId: string,
    path: string,
    recursive: boolean,
    expectedETag?: string,
  ): Promise<void> {
    const headers = new Headers();
    if (expectedETag) {
      headers.set("If-Match", expectedETag);
    }
    await this.raw(
      `${this.workspacePath(workspaceId, "entry")}?${query({
        path,
        recursive: String(recursive),
      })}`,
      { method: "DELETE", headers, security: "full" },
    );
  }

  public async move(
    workspaceId: string,
    sourceRelativePath: string,
    targetRelativePath: string,
    overwrite: boolean,
    expectedETag: string,
  ): Promise<void> {
    await this.raw(
      this.workspacePath(workspaceId, "move"),
      this.jsonPost(
        { sourceRelativePath, targetRelativePath, overwrite, expectedETag },
        "full",
      ),
    );
  }

  public openEvents(workspaceId: string, signal: AbortSignal): Promise<Response> {
    return this.raw(
      `${this.workspacePath(workspaceId, "events")}?replayBuffered=false`,
      {
        headers: { Accept: "text/event-stream" },
        security: "full",
        signal,
      },
    );
  }

  private workspacePath(workspaceId: string, operation: string): string {
    return `/api/Senparc.Xncf.Workspace/workspaces/${encodeURIComponent(workspaceId)}/${operation}`;
  }

  private jsonPost(body: unknown, security: SecurityLevel): RequestOptions {
    return {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      security,
    };
  }

  private async json<T>(path: string, options: RequestOptions): Promise<T> {
    const response = await this.raw(path, options);
    if (response.status === 204) {
      return undefined as T;
    }
    return (await response.json()) as T;
  }

  private async raw(path: string, options: RequestOptions): Promise<Response> {
    const headers = new Headers(options.headers);
    headers.set("Accept", headers.get("Accept") ?? "application/json");

    const security = options.security ?? "none";
    const credentials = this.getCredentials();
    if (security !== "none") {
      if (!credentials.desktopToken) {
        throw new HttpError(401, "DesktopBridge 会话不存在，请重新连接。");
      }
      headers.set("X-Ncf-Desktop-Token", credentials.desktopToken);
    }
    if (security === "full") {
      if (!credentials.accessToken) {
        throw new HttpError(401, "管理员授权不存在或已过期，请重新连接。");
      }
      headers.set("Authorization", `Bearer ${credentials.accessToken}`);
    }

    const signal = options.signal
      ? combineSignals(this.getConnectionSignal(), options.signal)
      : this.getConnectionSignal();
    let response: Response;
    try {
      response = await fetch(`${this.baseUrl}${path}`, {
        ...options,
        headers,
        signal,
      });
    } catch (error) {
      if (signal.aborted) {
        throw signal.reason instanceof Error ? signal.reason : abortDomError();
      }
      throw new Error(`无法访问 NCF 站点：${error instanceof Error ? error.message : String(error)}`);
    }

    if (!response.ok) {
      throw await responseError(response);
    }
    return response;
  }
}

function query(values: Record<string, string>): string {
  return new URLSearchParams(values).toString();
}

function combineSignals(first: AbortSignal, second: AbortSignal): AbortSignal {
  if (first.aborted) {
    return first;
  }
  if (second.aborted) {
    return second;
  }
  const any = (AbortSignal as typeof AbortSignal & {
    any?: (signals: AbortSignal[]) => AbortSignal;
  }).any;
  if (any) {
    return any([first, second]);
  }
  const controller = new AbortController();
  const abort = (signal: AbortSignal) => controller.abort(signal.reason);
  first.addEventListener("abort", () => abort(first), { once: true });
  second.addEventListener("abort", () => abort(second), { once: true });
  return controller.signal;
}

async function responseError(response: Response): Promise<HttpError> {
  let title: string | undefined;
  let message = response.statusText || "NCF 请求失败。";
  try {
    const body = (await response.json()) as {
      title?: string;
      detail?: string;
      message?: string;
      status?: string;
    };
    title = body.title;
    message = body.detail ?? body.message ?? title ?? message;
  } catch {
    // 非 JSON 错误正文不应显示，避免意外泄露服务端页面内容。
  }
  return new HttpError(response.status, message, title);
}

function abortDomError(): Error {
  const error = new Error("操作已取消。");
  error.name = "AbortError";
  return error;
}
