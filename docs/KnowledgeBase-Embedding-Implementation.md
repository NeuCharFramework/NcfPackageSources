[中文版](KnowledgeBase-Embedding-Implementation.cn.md)

# Knowledge base vectorization function implementation instructions

## Function Overview

It implements the complete vectorization (Embedding) function of the knowledge base, converts text slices into vectors and stores them in the vector database, laying the foundation for subsequent RAG query functions.

## Implementation content

### 1. Data model extension

**File**: `KnowledgeBasesDetail.cs`

New fields:
- `FileName` (string, 500): source file name
- `ChunkIndex` (int): Text slice index
- `IsEmbedded` (bool): Whether the marker has been vectorized
- `EmbeddedTime` (DateTime?): Vectorization completion time

### 2. Core service implementation

**File**: `KnowledgeBaseAppService.cs` → `EmbeddingKnowledgeBaseAsync()`

**Processing Flow**:

1. **Configuration Verification**
   - Check whether the knowledge base is configured with Embedding model ID
   - Get AI Model configuration information

2. **Get data to be processed**```csharp
   var details = await _knowledgeBasesDetailService.GetFullListAsync(z => 
       z.KnowledgeBasesId == knowledgeBaseId && !z.IsEmbedded);
   ```3. **Initialize AI Kernel**```csharp
   var semanticAiHandler = _serviceProvider.GetService<SemanticAiHandler>();
   var iWantToRunEmbedding = semanticAiHandler.IWantTo(senparcAiSetting)
       .ConfigModel(ConfigModel.TextEmbedding, $"KnowledgeBase_{knowledgeBaseId}")
       .BuildKernel();
   ```4. **Batch generation of Embeddings**```csharp
   await iWantToRunEmbedding.MemorySaveAsync(
       modelName: senparcAiSetting.ModelName.Embedding,
       azureDeployName: senparcAiSetting.DeploymentName,
       memoryCollectionName: collectionName,
       text: detail.Content,
       key: detail.Id.ToString(),
       description: $"文档: {detail.FileName}, 切片索引: {detail.ChunkIndex}"
   );
   ```5. **Update status**
   - Flag `IsEmbedded = true`
   - Logging `EmbeddedTime`

### 3. Front-end interaction optimization

**File**: `knowledgeBases.js` → `handleEmbeddingBtn()`

**Optimization points**:
- ✅ Add confirmation dialog box
- ✅ Show loading status (Loading)
- ✅ Show detailed success/failure information
- ✅ Error catching and prompts

**User Experience Process**:
1. Click the "Vectorize" button
2. Pop up a confirmation dialog box
3. Show loading mask
4. Display detailed result notification after completion

### 4. API endpoint

**URL**: `/api/Senparc.Xncf.KnowledgeBase/KnowledgeBasesAppService/Xncf.KnowledgeBase_KnowledgeBasesAppService.EmbeddingKnowledgeBase`

**Request Parameters**:```json
{
  "id": "knowledge_base_id"
}
```**Return to example**:```json
{
  "success": true,
  "data": "知识库 'My KB' 向量化完成！\n总计: 150 个切片\n成功: 150\n失败: 0\n集合名称: KB_xxx"
}
```## Technical points

### Vector storage strategy

- **Collection naming**: `KB_{knowledgeBaseId}`
- **Vector Key**: Use `KnowledgeBasesDetail.Id`
- **Description information**: includes file name and slice index

### Error handling

1. **Configuration Missing**: Prompts the user to configure the Embedding model
2. **Single slice failure**: log but continue processing other slices
3. **Global failure**: Return error information to the front end

### Performance considerations

- Batch processing: call API slice by slice (can be optimized to batch API later)
- Incremental processing: only process slices with `IsEmbedded = false`
- Resumable download: failed slices will not affect successful ones

## Usage steps

### 1. Configure knowledge base

In the knowledge base list, click the "Configure" button:
- Select **Embedding model**
- Select **Vector Database**
- Select **Chat Model** (for subsequent queries)

### 2. Import files

Click "Configuration" → "Import File":
- Select files to add
- The system automatically slices and stores it into `KnowledgeBasesDetail`

### 3. Perform vectorization

In the knowledge base list, click the "Vectorize" button:
- Confirm operation
- Wait for processing to complete
- View processing result statistics

## Database migration

### Migration that needs to be performed```bash
cd /path/to/Senparc.Xncf.KnowledgeBase

# SQLite (默认)
dotnet ef migrations add Add_KnowledgeBasesDetail_EmbeddingFields \
  --context KnowledgeBaseSenparcEntities \
  --output-dir Domain/Migrations/Sqlite

# 其他数据库类似
```### Migrate content

Add columns to the `KnowledgeBasesDetail` table:
- `FileName` VARCHAR(500)
- `ChunkIndex` INT
- `IsEmbedded` BIT/BOOLEAN
- `EmbeddedTime` DATETIME NULL

## Dependencies

### Module dependencies

- `Senparc.Xncf.AIKernel`: Provides AI model configuration and Embedding interface
- `Senparc.AI.Kernel`: core AI framework
- `Senparc.Xncf.FileManager`: File storage management

### Service dependencies```csharp
// 注入的服务
private readonly SemanticAiHandler _semanticAiHandler;
private readonly AIModelService _aiModelService;
private readonly KnowledgeBasesService _knowledgeBasesService;
private readonly KnowledgeBasesDetailService _knowledgeBasesDetailService;
```## Follow-up optimization suggestions

### Stage 1 completed ✅
- [x] File reading and slicing
- [x] Vectorized core logic
- [x] Status management and tracking

### Stage 2 Program
- [ ] Implement RAG query function
- [ ] Vector similarity search
- [ ] Combined with Chat model to generate answers

### Stage 3 Enhancements
- [ ] Batch Embedding API optimization
- [ ] Supports more file formats (PDF, Word)
- [ ] Vector index optimization
- [ ] Revectorization function

## Test verification

### Test scenario

1. **Normal process**
   - Create knowledge base → Configure model → Import file → Vectorize → Validate results

2. **Exception handling**
   - Model not configured → prompt error
   - No file slice → Prompt no data
   - Single slice fails → continue processing other

3. **Incremental processing**
   - Vectorized data → skip
   - New file → only process new slices

### Verification points

- ✅ Database fields are stored normally
- ✅ Store vectors in Memory/VectorDB
- ✅ IsEmbedded is marked correctly
- ✅ Smooth front-end interaction
- ✅ Clear error messages

## Reference example

Please refer to the RAG implementation in `CrawlPlugin.cs` of `Senparc.Xncf.AgentsManager`:
- Vectorized storage: `MemorySaveAsync()`
- Vector search: `MemorySearchAsync()`
- Build prompt words: System Message + search results

---

**Update time**: 2025-12-25
**Version**: v1.0
**Status**: Stage 1 completed, pending testing
