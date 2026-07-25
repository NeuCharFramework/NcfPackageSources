# Senparc.Xncf.DesktopBridge

DesktopBridge 是 NCF 桌面助手的可选 XNCF 模块。它通过逆变事件处理器的闭合服务映射旁路观察 `EventBus` 中的集成事件，并通过仅限本机会话令牌访问的 HTTP/SSE 接口，把当前活动安全地发送给桌面 GUI。

- 不读取或修改业务 `MemoryCache`。
- 不替代原有 EventBus 消费者，也不改变事件处理顺序。
- 仅当桌面应用通过 `NCF_DESKTOP_BRIDGE_TOKEN` 启动站点时启用接口。
- 未安装或未启用本模块时，NCF 本身仍可正常运行，GUI 自动降级为进程/日志兼容模式。

模块安装或更新后需要重启 NCF 站点。
