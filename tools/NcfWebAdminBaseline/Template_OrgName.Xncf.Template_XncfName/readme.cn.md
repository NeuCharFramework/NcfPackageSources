## Template_OrgName.Xncf.Template_XncfName 项目说明

> 本项目为 Xncf 模板项目，用于快速创建可快速嵌入 NeuCharFramework（NCF）的 Xncf 模板项目。

Template_Description

## EventBus 内部回环检查

启用 Function 示例后，可以执行“EventBus 内部回环检查”。该功能使用
`IEventBusRequestClient` 在当前 XNCF 进程内完成请求、处理、派生响应及关联等待，
用于确认宿主 EventBus 注册与程序集扫描是否正常。

此检查不读取数据库、配置、文件、用户信息或其他模块数据；它使用 5 秒有限超时，
不会创建 HTTP、SSE 或其他额外对外端点。EventBus 是进程内通信机制，不应被视为模块间的安全隔离边界。
