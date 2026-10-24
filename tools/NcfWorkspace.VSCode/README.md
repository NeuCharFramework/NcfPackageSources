# NCF Workspace for VS Code / Cursor

通过 NCF DesktopBridge，把服务器上的 Workspace 以虚拟文件夹形式安全地打开到现代 VS Code 或 Cursor。扩展支持目录浏览、二进制文件、创建/修改/删除/重命名、ETag 冲突保护，以及服务端外部变更的 SSE 实时刷新。

## 小白三步上手

1. 在命令面板运行 **NCF Workspace: 连接 NCF Workspace**。
2. 输入 NCF 站点地址。扩展会打开浏览器，请先确认配对码，再批准管理员授权。
3. 选择一个 Workspace。编辑器会打开 `ncf-workspace://<id>/`，之后像普通目录一样使用即可。

没有 Workspace 时，可运行 **NCF Workspace: 新建 Workspace**。状态栏左下角的 `$(remote) NCF` 可查看连接状态并快速连接。

## 命令

- **连接 NCF Workspace**：发起新的 DesktopBridge 配对、PKCE 管理员换票并选择 Workspace。
- **重新连接**：优先复用安全存储中的 DesktopBridge 会话；管理员票据过期时重新换票。
- **断开连接**：立即中止配对轮询和 SSE，删除本机保存的会话及管理员票据。
- **打开 Workspace**：列出当前管理员的 Workspace 并打开。
- **新建 Workspace**：创建后直接打开。

## 安全说明

- 远程站点必须使用 HTTPS。HTTP 仅允许 `localhost`、`127.0.0.1` 和 `[::1]`。
- DesktopBridge session token 与 Admin access token 只保存在 VS Code/Cursor `SecretStorage` 中；普通设置仅保存站点地址和最近 Workspace 名称等非敏感信息。
- 文件覆盖和重命名使用 ETag / `If-Match`。若服务器返回 `409 Conflict`，扩展会明确提示先刷新文件，避免静默覆盖其他修改。
- 扩展不会输出或记录 token。

## 开发

```bash
npm install
npm run check
npm run compile
```

本扩展无运行时 npm 依赖，HTTP 与 SSE 使用扩展宿主提供的 Node.js `fetch`。
