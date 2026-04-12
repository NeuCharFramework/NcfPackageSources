[中文版](FUNCTIONS_MANAGEMENT_GUIDE.cn.md)

# Complete Guide to NeuCharFramework (NCF) Functions Management System

## Overview
Functions in NeuCharFramework are managed through the **FunctionRender system**, which is used to define and expose executable operation methods in XNCF modules. This is a dynamic registration system based on feature tags and reflection scanning.

---

## 1. Core data structure

### 1. FunctionRenderAttribute attribute
**Location**: `src/Basic/Senparc.Ncf.Core/AppServices/FunctionRenderAttribute.cs````csharp
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
```### 2. FunctionRenderBag data package
**Location**: `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderBag.cs````csharp
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
```### 3. FunctionRenderCollection collection
**Location**: `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderCollection.cs````csharp
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
```---

## 2. Global registration management

### 1. Register static class (core manager)
**Location**: `src/Basic/Senparc.Ncf.XncfBase/Register.cs````csharp
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
```### 2. Automatic scanning registration process
**Timing**: Call `services.StartNcfEngine(configuration, env, dllFilePatterns)`

**Process**:
1. Scan all assemblies
2. Iterate through all types in each assembly
3. **Filter condition**: Class must inherit from `AppServiceBase`
4. **Method filtering**: Get methods marked with the `[FunctionRender]` attribute
5. **Registration**: Call `FunctionRenderCollection.Add(methodInfo, attribute)`
6. **DI Registration**: Add the AppService type to the dependency injection container

**Core code** (Register.cs, lines 175-193):```csharp
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
```---

## 3. IXncfRegister interface

**Location**: `src/Basic/Senparc.Ncf.XncfBase/Interfaces/IXncfRegister.cs````csharp
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
```**NOTE**: The `Functions` attribute has been commented out (see lines 45-47) and is instead scanned dynamically via the `FunctionRenderAttribute` attribute.

---

## 4. Actual usage examples

### Example: AgentsManager module
**Location**: `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/ChatGroupAppService.cs````csharp
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
```---

## 5. Three ways to obtain Functions

### Method 1: Obtain through Register type```csharp
// 获取 AgentsManager.Register 所属的所有 Functions
var functions = Register.FunctionRenderCollection.GetByRegisterType(typeof(Senparc.Xncf.AgentsManager.Register));
foreach (var func in functions)
{
    Console.WriteLine($"函数: {func.FunctionRenderAttribute.Name}");
    Console.WriteLine($"说明: {func.FunctionRenderAttribute.Description}");
    Console.WriteLine($"方法: {func.MethodInfo.Name}");
}
```### Method 2: Obtain through module UID```csharp
// 获取 uid 为 "agentsmanager" 的模块的所有 Functions
var functions = Register.FunctionRenderCollection.GetByModuleUid("agentsmanager");
foreach (var func in functions)
{
    Console.WriteLine($"Function: {func.Key}");
}
```### Method 3: Parse symbol plug-in (advanced usage)```csharp
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
```---

## 6. Summary of key file paths

| Component | Path |
|------|------|
| **FunctionRenderAttribute** | `src/Basic/Senparc.Ncf.Core/AppServices/FunctionRenderAttribute.cs` |
| **FunctionRenderBag** | `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderBag.cs` |
| **FunctionRenderCollection** | `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionRenderCollection.cs` |
| **Core Register Class** | `src/Basic/Senparc.Ncf.XncfBase/Register.cs` |
| **IXncfRegister interface** | `src/Basic/Senparc.Ncf.XncfBase/Interfaces/IXncfRegister.cs` |
| **XncfRegisterBase base class** | `src/Basic/Senparc.Ncf.XncfBase/XncfRegisterBase.cs` |
| **FunctionAppRequestBase** | `src/Basic/Senparc.Ncf.XncfBase/FunctionRenders/FunctionAppRequestBase.cs` |
| **FunctionRenderSymbolHelper** | `src/Basic/Senparc.Ncf.Core/AppServices/FunctionRenderSymbolHelper.cs` |

---

## 7. Best Practices

### 1. Define Function (in AppService)```csharp
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
```### 2. Define Function parameter request```csharp
public class MyFunctionRequest : FunctionAppRequestBase
{
    // FunctionAppRequestBase 继承自 AppRequestBase
    // 可定义自定义参数属性
    
    [XncfField("参数名", "参数说明")]
    public string Parameter1 { get; set; }
}
```### 3. Access Functions in the module```csharp
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
```---

## 8. Architecture design features

1. **Dynamic Scanning**: No manual registration required, the [FunctionRender] feature in all AppServices will be automatically scanned when the system starts.

2. **Hierarchical isolation**: Functions are stored in groups according to Register type (module) to facilitate modular management.

3. **Metadata retention**: FunctionRenderBag retains complete method metadata (MethodInfo) and supports runtime reflection calls

4. **Thread Safety**: Use ConcurrentDictionary to support concurrent access

5. **Symbol parsing**: Supports automatic parsing and importing plug-ins through [#sym:FunctionRender] symbols

6. **Parameter streaming**: All Function methods use a unified request/response mode and support automatic parameter mapping.

---

## 9. Integration suggestions with KnowledgeBase module

For the KnowledgeBase module of the RAG system:```csharp
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
```In this way, the operation methods of the KnowledgeBase module will be automatically registered in the global Functions system.
