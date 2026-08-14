/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseService.cs
    文件功能描述：KnowledgeBaseService 服务逻辑


    创建标识：Senparc - 20251225

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260729
    修改描述：v0.3.1-preview4 通过文件服务读取知识库文件并限制物理路径

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview6 新增知识库生命周期管理与 Agent 模板集成

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview8 完善知识库文件删除保护、召回测试与管理界面

    修改标识：Senparc - 20260815
    修改描述：v0.7.0-preview9 增强知识库向量发布状态识别

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Text;
using OllamaSharp.Models;
using Senparc.AI;
using Senparc.AI.Entities.Keys;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Response;
using Senparc.Xncf.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services
{
    public class KnowledgeBaseService : ServiceBase<Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.KnowledgeBase>
    {
        private readonly KnowledgeBaseItemService _knowledgeBaseDetailService;
        private readonly NcfFileService _ncfFileService;
        private readonly AIModelService _aIModelService;

        public KnowledgeBaseService(
            RepositoryBase<KnowledgeBase.Models.DatabaseModel.KnowledgeBase> repo,
            KnowledgeBaseItemService knowledgeBasesDetailService,
            NcfFileService ncfFileService,
            AIModelService aIModelService,
            IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            _knowledgeBaseDetailService = knowledgeBasesDetailService;
            _ncfFileService = ncfFileService;
            _aIModelService = aIModelService;
        }

        public async Task<IEnumerable<KnowledgeBaseDto>> GetKnowledgeBasesList(int PageIndex, int PageSize)
        {
            PageIndex = Math.Max(1, PageIndex);
            PageSize = Math.Clamp(PageSize, 1, 200);
            List<KnowledgeBase.Models.DatabaseModel.KnowledgeBase> knowledgeBases =
                (await GetFullListAsync(_ => true).ConfigureAwait(false))
                .OrderByDescending(_ => _.AddTime)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            return this.Mapper.Map<List<KnowledgeBaseDto>>(knowledgeBases);
        }

        /// <summary>
        /// 获取知识库的向量发布状态。新版只有在生成独立集合后才可供 Agent 绑定；
        /// 同时识别旧版仅在切片上保存 <see cref="KnowledgeBaseItem.IsEmbedded"/> 的记录，
        /// 以免管理界面将其误报为“未向量化”。
        /// </summary>
        public async Task<IReadOnlyDictionary<int, KnowledgeBaseEmbeddingStatus>> GetEmbeddingStatusesAsync(
            IEnumerable<KnowledgeBase.Models.DatabaseModel.KnowledgeBase> knowledgeBases)
        {
            var knowledgeBaseList = knowledgeBases?
                .Where(z => z != null)
                .GroupBy(z => z.Id)
                .Select(z => z.First())
                .ToList() ?? new List<KnowledgeBase.Models.DatabaseModel.KnowledgeBase>();
            var result = knowledgeBaseList.ToDictionary(
                z => z.Id,
                z => IsEmbeddingPublished(z)
                    ? KnowledgeBaseEmbeddingStatus.Published
                    : KnowledgeBaseEmbeddingStatus.Pending);

            var legacyCandidateIds = knowledgeBaseList
                .Where(z => result[z.Id] == KnowledgeBaseEmbeddingStatus.Pending)
                .Select(z => z.Id)
                .ToList();
            if (legacyCandidateIds.Count == 0)
            {
                return result;
            }

            var items = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                legacyCandidateIds.Contains(z.KnowledgeBasesId));
            foreach (var itemGroup in items
                         .Where(z => !string.IsNullOrWhiteSpace(z.Content))
                         .GroupBy(z => z.KnowledgeBasesId))
            {
                // 旧实现只在资料切片中记录 IsEmbedded，并且按 Embedding 模型共享集合。
                // 这不能等同于新版 Agent 所需的“已发布、知识库隔离”的状态，故只标识为 legacy。
                if (itemGroup.Any() && itemGroup.All(z => z.IsEmbedded))
                {
                    result[itemGroup.Key] = KnowledgeBaseEmbeddingStatus.Legacy;
                }
            }

            return result;
        }

        public static bool IsEmbeddingPublished(KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBase)
        {
            return knowledgeBase?.EmbeddedTime.HasValue == true
                   && !string.IsNullOrWhiteSpace(knowledgeBase.VectorCollectionName);
        }

        public async Task CreateOrUpdateAsync(KnowledgeBase_InsertDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            dto.Name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new NcfExceptionBase("知识库名称不能为空。");
            }
            if (dto.EmbeddingModelId <= 0 || dto.VectorDBId <= 0)
            {
                throw new NcfExceptionBase("知识库必须配置 Embedding 模型和持久化向量数据库。");
            }

            await ValidateConfigurationAsync(dto.EmbeddingModelId, dto.VectorDBId);

            KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBase;
            if (dto.Id == 0)
            {
                knowledgeBase = new KnowledgeBase.Models.DatabaseModel.KnowledgeBase(dto);
            }
            else
            {
                knowledgeBase = await GetObjectAsync(_ => _.Id == dto.Id)
                    ?? throw new NcfExceptionBase($"知识库不存在：{dto.Id}");
                knowledgeBase.Update(dto);
            }

            await SaveObjectAsync(knowledgeBase);
            await SyncInlineContentAsync(knowledgeBase, dto.Content);

            if (dto.NcfFileIds != null)
            {
                await SyncFilesAsync(knowledgeBase, dto.NcfFileIds);
            }

            knowledgeBase.InvalidateEmbedding();
            await SaveObjectAsync(knowledgeBase);
        }

        private async Task ValidateConfigurationAsync(int embeddingModelId, int vectorDbId)
        {
            var aiModel = await _aIModelService.GetObjectAsync(z => z.Id == embeddingModelId);
            if (aiModel == null)
            {
                throw new NcfExceptionBase($"Embedding 模型不存在：{embeddingModelId}");
            }
            if (aiModel.ConfigModelType != AIKernel.Domain.Models.ConfigModelType.TextEmbedding)
            {
                throw new NcfExceptionBase($"模型“{aiModel.Alias}”不是 TextEmbedding 模型，不能用于知识库向量化。");
            }

            var vectorService = base.ServiceProvider.GetRequiredService<AIVectorService>();
            var vector = await vectorService.GetObjectAsync(z => z.Id == vectorDbId);
            if (vector == null)
            {
                throw new NcfExceptionBase($"向量数据库不存在：{vectorDbId}");
            }

            EnsureSupportedPersistentVectorStore(new AIVectorDto(vector));
        }

        private async Task SyncInlineContentAsync(
            KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBase,
            string content)
        {
            var existing = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                z.KnowledgeBasesId == knowledgeBase.Id && z.NcfFileId == null && z.ContentType == ContentType.Text);
            foreach (var item in existing)
            {
                await _knowledgeBaseDetailService.DeleteObjectAsync(item);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var chunks = SplitText(content.Trim(), 800, 120);
            for (var index = 0; index < chunks.Count; index++)
            {
                await _knowledgeBaseDetailService.SaveObjectAsync(new KnowledgeBaseItem(
                    new KnowledgeBaseItemDto(0, knowledgeBase.Id, ContentType.Text, chunks[index], string.Empty, index)));
            }
        }

        private async Task SyncFilesAsync(
            KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBase,
            IEnumerable<int> fileIds)
        {
            var selectedIds = fileIds.Where(z => z > 0).Distinct().ToHashSet();
            var existing = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                z.KnowledgeBasesId == knowledgeBase.Id && z.NcfFileId != null);

            var existingIds = existing.Where(z => z.NcfFileId.HasValue)
                .Select(z => z.NcfFileId.Value)
                .ToHashSet();
            foreach (var fileId in selectedIds.Where(z => !existingIds.Contains(z)))
            {
                await AddFileToKnowledgeBaseAsync(knowledgeBase.Id, fileId);
            }

            // 先确保所有新增文件均可读取，再删除已取消的关联，避免坏文件导致旧资料先丢失。
            foreach (var obsolete in existing.Where(z => !selectedIds.Contains(z.NcfFileId.Value)))
            {
                await _knowledgeBaseDetailService.DeleteObjectAsync(obsolete);
            }
        }

        public async Task SyncFilesToKnowledgeBaseAsync(int knowledgeBaseId, IEnumerable<int> fileIds)
        {
            var knowledgeBase = await GetObjectAsync(z => z.Id == knowledgeBaseId)
                ?? throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");

            // 必须先落库失效状态；后续任一步失败时，调用方都不会继续使用旧向量集合。
            knowledgeBase.InvalidateEmbedding();
            await SaveObjectAsync(knowledgeBase);
            await SyncFilesAsync(knowledgeBase, fileIds ?? Array.Empty<int>());
        }

        public async Task SyncInlineContentToKnowledgeBaseAsync(int knowledgeBaseId, string content)
        {
            var knowledgeBase = await GetObjectAsync(z => z.Id == knowledgeBaseId)
                ?? throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");

            knowledgeBase.SetContent(content?.Trim());
            await SaveObjectAsync(knowledgeBase);
            await SyncInlineContentAsync(knowledgeBase, knowledgeBase.Content);
        }

        public async Task DeleteKnowledgeBaseAsync(int knowledgeBaseId)
        {
            var knowledgeBase = await GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return;
            }

            var items = await _knowledgeBaseDetailService.GetFullListAsync(z => z.KnowledgeBasesId == knowledgeBaseId);
            foreach (var item in items)
            {
                await _knowledgeBaseDetailService.DeleteObjectAsync(item);
            }

            await DeleteObjectAsync(knowledgeBase);
        }

        /// <summary>
        /// 批量将文件添加到知识库（读取、切片、保存详情）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="fileIds"></param>
        /// <returns>总切片数</returns>
        public async Task<int> AddFilesToKnowledgeBaseAsync(int knowledgeBaseId, List<int> fileIds)
        {
            if (fileIds == null || fileIds.Count == 0)
            {
                return 0;
            }

            var knowledgeBase = await GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            knowledgeBase.InvalidateEmbedding();
            await SaveObjectAsync(knowledgeBase);

            int totalChunks = 0;
            foreach (var fileId in fileIds.Where(z => z > 0).Distinct())
            {
                var chunks = await AddFileToKnowledgeBaseAsync(knowledgeBaseId, fileId);
                totalChunks += chunks;
            }

            return totalChunks;
        }

        /// <summary>
        /// 将文件添加到知识库（读取、切片、保存详情）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="fileId"></param>
        /// <returns>切片数</returns>
        public async Task<int> AddFileToKnowledgeBaseAsync(int knowledgeBaseId, int fileId)
        {
            // 1. 获取文件信息
            var file = await _ncfFileService.GetObjectAsync(z => z.Id == fileId);
            if (file == null)
            {
                throw new NcfExceptionBase($"File with ID {fileId} not found.");
            }

            if (file.ResourceScope != NcfFileResourceScope.KnowledgeBase)
            {
                throw new NcfExceptionBase("Only KnowledgeBase source files can be added to a knowledge base.");
            }

            var knowledgeBase = await GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            // 所有物理路径、编码和 Open XML 安全解析都由 FileManager 统一处理。
            var extraction = await _ncfFileService.GetExtractedTextAsync(fileId);
            var content = extraction.Text;

            // 文件确认可提取后、修改切片前先使已发布集合失效。
            knowledgeBase.InvalidateEmbedding();
            await SaveObjectAsync(knowledgeBase);

            var existingItems = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                z.KnowledgeBasesId == knowledgeBaseId && z.NcfFileId == fileId);
            foreach (var existingItem in existingItems)
            {
                await _knowledgeBaseDetailService.DeleteObjectAsync(existingItem);
            }

            // 3. 文本切片
            var chunks = SplitText(content, 800, 120);

            // 4. 保存切片到 KnowledgeBasesDetail
            int chunkIndex = 0;
            foreach (var chunk in chunks)
            {
                var detailDto = new KnowledgeBaseItemDto
                {
                    KnowledgeBasesId = knowledgeBaseId,
                    Content = chunk,
                    ContentType = ContentType.TextFile,
                    NcfFileId = file.Id,
                    FileName = file.FileName,
                    ChunkIndex = chunkIndex++
                };

                await _knowledgeBaseDetailService.CreateOrUpdateAsync(detailDto);
            }

            return chunks.Count;
        }

        /// <summary>
        /// 对知识库进行向量化（Embedding）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="tags">当前 Embedding 记录的 Tag</param>
        /// <returns></returns>
        public async Task<string> EmbeddingKnowledgeBaseAsync(int knowledgeBaseId, params string[] tags)
        {
            var knowledgeBase = await base.GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            var details = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                z.KnowledgeBasesId == knowledgeBaseId);
            var validDetails = details
                .Where(z => !string.IsNullOrWhiteSpace(z.Content))
                .OrderBy(z => z.NcfFileId)
                .ThenBy(z => z.ChunkIndex)
                .ThenBy(z => z.Id)
                .ToList();
            if (validDetails.Count == 0)
            {
                throw new NcfExceptionBase($"知识库 '{knowledgeBase.Name}' 没有可向量化的文本切片。");
            }

            var collectionName = BuildVectorCollectionName(knowledgeBase.Id);
            var runner = await BuildEmbeddingRunnerAsync(knowledgeBase, collectionName);
            var vectorStore = runner.CreateTextSearchStore();

            foreach (var detail in validDetails)
            {
                var sourceName = string.IsNullOrWhiteSpace(detail.FileName)
                    ? $"{knowledgeBase.Name}-内容-{detail.ChunkIndex + 1}"
                    : $"{detail.FileName}-片段-{detail.ChunkIndex + 1}";
                var document = new TextSearchDocument
                {
                    SourceId = (ulong)detail.Id,
                    SourceName = sourceName,
                    SourceLink = detail.FileName ?? string.Empty,
                    Text = detail.Content
                };

                await ExecuteWithRetryAsync(
                    () => vectorStore.UpsertDocumentsAsync([document]),
                    $"知识切片 {detail.Id}");
            }

            // 只有整批写入成功后才发布新集合，召回请求不会看到半成品。
            foreach (var detail in validDetails)
            {
                detail.EmbeddingSucceeded();
                await _knowledgeBaseDetailService.SaveObjectAsync(detail);
            }

            knowledgeBase.MarkEmbeddingCompleted(collectionName);
            await SaveObjectAsync(knowledgeBase);

            return $"知识库 '{knowledgeBase.Name}' 向量化完成！\n" +
                   $"总计: {validDetails.Count} 个切片\n" +
                   $"成功: {validDetails.Count}\n" +
                   $"失败: 0\n" +
                   $"集合名称: {collectionName}";
        }

        /// <summary>
        /// 召回测试（Embedding）
        /// </summary>
        /// <param name="knowledgeBaseId"></param>
        /// <param name="tags">当前 Embedding 记录的 Tag</param>
        /// <returns></returns>
        public async Task<List<RecallTestResponse>> RecallTestAsync(int knowledgeBaseId, string content, int topK = 5)
        {
            var knowledgeBase = await base.GetObjectAsync(z => z.Id == knowledgeBaseId);
            if (knowledgeBase == null)
            {
                throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new NcfExceptionBase("召回查询内容不能为空。");
            }
            if (string.IsNullOrWhiteSpace(knowledgeBase.VectorCollectionName) || !knowledgeBase.EmbeddedTime.HasValue)
            {
                throw new NcfExceptionBase($"知识库 '{knowledgeBase.Name}' 尚未完成向量化，不能执行 RAG 召回。");
            }

            content = content.Trim();
            if (content.Length > 2000)
            {
                throw new NcfExceptionBase("召回查询内容不能超过 2000 个字符。");
            }

            topK = Math.Clamp(topK, 1, 20);
            var runner = await BuildEmbeddingRunnerAsync(knowledgeBase, knowledgeBase.VectorCollectionName);
            var store = runner.CreateTextSearchStore();
            IEnumerable<TextSearchDocument> vectorResult = null;
            var stopwatch = Stopwatch.StartNew();
            await ExecuteWithRetryAsync(async () =>
            {
                vectorResult = await store.SearchAsync(content, topK);
            }, "知识库召回");
            stopwatch.Stop();

            return (vectorResult ?? Array.Empty<TextSearchDocument>())
                .Select((item, index) => new RecallTestResponse
                {
                    Rank = index + 1,
                    Score = item.Score,
                    Content = item.Text,
                    ContentLength = item.Text?.Length ?? 0,
                    SourceName = string.IsNullOrWhiteSpace(item.SourceName) ? "未命名来源" : item.SourceName,
                    SourceLink = GetSafeSourceLink(item.SourceLink),
                    RecallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                })
                .ToList();
        }

        private static string GetSafeSourceLink(string sourceLink)
        {
            return Uri.TryCreate(sourceLink, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.AbsoluteUri
                : null;
        }

        /// <summary>
        /// 为 Agent 等调用方生成有长度上限、带来源的 RAG 上下文。
        /// </summary>
        public async Task<string> BuildRagContextAsync(
            int knowledgeBaseId,
            string query,
            int topK = 5,
            int maxCharacters = 6000)
        {
            var results = await RecallTestAsync(knowledgeBaseId, query, topK);
            if (results.Count == 0)
            {
                return string.Empty;
            }

            maxCharacters = Math.Clamp(maxCharacters, 500, 20_000);
            var builder = new StringBuilder();
            foreach (var (item, index) in results.Select((value, index) => (value, index)))
            {
                var source = string.IsNullOrWhiteSpace(item.SourceName) ? "未命名来源" : item.SourceName;
                var block = $"[知识片段 {index + 1}｜来源：{source}]\n{item.Content?.Trim()}\n";
                if (builder.Length + block.Length > maxCharacters)
                {
                    var remaining = maxCharacters - builder.Length;
                    if (remaining > 100)
                    {
                        builder.Append(block.AsSpan(0, Math.Min(remaining, block.Length)));
                    }
                    break;
                }
                builder.AppendLine(block);
            }

            return builder.ToString().Trim();
        }

        private async Task<IWantToRun> BuildEmbeddingRunnerAsync(
            KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBase,
            string collectionName)
        {
            if (knowledgeBase.EmbeddingModelId <= 0 || knowledgeBase.VectorDBId <= 0)
            {
                throw new NcfExceptionBase($"知识库 '{knowledgeBase.Name}' 未配置 Embedding 模型或向量数据库。");
            }

            var embeddingModel = await _aIModelService.GetObjectAsync(z => z.Id == knowledgeBase.EmbeddingModelId)
                ?? throw new NcfExceptionBase($"Embedding 模型不存在：{knowledgeBase.EmbeddingModelId}");
            var vectorService = base.ServiceProvider.GetRequiredService<AIVectorService>();
            var vector = await vectorService.GetObjectAsync(z => z.Id == knowledgeBase.VectorDBId)
                ?? throw new NcfExceptionBase($"向量数据库不存在：{knowledgeBase.VectorDBId}");
            var vectorDto = new AIVectorDto(vector);
            EnsureSupportedPersistentVectorStore(vectorDto);

            var modelDto = _aIModelService.Mapper.Map<AIModelDto>(embeddingModel);
            var setting = _aIModelService.BuildSenparcAiSetting(modelDto, vectorDto);
            return new AgentAiHandler(setting)
                .IWantTo(setting)
                .ConfigTextEmbeddingModel($"NcfKnowledgeBase_{knowledgeBase.Id}", collectionName)
                .BuildKernel();
        }

        private static void EnsureSupportedPersistentVectorStore(AIVectorDto vector)
        {
            var vectorTypeName = vector.VectorDBType.ToString();
            if (string.Equals(vectorTypeName, "Memory", StringComparison.OrdinalIgnoreCase)
                || string.Equals(vectorTypeName, "VolatileInMemory", StringComparison.OrdinalIgnoreCase))
            {
                throw new NcfExceptionBase("内存向量库不能跨请求持久化，不可用于 KnowledgeBase。请选择 Redis 或 Qdrant。");
            }
            if (!string.Equals(vectorTypeName, "Redis", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(vectorTypeName, "Qdrant", StringComparison.OrdinalIgnoreCase))
            {
                throw new NcfExceptionBase($"当前 AgentKernel 尚不支持该向量库类型（{vector.VectorDBType}）。KnowledgeBase 目前支持 Redis 或 Qdrant。");
            }
        }

        private static string BuildVectorCollectionName(int knowledgeBaseId)
        {
            return $"ncf_kb_{knowledgeBaseId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        private static async Task ExecuteWithRetryAsync(Func<Task> action, string operationName, int maxAttempts = 3)
        {
            Exception lastException = null;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    lastException = ex;
                    var match = Regex.Match(ex.Message ?? string.Empty, @"retry after (\d+) seconds", RegexOptions.IgnoreCase);
                    var delaySeconds = match.Success && int.TryParse(match.Groups[1].Value, out var parsed)
                        ? Math.Clamp(parsed, 1, 30)
                        : attempt;
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break;
                }
            }

            throw new NcfExceptionBase($"{operationName}在 {maxAttempts} 次尝试后失败：{lastException?.Message}", lastException);
        }


        /// <summary>
        /// 简单的文本切片算法
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chunkSize"></param>
        /// <param name="overlap"></param>
        /// <returns></returns>
        private List<string> SplitText(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            if (string.IsNullOrEmpty(text)) return chunks;

            // 简单按字符数切分，后续可以优化为按 Token 或段落切分
            for (int i = 0; i < text.Length; i += (chunkSize - overlap))
            {
                int length = Math.Min(chunkSize, text.Length - i);
                if (length <= 0) break;

                chunks.Add(text.Substring(i, length));

                // 防止死循环（如果 overlap >= chunkSize）
                if (chunkSize - overlap <= 0) break;
            }

            return chunks;
        }
    }

    //public class Record
    //{
    //    [VectorStoreKey]
    //    public ulong Id { get; set; }

    //    [VectorStoreData(IsIndexed = true)]
    //    public string Name { get; set; }

    //    [VectorStoreData(IsFullTextIndexed = true)]
    //    public string Description { get; set; }

    //    [VectorStoreVector(Dimensions: 1536 /*根据模型调整，例如 text-embedding-ada-002 为 1536，Large 为 3072*/, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    //    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    //    [VectorStoreData(IsIndexed = true)]
    //    public string[] Tags { get; set; }
    //}
}
