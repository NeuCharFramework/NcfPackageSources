# Senparc.Xncf.DesktopBridge

DesktopBridge 是 NCF 桌面助手的可选 XNCF 模块。它通过逆变事件处理器的闭合服务映射旁路观察 `EventBus` 中的集成事件，并通过仅限本机会话令牌访问的 HTTP/SSE 接口，把当前活动安全地发送给桌面 GUI。

- 不读取或修改业务 `MemoryCache`。
- 不替代原有 EventBus 消费者，也不改变事件处理顺序。
- 仅当桌面应用通过 `NCF_DESKTOP_BRIDGE_TOKEN` 启动站点时启用接口。
- 未安装或未启用本模块时，NCF 本身仍可正常运行，GUI 自动降级为进程/日志兼容模式。

## 受权同步

`authorized-sync/events` 用于 Admin Chat 等需要登录身份的资源同步，并同时要求：

- 桌面会话头 `X-Ncf-Desktop-Token`；
- `Bearer_Backend` JWT；
- `AdminOnly` 授权策略；
- 事件的 `OwnerId` 与当前 JWT 的管理员 ID 一致。

EventBus 和 SSE 只传输频道、资源 ID 与变更类型，不传输密码、JWT 或聊天正文。桌面端收到通知后，使用同一管理员 JWT 从原业务 API 重新读取数据；未登录、非管理员、令牌过期或 Bridge 断开时，快捷聊天保持禁用。

模块安装或更新后需要重启 NCF 站点。
