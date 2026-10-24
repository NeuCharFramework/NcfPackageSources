export class HttpError extends Error {
  public constructor(
    public readonly status: number,
    message: string,
    public readonly title?: string,
  ) {
    super(message);
    this.name = "HttpError";
  }
}

export function normalizeSiteUrl(value: string): string {
  let url: URL;
  try {
    url = new URL(value.trim());
  } catch {
    throw new Error("请输入完整站点地址，例如 https://ncf.example.com。");
  }

  if (url.username || url.password) {
    throw new Error("站点地址不能包含用户名或密码。");
  }

  const hostname = url.hostname.toLowerCase();
  const isLoopback =
    hostname === "localhost" ||
    hostname === "127.0.0.1" ||
    hostname === "::1" ||
    hostname === "[::1]";
  if (url.protocol !== "https:" && !(url.protocol === "http:" && isLoopback)) {
    throw new Error("远程站点必须使用 HTTPS；HTTP 仅允许 localhost、127.0.0.1 或 [::1]。");
  }

  url.search = "";
  url.hash = "";
  url.pathname = url.pathname.replace(/\/+$/, "");
  return url.toString().replace(/\/$/, "");
}

export function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === "AbortError";
}

export function abortError(): Error {
  const error = new Error("操作已取消。");
  error.name = "AbortError";
  return error;
}

export function errorMessage(error: unknown): string {
  if (error instanceof HttpError) {
    return error.status
      ? `${error.message}（HTTP ${error.status}）`
      : error.message;
  }
  return error instanceof Error ? error.message : String(error);
}
