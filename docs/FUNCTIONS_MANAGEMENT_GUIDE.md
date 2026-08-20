# NeuCharFramework (NCF) Functions 管理体系完整指南

## 概述
NeuCharFramework 中的 Functions 是通过 **FunctionRender 系统** 管理的，用于在 XNCF 模块中定义和暴露可执行的操作方法。这是一个基于特性标记和反射扫描的动态注册系统。

---

## 一、核心数据结构

### 1. FunctionRenderAttribute 特性
**位置**: `src/Basic/Senparc.Ncf.Core/AppServices/FunctionRenderAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class FunctionRenderAttribute : Attribute
{
    /// <summary>
    /// 函数的显示名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 函数的说明描述
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// 分类到 XNCF 模块的 Register 类型（模块标识）
    /// </summary>
    public Type RegisterType { get; set; }

    public FunctionRenderAttribute(string name, string description, Type registerType)
}
```

### 2. FunctionRenderBag 数据包
**位置**: `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderBag.cs`

```csharp
public struct FunctionRenderBag
{
    // 唯一标识键 = "{DeclaringType.FullName}-{MethodName}"
    public string Key { get; set; }
    
    // 方法信息（通过反射获取）
    public MethodInfo MethodInfo { get; set; }
    
    // 函数参数类型（从方法第一个参数推断，默认为 FunctionAppRequestBase）
    public Type FunctionParameterType { get; set; }
    
    // 函数标记特性
    public FunctionRenderAttribute FunctionRenderAttribute { get; set; }
}
```

### 3. FunctionRenderCollection 集合
**位置**: `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderCollection.cs`

```csharp
// 结构: Type(Register类型) -> Dictionary<string(Key), FunctionRenderBag>
public class FunctionRenderCollection : 
    ConcurrentDictionary<Type, ConcurrentDictionary<string, FunctionRenderBag>>
{
    /// <summary>
    /// 添加一个 Function 到集合
    /// </summary>
    public FunctionRenderBag Add(MethodInfo methodInfo, FunctionRenderAttribute functionRenderAttribute)
    
    /// <summary>
    /// 获取指定注册类型下的所有 FunctionRender
    /// </summary>
    public IReadOnlyList<FunctionRenderBag> GetByRegisterType(Type registerType)
    
    /// <summary>
    /// 通过模块 UID 获取 FunctionRender 集合
    /// </summary>
    public IReadOnlyList<FunctionRenderBag> GetByModuleUid(string moduleUid)
    
    /// <summary>
    /// 解析符号插件 - 将 [#sym:FunctionRender] 符号转换为可导入的插件对象
    /// </summary>
    public IReadOnlyDictionary<string, object> ResolveSymbolPlugins(
        IServiceProvider serviceProvider, 
        string symbolExpression, 
        IEnumerable<string> moduleUids)
}
```

---

## 二、全局注册管理

### 1. Register 静态类（核心管理器）
**位置**: `src/Basic/Senparc.Ncf.XncfBase/Register.cs`

```csharp
public static class Register
{
    /// <summary>
    /// 全局 FunctionRender 集合 - 存储所有已注册的 Functions
    /// </summary>
    public static FunctionRenderCollection FunctionRenderCollection { get; set; } 
        = new FunctionRenderCollection();
    
    /// <summary>
    /// 所有线程的集合
    /// </summary>
    public static ConcurrentDictionary<ThreadInfo, Thread> ThreadCollection { get; set; }
}
```

### 2. 自动扫描注册流程
**时机**: 调用 `services.StartNcfEngine(configuration, env, dllFilePatterns)`

**流程**:
1. 扫描所有程序集
2. 遍历每个程序集中的所有类型
3. **筛选条件**: 类必须继承自 `AppServiceBase`
4. **方法筛选**: 获取有 `[FunctionRender]` 特性标记的方法
5. **注册**: 调用 `FunctionRenderCollection.Add(methodInfo, attribute)`
6. **DI注册**: 将 AppService 类型添加到依赖注入容器

**核心代码** (Register.cs, 第 175-193 行):
```csharp
//配置 FunctionRender
if (t.IsSubclassOf(typeof(AppServiceBase)))
{
    //遍历其中具体方法
    var methods = t.GetMethods();
    var hasFunctionMethod = false;
    foreach (var method in methods)
    {
        var attr = method.GetCustomAttributes(typeof(FunctionRenderAttribute), true)
                    .FirstOrDefault() as FunctionRenderAttribute;
        if (attr != null)
        {
            FunctionRenderCollection.Add(method, attr);
            hasFunctionMethod = true;
        }
    }

    services.AddScoped(t);
}
```

---

## 三、IXncfRegister 接口

**位置**: `src/Basic/Senparc.Ncf.XncfBase/Interfaces/IXncfRegister.cs`

```csharp
public interface IXncfRegister
{
    /// <summary>
    /// 模块名称（全局唯一）
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 模块编号（全局唯一）- 用于获取 Functions 时的查询
    /// </summary>
    string Uid { get; }
    
    /// <summary>
    /// 版本号
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// 菜单名称
    /// </summary>
    string MenuName { get; }
    
    /// <summary>
    /// 模块图标
    /// </summary>
    string Icon { get; }
    
    /// <summary>
    /// 模块说明
    /// </summary>
    string Description { get; }
    
    // 注：Functions 列表属性已注释掉，改为动态扫描注册
    // 旧设计: public abstract IList<Type> Functions { get; }
}
```

**注意**: `Functions` 属性已被注释掉（见第 45-47 行），改为通过 `FunctionRenderAttribute` 特性进行动态扫描。

---

## 四、实际使用示例

### 示例：AgentsManager 模块
**位置**: `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/ChatGroupAppService.cs`

```csharp
public class ChatGroupAppService : AppServiceBase
{
    [FunctionRender("管理 ChatGroup", "管理 ChatGroup", typeof(Register))]
    public async Task<StringAppResponse> ManageChatGroupManage(ChatGroup_ManageChatGroupRequest request)
    {
        // 实现...
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            // 业务逻辑
            return logger.ToString();
        });
    }

    [FunctionRender("启动 ChatGroup", "启动 ChatGroup", typeof(Register))]
    public async Task<StringAppResponse> RunChatGroup(ChatGroup_RunChatGroupRequest request)
    {
        // 实现...
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            // 业务逻辑
            return logger.ToString();
        });
    }
}
```

---

## 五、获取 Functions 的三种方式

### 方式 1：通过 Register 类型获取
```csharp
// 获取 AgentsManager.Register 所属的所有 Functions
var functions = Register.FunctionRenderCollection.GetByRegisterType(typeof(Senparc.Xncf.AgentsManager.Register));
foreach (var func in functions)
{
    Console.WriteLine($"函数: {func.FunctionRenderAttribute.Name}");
    Console.WriteLine($"说明: {func.FunctionRenderAttribute.Description}");
    Console.WriteLine($"方法: {func.MethodInfo.Name}");
}
```

### 方式 2：通过模块 UID 获取
```csharp
// 获取 uid 为 "agentsmanager" 的模块的所有 Functions
var functions = Register.FunctionRenderCollection.GetByModuleUid("agentsmanager");
foreach (var func in functions)
{
    Console.WriteLine($"Function: {func.Key}");
}
```

### 方式 3：解析符号插件（高级用法）
```csharp
// 当表达式包含 [#sym:FunctionRender] 时
var symbols = Register.FunctionRenderCollection.ResolveSymbolPlugins(
    serviceProvider,
    "[#sym:FunctionRender]",
    new[] { "agentsmanager", "aikernel" }
);

// 返回一个字典，key 为 AppService 的完全限定名，value 为实例
foreach (var kvp in symbols)
{
    var appServiceType = kvp.Key;  // e.g., "Senparc.Xncf.AgentsManager.Application.AppService.ChatGroupAppService"
    var appServiceInstance = kvp.Value;
}
```

---

## 六、关键文件路径总结

| 组件 | 路径 |
|------|------|
| **FunctionRenderAttribute** | `src/Basic/Senparc.Ncf.Core/AppServices/FunctionRenderAttribute.cs` |
| **FunctionRenderBag** | `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderBag.cs` |
| **FunctionRenderCollection** | `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderCollection.cs` |
| **核心 Register 类** | `src/Basic/Senparc.Ncf.XncfBase/Register.cs` |
| **IXncfRegister 接口** | `src/Basic/Senparc.Ncf.XncfBase/Interfaces/IXncfRegister.cs` |
| **XncfRegisterBase 基类** | `src/Basic/Senparc.Ncf.XncfBase/XncfRegisterBase.cs` |
| **FunctionAppRequestBase** | `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionAppRequestBase.cs` |
| **FunctionRenderSymbolHelper** | `src/Basic/Senparc.Ncf.Core/AppServices/FunctionRenderSymbolHelper.cs` |

---

## 七、最佳实践

### 1. 定义 Function（在 AppService 中）
```csharp
public class MyAppService : AppServiceBase
{
    [FunctionRender("操作名称", "操作描述", typeof(Register))]
    public async Task<StringAppResponse> MyFunction(MyFunctionRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            // 执行业务逻辑
            logger.Append("执行成功");
            return logger.ToString();
        });
    }
}
```

### 2. 定义 Function 参数请求
```csharp
public class MyFunctionRequest : FunctionAppRequestBase
{
    // FunctionAppRequestBase 继承自 AppRequestBase
    // 可定义自定义参数属性
    
    [XncfField("参数名", "参数说明")]
    public string Parameter1 { get; set; }
}
```

### 3. 在模块中访问 Functions
```csharp
public class MyRegister : XncfRegisterBase
{
    // ...其他实现...
    
    public void GetMyFunctions()
    {
        // 方式1：通过自己的类型
        var myFunctions = Register.FunctionRenderCollection.GetByRegisterType(this.GetType());
        
        // 方式2：通过 Uid
        var functionList = Register.FunctionRenderCollection.GetByModuleUid(this.Uid);
    }
}
```

---

## 八、架构设计特点

1. **动态扫描**: 无需手动注册，系统启动时自动扫描所有 AppService 中的 [FunctionRender] 特性

2. **分层隔离**: Functions 按 Register 类型（模块）分组存储，便于模块化管理

3. **元数据保留**: FunctionRenderBag 保留完整的方法元数据（MethodInfo），支持运行时反射调用

4. **线程安全**: 使用 ConcurrentDictionary，支持并发访问

5. **符号解析**: 支持通过 [#sym:FunctionRender] 符号自动解析和导入插件

6. **参数流式化**: 所有 Function 方法使用统一的请求/响应模式，支持参数自动映射

---

## 九、与 KnowledgeBase 模块的集成建议

对于 RAG 系统的 KnowledgeBase 模块：

```csharp
public class KnowledgeBasesAppService : AppServiceBase
{
    [FunctionRender("上传并处理文档", "上传文件并进行分块/嵌入处理", typeof(Register))]
    public async Task<StringAppResponse> UploadAndProcessDocument(
        UploadDocumentRequest request)
    {
        // 调用 KnowledgeBasesService 进行文件处理
    }
    
    [FunctionRender("执行 RAG 查询", "提交查询并获取增强的回答", typeof(Register))]
    public async Task<StringAppResponse> ExecuteRagQuery(RagQueryRequest request)
    {
        // 执行检索-生成流程
    }
    
    [FunctionRender("管理知识库", "创建/删除/更新知识库", typeof(Register))]
    public async Task<StringAppResponse> ManageKnowledgeBase(
        ManageKnowledgeBaseRequest request)
    {
        // 管理操作
    }
}
```

这样 KnowledgeBase 模块的操作方法将自动被注册到全局 Functions 系统中。
