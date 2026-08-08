# Local分支  
  
Local 分支主要负责处理系统内部各模块或领域之间的通信。它通过公开的接口来实现这些模块或领域之间的协作，从而确保它们之间的解耦和易于扩展。通常，Local分支在同一个系统或服务内实现，而不涉及跨系统或服务的通信。  

应用服务已统一放在 [`Application/AppServices`](../../Application/AppServices) 中，输入、输出模型放在 [`Application/DTOs`](../../Application/DTOs) 中。NCF 使用 `动态 WebApi`，可以将 `AppService` 方法自动生成为 WebApi 服务；只有需要手动控制 HTTP 协议细节时，才在 OHS 的 `Local` 或 `Remote` 分支中编写 Controller。