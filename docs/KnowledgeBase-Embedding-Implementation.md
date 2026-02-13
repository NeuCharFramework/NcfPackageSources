# 知识库向量化功能实现说明

## 功能概述

实现了知识库的完整向量化（Embedding）功能，将文本切片转换为向量并存储到向量数据库中，为后续的RAG查询功能奠定基础。

## 实现内容

### 1. 数据模型扩展

**文件**: `KnowledgeBasesDetail.cs`

新增字段：
- `FileName` (string, 500): 源文件名称
- `ChunkIndex` (int): 文本切片索引
- `IsEmbedded` (bool): 是否已向量化标记
- `EmbeddedTime` (DateTime?): 向量化完成时间

### 2. 核心服务实现

**文件**: `KnowledgeBaseAppService.cs` → `EmbeddingKnowledgeBaseAsync()`

**处理流程**:

1. **配置验证**
   - 检查知识库是否配置了Embedding模型ID
   - 获取AI Model配置信息

2. **获取待处理数据**
   ```csharp
   var details = await _knowledgeBasesDetailService.GetFullListAsync(z => 
       z.KnowledgeBasesId == knowledgeBaseId && !z.IsEmbedded);
   ```

3. **初始化AI Kernel**
   ```csharp
   var semanticAiHandler = _serviceProvider.GetService<SemanticAiHandler>();
   var iWantToRunEmbedding = semanticAiHandler.IWantTo(senparcAiSetting)
       .ConfigModel(ConfigModel.TextEmbedding, $"KnowledgeBase_{knowledgeBaseId}")
       .BuildKernel();
   ```

4. **批量生成Embeddings**
   ```csharp
   await iWantToRunEmbedding.MemorySaveAsync(
       modelName: senparcAiSetting.ModelName.Embedding,
       azureDeployName: senparcAiSetting.DeploymentName,
       memoryCollectionName: collectionName,
       text: detail.Content,
       key: detail.Id.ToString(),
       description: $"文档: {detail.FileName}, 切片索引: {detail.ChunkIndex}"
   );
   ```

5. **更新状态**
   - 标记 `IsEmbedded = true`
   - 记录 `EmbeddedTime`

### 3. 前端交互优化

**文件**: `knowledgeBases.js` → `handleEmbeddingBtn()`

**优化点**:
- ✅ 添加确认对话框
- ✅ 显示加载状态（Loading）
- ✅ 显示详细的成功/失败信息
- ✅ 错误捕获和提示

**用户体验流程**:
1. 点击"向量化"按钮
2. 弹出确认对话框
3. 显示加载遮罩
4. 完成后显示详细结果通知

### 4. API端点

**URL**: `/api/Senparc.Xncf.KnowledgeBase/KnowledgeBasesAppService/Xncf.KnowledgeBase_KnowledgeBasesAppService.EmbeddingKnowledgeBase`

**请求参数**:
```json
{
  "id": "knowledge_base_id"
}
```

**返回示例**:
```json
{
  "success": true,
  "data": "知识库 'My KB' 向量化完成！\n总计: 150 个切片\n成功: 150\n失败: 0\n集合名称: KB_xxx"
}
```

## 技术要点

### 向量存储策略

- **集合命名**: `KB_{knowledgeBaseId}`
- **向量Key**: 使用 `KnowledgeBasesDetail.Id`
- **描述信息**: 包含文件名和切片索引

### 错误处理

1. **配置缺失**: 提示用户配置Embedding模型
2. **单个切片失败**: 记录日志但继续处理其他切片
3. **全局失败**: 返回错误信息给前端

### 性能考虑

- 批量处理：逐个切片调用API（后续可优化为批量API）
- 增量处理：只处理 `IsEmbedded = false` 的切片
- 断点续传：失败的切片不会影响已成功的

## 使用步骤

### 1. 配置知识库

在知识库列表中，点击"配置"按钮：
- 选择 **Embedding模型**
- 选择 **向量数据库**
- 选择 **Chat模型**（用于后续查询）

### 2. 导入文件

点击"配置" → "导入文件"：
- 选择要添加的文件
- 系统自动切片并存储到 `KnowledgeBasesDetail`

### 3. 执行向量化

在知识库列表中，点击"向量化"按钮：
- 确认操作
- 等待处理完成
- 查看处理结果统计

## 数据库迁移

### 需要执行的迁移

```bash
cd /path/to/Senparc.Xncf.KnowledgeBase

# SQLite (默认)
dotnet ef migrations add Add_KnowledgeBasesDetail_EmbeddingFields \
  --context KnowledgeBaseSenparcEntities \
  --output-dir Domain/Migrations/Sqlite

# 其他数据库类似
```

### 迁移内容

为 `KnowledgeBasesDetail` 表添加列：
- `FileName` VARCHAR(500)
- `ChunkIndex` INT
- `IsEmbedded` BIT/BOOLEAN
- `EmbeddedTime` DATETIME NULL

## 依赖关系

### 模块依赖

- `Senparc.Xncf.AIKernel`: 提供AI模型配置和Embedding接口
- `Senparc.AI.Kernel`: 核心AI框架
- `Senparc.Xncf.FileManager`: 文件存储管理

### 服务依赖

```csharp
// 注入的服务
private readonly SemanticAiHandler _semanticAiHandler;
private readonly AIModelService _aiModelService;
private readonly KnowledgeBasesService _knowledgeBasesService;
private readonly KnowledgeBasesDetailService _knowledgeBasesDetailService;
```

## 后续优化建议

### Stage 1 完成 ✅
- [x] 文件读取和切片
- [x] 向量化核心逻辑
- [x] 状态管理和追踪

### Stage 2 计划
- [ ] 实现RAG查询功能
- [ ] 向量相似度搜索
- [ ] 结合Chat模型生成答案

### Stage 3 增强
- [ ] 批量Embedding API优化
- [ ] 支持更多文件格式（PDF, Word）
- [ ] 向量索引优化
- [ ] 重新向量化功能

## 测试验证

### 测试场景

1. **正常流程**
   - 创建知识库 → 配置模型 → 导入文件 → 向量化 → 验证结果

2. **异常处理**
   - 未配置模型 → 提示错误
   - 无文件切片 → 提示无数据
   - 单个切片失败 → 继续处理其他

3. **增量处理**
   - 已向量化数据 → 跳过
   - 新增文件 → 仅处理新切片

### 验证要点

- ✅ 数据库字段正常存储
- ✅ 向量存储到Memory/VectorDB
- ✅ IsEmbedded标记正确
- ✅ 前端交互流畅
- ✅ 错误提示清晰

## 参考示例

可参考 `Senparc.Xncf.AgentsManager` 的 `CrawlPlugin.cs` 中的RAG实现：
- 向量化存储: `MemorySaveAsync()`
- 向量搜索: `MemorySearchAsync()`
- 构建提示词: System Message + 检索结果

---

**更新时间**: 2025-12-25  
**版本**: v1.0  
**状态**: Stage 1 完成，待测试
