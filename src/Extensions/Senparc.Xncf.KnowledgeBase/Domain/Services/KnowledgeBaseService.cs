/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseService.cs
    File description: Related implementation.

    Created by: Senparc - 20251225

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260729
    Change description: v0.3.1-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

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
using Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval;
using NcfTextChunker = Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval.TextChunker;
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

        /// <summary>
        /// Initializes knowledge-base persistence, file extraction, item, and AI model services.
        /// </summary>
        /// <param name="repo">The knowledge-base repository.</param>
        /// <param name="knowledgeBasesDetailService">The chunk/item service.</param>
        /// <param name="ncfFileService">The FileManager service used for source extraction.</param>
        /// <param name="aIModelService">The AI model service used to validate embedding configuration.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
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

        /// <summary>
        /// Lists knowledge bases using one-based paging and maps them to DTOs.
        /// </summary>
        /// <param name="PageIndex">The one-based page index.</param>
        /// <param name="PageSize">The requested page size, clamped to 1-200.</param>
        /// <returns>A task containing the newest knowledge bases first.</returns>
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
        /// Resolves current embedding status for knowledge bases, including legacy embedded-item detection.
        /// </summary>
        /// <param name="knowledgeBases">The knowledge bases to inspect.</param>
        /// <returns>A task containing status keyed by knowledge-base ID.</returns>
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
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                if (itemGroup.Any() && itemGroup.All(z => z.IsEmbedded))
                {
                    result[itemGroup.Key] = KnowledgeBaseEmbeddingStatus.Legacy;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether a knowledge base has a completed embedding timestamp and collection name.
        /// </summary>
        /// <param name="knowledgeBase">The knowledge base to inspect.</param>
        /// <returns><see langword="true"/> when the published embedding metadata is complete.</returns>
        public static bool IsEmbeddingPublished(KnowledgeBase.Models.DatabaseModel.KnowledgeBase knowledgeBase)
        {
            return knowledgeBase?.EmbeddedTime.HasValue == true
                   && !string.IsNullOrWhiteSpace(knowledgeBase.VectorCollectionName);
        }

        /// <summary>
        /// Creates or updates a knowledge base, validates embedding/vector configuration,
        /// and synchronizes inline/file content before invalidating embeddings.
        /// </summary>
        /// <param name="dto">The knowledge-base definition and content selections.</param>
        /// <exception cref="NcfExceptionBase">Thrown when names, model/vector configuration, or referenced data is invalid.</exception>
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

            //Legacy comment translated to English.
            var retrievalConfig = dto.RetrievalConfig?.Clone().Sanitized();
            if (retrievalConfig != null && retrievalConfig.IsDefault())
            {
                retrievalConfig = null;
            }

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
            knowledgeBase.SetRetrievalConfig(retrievalConfig);

            await SaveObjectAsync(knowledgeBase);
            await SyncInlineContentAsync(knowledgeBase, dto.Content);

            if (dto.NcfFileIds != null)
            {
                await SyncFilesAsync(knowledgeBase, dto.NcfFileIds);
            }

            knowledgeBase.InvalidateEmbedding();
            await SaveObjectAsync(knowledgeBase);
        }

        /// <summary>
        /// Validates that the selected model is TextEmbedding and the vector database
        /// is an allowed persistent provider.
        /// </summary>
        /// <param name="embeddingModelId">The embedding model identifier.</param>
        /// <param name="vectorDbId">The vector database identifier.</param>
        /// <exception cref="NcfExceptionBase">Thrown when either configuration is missing or unsupported.</exception>
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

        /// <summary>
        /// Replaces inline text chunks for a knowledge base using the configured chunking policy.
        /// </summary>
        /// <param name="knowledgeBase">The knowledge base receiving content.</param>
        /// <param name="content">The inline content, or blank to remove existing inline chunks.</param>
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

            var chunks = NcfTextChunker.Split(content.Trim(), knowledgeBase.GetRetrievalConfig());
            for (var index = 0; index < chunks.Count; index++)
            {
                await _knowledgeBaseDetailService.SaveObjectAsync(new KnowledgeBaseItem(
                    new KnowledgeBaseItemDto(0, knowledgeBase.Id, ContentType.Text, chunks[index], string.Empty, index)));
            }
        }

        /// <summary>
        /// Synchronizes selected source files with the knowledge-base item records.
        /// </summary>
        /// <param name="knowledgeBase">The knowledge base receiving files.</param>
        /// <param name="fileIds">The selected file identifiers.</param>
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

            //Legacy comment translated to English.
            foreach (var obsolete in existing.Where(z => !selectedIds.Contains(z.NcfFileId.Value)))
            {
                await _knowledgeBaseDetailService.DeleteObjectAsync(obsolete);
            }
        }

        /// <summary>
        /// Replaces the file selection for a knowledge base and invalidates its embedding state.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="fileIds">The selected source file identifiers.</param>
        /// <exception cref="NcfExceptionBase">Thrown when the knowledge base does not exist.</exception>
        public async Task SyncFilesToKnowledgeBaseAsync(int knowledgeBaseId, IEnumerable<int> fileIds)
        {
            var knowledgeBase = await GetObjectAsync(z => z.Id == knowledgeBaseId)
                ?? throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");

            //Legacy comment translated to English.
            knowledgeBase.InvalidateEmbedding();
            await SaveObjectAsync(knowledgeBase);
            await SyncFilesAsync(knowledgeBase, fileIds ?? Array.Empty<int>());
        }

        /// <summary>
        /// Replaces inline knowledge-base content and regenerates its text chunks.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="content">The new inline content.</param>
        /// <exception cref="NcfExceptionBase">Thrown when the knowledge base does not exist.</exception>
        public async Task SyncInlineContentToKnowledgeBaseAsync(int knowledgeBaseId, string content)
        {
            var knowledgeBase = await GetObjectAsync(z => z.Id == knowledgeBaseId)
                ?? throw new NcfExceptionBase($"Knowledge Base with ID {knowledgeBaseId} not found.");

            knowledgeBase.SetContent(content?.Trim());
            await SaveObjectAsync(knowledgeBase);
            await SyncInlineContentAsync(knowledgeBase, knowledgeBase.Content);
        }

        /// <summary>
        /// Deletes a knowledge base and all associated item records.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
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
        /// Adds multiple source files to a knowledge base and returns the number of created chunks.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="fileIds">The source file identifiers.</param>
        /// <returns>A task containing the total chunk count.</returns>
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
        /// Extracts one source file, replaces its existing chunks, and creates new chunks
        /// using the knowledge-base retrieval configuration.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="fileId">The source file identifier.</param>
        /// <returns>A task containing the number of created chunks.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the file or knowledge base is missing or the file scope is invalid.</exception>
        public async Task<int> AddFileToKnowledgeBaseAsync(int knowledgeBaseId, int fileId)
        {
            // 1. getfileinformation
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

            //Legacy comment translated to English.
            var extraction = await _ncfFileService.GetExtractedTextAsync(fileId);
            var content = extraction.Text;

            //Legacy comment translated to English.
            knowledgeBase.InvalidateEmbedding();
            await SaveObjectAsync(knowledgeBase);

            var existingItems = await _knowledgeBaseDetailService.GetFullListAsync(z =>
                z.KnowledgeBasesId == knowledgeBaseId && z.NcfFileId == fileId);
            foreach (var existingItem in existingItems)
            {
                await _knowledgeBaseDetailService.DeleteObjectAsync(existingItem);
            }

            //Legacy comment translated to English.
            var chunks = NcfTextChunker.Split(content, knowledgeBase.GetRetrievalConfig());

            //Legacy comment translated to English.
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
        /// Embeds all non-empty knowledge-base chunks into the configured persistent vector store,
        /// marks successful chunks, and records the generated collection name.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="tags">Optional operation tags reserved for caller diagnostics.</param>
        /// <returns>A task containing a human-readable embedding summary.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when configuration, content, vector storage, or retry operations fail.</exception>
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

            //Legacy comment translated to English.
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
        /// Performs recall testing using the default retrieval options.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="content">The query text.</param>
        /// <param name="topK">The maximum number of returned results.</param>
        /// <returns>A task containing ranked recall results.</returns>
        public Task<List<RecallTestResponse>> RecallTestAsync(int knowledgeBaseId, string content, int topK = 5)
        {
            return RecallTestAsync(knowledgeBaseId, content, topK, null);
        }

        /// <summary>
        /// Performs vector recall and optional lexical/LLM reranking with configurable candidate limits.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="content">The query text, limited to 2000 characters.</param>
        /// <param name="topK">The requested result count, clamped to 1-20.</param>
        /// <param name="options">Optional reranking configuration.</param>
        /// <returns>A task containing ranked recall results and timing/source metadata.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the knowledge base is missing, unembedded, or query content is invalid.</exception>
        public async Task<List<RecallTestResponse>> RecallTestAsync(
            int knowledgeBaseId,
            string content,
            int topK = 5,
            RecallOptions options = null)
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
            var config = knowledgeBase.GetRetrievalConfig();
            var effectiveMode = ResolveRerankMode(config, options);
            var candidateCount = ResolveCandidateCount(config, options, effectiveMode, topK);

            var runner = await BuildEmbeddingRunnerAsync(knowledgeBase, knowledgeBase.VectorCollectionName);
            var store = runner.CreateTextSearchStore();
            IEnumerable<TextSearchDocument> vectorResult = null;
            var stopwatch = Stopwatch.StartNew();
            await ExecuteWithRetryAsync(async () =>
            {
                vectorResult = await store.SearchAsync(content, candidateCount);
            }, "知识库召回");
            stopwatch.Stop();
            var recallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var vectorItems = (vectorResult ?? Array.Empty<TextSearchDocument>()).ToList();
            if (effectiveMode == RerankModes.None || vectorItems.Count <= 1)
            {
                return vectorItems
                    .Take(topK)
                    .Select((item, index) => new RecallTestResponse
                    {
                        Rank = index + 1,
                        OriginalRank = index + 1,
                        Score = item.Score,
                        RerankScore = null,
                        RerankMode = effectiveMode,
                        Content = item.Text,
                        ContentLength = item.Text?.Length ?? 0,
                        SourceName = string.IsNullOrWhiteSpace(item.SourceName) ? "未命名来源" : item.SourceName,
                        SourceLink = GetSafeSourceLink(item.SourceLink),
                        RecallTime = recallTime,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    })
                    .ToList();
            }

            //Legacy comment translated to English.
            var vectorScores = vectorItems.Select(z => z.Score ?? 0d).ToArray();
            var lexicalScores = vectorItems.Select(z => LexicalReranker.Score(content, z.Text)).ToArray();
            var normalizedVector = LexicalReranker.NormalizeScores(vectorScores);
            var fusedScores = new double[vectorItems.Count];
            for (var i = 0; i < vectorItems.Count; i++)
            {
                fusedScores[i] = LexicalReranker.Fuse(normalizedVector[i], lexicalScores[i], config.LexicalWeight);
            }
            var finalOrder = Enumerable.Range(0, vectorItems.Count)
                .OrderByDescending(i => fusedScores[i])
                .ThenBy(i => i)
                .ToArray();

            if (effectiveMode == RerankModes.Llm)
            {
                var rerankModelId = options?.RerankModelId ?? config.RerankModelId;
                if (rerankModelId <= 0)
                {
                    rerankModelId = knowledgeBase.ChatModelId;
                }
                if (rerankModelId > 0)
                {
                    var reranker = new LlmReranker(_aIModelService);
                    var outcome = await reranker
                        .RankAsync(content, vectorItems.Select(z => z.Text).ToList(), rerankModelId)
                        .ConfigureAwait(false);
                    if (outcome.Fallback)
                    {
                        SenparcTrace.SendCustomLog(
                            "KnowledgeBase.LlmRerank.Fallback",
                            $"知识库 #{knowledgeBaseId} LLM 重排回退到词法混合：{outcome.FallbackReason}");
                    }
                    else
                    {
                        finalOrder = outcome.Order;
                    }
                }
            }

            return finalOrder
                .Take(topK)
                .Select((originalIndex, rank) =>
                {
                    var item = vectorItems[originalIndex];
                    return new RecallTestResponse
                    {
                        Rank = rank + 1,
                        OriginalRank = originalIndex + 1,
                        Score = item.Score,
                        RerankScore = Math.Round(fusedScores[originalIndex], 6),
                        RerankMode = effectiveMode,
                        Content = item.Text,
                        ContentLength = item.Text?.Length ?? 0,
                        SourceName = string.IsNullOrWhiteSpace(item.SourceName) ? "未命名来源" : item.SourceName,
                        SourceLink = GetSafeSourceLink(item.SourceLink),
                        RecallTime = recallTime,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Resolves the rerank mode from per-request options or knowledge-base defaults.
        /// </summary>
        /// <param name="config">The stored retrieval configuration.</param>
        /// <param name="options">Optional request overrides.</param>
        /// <returns>The normalized rerank mode.</returns>
        private static string ResolveRerankMode(KnowledgeBaseRetrievalConfig config, RecallOptions options)
        {
            if (options != null && !string.IsNullOrWhiteSpace(options.RerankMode))
            {
                if (RerankModes.TryParse(options.RerankMode, out var overrideMode))
                {
                    return overrideMode;
                }
            }
            return config.RerankMode;
        }

        /// <summary>
        /// Resolves and clamps the vector candidate pool used before reranking.
        /// </summary>
        /// <param name="config">The stored retrieval configuration.</param>
        /// <param name="options">Optional request overrides.</param>
        /// <param name="effectiveMode">The selected rerank mode.</param>
        /// <param name="topK">The final requested result count.</param>
        /// <returns>The bounded candidate count.</returns>
        private static int ResolveCandidateCount(
            KnowledgeBaseRetrievalConfig config,
            RecallOptions options,
            string effectiveMode,
            int topK)
        {
            if (effectiveMode == RerankModes.None)
            {
                return topK;
            }

            var pool = options?.CandidateCount ?? config.RerankCandidateCount;
            if (pool <= 0)
            {
                pool = config.RerankCandidateCount;
            }

            return Math.Clamp(Math.Max(topK, pool), topK, 100);
        }

        /// <summary>
        /// Allows only absolute HTTP(S) source links to leave the retrieval boundary.
        /// </summary>
        /// <param name="sourceLink">The source link from vector metadata.</param>
        /// <returns>The absolute URL, or <see langword="null"/> for unsafe/invalid links.</returns>
        private static string GetSafeSourceLink(string sourceLink)
        {
            return Uri.TryCreate(sourceLink, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.AbsoluteUri
                : null;
        }

        /// <summary>
        /// Builds bounded RAG context text from recall results.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <param name="query">The retrieval query.</param>
        /// <param name="topK">The maximum number of recall results.</param>
        /// <param name="maxCharacters">The maximum context length.</param>
        /// <param name="options">Optional reranking configuration.</param>
        /// <returns>A task containing formatted context text.</returns>
        public async Task<string> BuildRagContextAsync(
            int knowledgeBaseId,
            string query,
            int topK = 5,
            int maxCharacters = 6000,
            RecallOptions options = null)
        {
            var results = await RecallTestAsync(knowledgeBaseId, query, topK, options);
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

        /// <summary>
        /// Builds an embedding runner using the configured model and persistent vector database.
        /// </summary>
        /// <param name="knowledgeBase">The knowledge base containing model/vector configuration.</param>
        /// <param name="collectionName">The vector collection name.</param>
        /// <returns>A task containing the configured runner.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when configuration is missing or the vector store is unsupported.</exception>
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

        /// <summary>
        /// Rejects volatile/in-memory or unsupported vector stores for persistent knowledge bases.
        /// </summary>
        /// <param name="vector">The vector database DTO.</param>
        /// <exception cref="NcfExceptionBase">Thrown when the provider is not Redis or Qdrant.</exception>
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

        /// <summary>
        /// Creates a timestamped vector collection name for a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <returns>The collection name.</returns>
        private static string BuildVectorCollectionName(int knowledgeBaseId)
        {
            return $"ncf_kb_{knowledgeBaseId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        /// <summary>
        /// Executes an operation with bounded retries and honors provider retry-after hints when present.
        /// </summary>
        /// <param name="action">The asynchronous operation.</param>
        /// <param name="operationName">The diagnostic operation name.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <exception cref="Exception">Propagates the final operation failure after retries.</exception>
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

    }

    //public class Record
    //{
    //    [VectorStoreKey]
    //    public ulong Id { get; set; }

    //    [VectorStoreData(IsIndexed = true)]
    //    public string Name { get; set; }

    //    [VectorStoreData(IsFullTextIndexed = true)]
    //    public string Description { get; set; }

    //Legacy comment translated to English.
    //    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    //    [VectorStoreData(IsIndexed = true)]
    //    public string[] Tags { get; set; }
    //}
}
